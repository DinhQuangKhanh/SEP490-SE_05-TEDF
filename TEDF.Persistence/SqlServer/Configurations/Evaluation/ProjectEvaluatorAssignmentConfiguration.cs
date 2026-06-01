using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.EvaluationAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Evaluation
{
    /// <summary>
    /// EF Core configuration for ProjectEvaluatorAssignment entity.
    /// </summary>
    public class ProjectEvaluatorAssignmentConfiguration : IEntityTypeConfiguration<ProjectEvaluatorAssignment>
    {
        public void Configure(EntityTypeBuilder<ProjectEvaluatorAssignment> builder)
        {
            builder.ToTable("ProjectEvaluatorAssignments");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.ProjectId)
                .IsRequired();

            builder.Property(e => e.EvaluatorId)
                .IsRequired();

            builder.Property(e => e.EvaluatorOrder)
                .IsRequired();

            builder.Property(e => e.AssignedAt)
                .IsRequired();

            builder.Property(e => e.AssignedBy)
                .IsRequired();

            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.Feedback)
                .HasMaxLength(2000);

            // Indexes
            builder.HasIndex(e => e.ProjectId);

            builder.HasIndex(e => e.EvaluatorId);

            builder.HasIndex(e => e.AssignedBy);

            builder.HasIndex(e => new { e.ProjectId, e.EvaluatorId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            builder.HasIndex(e => new { e.ProjectId, e.EvaluatorOrder })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            builder.HasIndex(e => e.IsActive);

            builder.HasIndex(e => e.IndividualResult);

            // Foreign key to Project
            builder.HasOne<Domain.Aggregates.ProjectAggregate.Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to User (Evaluator)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(e => e.EvaluatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to User (AssignedBy - Admin)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(e => e.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
