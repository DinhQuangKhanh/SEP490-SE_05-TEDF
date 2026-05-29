using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.ProjectAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Project
{
    /// <summary>
    /// EF Core configuration for Document entity.
    /// </summary>
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(d => d.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(d => d.FileType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(d => d.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(d => d.DocumentType)
                .HasConversion<int>();

            builder.Property(d => d.Version)
                .HasMaxLength(20);

            builder.Property(d => d.Description)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(d => d.ProjectId);
            builder.HasIndex(d => d.DocumentType);
            builder.HasIndex(d => d.UploadedAt);
            builder.HasIndex(d => d.IsDeleted);

            // Foreign key to User (UploadedBy)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Global query filter for soft delete
            builder.HasQueryFilter(d => !d.IsDeleted);
        }
    }
}
