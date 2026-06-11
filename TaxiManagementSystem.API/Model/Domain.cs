namespace TaxiManagementSystem.API.Model;

// JOBデータ
public record Job(
    string Id,
    JobStatus Status,
    string FromLoc,
    string ToLoc,
    string? TaxiId,
    string? DriverName,
    DateTime? ClosedAt);

// タクシーデータ
public record Taxi(
    string Id,
    TaxiStatus Status,
    string DriverName,
    string? JobId);

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

// タクシー情報
public record TaxiInfo(
    string TaxiId,
    TaxiStatus Status,
    string DriverName,
    string? JobId,
    JobStatus? JobStatus,
    string? FromLoc,
    string? ToLoc);

// フィルタリング用クエリ
public record HistoryFilter(
    string? Status, string? TaxiId, string? DriverName, DateTime? From, DateTime? To);
