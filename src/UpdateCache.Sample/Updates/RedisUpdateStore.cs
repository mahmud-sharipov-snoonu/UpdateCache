using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace UpdateCache.Sample.Updates;

public sealed class RedisUpdateStore(IConnectionMultiplexer redis) : IUpdateStore
{
    // LRANGE + DEL must not be two round trips: a consumer's RPUSH could land between them
    // and be deleted without ever being returned. Redis runs a Lua script as one atomic
    // unit, so a concurrent RPUSH is either fully applied before the script or queued until
    // after the DEL.
    private const string DrainScript =
        """
        local items = redis.call('LRANGE', KEYS[1], 0, -1)
        redis.call('DEL', KEYS[1])
        return items
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task AddAsync(Guid userId, UpdateModel update, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(update, JsonOptions);
        await redis.GetDatabase().ListRightPushAsync(KeyFor(userId), payload);
    }

    public async Task<IReadOnlyList<UpdateModel>> DrainAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await redis.GetDatabase()
            .ScriptEvaluateAsync(DrainScript, [KeyFor(userId)]);

        if (result.IsNull)
            return [];

        return ((RedisValue[])result!)
            .Select(value => JsonSerializer.Deserialize<UpdateModel>((string)value!, JsonOptions)!)
            .ToList();
    }

    private static RedisKey KeyFor(Guid userId) => $"updates:{userId}";
}
