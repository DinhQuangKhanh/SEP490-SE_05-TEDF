using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    /// <summary>
    /// EF Core configuration for Major entity.
    /// </summary>
    public class MajorConfiguration : IEntityTypeConfiguration<Major>
    {
        public void Configure(EntityTypeBuilder<Major> builder)
        {
            builder.ToTable("Majors");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .ValueGeneratedOnAdd();

            builder.Property(m => m.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(m => m.Code)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(m => m.Description)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(m => m.Code).IsUnique();
            builder.HasIndex(m => m.DepartmentId);
            builder.HasIndex(m => m.IsActive);

            // Foreign key to Department
            builder.HasOne<Department>()
                .WithMany()
                .HasForeignKey(m => m.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
