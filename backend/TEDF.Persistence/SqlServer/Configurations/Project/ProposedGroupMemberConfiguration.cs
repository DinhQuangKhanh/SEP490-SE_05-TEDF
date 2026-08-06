using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.ProjectAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Project
{
    /// <summary>
    /// EF Core configuration for ProposedGroupMember — the roster parsed from the register form
    /// the mentor attached when proposing a topic.
    /// </summary>
    public class ProposedGroupMemberConfiguration : IEntityTypeConfiguration<ProposedGroupMember>
    {
        public void Configure(EntityTypeBuilder<ProposedGroupMember> builder)
        {
            builder.ToTable("ProjectProposedMembers");

            builder.HasKey(pm => pm.Id);

            builder.Property(pm => pm.Id)
                .ValueGeneratedOnAdd();

            builder.Property(pm => pm.IsLeader)
                .IsRequired();

            // A student may appear only once per topic roster.
            builder.HasIndex(pm => new { pm.ProjectId, pm.StudentId }).IsUnique();

            // Foreign key to User (Student)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(pm => pm.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
