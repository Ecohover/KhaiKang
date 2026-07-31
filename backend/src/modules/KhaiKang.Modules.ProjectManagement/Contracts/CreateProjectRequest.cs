namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record CreateProjectRequest(string Code, string Name, string? Description);
