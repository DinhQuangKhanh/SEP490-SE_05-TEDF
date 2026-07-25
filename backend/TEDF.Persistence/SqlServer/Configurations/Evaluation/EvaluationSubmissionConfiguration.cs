using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Persistence.ValueConverters;

namespace TEDF.Persistence.SqlServer.Configurations.Evaluation
{
    /// <summary>
    /// EF Core configuration for EvaluationSubmission aggregate root.
    /// </summary>
    public class EvaluationSubmissionConfiguration : IEntityTypeConfiguration<EvaluationSubmission>
    {
        public void Configure(EntityTypeBuilder<EvaluationSubmission> builder)
        {
            builder.ToTable("EvaluationSubmissions");

            builder.HasKey(e => e.Id);
            
            builder.Property(e => e.PhaseId).IsRequired();

            // Value Object conversions
            builder.Property(e => e.SubmissionNumber)
                .HasConversion<SubmissionNumberConverter>()
                .IsRequired();

            builder.Property(e => e.Status)
                .HasConversion<int>();

            builder.Property(e => e.Result)
                .HasConversion<int>();

            builder.Property(e => e.Notes)
                .HasMaxLength(2000);

            // ProjectSnapshot stored as JSON
            builder.Property(e => e.Snapshot)
                .HasConversion<ProjectSnapshotConverter>()
                .HasColumnType("nvarchar(max)");

            // Indexes
            builder.HasIndex(e => e.ProjectId);
            builder.HasIndex(e => e.AssignedEvaluatorId);
            builder.HasIndex(e => e.SubmittedBy);
            builder.HasIndex(e => e.AssignedBy);
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.Result);
            builder.HasIndex(e => e.SubmittedAt);
            builder.HasIndex(e => new { e.ProjectId, e.SubmissionNumber }).IsUnique();

            // Foreign key to Project
            builder.HasOne<Domain.Aggregates.ProjectAggregate.Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // Foreign key to User (AssignedEvaluator)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(e => e.AssignedEvaluatorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Foreign key to User (SubmittedBy)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(e => e.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to User (AssignedBy)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(e => e.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore domain events
            builder.Ignore(e => e.DomainEvents);
        }
    }
}
