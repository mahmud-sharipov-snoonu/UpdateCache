using MassTransit;
using UpdateCache.Sample.Contracts;
using UpdateCache.Sample.Updates;

namespace UpdateCache.Sample.Consumers;

public class CoinsEarnedConsumer(IUpdateStore store, ILogger<CoinsEarnedConsumer> logger) : IConsumer<CoinsEarned>
{
    public async Task Consume(ConsumeContext<CoinsEarned> context)
    {
        var message = context.Message;

        var update = new UpdateModel(
            UpdateSource.Coins,
            UpdateType.PointEarned,
            $"You earned {message.Amount} coins!",
            message.Reason,
            message.OccurredAtUtc);

        await store.AddAsync(message.UserId, update, context.CancellationToken);

        logger.LogInformation("Cached coins update for user {UserId}", message.UserId);
    }
}
