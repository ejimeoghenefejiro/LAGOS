-- Latest merchant OTP state and readable demo code.
SELECT TOP (1000)
    m.MerchantId, m.BusinessName, m.LgrrsSystemId,
    o.OtpChallengeId, o.Purpose, o.Code, o.CreatedAt AS OtpCreatedAt,
    o.ExpiresAt, o.ConsumedAt, o.FailedAttempts,
    CASE
        WHEN o.OtpChallengeId IS NULL THEN 'Not requested'
        WHEN o.ConsumedAt IS NOT NULL THEN 'Used'
        WHEN o.ExpiresAt <= SYSDATETIMEOFFSET() THEN 'Expired'
        WHEN o.FailedAttempts >= 5 THEN 'Locked'
        ELSE 'Active'
    END AS OtpStatus
FROM [Lgrrs].[dbo].[Merchants] m
LEFT JOIN [Lgrrs].[dbo].[OtpChallenges] o
    ON o.MerchantId = m.MerchantId AND o.Purpose = 'Merchant'
ORDER BY o.CreatedAt DESC;
