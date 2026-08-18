using KhaiKang.CommonUtils.Storage;

namespace KhaiKang.Api.IntegrationTests;

internal sealed class ControllableFileStorage : IFileStorage
{
    private readonly Dictionary<string, byte[]> _files = [];

    public string Provider => "test-memory";

    public bool ReadsAreAvailable { get; set; } = true;

    public async Task WriteAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _files[storageKey] = buffer.ToArray();
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        if (!ReadsAreAvailable || !_files.TryGetValue(storageKey, out var content))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(content, writable: false));
    }
}
