using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    /// <summary>
    /// EF Core configuration for MajorProgram (chuyên ngành hẹp) entity.
    /// </summary>
    public class MajorProgramConfiguration : IEntityTypeConfiguration<MajorProgram>
    {
        public void Configure(EntityTypeBuilder<MajorProgram> builder)
        {
            builder.ToTable("MajorPrograms");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.ProgramCode)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.ProgramDescription)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(p => p.ProgramCode).IsUnique();
            builder.HasIndex(p => p.MajorId);
            builder.HasIndex(p => p.IsActive);

            // Foreign key to Major (chuyên ngành)
            builder.HasOne<Major>()
                .WithMany()
                .HasForeignKey(p => p.MajorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
