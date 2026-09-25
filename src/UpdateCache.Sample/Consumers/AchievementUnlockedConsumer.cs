using MassTransit;
using UpdateCache.Sample.Contracts;
using UpdateCache.Sample.Updates;

namespace UpdateCache.Sample.Consumers;

public class AchievementUnlockedConsumer(IUpdateStore store, ILogger<AchievementUnlockedConsumer> logger)
    : IConsumer<AchievementUnlocked>
{
    public async Task Consume(ConsumeContext<AchievementUnlocked> context)
    {
        var message = context.Message;

        var update = new UpdateModel(
            UpdateSource.Badge,
            UpdateType.BadgeCompleted,
            $"Badge unlocked: {message.AchievementName}",
            $"{message.BadgeTier} tier",
            message.OccurredAtUtc);

        await store.AddAsync(message.UserId, update, context.CancellationToken);

        logger.LogInformation("Cached badge update for user {UserId}", message.UserId);
    }
}
