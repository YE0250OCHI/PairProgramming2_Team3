using Microsoft.AspNetCore.Mvc;
using TaxiManagementSystem.API.Notifier;

namespace TaxiManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController(EventNotifier notifier) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken token)
    {
        try
        {
            // 変更通知用タスク
            var notifyTask = notifier.WaitAsync(token);

            // 40秒以内に変更通知があるか？
            var completed = await Task.WhenAny(
                notifyTask,
                Task.Delay(40000, CancellationToken.None));

            // 変更通知があったら200、40秒経過したら204を返す
            return completed == notifyTask
                ? Ok()
                : NoContent();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // クライアント切断などがあったとき
            return new EmptyResult();
        }
    }
}
