namespace ClubManagement.Shared.Models.Authorization;

public enum EventAction
{
    View,
    Create,
    Edit,
    Delete,
    RegisterSelf,
    RegisterOthers,
    CheckInSelf,
    CheckInOthers,
    ViewRegistrations,
    ModifyRegistrations,
    CancelEvent,
    RescheduleEvent,
    ManageEventStatus
}