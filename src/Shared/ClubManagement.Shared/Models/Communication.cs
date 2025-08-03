namespace ClubManagement.Shared.Models;

public class Communication : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public CommunicationType Type { get; set; } = CommunicationType.Announcement;
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.InApp;
    public CommunicationStatus Status { get; set; } = CommunicationStatus.Draft;
    public Priority Priority { get; set; } = Priority.Normal;
    public Guid? SenderId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public List<Guid> RecipientMemberIds { get; set; } = new();
    public List<UserRole> RecipientRoles { get; set; } = new();
    public bool SendToAllMembers { get; set; } = false;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation properties
    public User? Sender { get; set; }
    public List<CommunicationDelivery> Deliveries { get; set; } = new();
}

public enum CommunicationType
{
    Announcement,
    Reminder,
    Alert,
    Message,
    Newsletter,
    Promotional,
    Emergency
}

public enum CommunicationChannel
{
    InApp,
    Email,
    SMS,
    Push,
    All
}

public enum CommunicationStatus
{
    Draft,
    Scheduled,
    Sending,
    Sent,
    Failed,
    Cancelled
}

public enum Priority
{
    Low,
    Normal,
    High,
    Urgent
}