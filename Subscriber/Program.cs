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
Console.WriteLine("Consuming messages with ConsumeAsync, max 3 parallel processing...");

// 最大並列数を3に制限
using var semaphore = new SemaphoreSlim(3);

// 無期限ループで常駐
while (true)
{
    try
    {
        // メッセージを逐次処理
        bool hasMessages = false;

        await foreach (var msg in consumer.ConsumeAsync<Order>())
        {
            hasMessages = true; // メッセージが存在したことを記録

            // 並列タスクを作成
            _ = Task.Run(async () =>
            {
                await semaphore.WaitAsync(); // スロットが空くまで待機

                try
                {
                    Console.WriteLine($"Starting processing for Order {msg.Data.Id}: {msg.Data.Description}");

                    // 3秒の遅延を追加（処理開始タイミングをずらす）
                    await Task.Delay(3000);

                    //// 数時間かかる処理をシミュレート（例：2時間）
                    //await Task.Delay(TimeSpan.FromHours(2));

                    Console.WriteLine($"Finished processing Order {msg.Data.Id}");

                    // メッセージをACK
                    await msg.AckAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing Order {msg.Data.Id}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release(); // スロットを解放
                }
            });
        }

        // メッセージがなかった場合、待機を入れる
        if (!hasMessages)
        {
            Console.WriteLine("No messages available. Waiting for new messages...");
            await Task.Delay(1000); // 1秒間待機（待機時間は調整可能）
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during message consumption: {ex.Message}");
        await Task.Delay(1000); // エラーバックオフ（待機時間は調整可能）
    }
}







// モデルクラス
public record Order
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
