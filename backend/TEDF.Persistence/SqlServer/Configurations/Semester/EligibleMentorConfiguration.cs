using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Semester
{
    public class EligibleMentorConfiguration : IEntityTypeConfiguration<EligibleMentor>
    {
        public void Configure(EntityTypeBuilder<EligibleMentor> builder)
        {
            builder.ToTable("EligibleMentors");

            builder.HasKey(e => e.Id);

            // Auto increment ID
            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.EmployeeCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Email).HasMaxLength(256);
            builder.Property(e => e.PhoneNumber).HasMaxLength(30);
            builder.Property(e => e.Division).HasMaxLength(50);

            builder.HasIndex(e => new { e.SemesterId, e.MentorId }).IsUnique();
        }
    }
}
