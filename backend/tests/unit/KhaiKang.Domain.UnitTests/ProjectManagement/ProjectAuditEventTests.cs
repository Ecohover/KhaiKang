using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectAuditEventTests
{
    [Fact]
    public void NamedFactories_CreateStableSuccessfulHumanEvents()
    {
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero);
        var context = new ChangeContext(actorId, occurredAt);
        (string EventType, ProjectAuditEvent AuditEvent)[] events =
        [
            ("project_created", ProjectAuditEvent.ProjectCreated(targetId, context)),
            ("project_updated", ProjectAuditEvent.ProjectUpdated(targetId, context)),
            ("project_member_added", ProjectAuditEvent.ProjectMemberAdded(targetId, context)),
            ("project_member_roles_changed", ProjectAuditEvent.ProjectMemberRolesChanged(targetId, context)),
            ("project_member_removed", ProjectAuditEvent.ProjectMemberRemoved(targetId, context)),
            ("issue_created", ProjectAuditEvent.IssueCreated(targetId, context)),
            ("issue_status_changed", ProjectAuditEvent.IssueStatusChanged(targetId, context)),
            ("issue_updated", ProjectAuditEvent.IssueUpdated(targetId, context)),
            ("issue_assignee_changed", ProjectAuditEvent.IssueAssigneeChanged(targetId, context)),
            ("issue_relation_created", ProjectAuditEvent.IssueRelationCreated(targetId, context)),
            ("issue_relation_deleted", ProjectAuditEvent.IssueRelationDeleted(targetId, context)),
        ];

        Assert.All(events, item =>
        {
            Assert.NotEqual(Guid.Empty, item.AuditEvent.Id);
            Assert.Equal(actorId, item.AuditEvent.ActorId);
            Assert.Equal("human", item.AuditEvent.ActorType);
            Assert.Equal(item.EventType, item.AuditEvent.EventType);
            Assert.Equal(occurredAt, item.AuditEvent.OccurredAt);
            Assert.Equal(targetId, item.AuditEvent.TargetId);
            Assert.Equal("succeeded", item.AuditEvent.Outcome);
        });
    }
}
