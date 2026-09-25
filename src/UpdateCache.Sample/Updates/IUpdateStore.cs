namespace UpdateCache.Sample.Updates;

public interface IUpdateStore
{
    Task AddAsync(Guid userId, UpdateModel update, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpdateModel>> DrainAsync(Guid userId, CancellationToken cancellationToken = default);
}
