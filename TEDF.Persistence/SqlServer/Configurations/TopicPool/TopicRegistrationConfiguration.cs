using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.TopicPool;

/// <summary>
/// EF Core configuration for TopicRegistration entity.
/// </summary>
public class TopicRegistrationConfiguration : IEntityTypeConfiguration<TopicRegistration>
{
    public void Configure(EntityTypeBuilder<TopicRegistration> builder)
    {
        builder.ToTable("TopicRegistrations");

        builder.HasKey(tr => tr.Id);

        builder.Property(tr => tr.ProjectId)
            .IsRequired();

        builder.Property(tr => tr.GroupId)
            .IsRequired();

        builder.Property(tr => tr.RegisteredBy)
            .IsRequired();

        builder.Property(tr => tr.RegisteredAt)
            .IsRequired();

        builder.Property(tr => tr.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(tr => tr.Priority)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(tr => tr.Note)
            .HasMaxLength(500);

        builder.Property(tr => tr.RejectReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(tr => tr.ProjectId)
            .HasDatabaseName("IX_TopicRegistrations_ProjectId");

        builder.HasIndex(tr => tr.GroupId)
            .HasDatabaseName("IX_TopicRegistrations_GroupId");

        builder.HasIndex(tr => tr.Status)
            .HasDatabaseName("IX_TopicRegistrations_Status");

        builder.HasIndex(tr => new { tr.ProjectId, tr.GroupId })
            .HasDatabaseName("IX_TopicRegistrations_ProjectId_GroupId");

        // Ensure only one confirmed registration per project
        builder.HasIndex(tr => tr.ProjectId)
            .HasFilter("[Status] = 'Confirmed'")
            .IsUnique()
            .HasDatabaseName("IX_TopicRegistrations_ProjectId_Confirmed_Unique");

        // Relationships
        builder.HasOne<Domain.Aggregates.ProjectAggregate.Project>()
            .WithMany()
            .HasForeignKey(tr => tr.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Aggregates.GroupAggregate.Group>()
            .WithMany()
            .HasForeignKey(tr => tr.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to User (RegisteredBy)
        builder.HasOne<Domain.Aggregates.UserAggregate.User>()
            .WithMany()
            .HasForeignKey(tr => tr.RegisteredBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to User (ProcessedBy) - nullable
        builder.HasOne<Domain.Aggregates.UserAggregate.User>()
            .WithMany()
            .HasForeignKey(tr => tr.ProcessedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
