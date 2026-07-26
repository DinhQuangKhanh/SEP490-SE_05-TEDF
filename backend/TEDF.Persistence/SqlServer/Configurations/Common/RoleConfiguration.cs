using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedNever();

            builder.Property(r => r.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(r => r.Name).IsUnique();

            builder.HasData(
                new { Id = 1, Name = "Admin" },
                new { Id = 2, Name = "Mentor" },
                new { Id = 3, Name = "Student" },
                new { Id = 4, Name = "Evaluator" },
                new { Id = 5, Name = "DepartmentHead" }
            );
        }
    }
}
