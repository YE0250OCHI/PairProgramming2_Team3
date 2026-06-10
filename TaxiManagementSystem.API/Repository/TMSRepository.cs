using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using TaxiManagementSystem.API.Model;

namespace TaxiManagementSystem.API.Repository;

public class TMSRepository(IConfiguration configuration, ILogger logger) : ITMSRepository
{
    // ===== フィールド =====

    // DB接続文字列
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection") ?? "";


    // ===== パブリックメソッド =====

    // 実行中JOBの一覧を取得する
    public async Task<List<Job>> GetActiveJobsAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var jobList = await connection.QueryAsync<Job>(
                new CommandDefinition(
                    """
                    SELECT                    
                        j.id AS [Id],
                        j.job_status_id AS [Status],
                        j.taxi_id AS [TaxiId],
                        j.from_loc AS [FromLoc],
                        j.to_loc AS [ToLoc],
                        j.closed_at AS [ClosedAt]
                    FROM jobs j
                    WHERE j.closed_at IS NULL;
                    """,
                    cancellationToken: token));

            logger.LogInformation("[成功]JOB一覧取得");
            return [.. jobList];
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]JOB一覧取得");
            throw;
        }
    }

    // JOB登録
    public async Task<bool> TryCreateNewJobAsync(string fromLoc, string toLoc, string? taxiId, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            // トランザクション開始
            await using var tran = await connection.BeginTransactionAsync(token);

            try
            {
                // 新しいJOB番号を取得
                var newJobId = await GetNewJobIdAsync(connection, tran, token);

                // JOB状態の決定
                var jobStatus = taxiId is null ?
                    JobStatus.Queued : /* 割り当てがないとき：Queued */
                    JobStatus.Waiting; /* 割り当てられたとき：Waiting */

                // タクシーの割当チェック
                if (taxiId is not null)
                {
                    // タクシーステータスをtran内で変更
                    var taxiAffected = await connection.ExecuteAsync(
                        """
                    UPDATE taxis
                    SET taxi_status_id  = @reserved
                    WHERE id = @id AND taxi_status_id = @idle;
                    """,
                        new
                        {
                            id = taxiId,
                            reserved = TaxiStatus.Reserved,
                            idle = TaxiStatus.Idle
                        }
                        , tran);

                    // 影響したレコードが0件（=Idleじゃなかった）
                    if (taxiAffected == 0)
                    {
                        /* ロールバックしてfalse */
                        await tran.RollbackAsync(token);
                        logger.LogError("[失敗]タクシー割当拒否");
                        return false;
                    }
                }

                // JOB更新
                await connection.ExecuteAsync(
                    """
                INSERT INTO jobs(
                    id,
                    job_status_id,
                    from_loc,
                    to_loc,
                    taxi_id
                )
                VALUES (
                    @Id,
                    @Status,
                    @FromLoc,
                    @ToLoc,
                    @TaxiId
                );
                """,
                    new
                    {
                        Id = newJobId,
                        Status = jobStatus,
                        FromLoc = fromLoc,
                        ToLoc = toLoc,
                        TaxiId = taxiId
                    },
                    tran);

                // コミットしてture
                await tran.CommitAsync(token);
                logger.LogInformation("[成功]JOB登録完了");
                return true;
            }
            catch
            {
                await tran.RollbackAsync(token); // ロールバックしてthrow
                throw;
            }
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]JOB登録失敗");
            throw;
        }
    }

    // 実行中JOB個数取得
    public async Task<int> GetActiveJobsCountAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            logger.LogInformation("[成功]実行中JOB個数取得");
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                SELECT Count(*)
                FROM jobs j
                WHERE j.closed_at IS NULL;
                """,
                    cancellationToken: token));
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]実行中JOB個数取得");
            throw;
        }
    }

    // タクシー再割当
    public async Task<bool> TryReassignTaxiAsync(string jobId, string taxiId, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            /* 割当失敗：bool */

        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]実行中JOB個数取得");
            throw;
        }
    }

    // JOB中断
    public async Task<bool> TryCancelJobAsync(string jobId, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            /* 割当失敗：bool */

        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]実行中JOB個数取得");
            throw;
        }
    }

    // JOB中断
    public async Task<bool> TryAbortJobAsync(string abortId, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            /* 割当失敗：bool */

        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]実行中JOB個数取得");
            throw;
        }
    }

    // 運行履歴取得
    public async Task<List<Job>> GetHistoryAsync(HistoryFilter filter, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            return [];

        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]実行中JOB個数取得");
            throw;
        }
    }

    // 本日完了済みJOBの個数取得
    public async Task<int> GetTodayHistoryCountAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            logger.LogInformation("[成功]本日完了済みJOBの個数取得");
                return await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                    SELECT Count(*)
                    FROM jobs j
                    WHERE j.closed_at  >= CAST(GETDATE() AS DATE)
                        AND  j.closed_at  < DATEADD(day, 1, CAST(GETDATE() AS DATE));
                    """,
                        cancellationToken: token));
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]本日完了済みJOBの個数取得");
            throw;
        }
    }

    // タクシー一覧取得
    public async Task<List<(Taxi Taxi, string JobId)>> GetTaxisAsync(CancellationToken token)
    {
        return [];
    }

    // タクシーの台数取得
    public async Task<int> GetTaxisCountAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            logger.LogInformation("[成功]タクシーの台数取得");
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                SELECT Count(*)
                FROM taxis;
                """,
                    cancellationToken: token));
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]タクシーの台数取得");
            throw;
        }
    }

    // 割当可能なタクシー一覧取得(stringリスト)
    public async Task<List<string>> GetAvailableTaxisAsync(CancellationToken token)
    {
        return [];
    }

    // 割当可能なタクシーの台数取得
    public async Task<int> GetAvailableTaxisCountAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            logger.LogInformation("[成功]割当可能なタクシーの台数取得");
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                SELECT Count(*)
                FROM taxis
                WHERE taxi_status_id = @idle;
                """,
                    new
                    {
                        idle = TaxiStatus.Idle
                    },
                    cancellationToken: token));
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]タクシーの台数取得");
            throw;
        }
    }

    // タクシーの情報取得
    public async Task<(Taxi CurrentTaxi, Job CurrentJob)> GetCurrentTaxiInfoAsync(string id, CancellationToken token)
    {
        return ;
    }

    // タクシー状態の更新
    public async Task SetCurrentTaxiStatusAsync(string id, string status, CancellationToken token)
    {

    }


    // JOB IDの存在確認
    public async Task<bool> AnyJobAsync(string id, CancellationToken token)
    {
        return false;
    }

    // タクシーIDの存在確認
    public async Task<bool> AnyTaxiAsync(string id, CancellationToken token)
    {
        return false;
    }


    // ===== プライベートメソッド =====

    // 新しいJOB IDを取得
    private async Task<string> GetNewJobIdAsync(SqlConnection connection, IDbTransaction tran, CancellationToken token)
    {
        /* DB接続開始 */
        try
        {
            var latestJobId = await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(
                    """
                SELECT TOP 1 id
                FROM jobs
                ORDER BY created_at DESC;
                """,
                    transaction: tran,
                    cancellationToken: token));

            // nullのとき：0件なので新規
            var today = DateTime.Today;
            if (latestJobId is null)
            {
                return $"J{today:yyyyMMdd}-0001"; // 今日の1件目として
            }

            // 文字列を分解
            var segments = latestJobId.Substring(1).Split('-', StringSplitOptions.RemoveEmptyEntries);
            var isDate = DateTime.TryParseExact(
                segments[0],
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedDate);
            var isBranch = int.TryParse(segments[1], out var parsedBranch);

            if (!isDate || !isBranch)
                throw new KeyNotFoundException($"正常なJOB IDが取れなかった:{latestJobId}");

            // 今日の日付と異なる場合
            if (parsedDate.Date != today)
            {
                return $"J{today:yyyyMMdd}-0001"; // 今日の1件目として
            }

            // 枝番を増やして返却
            return $"J{parsedDate:yyyyMMdd}-{parsedBranch + 1:0000}";
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[失敗]DBアクセスエラー");
            throw;
        }
    }

}
