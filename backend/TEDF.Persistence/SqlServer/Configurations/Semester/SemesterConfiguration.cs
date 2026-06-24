using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Persistence.ValueConverters;

namespace TEDF.Persistence.SqlServer.Configurations.Semester
{
    /// <summary>
    /// EF Core configuration for Semester aggregate root.
    /// </summary>
    public class SemesterConfiguration : IEntityTypeConfiguration<Domain.Aggregates.SemesterAggregate.Semester>
    {
        public void Configure(EntityTypeBuilder<Domain.Aggregates.SemesterAggregate.Semester> builder)
        {
            builder.ToTable("Semesters");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .ValueGeneratedNever(); // ID is assigned, not auto-generated

            builder.Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Value Object conversions
            builder.Property(s => s.Code)
                .HasConversion<SemesterCodeConverter>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(s => s.AcademicYear)
                .HasConversion<AcademicYearConverter>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(500);
            // Relationships
            builder.HasMany(s => s.Phases)
                .WithOne()
                .HasForeignKey(p => p.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.EligibleStudents)
                .WithOne()
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.EligibleMentors)
                .WithOne()
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(s => s.Code).IsUnique();
            builder.HasIndex(s => s.AcademicYear);
            builder.HasIndex(s => s.StartDate);

            // Ignore domain events and computed properties
            builder.Ignore(s => s.DomainEvents);
            builder.Ignore(s => s.CurrentPhase);
            builder.Ignore(s => s.IsActive);
            builder.Ignore(s => s.Status);
            builder.Ignore(s => s.IsRosterPublished);
        }
    }
}
