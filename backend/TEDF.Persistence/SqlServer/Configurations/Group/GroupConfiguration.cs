using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Persistence.ValueConverters;

namespace TEDF.Persistence.SqlServer.Configurations.Group
{
    /// <summary>
    /// EF Core configuration for Group aggregate root.
    /// </summary>
    public class GroupConfiguration : IEntityTypeConfiguration<Domain.Aggregates.GroupAggregate.Group>
    {
        public void Configure(EntityTypeBuilder<Domain.Aggregates.GroupAggregate.Group> builder)
        {
            builder.ToTable("Groups");

            builder.HasKey(g => g.Id);

            // Value Object conversions
            builder.Property(g => g.Code)
                .HasConversion<GroupCodeConverter>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(g => g.Name)
                .HasMaxLength(200);

            builder.Property(g => g.Status)
                .HasConversion<int>();

            // Relationships
            builder.HasMany(g => g.Members)
                .WithOne()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(g => g.Invitations)
                .WithOne()
                .HasForeignKey(i => i.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(g => g.JoinRequests)
                .WithOne()
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(g => g.Code).IsUnique();
            builder.HasIndex(g => g.Status);
            builder.HasIndex(g => g.SemesterId);
            builder.HasIndex(g => g.LeaderId);
            builder.HasIndex(g => g.ProjectId);

            // Foreign key to Semester
            builder.HasOne<Domain.Aggregates.SemesterAggregate.Semester>()
                .WithMany()
                .HasForeignKey(g => g.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to User (Leader)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(g => g.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Project
            builder.HasOne<Domain.Aggregates.ProjectAggregate.Project>()
                .WithMany()
                .HasForeignKey(g => g.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore domain events and computed properties
            builder.Ignore(g => g.DomainEvents);
            builder.Ignore(g => g.ActiveMemberCount);
            builder.Ignore(g => g.Leader);
        }
    }
}
