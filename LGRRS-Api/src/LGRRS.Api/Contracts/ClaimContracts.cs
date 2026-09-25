namespace LGRRS.Api.Contracts;

public record SubmitClaimRequest(
    Guid ReceiptId,
    string FullName,
    string BankName,
    string AccountNumber);

public record ClaimSubmittedResponse(string ClaimRef, string Status);

public record ClaimStatusResponse(string ClaimRef, string Status);
