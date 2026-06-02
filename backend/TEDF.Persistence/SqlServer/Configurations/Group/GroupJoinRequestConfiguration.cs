using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.GroupAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Group
{
    /// <summary>
    /// EF Core configuration for GroupJoinRequest entity.
    /// </summary>
    public class GroupJoinRequestConfiguration : IEntityTypeConfiguration<GroupJoinRequest>
    {
        public void Configure(EntityTypeBuilder<GroupJoinRequest> builder)
        {
            builder.ToTable("GroupJoinRequests");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            builder.Property(r => r.Status)
                .HasConversion<int>();

            builder.Property(r => r.Message)
                .HasMaxLength(500);

            builder.Property(r => r.ExpiresAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(r => r.GroupId);
            builder.HasIndex(r => r.StudentId);
            builder.HasIndex(r => new { r.Status, r.ExpiresAt });
            builder.HasIndex(r => new { r.GroupId, r.StudentId, r.Status });
            builder.HasIndex(r => new { r.GroupId, r.StudentId })
                .IsUnique()
                .HasFilter("[Status] = 0");

            // Foreign key to User (Student)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore computed properties
            builder.Ignore(r => r.IsPending);
            builder.Ignore(r => r.IsExpired);
        }
    }
}
