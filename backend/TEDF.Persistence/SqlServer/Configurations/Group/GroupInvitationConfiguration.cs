using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.GroupAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Group
{
    /// <summary>
    /// EF Core configuration for GroupInvitation entity.
    /// </summary>
    public class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
    {
        public void Configure(EntityTypeBuilder<GroupInvitation> builder)
        {
            builder.ToTable("GroupInvitations");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                .ValueGeneratedOnAdd();

            builder.Property(i => i.Status)
                .HasConversion<int>();

            builder.Property(i => i.Message)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(i => i.GroupId);
            builder.HasIndex(i => i.InviteeId);
            builder.HasIndex(i => new { i.GroupId, i.InviteeId, i.Status });
            builder.HasIndex(i => new { i.GroupId, i.InviteeId })
                .IsUnique()
                .HasFilter("[Status] = 0");

            // Foreign key to User (Inviter)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(i => i.InviterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to User (Invitee)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(i => i.InviteeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore computed properties
            builder.Ignore(i => i.IsExpired);
            builder.Ignore(i => i.IsPending);
        }
    }
}
