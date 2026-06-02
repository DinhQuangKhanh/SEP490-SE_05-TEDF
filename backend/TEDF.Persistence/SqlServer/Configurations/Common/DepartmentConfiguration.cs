using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    /// <summary>
    /// EF Core configuration for Department entity.
    /// </summary>
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                .ValueGeneratedOnAdd();

            builder.Property(d => d.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(d => d.Code)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(d => d.Description)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(d => d.Code).IsUnique();
            builder.HasIndex(d => d.IsActive);

            builder.HasIndex(d => d.HeadOfDepartmentId);

            // Foreign key to User (HeadOfDepartment) - nullable
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(d => d.HeadOfDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
