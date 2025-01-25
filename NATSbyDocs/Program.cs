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

// ストリーム名とサブジェクト
var streamName = "ORDERS";
var subjectName = "orders.>";
var consumerName = "durable_processor";

// -------------------------------------------
// ストリームの作成
// -------------------------------------------
Console.WriteLine("Creating stream...");
var streamConfig = new StreamConfig(name: streamName, subjects: new[] { subjectName });
await js.CreateStreamAsync(streamConfig);
Console.WriteLine($"Stream '{streamName}' created.");

// -------------------------------------------
// Durable Consumerの作成
// -------------------------------------------
Console.WriteLine("Creating durable consumer...");
var durableConfig = new ConsumerConfig
{
    Name = consumerName,
    DurableName = consumerName,
};
var consumer = await js.CreateOrUpdateConsumerAsync(stream: streamName, durableConfig);

Console.WriteLine($"Consumer Name: {consumer.Info.Name}"); // durable_processor
Console.WriteLine($"Consumer DurableName: {consumer.Info.Config.DurableName}"); // durable_processor

// -------------------------------------------
// メッセージを発行
// -------------------------------------------
Console.WriteLine("Publishing messages...");
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 1, Description = "Order 1" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 2, Description = "Order 2" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 3, Description = "Order 3" });
Console.WriteLine("Messages published.");

//// -------------------------------------------
//// NextAsyncで1件ずつ取得
//// -------------------------------------------
//Console.WriteLine("Fetching one message with NextAsync...");
//var next = await consumer.NextAsync<Order>();
//if (next is { } msg)
//{
//    Console.WriteLine($"【NextAsync】Processing {msg.Subject}: {msg.Data.Id} - {msg.Data.Description}");
//    await msg.AckAsync();
//}

// -------------------------------------------
// ConsumeAsyncで連続処理
// -------------------------------------------
Console.WriteLine("Consuming messages with ConsumeAsync...");
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10秒でキャンセル
await foreach (var msg2 in consumer.ConsumeAsync<Order>().WithCancellation(cts.Token))
{
    Console.WriteLine($"【ConsumeAsync】Processing {msg2.Subject}: {msg2.Data.Id} - {msg2.Data.Description}");
    await msg2.AckAsync();
}

//// -------------------------------------------
//// Durable ConsumerでFetchAsyncによるバッチ処理
//// -------------------------------------------
//Console.WriteLine("Fetching messages with FetchAsync...");
//await foreach (var msg3 in consumer.FetchAsync<Order>(new NatsJSFetchOpts { MaxMsgs = 10 }))
//{
//    Console.WriteLine($"【FetchAsync】Processing {msg3.Subject}: {msg3.Data.Id} - {msg3.Data.Description}");
//    await msg3.AckAsync();
//}

// -------------------------------------------
// ストリームの削除（基本的に本番運用では削除しない、別項目管理とか？？）
// -------------------------------------------
Console.WriteLine("Deleting stream...");
await js.DeleteStreamAsync(streamName);
Console.WriteLine($"Stream '{streamName}' deleted.");

// プロセス終了
Console.WriteLine("Finished.");

// -------------------------------------------
// モデルクラス
// -------------------------------------------
public record Order
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
