using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.EvaluationChecklist;

/// <summary>EF Core configuration for the <see cref="ProjectEvaluationChecklist"/> aggregate root.</summary>
public class ProjectEvaluationChecklistConfiguration : IEntityTypeConfiguration<ProjectEvaluationChecklist>
{
    public void Configure(EntityTypeBuilder<ProjectEvaluationChecklist> builder)
    {
        builder.ToTable("ProjectEvaluationChecklists");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProjectId).IsRequired();
        builder.Property(c => c.EvaluatorId).IsRequired();
        builder.Property(c => c.SemesterId).IsRequired();
        builder.Property(c => c.ChecklistConfigId).IsRequired();
        builder.Property(c => c.SubmissionNumber).IsRequired();
        builder.Property(c => c.RequiredPassCount).IsRequired();
        builder.Property(c => c.PassedCount).IsRequired();
        builder.Property(c => c.EvaluatorNote).HasMaxLength(2000);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // One result per evaluator per project per evaluation round.
        builder.HasIndex(c => new { c.ProjectId, c.EvaluatorId, c.SubmissionNumber }).IsUnique();
        builder.HasIndex(c => c.ChecklistConfigId);

        // Qualified type names (matches EvaluationSubmissionConfiguration): the sibling namespaces
        // Configurations.Project / Configurations.User shadow the plain type names.
        builder.HasOne<Domain.Aggregates.ProjectAggregate.Project>()
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Aggregates.UserAggregate.User>()
            .WithMany()
            .HasForeignKey(c => c.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ChecklistConfig>()
            .WithMany()
            .HasForeignKey(c => c.ChecklistConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(x => x.ProjectEvaluationChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
    }
}

/// <summary>EF Core configuration for the <see cref="ChecklistResultItem"/> entity.</summary>
public class ChecklistResultItemConfiguration : IEntityTypeConfiguration<ChecklistResultItem>
{
    public void Configure(EntityTypeBuilder<ChecklistResultItem> builder)
    {
        builder.ToTable("ChecklistResultItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CriterionId).IsRequired();
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.TitleVi).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.IsPassed).IsRequired();

        builder.HasIndex(x => x.ProjectEvaluationChecklistId);
    }
}
