namespace KhaiKang.CommonUtils.Storage;

public sealed class LocalFileStorage
    : IFileStorage
{
    private readonly string rootPath;
    private readonly string rootPathPrefix;

    public LocalFileStorage(string rootPath)
    {
        this.rootPath = Path.GetFullPath(rootPath);
        rootPathPrefix = this.rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? this.rootPath
            : this.rootPath + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(this.rootPath);
    }

    public string Provider => "local";

    public async Task WriteAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var output = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
        return Task.FromResult(stream);
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
        {
            throw new ArgumentException("A relative storage key is required.", nameof(storageKey));
        }

        var relativePath = storageKey
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        if (!path.StartsWith(rootPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The storage key escapes the configured root.", nameof(storageKey));
        }

        return path;
    }
}
