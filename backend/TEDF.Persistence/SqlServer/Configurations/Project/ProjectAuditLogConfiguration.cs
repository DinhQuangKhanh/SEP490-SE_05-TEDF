using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Project
{
    /// <summary>
    /// EF Core configuration for the project approval audit trail.
    /// </summary>
    public class ProjectAuditLogConfiguration : IEntityTypeConfiguration<ProjectAuditLog>
    {
        public void Configure(EntityTypeBuilder<ProjectAuditLog> builder)
        {
            builder.ToTable("ProjectAuditLogs");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Action)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(l => l.PerformedByName)
                .HasMaxLength(200);

            builder.Property(l => l.OldStatus)
                .HasConversion<int?>();

            builder.Property(l => l.NewStatus)
                .HasConversion<int?>();

            builder.Property(l => l.Timestamp)
                .IsRequired();

            // Relationships are declared without navigations: the audit trail is read on its own
            // and must not widen the Project/User aggregates.
            builder.HasOne<Domain.Aggregates.ProjectAggregate.Project>()
                .WithMany()
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(l => l.PerformedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Primary access path: full trail of one project, newest first.
            builder.HasIndex(l => new { l.ProjectId, l.Timestamp })
                .HasDatabaseName("IX_ProjectAuditLogs_ProjectId_Timestamp");

            builder.HasIndex(l => l.PerformedBy)
                .HasDatabaseName("IX_ProjectAuditLogs_PerformedBy");
        }
    }
}
