param(
    [Parameter(Mandatory=$true)][string]$Otp,
    [string]$Phone = '08000000991',
    [string]$BaseUrl = 'http://localhost:5080'
)
$ErrorActionPreference = 'Stop'
function Call-Api($Method, $Path, $Body, $Token) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $args = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $headers; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) { $args.Body = $Body | ConvertTo-Json; $args.ContentType = 'application/json' }
    Invoke-WebRequest @args
}
function Assert-Status($Response, $Expected, $Name) {
    if ([int]$Response.StatusCode -ne $Expected) { throw "$Name failed: $($Response.StatusCode) $($Response.Content)" }
    Write-Output "PASS: $Name"
}
$login = Call-Api POST '/api/auth/consumer/verify-otp' @{ phoneNumber=$Phone; otp=$Otp } $null
Assert-Status $login 200 'Consumer OTP authentication'
$consumer = ($login.Content | ConvertFrom-Json).accessToken
$login = Call-Api POST '/api/auth/staff/login' @{ email='admin@lgrrs.demo'; password='Demo#12345' } $null
Assert-Status $login 200 'Demo administrator authentication'
$admin = ($login.Content | ConvertFrom-Json).accessToken
$before = ((Call-Api GET '/api/admin/overview' $null $admin).Content | ConvertFrom-Json).totalTransactionValue.value
Assert-Status (Call-Api GET '/api/consumer/receipts' $null $null) 401 'Anonymous wallet access rejected'
Assert-Status (Call-Api GET '/api/consumer/receipts' $null $consumer) 200 'Private wallet loads'
Assert-Status (Call-Api GET '/api/sale-reports' $null $consumer) 403 'Consumer cannot access administrator queue'
$report = @{
    businessName="Synthetic Demo Shop $([guid]::NewGuid().ToString('N').Substring(0,6))"
    businessLocation='Demo location only'; lgaCode='Ikeja'
    amount=4500; purchaseDate=[DateTimeOffset]::UtcNow.AddHours(-1).ToString('o')
    issue='MissingReceipt'; details='Synthetic smoke-test purchase; no real business allegation.'
}
$invalid = $report.Clone()
$invalid.amount = -1
Assert-Status (Call-Api POST '/api/sale-reports' $invalid $consumer) 400 'Negative amount rejected'
$created = Call-Api POST '/api/sale-reports' $report $consumer
Assert-Status $created 200 'Customer submits report'
$id = ($created.Content | ConvertFrom-Json).saleReportId
Assert-Status (Call-Api POST '/api/sale-reports' $report $consumer) 409 'Duplicate purchase report rejected'
Assert-Status (Call-Api PATCH "/api/sale-reports/$id" @{status='Reviewing';note='Test'} $consumer) 403 'Customer cannot review report'
Assert-Status (Call-Api PATCH "/api/sale-reports/$id" @{status='Resolved';note='Test'} $admin) 409 'Cannot skip review stage'
Assert-Status (Call-Api PATCH "/api/sale-reports/$id" @{status='Reviewing';note='Reviewing synthetic test.'} $admin) 200 'Administrator starts review'
Assert-Status (Call-Api PATCH "/api/sale-reports/$id" @{status='Dismissed';note='Closed: synthetic smoke-test record, not a real allegation.'} $admin) 200 'Administrator closes with reason'
Assert-Status (Call-Api PATCH "/api/sale-reports/$id" @{status='Reviewing';note='Test'} $admin) 409 'Closed report cannot be overwritten'
$queue = (Call-Api GET '/api/sale-reports' $null $admin).Content
if ($queue -match 'reporterPhoneHash|fingerprint') { throw 'Reporter identity leaked to queue.' }
Write-Output 'PASS: Administrator queue omits reporter identity'
$mine = (Call-Api GET '/api/sale-reports/mine' $null $consumer).Content | ConvertFrom-Json
if (($mine | Where-Object saleReportId -eq $id).status -ne 'Dismissed') { throw 'Customer status is stale.' }
Write-Output 'PASS: Customer sees review outcome'
$after = ((Call-Api GET '/api/admin/overview' $null $admin).Content | ConvertFrom-Json).totalTransactionValue.value
if ($before -ne $after) { throw 'Customer report changed recorded sales total.' }
Write-Output 'PASS: Reports do not inflate recorded sales'
