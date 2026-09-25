namespace UpdateCache.Sample.Updates;

public enum UpdateSource
{
    Coins,
    Quest,
    Badge,
    Order
}

public enum UpdateType
{
    PointEarned,
    QuestProgressUpdated,
    QuestCompleted,
    BadgeCompleted
}

public record UpdateModel(
    UpdateSource Source,
    UpdateType Type,
    string Title,
    string Subtitle,
    DateTime OccurredAtUtc);
