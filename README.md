# NATS Client with JetStream in C#

This repository demonstrates how to use NATS with JetStream in C# for reliable message streaming and processing. The example showcases creating a stream, publishing messages, processing them using consumers, and managing streams efficiently.

## Requirements

- .NET 6.0 or later
- [NATS.Net NuGet package](https://www.nuget.org/packages/NATS.Net)

## Installation

Before running the example, install the required NuGet package:

```bash
dotnet add package NATS.Net
```

Ensure that you have a NATS server running locally or accessible via a URL. By default, this example connects to `nats://127.0.0.1:4222`.

## Running the Example

1. Clone this repository and navigate to the project directory.
2. Build and run the project:

```bash
dotnet run
```

The script performs the following steps:

### 1. Connect to the NATS Server

The example connects to the NATS server using a URL from the `NATS_URL` environment variable. If not set, it defaults to `nats://127.0.0.1:4222`:

```csharp
var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
await using var nc = new NatsClient(url);
await nc.ConnectAsync();
```

### 2. Create a JetStream Context

A JetStream context is created for managing streams and consumers:

```csharp
var js = nc.CreateJetStreamContext();
```

### 3. Define and Create a Stream

A stream named `ORDERS` is created, with the subject pattern `orders.>`:

```csharp
var streamConfig = new StreamConfig(name: "ORDERS", subjects: new[] { "orders.>" });
await js.CreateStreamAsync(streamConfig);
```

### 4. Create a Durable Consumer

A durable consumer named `durable_processor` is created to process messages:

```csharp
var durableConfig = new ConsumerConfig
{
    Name = "durable_processor",
    DurableName = "durable_processor",
};
var consumer = await js.CreateOrUpdateConsumerAsync(stream: "ORDERS", durableConfig);
```

### 5. Publish Messages

Messages are published to the stream under the subject `orders.new`:

```csharp
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 1, Description = "Order 1" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 2, Description = "Order 2" });
await js.PublishAsync(subject: "orders.new", data: new Order { Id = 3, Description = "Order 3" });
```

### 6. Consume Messages

Messages are consumed using the `ConsumeAsync` method. The process runs for a maximum of 10 seconds before cancellation:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var msg in consumer.ConsumeAsync<Order>().WithCancellation(cts.Token))
{
    Console.WriteLine($"Processing {msg.Subject}: {msg.Data.Id} - {msg.Data.Description}");
    await msg.AckAsync();
}
```

### 7. Delete the Stream (Optional)

For cleanup, the stream is deleted. This step is optional and primarily for testing purposes:

```csharp
await js.DeleteStreamAsync("ORDERS");
```

### 8. Define the Data Model

A simple data model is used for the orders:

```csharp
public record Order
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
```

## Notes

- This example demonstrates durable consumers for reliable message processing. Messages are acknowledged (`AckAsync`) to indicate successful processing.
- Alternative methods such as `NextAsync` (fetch a single message) and `FetchAsync` (batch processing) are commented out in the code for reference.
- Deleting the stream is included for cleanup purposes during development but is not typically used in production environments.

## Troubleshooting

- Ensure the NATS server is running and accessible.
- Verify that the `NATS_URL` environment variable is set correctly, or use the default `nats://127.0.0.1:4222`.
- Check NuGet package installation with `dotnet list package`.

## References

- [NATS Documentation](https://docs.nats.io/)
- [NATS.Net GitHub Repository](https://github.com/nats-io/nats.net)

