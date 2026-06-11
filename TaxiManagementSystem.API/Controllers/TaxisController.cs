using Microsoft.AspNetCore.Mvc;
using TaxiManagementSystem.API.Model;
using TaxiManagementSystem.API.Notifier;
using TaxiManagementSystem.API.Repository;

namespace TaxiManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TaxisController(ITMSRepository repository, ILogger<TaxisController> logger, EventNotifier notifier) : ControllerBase
{
    // ==============================
    //   メイン画面用
    // ==============================

    // タクシーの台数取得
    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken token)
    {
        try
        {
            var taxiCount = await repository.GetTaxisCountAsync(token);
            logger.LogInformation("[200]タクシーの台数取得成功");
            return Ok(new { count = taxiCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }


    // 割当可能なタクシーの台数取得
    [HttpGet("available/count")]
    public async Task<IActionResult> GetAvailableCount(CancellationToken token)
    {
        try
        {
            var taxiCount = await repository.GetAvailableTaxisCountAsync(token);
            logger.LogInformation("[200]割当可能なタクシーの台数取得成功");
            return Ok(new { count = taxiCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }


    // 割当可能なタクシー一覧取得
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(CancellationToken token)
    {
        try
        {
            var availableTaxis = await repository.GetAvailableTaxisAsync(token);

            logger.LogInformation("[200]割当可能なタクシー一覧取得成功");
            return Ok(availableTaxis.Select(x => new AvailableActiveTaxi(x)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // ==============================
    //   タクシー一覧用
    // ==============================

    // タクシー一覧取得
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token)
    {
        try
        {
            var taxiList = await repository.GetTaxisAsync(token);

            logger.LogInformation("[200]タクシー一覧取得 件数:{Count}", taxiList.Count);
            return Ok(taxiList.Select(x =>
                new ActiveTaxi(x.Id, x.Status.ToString(), x.DriverName, x.JobId)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }


    // ==============================
    //   スタブ用
    // ==============================

    // タクシーの情報取得
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaxiInfo(string id, CancellationToken token)
    {
        try
        {
            /* ID存在チェック */
            if (!await repository.AnyTaxiAsync(id, token))
            {
                logger.LogWarning("[404]存在しないJOBが指定された");
                return NotFound(new { error = "TAXI_NOT_FOUND" });
            }

            var taxiInfo = await repository.GetCurrentTaxiInfoAsync(id, token);

            logger.LogInformation("[200]タクシーの情報取得成功");
            return Ok(new TaxiInfoResponse(
                taxiInfo.TaxiId,
                taxiInfo.Status.ToString(),
                taxiInfo.DriverName,
                taxiInfo.JobId,
                taxiInfo.JobStatus?.ToString(),
                taxiInfo.FromLoc,
                taxiInfo.ToLoc));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "[404]タクシーIDが存在しない");
            return NotFound(new { error = "TAXI_NOT_FOUND" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }

    // タクシー状態の更新
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUpdateTaxiState(string id, UpdateTaxiStatusRequest newStatus, CancellationToken token)
    {

        try
        {
            /* ID存在チェック */
            if (!await repository.AnyTaxiAsync(id, token))
            {
                logger.LogWarning("[404]タクシーIDが存在しない");
                notifier.Publish(); // 変更イベント発火
                return NotFound(new { error = "TAXI_NOT_FOUND" });
            }

            var result = await repository.SetCurrentTaxiStatusAsync(id, newStatus.StatusId, token);

            if (result)
            {
                /* 更新成功 */
                logger.LogInformation("[200]タクシー状態の更新成功");
                return NoContent();
            }
            else
            {
                logger.LogWarning("[400]状態遷移に失敗");
                return BadRequest(new { error = "INVALID_STATUS" });

            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[500]エラー発生");
            return StatusCode(500);
        }
    }
}
