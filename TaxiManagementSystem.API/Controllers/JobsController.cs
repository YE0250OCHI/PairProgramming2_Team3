using Microsoft.AspNetCore.Mvc;
using TaxiManagementSystem.API.Model;
using TaxiManagementSystem.API.Notifier;
using TaxiManagementSystem.API.Repository;

namespace TaxiManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class JobsController(ITMSRepository repository, ILogger<JobsController> logger, EventNotifier notifier) : ControllerBase
{
    // ==============================
    //   メイン画面用
    // ==============================

    // 実行中JOB一覧取得
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token)
    {
        try
        {
            var jobList = await repository.GetActiveJobsAsync(token);

            logger.LogInformation("[200]実行中JOB一覧取得 件数:{Count}", jobList.Count);
            return Ok(jobList.Select(x =>
                new ActiveJob(x.Id, x.Status.ToString(), x.FromLoc, x.ToLoc, x.TaxiId, x.DriverName)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // 実行中JOB個数取得
    [HttpGet("count")]
    public async Task<IActionResult> GetTodayActiveJobs(CancellationToken token)
    {
        try
        {
            var jobCount = await repository.GetActiveJobsCountAsync(token);
            logger.LogInformation("[200]実行中JOB個数取得成功");
            return Ok(new { count = jobCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // 本日完了済みJOBの個数取得
    [HttpGet("history/count/today")]
    public async Task<IActionResult> GetTodayHistoryCount(CancellationToken token)
    {
        try
        {
            var jobCount = await repository.GetTodayHistoryCountAsync(token);
            logger.LogInformation("[200]本日完了済みJOBの個数取得成功");
            return Ok(new { count = jobCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // タクシー再割当
    [HttpPut("{id}/reassign")]
    public async Task<IActionResult> PutReassignTaxi(
        string id,
        ReassignTaxiRequest reassignRequest,
        CancellationToken token)
    {
        try
        {
            /* ID存在チェック */
            if (!await repository.AnyActiveJobAsync(id, token))
            {
                logger.LogWarning("[404]存在しないJOBが指定された");
                return NotFound(new { error = "JOB_NOT_FOUND" });
            }

            var result = await repository.TryReassignTaxiAsync(id, reassignRequest.TaxiId, token);

            if (result)
            {
                /* 正常終了 */
                logger.LogInformation("[204]タクシー再割当成功");
                notifier.Publish(); // 変更イベント発火
                return NoContent();
            }
            else
            {
                /* タクシー割当拒否 */
                logger.LogWarning("[400]タクシーの割当が拒否された");
                return BadRequest(new { error = "CANNOT_ASSIGN" });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // JOBキャンセル
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> PutCancelJob(string id, CancellationToken token)
    {
        try
        {
            /* ID存在チェック */
            if (!await repository.AnyActiveJobAsync(id, token))
            {
                logger.LogWarning("[404]存在しないJOBが指定された");
                return NotFound(new { error = "JOB_NOT_FOUND" });
            }

            var result = await repository.TryCancelJobAsync(id, token);

            if (result)
            {
                /* 正常終了 */
                logger.LogInformation("[204]JOBキャンセル成功");
                notifier.Publish(); // 変更イベント発火
                return NoContent();
            }
            else
            {
                /* JOBキャンセル拒否 */
                logger.LogWarning("[400]JOBキャンセルが拒否された");
                return BadRequest(new { error = "JOB_CANCEL_FAILED" });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // JOB中断
    [HttpPut("{id}/abort")]
    public async Task<IActionResult> PutAbortJob(string id, CancellationToken token)
    {
        try
        {
            /* ID存在チェック */
            if (!await repository.AnyActiveJobAsync(id, token))
            {
                logger.LogWarning("[404]存在しないJOBが指定された");
                return NotFound(new { error = "JOB_NOT_FOUND" });
            }

            var result = await repository.TryAbortJobAsync(id, token);

            if (result)
            {
                /* 正常終了 */
                logger.LogInformation("[204]JOB中断成功");
                notifier.Publish(); // 変更イベント発火
                return NoContent();
            }
            else
            {
                /* JOB中断拒否 */
                logger.LogWarning("[400]JOB中断が拒否された");
                return BadRequest(new { error = "JOB_ABORT_FAILED" });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // ==============================
    //   JOB登録画面用
    // ==============================

    // JOB登録
    [HttpPost]
    public async Task<IActionResult> PostCreateJob([FromBody] RegisterJobRequest registerJob, CancellationToken token)
    {
        try
        {
            /* 入力値不正確認 */
            if ((registerJob is null) ||
                (registerJob.FromLoc.Length is < 1 or > 20) ||
                (registerJob.ToLoc.Length is < 1 or > 20) ||
                ((registerJob.TaxiId is not null) && (!await repository.AnyTaxiAsync(registerJob.TaxiId, token))))
            {
                return BadRequest(new { error = "INVALID_REQUEST" });
            }

            // タスク登録
            var result = await repository.TryCreateNewJobAsync(
                registerJob.FromLoc,
                registerJob.ToLoc,
                registerJob.TaxiId,
                token);

            if (result)
            {
                /* 正常終了 */
                logger.LogInformation("[204]JOB中断成功");
                notifier.Publish(); // 変更イベント発火
                return Created();
            }
            else
            {
                /* 登録失敗 */
                logger.LogWarning("[400]JOB作成時にタクシー割当失敗");
                return BadRequest(new { error = "CANNOT_ASSIGN" });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // ==============================
    //   履歴表示用
    // ==============================

    // 運行履歴取得
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] HistoryFilter filter,
        CancellationToken token)
    {
        try
        {
            var jobList = await repository.GetHistoryAsync(filter, token);

            logger.LogInformation("[200]運行履歴取得 件数:{Count}", jobList.Count);
            return Ok(jobList.Select(x =>
                new CompletedJob(x.Id, x.Status.ToString(), x.FromLoc, x.ToLoc, x.TaxiId, x.DriverName,x.ClosedAt)));
        }
        catch (ArgumentException ex)
        {
            /* フィルター状態がおかしい */
            logger.LogWarning(ex, "[400]クエリパラメータ不正");
            return BadRequest(new { error = "INVALID_QUERY" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }
}
