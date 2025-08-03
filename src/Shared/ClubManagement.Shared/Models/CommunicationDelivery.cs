namespace ClubManagement.Shared.Models;

public class CommunicationDelivery : BaseEntity
{
    public Guid CommunicationId { get; set; }
    public Guid RecipientMemberId { get; set; }
    public CommunicationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }
    
    // Navigation properties
    public Communication Communication { get; set; } = null!;
    public Member RecipientMember { get; set; } = null!;
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed,
    Bounced,
    Unsubscribed
}