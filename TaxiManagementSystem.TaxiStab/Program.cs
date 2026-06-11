using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var baseUri = new Uri("http://172.16.7.10:8080/api/");
var taxisPrefix = "taxis/";
var eventsPrefix = "events/";


bool isRunning = false;
HttpClient client = new();
TaxiInfo? taxi = null;

string? targetTaxiId = "";
string? viewMessage = "";

using CancellationTokenSource mainCts = new();
try
{
    // タクシーIDの入力
    while (!mainCts.Token.IsCancellationRequested)
    {
        if (!isRunning)
        {
            // タクシー状態の取得
            Console.Write("監視するタクシーIDを入力してください > ");
            var input = Console.ReadLine();

            targetTaxiId = input;
            taxi = await GetTaxiInfoAsync(targetTaxiId, mainCts.Token);
            if(taxi == null)
            {
                Console.Clear();
                Console.WriteLine("入力値が不正か、IDが存在しません。");
                continue;
            }
            
            isRunning = true;
        }

        // 表示ループ開始
        Console.Clear();

        // 表示
        Console.WriteLine($"""
            ==============================
              JOB ID : {taxi?.TaxiId ?? "N/A"}
              STATUS : {taxi?.Status ?? "N/A"}
              DRIVER : {taxi?.DriverName ?? "N/A"}
              JOB ID : {taxi?.JobId ?? "-"}
              FROM   : {taxi?.FromLoc ?? "-"}
              TO     : {taxi?.ToLoc ?? "-"}
            ==============================

              MESSAGE : {viewMessage}

              [ESC] -> EXIT           
              [1]   -> CHANGE TO IDLE
              [2]   -> CHANGE TO RESERVED
              [3]   -> CHANGE TO OCCUPIED
              [4]   -> CHANGE TO OFFDUTY
            """);

        // ポーリング用キャンセル変数
        using var pollingCts = CancellationTokenSource.CreateLinkedTokenSource(mainCts.Token);

        // 入力タスク
        var inputTask = Task.Run(async () =>
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var input = Console.ReadKey(true);
                    if(input.Key is ConsoleKey.Escape
                    or ConsoleKey.D1
                    or ConsoleKey.D2
                    or ConsoleKey.D3
                    or ConsoleKey.D4)
                    {
                        return input;
                    }
                }
                await Task.Delay(50, pollingCts.Token);
            }

        }, pollingCts.Token);

        // 通信タスク
        var eventsUri = new Uri(baseUri, eventsPrefix);
        var getEventsTask = client.GetAsync(eventsUri, pollingCts.Token);

        // タイムアウト
        var timeoutTask = Task.Delay(50000, pollingCts.Token);

        // 同時実行して中身取り出し
        var result = await Task.WhenAny(inputTask, getEventsTask, timeoutTask);
        pollingCts.Cancel();

        if (result == inputTask)
        {
            /* キー入力受付 */
            var inputKeyInfo = await inputTask;

            if (inputKeyInfo.Key == ConsoleKey.Escape)
            {
                /* ESCのとき：アプリ終了 */
                mainCts.Cancel();
                break;
            }

            // int変換
            int newState = inputKeyInfo.KeyChar - '0';

            // レスポンス取得
            using var response = await PutTaxiStatusAsync(targetTaxiId, newState, mainCts.Token);
            viewMessage = response.StatusCode switch
            {
                HttpStatusCode.NoContent => "リクエスト処理は正常に完了しました。",
                HttpStatusCode.BadRequest => "ステータス遷移が不正です。",
                HttpStatusCode.NotFound => "タクシーIDが不正です。",
                _ => $"不明なエラー：{response.StatusCode}"
            };

            // タクシー状態を再取得
            taxi = await GetTaxiInfoAsync(targetTaxiId, mainCts.Token);
        }
        else if(result == getEventsTask)
        {
            /* 変更通知受信 */
            using var response = await getEventsTask;

            if(response.StatusCode != HttpStatusCode.OK)
            {
                /* OK：変更あり以外ならリロード */
                continue;
            }

            // タクシー状態を再取得
            taxi = await GetTaxiInfoAsync(targetTaxiId, mainCts.Token);
        }
        else
        {
            /* タイムアウト：リロード */
            continue;
        }
    }
}
catch (OperationCanceledException) when (mainCts.Token.IsCancellationRequested)
{
    Console.WriteLine("アプリケーションは正常に終了しました。");
}
catch (OperationCanceledException)
{
    Console.WriteLine("操作がキャンセルされました。");
}
catch (HttpRequestException)
{
    Console.WriteLine("通信が確立できませんでした。");
}

async Task<TaxiInfo?> GetTaxiInfoAsync(string? taxiId, CancellationToken token)
{
    var taxiUri = new Uri(baseUri, taxisPrefix);
    using var response = await client.GetAsync(new Uri(taxiUri, taxiId), token); // タクシー情報の取得

    if (response.StatusCode != HttpStatusCode.OK)
    {
        return null;
    }

    try
    {
        var taxiInfo = await response.Content.ReadFromJsonAsync<TaxiInfo>(token)
            ?? throw new JsonException();

        return taxiInfo;
    }
    catch (JsonException)
    {
        return null;
    }
}

async Task<HttpResponseMessage> PutTaxiStatusAsync(string? taxiId, int id, CancellationToken token)
{
    /* 入力値をJSON化 */
    var json = $$"""
        { "statusId" : {{id}} }
        """;

    // 送信
    var request = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var taxiUri = new Uri(baseUri, taxisPrefix);
    return await client.PutAsync(new Uri(taxiUri, taxiId), request, token);
}


public record TaxiInfo(
    string TaxiId,
    string Status,
    string DriverName,
    string? JobId,
    string? FromLoc,
    string? ToLoc);

