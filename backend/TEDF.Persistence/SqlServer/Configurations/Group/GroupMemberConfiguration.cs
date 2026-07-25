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

            // A student can be an ACTIVE member of a given group at most once.
            // Filtered so historical "Left" rows (Status = 1) don't block re-joining
            // (members leave via soft state, and re-joining inserts a new row).
            builder.HasIndex(m => new { m.GroupId, m.StudentId })
                .IsUnique()
                .HasFilter("[Status] = 0")
                .HasDatabaseName("UX_GroupMembers_GroupId_StudentId_Active");

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
