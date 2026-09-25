using MassTransit;
using UpdateCache.Sample.Contracts;
using UpdateCache.Sample.Updates;

namespace UpdateCache.Sample.Consumers;

public class QuestCompletedConsumer(IUpdateStore store, ILogger<QuestCompletedConsumer> logger)
    : IConsumer<QuestCompleted>
{
    public async Task Consume(ConsumeContext<QuestCompleted> context)
    {
        var message = context.Message;

        var update = new UpdateModel(
            UpdateSource.Quest,
            UpdateType.QuestCompleted,
            $"Quest complete: {message.QuestName}",
            $"Reward: {message.RewardCoins} coins",
            message.OccurredAtUtc);

        await store.AddAsync(message.UserId, update, context.CancellationToken);

        logger.LogInformation("Cached quest update for user {UserId}", message.UserId);
    }
}
