using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveProjectWithInitialAuditMetadata()
    {
        var actorId = Guid.NewGuid();

        var project = new Project(
            Guid.NewGuid(), "APP", "Application", "Description", actorId, CreatedAt);

        Assert.Equal("APP", project.Code);
        Assert.Equal("Application", project.Name);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Equal(actorId, project.CreatedByAccountId);
        Assert.Equal(actorId, project.UpdatedByAccountId);
        Assert.Equal(1, project.Version);
    }

    [Fact]
    public void Update_ChangesEditableFieldsAndAuditMetadata()
    {
        var project = CreateProject();
        var actorId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddHours(1);

        project.Update(
            "Renamed application",
            "Updated description",
            ProjectStatus.Inactive,
            actorId,
            updatedAt);

        Assert.Equal("Renamed application", project.Name);
        Assert.Equal("Updated description", project.Description);
        Assert.Equal(ProjectStatus.Inactive, project.Status);
        Assert.Equal(actorId, project.UpdatedByAccountId);
        Assert.Equal(updatedAt, project.UpdatedAt);
        Assert.Equal(2, project.Version);
    }

    private static Project CreateProject()
    {
        return new Project(
            Guid.NewGuid(), "APP", "Application", null, Guid.NewGuid(), CreatedAt);
    }
}
