# API仕様書

## API一覧

|Method|URI|Request Body|Query|Description|
|:---|:---|:---:|:---:|:---|
|GET|/api/jobs|-|-|実行中JOB一覧取得|
|POST|/api/jobs|○|-|JOB登録|
|GET|/api/jobs/count|-|-|実行中JOB個数取得|
|PUT|/api/jobs/{id}/reassign|○|-|タクシー再割当|
|PUT|/api/jobs/{id}/cancel|-|-|JOBキャンセル|
|PUT|/api/jobs/{id}/abort|-|-|JOB中断|
|GET|/api/jobs/history|-|○|運行履歴取得|
|GET|/api/jobs/history/count/today|-|-|本日完了済みJOBの個数取得|
|GET|/api/taxis|-|-|タクシー一覧取得|
|GET|/api/taxis/count|-|-|タクシーの台数取得|
|GET|/api/taxis/available|-|-|割当可能なタクシー一覧取得|
|GET|/api/taxis/available/count|-|-|割当可能なタクシーの台数取得|
|GET|/api/taxis/status|-|-|タクシー状態リストの取得|
|GET|/api/taxis/{id}|-|-|タクシーの情報取得|
|PUT|/api/taxis/{id}|○|-|タクシー状態の更新|
|GET|/api/events|-|-|JOBまたはタクシーの状態変更通知|

## 実行中JOB一覧取得

### Request

``` http
GET /api/jobs
```

### Response

#### 200 OK

正常
``` json
[
  {
    "jobId": "J20260609-0051",
    "status": "Active",
    "fromLoc": "新居浜駅",
    "toLoc": "イオンモール新居浜",
    "taxiId": "TX002",
    "driverName": "鈴木　次郎"
  },
  {
    "jobId": "J20260609-0052",
    "status": "Waiting",
    "fromLoc": "フレッシュバリュー喜光地",
    "toLoc": "喜光地自治会館",
    "taxiId": "TX003",
    "driverName": "高橋　三郎"
  },
  {
    "jobId": "J20260609-0053",
    "status": "Queued",
    "fromLoc": "新須賀自治会館",
    "toLoc": "フジ新居浜",
    "taxiId": null,
    "driverName": null
  }
]
```

## JOB登録

### Request

``` http
POST /api/jobs
```

#### Request Body

``` json
{
  "fromLoc": "新居浜駅",
  "toLoc": "イオンモール新居浜",
  "taxiId": "TX003"
}
```

### Response

#### 201 Created

正常
``` json
なし
```

#### 400 Bad Request

異常なリクエスト
``` json
{
  "error": "INVALID_REQUEST"
}
```

タクシー割当不可
``` json
{
  "error": "CANNOT_ASSIGN"
}
```

## 実行中JOB個数取得

### Request

``` http
GET /api/jobs/count
```

### Response

#### 200 OK

正常
``` json
{
  "count": 12
}
```

## タクシー再割当

### Request

``` http
PUT /api/jobs/{id}/reassign
```

#### Request Body

``` json
{
  "taxiId": "TX002"
}
```

### Response

#### 204 No Content

正常
``` json
なし
```

#### 400 Bad Request

タクシー割当不可
``` json
{
  "error": "CANNOT_ASSIGN"
}
```

#### 404 Not Found

JobIDが存在しない
``` json
{
  "error": "JOB_NOT_FOUND"
}
```

## JOBキャンセル

### Request

``` http
PUT /api/jobs/{id}/cancel
```

### Response

#### 204 No Content

正常
``` json
なし
```

#### 400 Bad Request

キャンセルできない状態のためキャンセルに失敗
``` json
{
  "error": "JOB_CANCEL_FAILED"
}
```

#### 404 Not Found

JobIDが存在しない
``` json
{
  "error": "JOB_NOT_FOUND"
}
```

## JOB中断

### Request

``` http
PUT /api/jobs/{id}/abort
```

### Response

#### 204 No Content

正常
``` json
なし
```

#### 400 Bad Request

中断できない状態のため中断に失敗
``` json
{
  "error": "JOB_ABORT_FAILED"
}
```

#### 404 Not Found

JobIDが存在しない
``` json
{
  "error": "JOB_NOT_FOUND"
}
```

## 運行履歴取得

### Request

``` http
GET /api/jobs/history
```

#### Query Parameter

|Query|Parameter|Description|
|---|---|---|
|status|completed,aborted,canceled|JOB状態指定で絞り込み|
|taxiId|(タクシーの番号)|タクシーID指定で絞り込み|
|driverName|(ドライバーの名前)|ドライバー指定で絞り込み|
|from|(JOB終了日の開始)|日付指定で絞り込み（toとセット）|
|to|(JOB終了日の終了)|日付指定で絞り込み（fromとセット）|

