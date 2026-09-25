namespace LGRRS.Api.Contracts;

public record CreateReceiptRequest(
    string? CustomerName,
    string? CustomerPhone,
    string ItemService,
    decimal Amount,
    List<string> DeliveryChannels);

public record ReceiptHistoryItem(
    string ReceiptRefMasked,
    string? CustomerName,
    string CustomerPhoneMasked,
    string ItemService,
    decimal Amount,
    string DeliveryStatus,
    List<string> Channels,
    DateTimeOffset TransactionDate,
    string? ClaimCode = null,
    string? ClaimUrl = null);

public record PublicTicketCheckResponse(
    bool Valid,
    string? Status,
    string? DrawStatus,
    bool Winner,
    decimal? PrizeAmount,
    bool ClaimEligible,
    string? MerchantName,
    string? LgaName,
    DateTimeOffset? ReceiptDate,
    decimal? Amount,
    string? EntryRef,
    string? DrawName,
    DateTimeOffset? ClaimDeadline);
