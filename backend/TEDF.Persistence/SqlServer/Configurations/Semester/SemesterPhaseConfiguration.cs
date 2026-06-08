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

            // Status is a database COMPUTED column derived from StartDate/EndDate and the
            // current UTC time. SYSUTCDATETIME() matches the UTC semantics used in the domain
            // (DateTime.UtcNow). The expression returns the integer values of SemesterPhaseStatus:
            //   0 = NotStarted (now < StartDate), 1 = InProgress (StartDate <= now <= EndDate),
            //   2 = Completed (now > EndDate).
            // The column is store-generated, so EF never writes it (read-only on the entity).
            builder.Property(p => p.Status)
                .HasColumnType("int")
                .HasComputedColumnSql(
                    "CASE " +
                    "WHEN SYSUTCDATETIME() < [StartDate] THEN 0 " +
                    "WHEN SYSUTCDATETIME() > [EndDate] THEN 2 " +
                    "ELSE 1 END")
                .ValueGeneratedOnAddOrUpdate();

            // Indexes
            builder.HasIndex(p => p.SemesterId);
            builder.HasIndex(p => p.Type);
            // NOTE: No index on Status — it is a non-deterministic computed column
            // (depends on SYSUTCDATETIME()), so SQL Server cannot persist or index it.
            builder.HasIndex(p => p.Order);
            builder.HasIndex(p => new { p.SemesterId, p.Order });

            // Ignore computed properties
            builder.Ignore(p => p.IsCurrent);
            builder.Ignore(p => p.DurationDays);
        }
    }
}
