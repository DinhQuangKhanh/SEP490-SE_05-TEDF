using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Semester
{
    /// <summary>
    /// EF Core configuration for SemesterPhase entity.
    /// </summary>
    public class SemesterPhaseConfiguration : IEntityTypeConfiguration<SemesterPhase>
    {
        public void Configure(EntityTypeBuilder<SemesterPhase> builder)
        {
            builder.ToTable("SemesterPhases");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.Type)
                .HasConversion<int>();

            builder.Property(p => p.Status)
                .HasConversion<int>();

            // Indexes
            builder.HasIndex(p => p.SemesterId);
            builder.HasIndex(p => p.Type);
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.Order);
            builder.HasIndex(p => new { p.SemesterId, p.Order });

            // Ignore computed properties
            builder.Ignore(p => p.IsCurrent);
            builder.Ignore(p => p.DurationDays);
        }
    }
}
