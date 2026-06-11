namespace TaxiManagementSystem.API.Notifier;

public class EventNotifier
{
    // ロックオブジェクト
    private readonly object _lock = new();

    // 外部トリガーで完了する特殊なタスク
    private TaskCompletionSource _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously); 

    // 待機用メソッド
    public Task WaitAsync(CancellationToken token)
    {
        // 別スレッドによるWaitAsync/Publishとの競合を防ぐ
        lock (_lock)
        {
            return _tcs.Task.WaitAsync(token); // TCSのタスク（進捗）を返す
            // 標準：未完了状態　→　awaitすると待ちになる
            // Publishされる：完了状態になる　→　awaitが終了する
        }
    }

    // 外部トリガー
    public void Publish()
    {
        TaskCompletionSource currentTcs;

        // WaitAsync/別スレッドによるPublishとの競合を防ぐ
        lock (_lock)
        {
            // このlockに入るまでにWaitAsyncした呼出し元用のTCSを保存
            currentTcs = _tcs;

            // このlock以降にWaitAsyncする呼出し元用のTCSを新規発行
            _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // currentTcsのトリガー起動（完了切替）
        _ = currentTcs.TrySetResult(); 
    }

}
