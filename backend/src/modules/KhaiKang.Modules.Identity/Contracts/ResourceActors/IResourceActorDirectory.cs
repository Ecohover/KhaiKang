namespace KhaiKang.Modules.Identity.Contracts;

public interface IResourceActorDirectory
{
    Task<ResourceActorSnapshot?> FindActiveAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}
