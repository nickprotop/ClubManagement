namespace ClubManagement.Shared.Models.Authorization;

public class EventPermissions
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanRegisterSelf { get; set; }
    public bool CanRegisterOthers { get; set; }
    public bool CanCheckInSelf { get; set; }
    public bool CanCheckInOthers { get; set; }
    public bool CanViewRegistrations { get; set; }
    public bool CanModifyRegistrations { get; set; }
    public bool CanCancelEvent { get; set; }
    public bool CanRescheduleEvent { get; set; }
    public bool CanManageEventStatus { get; set; }
    public bool CanBulkCheckIn { get; set; }
    public string[] Restrictions { get; set; } = Array.Empty<string>();
    public string[] ReasonsDenied { get; set; } = Array.Empty<string>();
}