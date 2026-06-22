using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Semester
{
    public class EligibleStudentConfiguration : IEntityTypeConfiguration<EligibleStudent>
    {
        public void Configure(EntityTypeBuilder<EligibleStudent> builder)
        {
            builder.ToTable("EligibleStudents");

            builder.HasKey(e => e.Id);

            // Auto increment ID
            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Email).HasMaxLength(256);
            builder.Property(e => e.PhoneNumber).HasMaxLength(30);

            builder.HasIndex(e => new { e.SemesterId, e.StudentId }).IsUnique();
        }
    }
}
