using System.Net;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static readonly object eventLock = new();

    private static TaskCompletionSource eventSignal =
    new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Task WaitEventAsync()
    {
        lock (eventLock)
        {
            return eventSignal.Task;
        }
    }

    private static void PublishEvent()
    {
        TaskCompletionSource oldSignal;

        lock (eventLock)
        {
            oldSignal = eventSignal;
            eventSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        oldSignal.TrySetResult();
    }



    private static string _taxiStatus = "Reserved";

    private static async Task Main(string[] args)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


        HttpListener listener = new();
        listener.Prefixes.Add("http://+:8080/api/");
        listener.Start();

        Console.WriteLine("Listen.");

        using CancellationTokenSource cts = new();

        while (true)
        {
            var context = await listener.GetContextAsync();

            _ = Task.Run(async () =>
            {
                var request = context.Request;
                var response = context.Response;

                var url = request.Url?.AbsolutePath;
                var segments = url?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var method = request.HttpMethod;
                Console.WriteLine($"Request Recieved.Url={url},Method={method}");

                if (segments is null || segments.Length < 2)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                    Console.WriteLine(HttpStatusCode.NotFound);
                    return;
                }

                Console.Write("  ");
                foreach (var s in segments)
                {
                    Console.Write($"seg:{s} ");
                }
                Console.WriteLine();

                switch (segments[1])
                {
                    case "events":
                        var eventTask = WaitEventAsync();

                        var completed = await Task.WhenAny(
                            eventTask,
                            Task.Delay(30000, cts.Token));

                        response.StatusCode =
                            completed == eventTask
                                ? (int)HttpStatusCode.OK        // 変更あり
                                : (int)HttpStatusCode.NoContent; // 変更なし

                        response.ContentLength64 = 0;
                        response.Close();
                        break;
                    case "taxis":
                        if (segments.Length != 3)
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.Close();
                            break;
                        }

                        if (string.Equals(segments[2], "status"))
                        {
                            string taxiStatusList = """
                    [
                        {
                        "id": 1,
                        "name": "Idle"
                        },
                        {
                        "id": 2,
                        "name": "Reserved"
                        },
                        {
                        "id": 3,
                        "name": "Occupied"
                        },
                        {
                        "id": 4,
                        "name": "OffDuty"
                        }
                    ]
                    """;
                            string cleanJson = taxiStatusList.Replace("\r\n", " ").Trim();

                            SetResponseBody(response, cleanJson);
                            break;
                        }


                        if (!string.Equals(segments[2], "TX001"))
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            var error = """
                        { "error": "TAXI_NOT_FOUND" }
                        """;

                            SetResponseBody(response, error);
                            break;
                        }

                        switch (method)
                        {
                            case "GET":
                                string taxiInfo = $$"""
                                    {
                                        "taxiId": "TX001",
                                        "status": "{{_taxiStatus}}",
                                        "driverName": "佐藤　一郎",
                                        "jobId": "J20260609-0034",
                                        "fromLoc": "新居浜駅",
                                        "toLoc": "銅夢キッチン"
                                    }
                                    """;
                                string cleanJson = taxiInfo.Replace("\r\n", " ").Trim();

                                SetResponseBody(response, cleanJson);

                                break;
                            case "PUT":


                                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                                {
                                    var jsonBody = reader.ReadToEnd();

                                    var taxiStatus = JsonSerializer.Deserialize<PutBody>(jsonBody, options);
                                    if (taxiStatus is null || taxiStatus.StatusId < 1 || taxiStatus.StatusId > 4)
                                    {
                                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                                        var error = """
                                    { "error": "INVALID_STATUS" }
                                    """;
                                        SetResponseBody(response, error);
                                        break;
                                    }

                                    _taxiStatus = taxiStatus.StatusId switch
                                    {
                                        1 => "Idle",
                                        2 => "Reserved",
                                        3 => "Occupied",
                                        _ => "OffDuty"
                                    };

                                    Console.WriteLine($"TaxiStatus:{_taxiStatus}");
                                }

                                response.StatusCode = (int)HttpStatusCode.NoContent;
                                response.Close();

                                PublishEvent();

                                break;
                            default:
                                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                                response.Close();
                                break;
                        }


                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        response.Close();
                        break;
                }

                Console.WriteLine((HttpStatusCode)response.StatusCode);
                
            },cts.Token);
            
        }
    }

    private static void SetResponseBody(HttpListenerResponse response, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = buffer.Length;

        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }

}

record PutBody(int StatusId);