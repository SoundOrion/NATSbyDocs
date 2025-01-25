// Publisher.csproj
// Install NuGet package `NATS.Net`
using System.Text.Json.Serialization;
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

Console.WriteLine("Publishing messages...");

// ストリームの作成（初回のみ必要）
var streamConfig = new StreamConfig(name: streamName, subjects: new[] { subjectName });
await js.CreateStreamAsync(streamConfig);
Console.WriteLine($"Stream '{streamName}' created.");

// メッセージを発行
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 1, Description = "Order 1" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 2, Description = "Order 2" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 3, Description = "Order 3" });
Console.WriteLine("Messages published.");

//// -------------------------------------------
//// ストリームの削除（基本的に本番運用では削除しない、別項目管理とか？？）
//// -------------------------------------------
//Console.WriteLine("Deleting stream...");
//await js.DeleteStreamAsync(streamName);
//Console.WriteLine($"Stream '{streamName}' deleted.");

// プロセス終了
Console.WriteLine("Publisher finished.");

// モデルクラス
public record Order
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
