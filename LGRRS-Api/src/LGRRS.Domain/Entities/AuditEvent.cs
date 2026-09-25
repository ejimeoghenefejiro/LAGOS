namespace LGRRS.Domain.Entities;

public class AuditEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Metadata { get; set; }
}
