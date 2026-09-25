using System.Text.Json.Serialization;
using MassTransit;
using StackExchange.Redis;
using UpdateCache.Sample.Consumers;
using UpdateCache.Sample.Contracts;
using UpdateCache.Sample.Updates;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddSingleton<IUpdateStore, RedisUpdateStore>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CoinsEarnedConsumer>();
    x.AddConsumer<QuestCompletedConsumer>();
    x.AddConsumer<AchievementUnlockedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")!);
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "UpdateCache Sample"));

app.MapGet("/users/{userId:guid}/updates", async (Guid userId, IUpdateStore store, CancellationToken ct) =>
        TypedResults.Ok(await store.DrainAsync(userId, ct)))
    .WithSummary("Drains all pending updates for a user, emptying the cache atomically.");

app.MapPost("/demo/publish/{eventType}", async (
        string eventType,
        PublishRequest request,
        IPublishEndpoint publishEndpoint,
        CancellationToken ct) =>
    {
        var now = DateTime.UtcNow;

        switch (eventType.ToLowerInvariant())
        {
            case "coins":
                await publishEndpoint.Publish(new CoinsEarned(request.UserId, 50, "Daily login bonus", now), ct);
                break;
            case "quest":
                await publishEndpoint.Publish(new QuestCompleted(request.UserId, "Order 3 times this week", 200, now),
                    ct);
                break;
            case "achievement":
                await publishEndpoint.Publish(new AchievementUnlocked(request.UserId, "Early Bird", "Gold", now), ct);
                break;
            default:
                return Results.BadRequest($"Unknown event type '{eventType}'. Use coins, quest or achievement.");
        }

        return Results.Accepted();
    })
    .WithSummary("Publishes a sample event (coins | quest | achievement) to RabbitMQ.");

app.Run();

record PublishRequest(Guid UserId);