using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.GroupAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Group
{
    /// <summary>
    /// EF Core configuration for GroupMember entity.
    /// </summary>
    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("GroupMembers");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .ValueGeneratedOnAdd();

            builder.Property(m => m.Role)
                .HasConversion<int>();

            builder.Property(m => m.Status)
                .HasConversion<int>();

            // Indexes
            builder.HasIndex(m => m.GroupId);
            builder.HasIndex(m => m.StudentId);
            builder.HasIndex(m => m.Status);
            builder.HasIndex(m => new { m.GroupId, m.StudentId, m.Status });

            // Foreign key to User (Student)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore computed properties
            builder.Ignore(m => m.IsActive);
            builder.Ignore(m => m.IsLeader);
        }
    }
}
