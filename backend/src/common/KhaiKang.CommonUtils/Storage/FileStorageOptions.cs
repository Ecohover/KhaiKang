namespace KhaiKang.CommonUtils.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "Attachments";
    public string Provider { get; init; } = "local";
    public string LocalRoot { get; init; } = "data/attachments";
    public long MaxFileSizeBytes { get; init; } = 20 * 1024 * 1024;
}
