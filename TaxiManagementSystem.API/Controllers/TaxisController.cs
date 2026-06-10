using Microsoft.AspNetCore.Mvc;
using TaxiManagementSystem.API.Notifier;
using TaxiManagementSystem.API.Repository;

namespace TaxiManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TaxisController(IEditableRepository repository, EventNotifier notifier) : ControllerBase
{
    // タクシー一覧取得
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token)
    {
        return Ok();
    }

    // タクシーの台数取得
    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken token)
    {
        return Ok();
    }

    // 割当可能なタクシー一覧取得
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(CancellationToken token)
    {
        return Ok();
    }

    // 割当可能なタクシーの台数取得
    [HttpGet("available/count")]
    public async Task<IActionResult> GetAvailableCount(CancellationToken token)
    {
        return Ok();
    }

    // タクシーの情報取得
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaxiInfo(string id, CancellationToken token)
    {
        /* ID不正値 */
        if (false)
        {
            return NotFound();
        }

        return Ok();
    }

    // タクシー状態の更新
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUpdateTaxiState(string id, CancellationToken token)
    {
        /* ID不正値 */
        if (false)
        {
            return NotFound();
        }


        /* Status更新拒否 */
        if (false)
        {
            return BadRequest();
        }

        notifier.Publish(); // 変更イベント発火
        return NoContent();
    }
}
