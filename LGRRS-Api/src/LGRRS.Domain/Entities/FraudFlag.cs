using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

public class FraudFlag
{
    public Guid FlagId { get; set; } = Guid.NewGuid();
    public FraudEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public FraudRuleCode RuleCode { get; set; }
    public FraudSeverity Severity { get; set; }
    public FraudFlagStatus Status { get; set; } = FraudFlagStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
