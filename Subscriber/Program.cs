// Subscriber.csproj
// Install NuGet package `NATS.Net`
using System.Text.Json.Serialization;
using System.Threading;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
await using var nc = new NatsClient(url);

await nc.ConnectAsync();

// JetStreamコンテキストの作成
var js = nc.CreateJetStreamContext();

// ストリーム名とコンシューマー名
var streamName = "ORDERS";
var consumerName = "durable_processor";

// Durable Consumerの作成
Console.WriteLine("Creating durable consumer...");
var durableConfig = new ConsumerConfig
{
    Name = consumerName,
    //DurableName = consumerName, // Durable Consumerとして永続化
    //AckPolicy = ConsumerConfigAckPolicy.Explicit, // 明示的にACKを必要とする
    //MaxAckPending = 100, // ACK待ちのメッセージ上限（必要に応じて調整）
};
var consumer = await js.CreateOrUpdateConsumerAsync(stream: streamName, durableConfig);

Console.WriteLine($"Consumer Name: {consumer.Info.Name}"); // durable_processor
Console.WriteLine($"Consumer DurableName: {consumer.Info.Config.DurableName}"); // durable_processor

// ConsumeAsyncで連続処理
Console.WriteLine("Consuming messages with ConsumeAsync...");
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10秒でキャンセル
await foreach (var msg2 in consumer.ConsumeAsync<Order>().WithCancellation(cts.Token))
{
    Console.WriteLine($"【ConsumeAsync】Processing {msg2.Subject}: {msg2.Data.Id} - {msg2.Data.Description}");
    await msg2.AckAsync();
}

// プロセス終了
Console.WriteLine("Subscriber finished.");

// モデルクラス
public record Order
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
