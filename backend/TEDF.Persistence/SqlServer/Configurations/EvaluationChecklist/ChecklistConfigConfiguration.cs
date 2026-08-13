using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.EvaluationChecklist;

/// <summary>EF Core configuration for the <see cref="ChecklistConfig"/> aggregate root.</summary>
public class ChecklistConfigConfiguration : IEntityTypeConfiguration<ChecklistConfig>
{
    public void Configure(EntityTypeBuilder<ChecklistConfig> builder)
    {
        builder.ToTable("ChecklistConfigs");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SemesterId).IsRequired();
        builder.Property(c => c.Version).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Mapped to the existing "PassThreshold" column (same semantics: minimum criteria to pass) so the
        // rename to RequiredPassCount carries no data migration and preserves existing configs' values.
        builder.Property(c => c.RequiredPassCount).HasColumnName("PassThreshold").IsRequired();

        builder.Property(c => c.SourceFileName).HasMaxLength(255);
        builder.Property(c => c.CreatedAt).IsRequired();

        // At most one Active config per semester.
        builder.HasIndex(c => c.SemesterId)
            .HasFilter("[Status] = 'Active'")
            .IsUnique()
            .HasDatabaseName("IX_ChecklistConfigs_Active_Semester");

        builder.HasIndex(c => new { c.SemesterId, c.Version }).IsUnique();

        // Qualified type name (matches EvaluationSubmissionConfiguration): the sibling namespace
        // Configurations.Semester shadows the plain type name, so it cannot be shortened.
        builder.HasOne<Domain.Aggregates.SemesterAggregate.Semester>()
            .WithMany()
            .HasForeignKey(c => c.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Criteria are part of this aggregate — tracked through the backing field.
        builder.Navigation(c => c.Criteria).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(c => c.Criteria)
            .WithOne()
            .HasForeignKey(x => x.ChecklistConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
    }
}

/// <summary>EF Core configuration for the <see cref="ChecklistCriterion"/> entity.</summary>
public class ChecklistCriterionConfiguration : IEntityTypeConfiguration<ChecklistCriterion>
{
    public void Configure(EntityTypeBuilder<ChecklistCriterion> builder)
    {
        builder.ToTable("ChecklistCriteria");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.TitleVi).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TitleEn).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => x.ChecklistConfigId);
    }
}
