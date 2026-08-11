using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_CreatesActiveProjectWithInitialAuditMetadata()
    {
        var actorId = Guid.NewGuid();

        var project = Project.Create(
            new ProjectCreation
            {
                Id = Guid.NewGuid(),
                Code = "APP",
                Name = "Application",
                Description = "Description",
            },
            new ChangeContext(actorId, CreatedAt));

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
            new ProjectDetailsChange
            {
                Name = "Renamed application",
                Description = "Updated description",
                Status = ProjectStatus.Inactive,
            },
            new ChangeContext(actorId, updatedAt));

        Assert.Equal("Renamed application", project.Name);
        Assert.Equal("Updated description", project.Description);
        Assert.Equal(ProjectStatus.Inactive, project.Status);
        Assert.Equal(actorId, project.UpdatedByAccountId);
        Assert.Equal(updatedAt, project.UpdatedAt);
        Assert.Equal(2, project.Version);
    }

    private static Project CreateProject()
    {
        return Project.Create(
            new ProjectCreation
            {
                Id = Guid.NewGuid(),
                Code = "APP",
                Name = "Application",
            },
            new ChangeContext(Guid.NewGuid(), CreatedAt));
    }
}
