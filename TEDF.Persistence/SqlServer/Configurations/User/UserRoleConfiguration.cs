using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.UserAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.User
{
    /// <summary>
    /// EF Core configuration for UserRole entity.
    /// </summary>
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.RoleName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(r => r.AssignedAt)
                .IsRequired();

            builder.Property(r => r.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(r => r.UserId);

            builder.HasIndex(r => new { r.UserId, r.RoleName })
                .IsUnique();

            builder.HasIndex(r => r.RoleName);

            builder.HasIndex(r => r.IsActive);
        }
    }
}
