# C# での NATS Client と JetStream

このリポジトリでは、C# を使用して NATS と JetStream を利用する方法を示します。このサンプルでは、ストリームの作成、メッセージの発行、コンシューマによる処理、およびストリーム管理の手順を説明します。

## 必要条件

- .NET 6.0 以上
- [NATS.Net NuGet パッケージ](https://www.nuget.org/packages/NATS.Net)

## インストール

このサンプルを実行する前に、以下のコマンドで必要な NuGet パッケージをインストールしてください：

```bash
dotnet add package NATS.Net
```

また、NATS サーバーがローカル環境またはアクセス可能な URL 上で稼働していることを確認してください。デフォルトでは、このサンプルは `nats://127.0.0.1:4222` に接続します。

## サンプルの実行

1. このリポジトリをクローンし、プロジェクトディレクトリに移動します。
2. 以下のコマンドでビルドおよび実行します：

```bash
dotnet run
```

スクリプトは以下の手順を実行します：

### 1. NATS サーバーへの接続

`NATS_URL` 環境変数から URL を取得して NATS サーバーに接続します。設定されていない場合は、デフォルトで `nats://127.0.0.1:4222` を使用します：

```csharp
var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
await using var nc = new NatsClient(url);
await nc.ConnectAsync();
```

### 2. JetStream コンテキストの作成

ストリームやコンシューマを管理するための JetStream コンテキストを作成します：

```csharp
var js = nc.CreateJetStreamContext();
```

### 3. ストリームの定義と作成

`ORDERS` という名前のストリームを作成し、サブジェクトパターン `orders.>` を指定します：

```csharp
var streamConfig = new StreamConfig(name: "ORDERS", subjects: new[] { "orders.>" });
await js.CreateStreamAsync(streamConfig);
```

### 4. Durable Consumer の作成

`durable_processor` という名前の Durable Consumer を作成し、メッセージを処理します：

```csharp
var durableConfig = new ConsumerConfig
{
    Name = "durable_processor",
    DurableName = "durable_processor",
};
var consumer = await js.CreateOrUpdateConsumerAsync(stream: "ORDERS", durableConfig);
```

### 5. メッセージの発行

`orders.new` サブジェクトにメッセージを発行します：

```csharp
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 1, Description = "Order 1" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 2, Description = "Order 2" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 3, Description = "Order 3" });
```

### 6. メッセージの処理

`ConsumeAsync` メソッドを使用してメッセージを処理します。このプロセスは最大 10 秒間実行され、その後キャンセルされます：

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var msg in consumer.ConsumeAsync<Order>().WithCancellation(cts.Token))
{
    Console.WriteLine($"Processing {msg.Subject}: {msg.Data.Id} - {msg.Data.Description}");
    await msg.AckAsync();
}
```

### 7. ストリームの削除（オプション）

開発時のクリーンアップとしてストリームを削除します。本番環境では通常、このステップは不要です：

```csharp
await js.DeleteStreamAsync("ORDERS");
```

### 8. データモデルの定義

以下は注文用のシンプルなデータモデルです：

```csharp
public record Order
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
```

## 注意点

- このサンプルでは、信頼性の高いメッセージ処理のために Durable Consumer を使用しています。メッセージが正常に処理されたことを示すために `AckAsync` を呼び出します。
- `NextAsync`（1 件のメッセージを取得）や `FetchAsync`（バッチ処理）などの代替メソッドはコード内でコメントアウトされています。
- ストリームの削除は開発時のクリーンアップ用であり、本番環境では通常不要です。

## トラブルシューティング

- NATS サーバーが稼働していることを確認してください。
- `NATS_URL` 環境変数が正しく設定されているか、またはデフォルトの `nats://127.0.0.1:4222` を使用してください。
- `dotnet list package` を使用して NuGet パッケージがインストールされているか確認してください。

## 参考資料

- [NATS ドキュメント](https://docs.nats.io/)
- [NATS.Net GitHub リポジトリ](https://github.com/nats-io/nats.net)