### Response

#### 200 OK

正常
``` json
[
  {
    "jobId": "J20260609-0041",
    "status": "Completed",
    "fromLoc": "新居浜駅",
    "toLoc": "イオンモール新居浜",
    "taxiId": "TX001",
    "driverName": "佐藤　一郎",
    "closedAt": "2026-06-01T14:23:18"
  },
  {
    "jobId": "J20260609-0042",
    "status": "Canceled",
    "fromLoc": "フレッシュバリュー喜光地",
    "toLoc": "喜光地自治会館",
    "taxiId": null,
    "driverName": null,
    "closedAt": "2026-06-01T15:48:18"
  },
  {
    "jobId": "J20260609-0043",
    "status": "Canceled",
    "fromLoc": "新須賀自治会館",
    "toLoc": "フジ新居浜",
    "taxiId": "TX002",
    "driverName": "鈴木　次郎",
    "closedAt": "2026-06-02T09:21:18"
  },
  {
    "jobId": "J20260609-0044",
    "status": "Aborted",
    "fromLoc": "リーガロイヤルホテル新居浜",
    "toLoc": "住友別子病院",
    "taxiId": "TX003",
    "driverName": "高橋　三郎",
    "closedAt": "2026-06-02T11:37:22"
  }
]
```

### 400 Bad Request

クエリパラメータ異常
``` json
{
  "error": "INVALID_QUERY"
}
```

## 本日の完了済みJOB個数取得

### Request

``` http
GET /api/jobs/history/count/today
```

### Response

#### 200 OK

正常
``` json
{
  "count": 12
}
```

## タクシー一覧取得

### Request

``` http
GET /api/taxis
```

### Response

#### 200 OK

``` json
[
  {
    "taxiId": "TX001",
    "status": "Occupied",
    "driverName": "佐藤　一郎",
    "jobId": "J004"
  },
  {
    "taxiId": "TX002",
    "status": "Idle",
    "driverName": "鈴木　次郎",
    "jobId": null
  },
  {
    "taxiId": "TX003",
    "status": "Idle",
    "driverName": "高橋　三郎",
    "jobId": null
  },
  {
    "taxiId": "TX004",
    "status": "OffDuty",
    "driverName": "田中　史郎",
    "jobId": null
  }
]
```

## タクシーの台数取得

### Request

``` http
GET /api/taxis/count
```

### Response

#### 200 OK

正常
``` json
{
  "count": 4
}
```

## 割当可能なタクシー一覧取得

### Request

``` http
GET /api/taxis/available
```

### Response

#### 200 OK

割当可能なタクシーが存在するとき
``` json
[
  {
    "taxiId": "TX002"
  },
  {
    "taxiId": "TX003"
  }
]
```

割当可能なタクシーが存在しないとき
``` json
[]
```

## 割当可能なタクシーの台数取得

### Request

``` http
GET /api/taxis/available/count
```

### Response

#### 200 OK

正常
``` json
{
  "count": 2
}
```

## タクシー状態リストの取得

### Request

``` http
GET /api/taxis/status
```
### Response

#### 200 OK

正常
``` json
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
```

## タクシーの情報取得

### Request

``` http
GET /api/taxis/{id}
```

### Response

#### 200 OK

正常
``` json
{
  "taxiId": "TX001",
  "status": "Reserved",
  "driverName": "佐藤　一郎",
  "jobId": "J20260609-0034",
  "jobStatus": "Active",
  "fromLoc": "新居浜駅",
  "toLoc": "銅夢キッチン"
}
```

#### 404 Not Found

存在しないタクシーIDを指定した
``` json
{
  "error": "TAXI_NOT_FOUND"
}
```

## タクシー状態の更新

### Request

``` http
PUT /api/taxis/{id}
```

#### Request Body

``` json
{
  "statusId": 1
}
```

### Response

#### 204 No Content

正常
``` json
なし
```

#### 400 Bad Request

不正なステータスを送信
``` json
{
  "error": "INVALID_STATUS"
}
```

#### 404 Not Found

存在しないタクシーIDを指定した
``` json
{
  "error": "TAXI_NOT_FOUND"
}
```

## JOBまたはタクシーの状態変更通知

### Request

``` http
GET /api/events
```

### Response

#### 200 OK

変更イベントがあった
``` json
なし
```

#### 204 No Content

変更イベントなく、30秒経過した
``` json
なし
```


## その他共通エラー

### Response

#### 404 Not Found

/api等実装していないURIへのアクセス
``` http
なし
```

#### 405 Method Not Allowed

各APIの規定外メソッドを受信した
``` http
なし
```

#### 500 Internal Server Error

なんらかのサーバーエラー（DBエラー等）
``` http
なし
```

