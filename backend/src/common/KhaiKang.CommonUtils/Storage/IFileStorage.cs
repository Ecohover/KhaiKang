namespace KhaiKang.CommonUtils.Storage;

public interface IFileStorage
{
    string Provider { get; }

    Task WriteAsync(string storageKey, Stream content, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
