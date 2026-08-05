using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Enums.Project;
using TEDF.Persistence.ValueConverters;

namespace TEDF.Persistence.SqlServer.Configurations.Project
{
    /// <summary>
    /// EF Core configuration for Project aggregate root.
    /// </summary>
    public class ProjectConfiguration : IEntityTypeConfiguration<Domain.Aggregates.ProjectAggregate.Project>
    {
        public void Configure(EntityTypeBuilder<Domain.Aggregates.ProjectAggregate.Project> builder)
        {
            builder.ToTable("Projects");

            builder.HasKey(p => p.Id);

            // Value Object conversions
            builder.Property(p => p.Code)
                .HasConversion<ProjectCodeConverter>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(p => p.NameVi)
                .HasConversion<ProjectNameConverter>()
                .HasMaxLength(350)
                .IsRequired();

            builder.Property(p => p.NameEn)
                .HasConversion<ProjectNameConverter>()
                .HasMaxLength(350);

            builder.Property(p => p.NameAbbr)
                .HasMaxLength(50);

            builder.Property(p => p.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(p => p.Objectives)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(p => p.Scope)
                .HasMaxLength(2000);

            builder.Property(p => p.Technologies)
                .HasConversion<TechnologyStackConverter>()
                .HasMaxLength(500);

            builder.Property(p => p.ExpectedResults)
                .HasMaxLength(2000);

            // Enums stored as integers
            builder.Property(p => p.Status)
                .HasConversion<int>();

            builder.Property(p => p.Priority)
                .HasConversion<int>();

            builder.Property(p => p.SourceType)
                .HasConversion<int>();

            builder.Property(p => p.RegistrationType)
                .HasConversion<int>();

            builder.Property(p => p.LastEvaluationResult)
                .HasConversion<int?>();
            builder.Property(p => p.PoolStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(p => p.CreatedInSemesterId);

            builder.Property(p => p.ExpirationSemesterId);
            // Relationships
            builder.HasMany(p => p.Mentors)
                .WithOne()
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Documents)
                .WithOne()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.ProposedMembers)
                .WithOne()
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(p => p.Code).IsUnique();
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.SemesterId);
            builder.HasIndex(p => p.MajorId);
            builder.HasIndex(p => p.GroupId);
            builder.HasIndex(p => p.TopicPoolId);
            builder.HasIndex(p => p.CreatedAt);
            builder.HasIndex(p => p.SubmittedAt);
            builder.HasIndex(p => p.PoolStatus)
                .HasFilter($"[SourceType] = {(int)ProjectSourceType.FromPool}")
                .HasDatabaseName("IX_Projects_PoolStatus");

            builder.HasIndex(p => p.ExpirationSemesterId)
                .HasFilter($"[SourceType] = {(int)ProjectSourceType.FromPool}")
                .HasDatabaseName("IX_Projects_ExpirationSemesterId");


            // Foreign key to Semester
            builder.HasOne<Domain.Aggregates.SemesterAggregate.Semester>()
                .WithMany()
                .HasForeignKey(p => p.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to Major
            builder.HasOne<Domain.Entities.Major>()
                .WithMany()
                .HasForeignKey(p => p.MajorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to TopicPool (nullable)
            builder.HasOne<Domain.Aggregates.TopicPoolAggregate.TopicPool>()
                .WithMany()
                .HasForeignKey(p => p.TopicPoolId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to User (SubmittedBy) - nullable
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(p => p.SubmittedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Semester (CreatedInSemesterId) - nullable
            builder.HasOne<Domain.Aggregates.SemesterAggregate.Semester>()
                .WithMany()
                .HasForeignKey(p => p.CreatedInSemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to Semester (ExpirationSemesterId) - nullable
            builder.HasOne<Domain.Aggregates.SemesterAggregate.Semester>()
                .WithMany()
                .HasForeignKey(p => p.ExpirationSemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events collection
            builder.Ignore(p => p.DomainEvents);
            builder.Ignore(p => p.ActiveMentorCount);
        }
    }
}
