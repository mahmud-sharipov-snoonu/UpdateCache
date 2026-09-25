namespace UpdateCache.Sample.Contracts;

public record CoinsEarned(Guid UserId, int Amount, string Reason, DateTime OccurredAtUtc);

public record QuestCompleted(Guid UserId, string QuestName, int RewardCoins, DateTime OccurredAtUtc);

public record AchievementUnlocked(Guid UserId, string AchievementName, string BadgeTier, DateTime OccurredAtUtc);
