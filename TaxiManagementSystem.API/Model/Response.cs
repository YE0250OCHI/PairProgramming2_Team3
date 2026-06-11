namespace TaxiManagementSystem.API.Model;

// 実行中のJOB
public record ActiveJob(
    string JobId, string Status, string FromLoc, string ToLoc, string? TaxiId, string? DriverName);

// 完了JOB
public record CompletedJob(
    string JobId, string Status, string FromLoc, string ToLoc, string? TaxiId, string? DriverName, DateTime? ClosedAt);

// 割当可能タクシー用データ
public record AvailableActiveTaxi(string TaxiId);

// タクシー一覧用データ
public record ActiveTaxi(string TaxiId, string Status, string DriverName, string? JobId);

// タクシー用データ
public record TaxiInfoResponse(string TaxiId, string Status, string DriverName, string? JobId, string? JobStatus, string? FromLoc, string? ToLoc);


// 今回使わない

// 実行中のJOB件数
public record ActiveJobsCountRequest(int Count);

// 本日の完了JOB件数
public record TodayCompletedJobsCount(int Count);

// タクシーの台数
public record ActiveTaxisCount(int Count);

// 割当可能タクシー台数
public record AvailableActiveTaxisCount(int Count);
