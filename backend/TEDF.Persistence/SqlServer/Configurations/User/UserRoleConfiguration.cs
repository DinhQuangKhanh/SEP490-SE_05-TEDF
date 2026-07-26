using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.UserAggregate.Entities;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.User
{
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

            builder.Property(r => r.RoleId)
                .IsRequired();

            builder.Property(r => r.AssignedAt)
                .IsRequired();

            builder.Property(r => r.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // RoleName is a computed property, not stored in DB
            builder.Ignore(r => r.RoleName);

            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => new { r.UserId, r.RoleId }).IsUnique();
            builder.HasIndex(r => r.RoleId);
            builder.HasIndex(r => r.IsActive);

            builder.HasOne(r => r.Role)
                .WithMany()
                .HasForeignKey(r => r.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
