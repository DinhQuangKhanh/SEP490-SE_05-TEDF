using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TEDF.Persistence.SqlServer.Configurations.TopicPool;

/// <summary>
/// EF Core configuration for TopicPool entity.
/// Each major has exactly one permanent topic pool.
/// </summary>
public class TopicPoolConfiguration : IEntityTypeConfiguration<Domain.Aggregates.TopicPoolAggregate.TopicPool>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.TopicPoolAggregate.TopicPool> builder)
    {
        builder.ToTable("TopicPools");

        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(tp => tp.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tp => tp.Description)
            .HasMaxLength(1000);

        builder.Property(tp => tp.MajorId)
            .IsRequired();

        builder.Property(tp => tp.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(tp => tp.MaxActiveTopicsPerMentor)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(tp => tp.ExpirationSemesters)
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(tp => tp.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(tp => tp.Code)
            .IsUnique()
            .HasDatabaseName("IX_TopicPools_Code");

        // One pool per major (permanent)
        builder.HasIndex(tp => tp.MajorId)
            .IsUnique()
            .HasDatabaseName("IX_TopicPools_MajorId");

        builder.HasIndex(tp => tp.Status)
            .HasDatabaseName("IX_TopicPools_Status");

        // Ignore domain events
        builder.Ignore(tp => tp.DomainEvents);

        // Relationships
        builder.HasOne<Domain.Entities.Major>()
            .WithOne()
            .HasForeignKey<Domain.Aggregates.TopicPoolAggregate.TopicPool>(tp => tp.MajorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
