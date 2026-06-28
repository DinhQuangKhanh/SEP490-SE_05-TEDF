using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.UserAggregate.ValueObjects;

namespace TEDF.Persistence.SqlServer.Configurations.User
{
    /// <summary>
    /// EF Core configuration for User aggregate.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<Domain.Aggregates.UserAggregate.User>
    {
        public void Configure(EntityTypeBuilder<Domain.Aggregates.UserAggregate.User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            // Email as value object
            builder.Property(u => u.Email)
                .HasConversion(
                    v => v.Value,
                    v => Email.Create(v))
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(u => u.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(u => u.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(u => u.StudentCode)
                .HasMaxLength(20);

            builder.Property(u => u.EmployeeCode)
                .HasMaxLength(20);

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(u => u.AcademicTitle)
                .HasMaxLength(50);

            builder.Property(u => u.Division)
                .HasMaxLength(50);

            builder.Property(u => u.FirebaseUid)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(u => u.Status)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.HasIndex(u => u.FirebaseUid)
                .IsUnique();

            builder.HasIndex(u => u.StudentCode)
                .IsUnique()
                .HasFilter("[StudentCode] IS NOT NULL");

            builder.HasIndex(u => u.EmployeeCode)
                .IsUnique()
                .HasFilter("[EmployeeCode] IS NOT NULL");

            builder.HasIndex(u => u.Status);

            builder.HasIndex(u => u.FullName);

            builder.HasIndex(u => u.DepartmentId);

            builder.HasIndex(u => u.MajorId);

            // Relationships
            builder.HasMany(u => u.Roles)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Department
            builder.HasOne<Domain.Entities.Department>()
                .WithMany()
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Major (chuyên ngành) - nullable, set for students
            builder.HasOne<Domain.Entities.Major>()
                .WithMany()
                .HasForeignKey(u => u.MajorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore domain events (handled separately)
            builder.Ignore(u => u.DomainEvents);
        }
    }
}
