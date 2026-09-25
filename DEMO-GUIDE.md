# Receipt accountability demo

The pilot borrows Taiwan's demand-side incentive: a chance of winning encourages
customers to ask businesses for a receipt, making sales visible in the programme.
It is not a connection to Taiwan's system or an approved Lagos tax reporting service.

## Start locally

In LGRRS-Api:

```powershell
dotnet run --project src/LGRRS.Api -- --urls http://localhost:5080
```

The API uses the configured local SQL Server and applies EF migrations at startup.
The new migration adds SaleReports without changing existing receipt records.

In LGRRS-Web:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

Open http://127.0.0.1:5173. The API allows this frontend origin.

## Present the customer-to-government loop

New businesses only enter their name, category, LGA and phone number. Registration
assigns a unique six-digit LGRRS ID automatically and displays it after registration
and in the merchant profile. No manually invented ID or LIRS Tax ID is required.
The internal ID will remain stable when an official tax identifier is linked later.

1. Sign into a verified demo merchant and issue a receipt to a synthetic customer phone.
   Use the exact same phone format for merchant issuance and customer login.
2. Switch to Customer, request an OTP, and read the demo code from the API console.
3. Open My Receipts. The purchase appears automatically in the customer's private wallet.
   Open its check link to see receipt and draw status. Only the owning phone can check it.
4. Select Missing or incorrect receipt. Enter a fictional business, location, purchase
   time within 90 days, amount, and issue. Submit a report.
5. Switch to LGA Administrator. Demo login: admin@lgrrs.demo / Demo#12345.
6. Open Customer Reports. Start review with a note, then resolve or dismiss with a reason.
7. Return to the customer's report list to show the updated status.
8. Show Audit Log and recorded sales metrics. Reports never add to recorded receipt
   values or create prize entries, even when marked resolved.

The verification run leaves a clearly labelled, dismissed synthetic report in the
queue. Do not present it as a real allegation.

## Meaning of the data

- Recorded sales are receipts issued within this application, not independently
  verified turnover, tax paid, or a tax assessment.
- Customer reports are allegations requiring follow-up. Review completion does not
  prove a violation. The review queue does not expose the reporter's phone hash.
- The wallet returns the latest 100 receipts owned by the signed-in phone.
- The administrator queue returns up to 200 reports with open reports first.
- Duplicate reports for the same customer, business, location, date and amount
  are blocked. Customers have a basic ten-report daily limit.
- The existing weighted, purchase-tier draw is a local prototype policy, not a
  reproduction of Taiwan's invoice-number lottery. Do not describe its odds as Taiwan's.

## Known demo limits

OTP challenges now persist in dbo.OtpChallenges, with merchant challenges linked
to dbo.Merchants by MerchantId. Codes are readable in the Code column, expire after five minutes,
allow five failed attempts, and are consumed once. Resends replace the previous
challenge after a 30-second cooldown. Merchant and consumer challenges are separate.
The development API console still shows the code because SMS delivery is simulated.
Use LGRRS-Api/scripts/merchant-otp-status.sql to inspect merchant OTP status in SSMS.

Business verification and OTP are demo providers. SMS/WhatsApp receipt delivery is
still simulated; the wallet supplies a working private retrieval path. There is no
government reporting integration, evidence upload, tax calculation, or payout.
Reports do not automatically match to a registered merchant or reconcile a corrected
receipt. Reviewers must investigate outside this prototype and document the outcome.
Existing completed draw periods are preserved in the Draws & Winners history.
Administrators can create a weekly or monthly period when no draw is open.
A new weekly period with a ₦50,000 demo budget was created during verification.
Issue new qualifying receipts during that period, refresh entries, then use
Run demo draw and Confirm and publish. This ends collection immediately and
publishes results, even before the scheduled end; it does not transfer money.
Empty draws and duplicate open periods are rejected.

## POS API demo

Merchant Settings → Connect my POS → Generate API key exposes a business-scoped
receipt submission key and example request. POST /api/pos/receipts accepts completed
sales without importing inventory. Duplicate external sale IDs are deduplicated,
including concurrent retries. Keys expire after 90 days; replacement, revocation
and turning off POS invalidate them. Full keys are shown once, with only hashes
stored in SQL. See [POS-INTEGRATION.md](POS-INTEGRATION.md) for the request contract
and integration test command. This is a local API, not an automatic POS vendor
connector; remote stores need a deployed HTTPS endpoint.

## Build verification

Paper receipt claims: new anonymous POS sales return `claimCode` and `claimUrl`.
Print the code and a QR encoding of the URL. Customers can use Claim paper receipt
from login or their wallet, verify their phone, then claim once. Code validity is
30 days; reward eligibility requires the original sale's draw to remain open.
The full POS → phone verification → claim → wallet flow was tested in the browser.
Concurrent retries and cross-customer claim rejection were checked through the API.

Frontend: npm.cmd run build and npm.cmd run lint.
Backend: dotnet build LGRRS.sln --no-restore.

For the report workflow, request a consumer OTP for synthetic phone 08000000991,
then run scripts/verify-customer-reports.ps1 -Otp CODE from LGRRS-Api.
This creates and closes one synthetic report and checks access control, validation,
duplicate rejection, review transitions, customer status and unchanged sales totals.

## Reference

Taiwan Ministry of Finance, digital invoice history:
https://museum.mof.gov.tw/eng/singlehtml/4861f7618dec49f291f13eab71ad8e05?cntId=d54d06597ac14628bfab327fd5919ae9

Uniform Invoice Awarding Regulations, including missing/understated invoice reports:
https://law-out.mof.gov.tw/EngLawContent.aspx?id=10396&lan=E
