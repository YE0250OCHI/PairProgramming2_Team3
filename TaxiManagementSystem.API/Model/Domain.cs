namespace TaxiManagementSystem.API.Model;

// JOBデータ
public record Job(
    string Id,
    JobStatus Status,
    string? TaxiId,
    string FromLoc,
    string ToLoc,
    DateTime? ClosedAt);

// タクシーデータ
public record Taxi(
    string Id,
    TaxiStatus Status,
    string DriverName);

// JOB状態Enum
public enum JobStatus
{
    Queued = 1,
    Waiting,
    Active,
    Aborting,
    Completed,
    Canceled,
    Aborted
}

// タクシー状態Enum
public enum TaxiStatus
{
    Idle = 1,
    Reserved,
    Occupied,
    OffDuty
}
