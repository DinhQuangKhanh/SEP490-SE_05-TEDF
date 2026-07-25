using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.User
{
    public class LecturerConfiguration : IEntityTypeConfiguration<Lecturer>
    {
        public void Configure(EntityTypeBuilder<Lecturer> builder)
        {
            builder.ToTable("Lecturers");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                .ValueGeneratedNever();

            builder.Property(l => l.EmployeeCode)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(l => l.AcademicTitle)
                .HasMaxLength(50);

            builder.HasIndex(l => l.EmployeeCode).IsUnique();

            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithOne(u => u.Lecturer)
                .HasForeignKey<Lecturer>(l => l.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
