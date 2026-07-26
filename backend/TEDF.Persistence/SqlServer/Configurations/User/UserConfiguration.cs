using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.UserAggregate.ValueObjects;

namespace TEDF.Persistence.SqlServer.Configurations.User
{
    public class UserConfiguration : IEntityTypeConfiguration<Domain.Aggregates.UserAggregate.User>
    {
        public void Configure(EntityTypeBuilder<Domain.Aggregates.UserAggregate.User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

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

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(u => u.FirebaseUid)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(u => u.Status)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.FirebaseUid).IsUnique();
            builder.HasIndex(u => u.Status);
            builder.HasIndex(u => u.FullName);
            builder.HasIndex(u => u.DepartmentId);

            builder.HasMany(u => u.Roles)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Domain.Entities.Department>()
                .WithMany()
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Ignore(u => u.DomainEvents);
        }
    }
}
