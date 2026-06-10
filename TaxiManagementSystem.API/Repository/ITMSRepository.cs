using TaxiManagementSystem.API.Model;

namespace TaxiManagementSystem.API.Repository;

public interface ITMSRepository
{
    // 実行中JOBの一覧を取得する
    Task<List<Job>> GetActiveJobsAsync(CancellationToken token);

    // JOB登録
    Task<bool> TryCreateNewJobAsync(string fromLoc, string toLoc, string? taxiId, CancellationToken token);

    // 実行中JOB個数取得
    Task<int> GetActiveJobsCountAsync(CancellationToken token);

    // タクシー再割当
    Task<bool> TryReassignTaxiAsync(string jobId, string taxiId, CancellationToken token);

    // JOB中断
    Task<bool> TryCancelJobAsync(string jobId, CancellationToken token);

    // JOB中断
    Task<bool> TryAbortJobAsync(string abortId, CancellationToken token);

    // 運行履歴取得
    Task<List<Job>> GetHistoryAsync(HistoryFilter filter, CancellationToken token);

    // 本日完了済みJOBの個数取得
    Task<int> GetTodayHistoryCountAsync(CancellationToken token);

    // タクシー一覧取得
    Task<List<(Taxi Taxi,string JobId)>> GetTaxisAsync(CancellationToken token);

    // タクシーの台数取得
    Task<int> GetTaxisCountAsync(CancellationToken token);

    // 割当可能なタクシー一覧取得(stringリスト)
    Task<List<string>> GetAvailableTaxisAsync(CancellationToken token);

    // 割当可能なタクシーの台数取得
    Task<int> GetAvailableTaxisCountAsync(CancellationToken token);

    // タクシーの情報取得
    Task<(Taxi CurrentTaxi,Job CurrentJob)> GetCurrentTaxiInfoAsync(string id, CancellationToken token);

    // タクシー状態の更新
    Task SetCurrentTaxiStatusAsync(string id, string status, CancellationToken token);

    // JOB IDの存在確認
    Task<bool> AnyJobAsync(string id, CancellationToken token);

    // タクシーIDの存在確認
    Task<bool> AnyTaxiAsync(string id, CancellationToken token);

}
