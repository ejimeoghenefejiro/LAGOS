$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5082'
$phone = '080' + (Get-Random -Minimum 10000000 -Maximum 99999999)
function Call($path, $body, $expected) {
  $response = Invoke-WebRequest ($base + $path) -Method Post -ContentType application/json -Body ($body | ConvertTo-Json) -SkipHttpErrorCheck
  if ([int]$response.StatusCode -ne $expected) { throw "$path expected $expected, received $($response.StatusCode)" }
  if ($response.Content -and $expected -eq 200) { return ($response.Content | ConvertFrom-Json) }
}
$merchant = Call '/api/merchants/register' @{businessName='Synthetic Passcode Test';businessType='Barbershop';lgaCode='IKEJA';phoneNumber=$phone;businessAddress='Synthetic local test address'} 200
$id = $merchant.merchantId
function Code {
  $value = sqlcmd -b -I -S 'DESKTOP-3T0MF62\MSSQLSERVER1' -d Lgrrs -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT Code FROM OtpChallenges WHERE MerchantId='$id' AND Purpose='Merchant'"
  return ($value | Where-Object { $_ -match '^\d{6}$' } | Select-Object -First 1).Trim()
}
function RenewOtp {
  sqlcmd -b -I -S 'DESKTOP-3T0MF62\MSSQLSERVER1' -d Lgrrs -E -C -Q "UPDATE OtpChallenges SET CreatedAt=DATEADD(minute,-1,CreatedAt) WHERE MerchantId='$id'" | Out-Null
  Call '/api/auth/merchant/request-otp' @{phoneNumber=$phone} 202 | Out-Null
}
Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='123456'} 401 | Out-Null
Call '/api/auth/merchant/request-otp' @{phoneNumber=$phone} 202 | Out-Null
$otp = Code
Call '/api/auth/merchant/verify-otp' @{phoneNumber=$phone;otp=$otp;passcode='12345'} 400 | Out-Null
Call '/api/auth/merchant/verify-otp' @{phoneNumber=$phone;otp='000000';passcode='123456'} 401 | Out-Null
Call '/api/auth/merchant/verify-otp' @{phoneNumber=$phone;otp=$otp;passcode='123456'} 200 | Out-Null
Call '/api/auth/merchant/verify-otp' @{phoneNumber=$phone;otp=$otp;passcode='654321'} 401 | Out-Null
Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='123456'} 200 | Out-Null
1..5 | ForEach-Object { Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='000000'} 401 | Out-Null }
Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='123456'} 429 | Out-Null
RenewOtp
Call '/api/auth/merchant/verify-otp' @{phoneNumber=$phone;otp=(Code);passcode='654321'} 200 | Out-Null
Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='123456'} 401 | Out-Null
Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='654321'} 200 | Out-Null
sqlcmd -b -I -S 'DESKTOP-3T0MF62\MSSQLSERVER1' -d Lgrrs -E -C -Q "UPDATE Merchants SET Status=3 WHERE MerchantId='$id'" | Out-Null
Call '/api/auth/merchant/login' @{phoneNumber=$phone;passcode='654321'} 403 | Out-Null
Write-Output 'PASS: setup, invalid OTP, invalid passcode, OTP reuse, login, lockout, recovery, old passcode rejected, suspended merchant blocked.'


