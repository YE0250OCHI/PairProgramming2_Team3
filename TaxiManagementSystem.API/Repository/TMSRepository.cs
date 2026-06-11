using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net.NetworkInformation;
using System.Text;
using TaxiManagementSystem.API.Model;

namespace TaxiManagementSystem.API.Repository;

public class TMSRepository(IConfiguration configuration, ILogger<TMSRepository> logger) : ITMSRepository
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
                        j.from_loc AS [FromLoc],
                        j.to_loc AS [ToLoc],
                        j.taxi_id AS [TaxiId],
                        t.driver_name AS [DriverName],
                        j.closed_at AS [ClosedAt]
                    FROM jobs j
                    LEFT JOIN taxis t
                        ON t.id = j.taxi_id
                    WHERE j.closed_at IS NULL
                    ORDER BY j.id ASC;
                    """,
                    cancellationToken: token));

            logger.LogInformation("[成功]JOB一覧取得");
            return [.. jobList];
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]JOB一覧取得");
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
                    // タクシー状態をtran内で変更
                    var taxiAffected = await connection.ExecuteAsync(
                        """
                        UPDATE taxis
                        SET taxi_status_id  = @Reserved
                        WHERE id = @Id AND taxi_status_id = @Idle;
                        """,
                        new
                        {
                            Id = taxiId,
                            Reserved = TaxiStatus.Reserved,
                            Idle = TaxiStatus.Idle
                        }
                        , tran);

                    // 影響したレコードが0件（=Idleじゃなかった）
                    if (taxiAffected == 0)
                    {
                        /* ロールバックしてfalse */
                        await tran.RollbackAsync(token);
                        logger.LogWarning("[失敗]タクシー割当拒否");
                        return false;
                    }
                }

                // JOBを登録
                var jobAffected = await connection.ExecuteAsync(
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

                // 影響したレコードが0件（=カラム制約違反など）
                if (jobAffected == 0)
                {
                    /* ロールバックしてfalse */
                    await tran.RollbackAsync(token);
                    logger.LogWarning("[失敗]登録データ不正");
                    return false;
                }

                // コミットしてture
                await tran.CommitAsync(token);
                logger.LogInformation("[成功]JOB登録");
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
            logger.LogError(ex, "[エラー]JOB登録");
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

            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT Count(*)
                    FROM jobs j
                    WHERE j.closed_at IS NULL;
                    """,
                    cancellationToken: token));

            logger.LogInformation("[成功]実行中JOB個数取得");
            return count;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]実行中JOB個数取得");
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

            // トランザクション開始
            await using var tran = await connection.BeginTransactionAsync(token);

            /*
             * 割当成功条件：
             * JOB状態がQueuedのみ操作可能
             * タクシー状態がIdleのものを割当可能
             */
            try
            {
                // タクシー状態をtran内で変更
                var taxiAffected = await connection.ExecuteAsync(
                    """                    
                    UPDATE taxis
                    SET taxi_status_id  = @Reserved
                    WHERE id = @id AND taxi_status_id = @Idle;
                    
                    """,
                    new
                    {
                        Id = taxiId,
                        Reserved = TaxiStatus.Reserved,
                        Idle = TaxiStatus.Idle
                    }
                    , tran);

                // 影響したレコードが0件（=Idleじゃなかった）
                if (taxiAffected == 0)
                {
                    /* ロールバックしてfalse */
                    await tran.RollbackAsync(token);
                    logger.LogWarning("[失敗]タクシー割当拒否");
                    return false;
                }

                // JOB状態を更新
                var jobAffected = await connection.ExecuteAsync(
                    """
                    UPDATE jobs
                    SET
                        job_status_id = @Waiting,
                        taxi_id = @TaxiId
                    WHERE id = @Id
                    AND job_status_id = @Queued
                    AND closed_at IS NULL;
                    """,
                    new
                    {
                        Id = jobId,
                        Waiting = JobStatus.Waiting,
                        Queued = JobStatus.Queued,
                        TaxiId = taxiId
                    },
                    tran);

                // 影響したレコードが0件（=Queuedじゃなかった）
                if (jobAffected == 0)
                {
                    /* ロールバックしてfalse */
                    await tran.RollbackAsync(token);
                    logger.LogWarning("[失敗]状態遷移異常：");
                    return false;
                }

                // コミットしてture
                await tran.CommitAsync(token);
                logger.LogInformation("[成功]タクシー再割当");
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
            logger.LogError(ex, "[エラー]タクシー再割当");
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

            /*
             * 状態切替条件：
             * JOB状態が、QueuedかWaitngのときのみ、Canceledに変更可能
             */

            // JOB状態をCanceledに変更
            var jobAffected = await connection.ExecuteAsync(
                """                    
                UPDATE jobs
                SET
                    job_status_id = @Canceled,
                    closed_at = GETDATE()
                WHERE id = @Id AND job_status_id IN (@Queued, @Waiting);
                """,
                new
                {
                    Id = jobId,
                    Canceled = JobStatus.Canceled,
                    Queued = JobStatus.Queued,
                    Waiting = JobStatus.Waiting
                });

            // 影響したレコードが0件（=キャンセル可能なステータスじゃなかった）
            if (jobAffected == 0)
            {
                /* 状態切替失敗としてfalse */
                logger.LogWarning("[失敗]状態遷移異常：JOBキャンセル拒否");
                return false;
            }

            // 状態切替成功としてture
            logger.LogInformation("[成功]JOBキャンセル");
            return true;

        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]JOBキャンセル");
            throw;
        }
    }

    // JOB中断
    public async Task<bool> TryAbortJobAsync(string jobId, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            /*
             * 状態切替条件：
             * JOB状態が、ActiveのときにAbortingに変更可能
             * Aborting→Aborted（タクシー側操作）
             */

            // JOB状態をAbortingに変更
            var jobAffected = await connection.ExecuteAsync(
                """                    
                UPDATE jobs
                SET
                    job_status_id = @Aborting,
                    closed_at = GETDATE()
                WHERE id = @Id AND job_status_id = @Active;
                """,
                new
                {
                    Id = jobId,
                    Aborting = JobStatus.Aborting,
                    Active = JobStatus.Active
                });

            // 影響したレコードが0件（=中断可能なステータスじゃなかった）
            if (jobAffected == 0)
            {
                /* 状態切替失敗としてfalse */
                logger.LogWarning("[失敗]状態遷移異常：JOB中断拒否");
                return false;
            }

            // 状態切替成功としてture
            logger.LogInformation("[成功]JOB中断");
            return true;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]JOB中断");
            throw;
        }
    }

    // 運行履歴取得
    public async Task<List<Job>> GetHistoryAsync(HistoryFilter filter, CancellationToken token)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(filter, nameof(filter));

            // フィルターの追加
            StringBuilder querySb = new();

            // クエリ文本体
            querySb.AppendLine("""
                SELECT                    
                    j.id AS [Id],
                    j.job_status_id AS [Status],
                    j.from_loc AS [FromLoc],
                    j.to_loc AS [ToLoc],
                    j.taxi_id AS [TaxiId],
                    t.driver_name AS [DriverName],
                    j.closed_at AS [ClosedAt]
                FROM jobs j
                LEFT JOIN taxis t
                    ON t.id = j.taxi_id
                WHERE j.closed_at IS NOT NULL
                """);

            // ステータスフィルタ
            JobStatus? status = null;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (!Enum.TryParse<JobStatus>(filter.Status, out var parsed))
                {
                    throw new ArgumentException($"ステータス異常：{filter.Status}");
                }
                status = parsed;
                querySb.AppendLine("AND j.job_status_id = @Status");
            }

            // タクシーIDフィルタ
            if (!string.IsNullOrWhiteSpace(filter.TaxiId))
            {
                querySb.AppendLine("AND j.taxi_id = @TaxiId");
            }

            // ドライバー名フィルタ
            if (!string.IsNullOrWhiteSpace(filter.DriverName))
            {
                querySb.AppendLine("AND t.driver_name = @DriverName");
            }

            // 終了日フィルタ
            if ((filter.From is not null) != (filter.To is not null))
            {
                /* 終了日が両方指定されていないと例外スロー */
                throw new ArgumentException("フィルターは、開始日と終了日の両方を指定してください。");
            }

            if (filter.From is not null)
            {
                querySb.AppendLine("AND j.closed_at >= CAST(@From AS DATE)");
                querySb.AppendLine("AND j.closed_at < DATEADD(DAY, 1, CAST(@To AS DATE))");
            }

            // 降順ソート（最新が上）
            querySb.AppendLine("ORDER BY j.closed_at DESC");

            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var jobList = await connection.QueryAsync<Job>(
                new CommandDefinition(
                    querySb.ToString(),
                    new
                    {
                        Status = status,
                        TaxiId = filter.TaxiId,
                        DriverName = filter.DriverName,
                        From = filter.From,
                        To = filter.To
                    },
                    cancellationToken: token));

            logger.LogInformation("[成功]運行履歴取得");
            return [.. jobList];
        }
        catch (ArgumentException ex)
        {
            /* フィルター状態がおかしい */
            logger.LogWarning(ex, "[失敗]フィルター異常：運行履歴取得に失敗");
            throw;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]運行履歴取得");
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

            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT Count(*)
                    FROM jobs j
                    WHERE j.closed_at >= CAST(GETDATE() AS DATE)
                    AND j.closed_at < DATEADD(day, 1, CAST(GETDATE() AS DATE));
                    """,
                    cancellationToken: token));

            logger.LogInformation("[成功]本日完了済みJOBの個数取得");
            return count;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]本日完了済みJOBの個数取得");
            throw;
        }
    }

    // タクシー一覧取得
    public async Task<List<Taxi>> GetTaxisAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var taxiList = await connection.QueryAsync<Taxi>(
                new CommandDefinition(
                    """
                    SELECT                    
                        t.id AS [Id],
                        t.taxi_status_id AS [Status],
                        t.driver_name AS [DriverName],
                        j.id AS [JobId]
                    FROM taxis t
                    LEFT JOIN jobs j
                        ON j.taxi_id = t.id
                        AND j.closed_at IS NULL
                    ORDER BY t.id ASC;
                    """,
                    cancellationToken: token));

            logger.LogInformation("[成功]タクシー一覧取得");
            return [.. taxiList];
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]タクシー一覧取得");
            throw;
        }
    }

    // タクシーの台数取得
    public async Task<int> GetTaxisCountAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT Count(*)
                    FROM taxis;
                    """,
                    cancellationToken: token));

            logger.LogInformation("[成功]タクシーの台数取得");
            return count;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]タクシーの台数取得");
            throw;
        }
    }

    // 割当可能なタクシー一覧取得(stringリスト)
    public async Task<List<string>> GetAvailableTaxisAsync(CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var taxiList = await connection.QueryAsync<string>(
                new CommandDefinition(
                    """
                    SELECT t.id
                    FROM taxis t
                    WHERE t.taxi_status_id = @Idle
                    ORDER BY t.id ASC;
                    """,
                    new
                    {
                        Idle = TaxiStatus.Idle
                    },
                    cancellationToken: token));

            logger.LogInformation("[成功]割当可能タクシー一覧取得");
            return [.. taxiList];
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]割当可能タクシー一覧取得");
            throw;
        }
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
                    WHERE taxi_status_id = @Idle;
                    """,
                    new
                    {
                        Idle = TaxiStatus.Idle
                    },
                    cancellationToken: token));
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]タクシーの台数取得");
            throw;
        }
    }

    // ===== スタブ用 =====

    // タクシーの情報取得（割当JOB）
    public async Task<TaxiInfo> GetCurrentTaxiInfoAsync(string id, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var currentJob = await GetTaxiInfoAsync(connection, null, id, token);

            logger.LogInformation("[成功]タクシーの割当JOB取得");
            return currentJob ?? throw new ArgumentException("タクシーIDが不正");
        }
        catch (ArgumentException ex)
        {
            /* DBエラー、割当2件以上あった等 */
            logger.LogWarning(ex, "[失敗]タクシーIDが存在しない");
            throw;
        }
        catch (Exception ex)
        {
            /* DBエラー、割当2件以上あった等 */
            logger.LogError(ex, "[エラー]タクシーの割当JOB取得");
            throw;
        }
    }

    // タクシー状態の更新
    public async Task<bool> SetCurrentTaxiStatusAsync(string id, TaxiStatus status, CancellationToken token)
    {
        try
        {
            /* IDバリデーション */
            if (await AnyTaxiAsync(id, token))
            {
                logger.LogWarning("[失敗]存在しないタクシーID");
                return false;
            }

            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            // トランザクション開始
            await using var tran = await connection.BeginTransactionAsync(token);

            /* タクシー状態とJOB状態を更新 */
            try
            {
                /* タクシー状態遷移確認 */
                var currentTaxiStatus = await connection.ExecuteScalarAsync<TaxiStatus>(
                    new CommandDefinition(
                        """
                    SELECT t.taxi_status_id
                    FROM taxis t
                    WHERE t.id = @Id;
                    """,
                        new
                        {
                            Id = id
                        },
                        transaction: tran,
                        cancellationToken: token));

                // 遷移不可能ならば
                if (!CanTaxiTransition(currentTaxiStatus, status))
                {
                    /* ロールバックしてfalse */
                    await tran.RollbackAsync(token);
                    logger.LogWarning("[失敗]タクシー状態遷移失敗：{current}->{new}", currentTaxiStatus, status);
                    return false;
                }

                // タクシー状態をtran内で変更
                var taxiAffected = await connection.ExecuteAsync(
                    """                    
                    UPDATE taxis
                    SET taxi_status_id  = @NextTaxiStatus
                    WHERE id = @id
                    AND taxi_status_id = @CurrentTaxiStatus;
                    """,
                    new
                    {
                        Id = id,
                        CurrentTaxiStatus = currentTaxiStatus,
                        NextTaxiStatus = status
                    }
                    , tran);

                // 遷移に失敗したら
                if (taxiAffected == 0)
                {
                    /* ロールバックしてfalse */
                    await tran.RollbackAsync(token);
                    logger.LogWarning("[失敗]DB状態不整合");
                    return false;
                }


                /* JOB状態遷移確認 */
                // このタクシーに割り当てられているJOBからJOB状態を取得
                var assignedJob = await connection.QuerySingleOrDefaultAsync<Job>(new CommandDefinition(
                    """
                    SELECT                    
                        j.id AS [Id],
                        j.job_status_id AS [Status],
                        j.from_loc AS [FromLoc],
                        j.to_loc AS [ToLoc],
                        j.taxi_id AS [TaxiId],
                        t.driver_name AS [DriverName],
                        j.closed_at AS [ClosedAt]
                    FROM jobs j
                    LEFT JOIN taxis t
                        ON t.id = j.taxi_id
                    WHERE j.id = @Id
                    AND j.closed_at IS NULL
                    """,
                    new
                    {
                        Id = id
                    },
                    cancellationToken: token));

                // JOBが存在したら更新
                if (assignedJob is not null)
                {
                    // このJOB状態と、次の遷移状態を取得 => 割当なしならnull
                    var currentJobStatus = assignedJob.Status;
                    var nextJobStatus = GetNextJobStatus(currentJobStatus);

                    // JOB状態を更新
                    var jobAffected = await connection.ExecuteAsync(
                        """                    
                        UPDATE jobs
                        SET job_status_id  = @NextJobStatus
                        WHERE id = @id
                        AND job_status_id = @CurrentJobStatus;
                        """,
                        new
                        {
                            Id = assignedJob.Id,
                            CurrentJobStatus = currentJobStatus,
                            NextJobStatus = nextJobStatus
                        }
                        , tran);


                    // 影響したレコードが0件（=カラム制約違反など）
                    if (jobAffected == 0)
                    {
                        /* ロールバックしてfalse */
                        await tran.RollbackAsync(token);
                        logger.LogWarning("[失敗]登録データ不正");
                        return false;
                    }
                }

                // コミットしてture
                await tran.CommitAsync(token);
                logger.LogInformation("[成功]タクシー状態の更新に成功");
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
            /* DBエラー、割当2件以上あった等 */
            logger.LogError(ex, "[エラー]タクシーの割当JOB取得");
            throw;
        }
    }

    // ===== 汎用 =====

    // JOB IDの存在確認
    public async Task<bool> AnyActiveJobAsync(string id, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var jobExists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT
                        CASE
                            WHEN EXISTS(
                                SELECT 1
                                FROM jobs
                                WHERE id = @Id
                                AND closed_at IS NULL
                            )
                            THEN CAST(1 AS BIT)
                            ELSE CAST(0 AS BIT)
                        END;
                    """,
                    new
                    {
                        Id = id
                    },
                    cancellationToken: token));


            logger.LogInformation("[成功]JOB存在確認");
            return jobExists;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]JOB存在確認");
            throw;
        }
    }

    // タクシーIDの存在確認
    public async Task<bool> AnyTaxiAsync(string id, CancellationToken token)
    {
        try
        {
            /* DB接続開始 */
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var taxiExists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT
                        CASE
                            WHEN EXISTS(
                                SELECT 1
                                FROM taxis
                                WHERE id = @Id
                            )
                            THEN CAST(1 AS BIT)
                            ELSE CAST(0 AS BIT)
                        END;
                    """,
                    new
                    {
                        Id = id
                    },
                    cancellationToken: token));


            logger.LogInformation("[成功]タクシー存在確認");
            return taxiExists;
        }
        catch (Exception ex)
        {
            /* DBエラー等 */
            logger.LogError(ex, "[エラー]タクシー存在確認");
            throw;
        }
    }


    // ===== プライベート =====

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

            // 保存されているJOB IDの変換ができなかったとき
            if (!isDate || !isBranch)
            {
                logger.LogWarning("[失敗]JOB IDの生成に失敗");
                throw new KeyNotFoundException($"JOB IDの生成に失敗:{latestJobId}");
            }

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
            logger.LogError(ex, "[エラー]DBアクセスエラー");
            throw;
        }
    }

    // タクシー情報の取得
    private static async Task<TaxiInfo?> GetTaxiInfoAsync(
        SqlConnection connection,
        IDbTransaction? tran,
        string id,
        CancellationToken token)
    {
        return await connection.QuerySingleOrDefaultAsync<TaxiInfo>(
                new CommandDefinition(
                    """
                    SELECT                    
                        t.id AS [TaxiId],
                        t.taxi_status_id AS [Status],
                        t.driver_name AS [DriverName],
                        j.id AS [JobId],
                        j.job_status_id AS [JobStatus],
                        j.from_loc AS [FromLoc],
                        j.to_loc AS [ToLoc]
                    FROM taxis t
                    LEFT JOIN jobs j
                        ON j.taxi_id = t.id
                       AND j.closed_at IS NULL
                    WHERE t.id = @Id;
                    """,
                    new
                    {
                        Id = id
                    },
                    transaction: tran,
                    cancellationToken: token));
    }

    // タクシー遷移バリデーション
    private static bool CanTaxiTransition(TaxiStatus currentStatus, TaxiStatus newStatus) =>
        currentStatus switch
        {
            TaxiStatus.Idle => newStatus is TaxiStatus.Reserved or TaxiStatus.OffDuty,
            TaxiStatus.Reserved => newStatus is TaxiStatus.Occupied,
            TaxiStatus.Occupied => newStatus is TaxiStatus.Idle,
            TaxiStatus.OffDuty => newStatus is TaxiStatus.Idle,
            _ => false
        };

    // JOBの正常系遷移状態
    private static JobStatus GetNextJobStatus(JobStatus current) =>
        current switch
        {
            JobStatus.Queued => JobStatus.Waiting,
            JobStatus.Waiting => JobStatus.Active,
            JobStatus.Active => JobStatus.Completed,
            JobStatus.Aborting => JobStatus.Aborted,
            _ => throw new InvalidOperationException($"JOB状態が不正です。JobStatus：{current}")
        };
}
