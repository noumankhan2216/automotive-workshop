namespace AutomotiveWorkshop.Domain.Enums;

public enum WorkOrderStatus
{
    Draft = 0,
    InProgress = 1,
    WaitingParts = 2,
    Completed = 3,
    Invoiced = 4,
    Paid = 5,
    Cancelled = 6
}
