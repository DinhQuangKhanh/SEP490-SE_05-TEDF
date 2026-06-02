using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.ProjectAggregate.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Project
{
    /// <summary>
    /// EF Core configuration for ProjectMentor entity.
    /// </summary>
    public class ProjectMentorConfiguration : IEntityTypeConfiguration<ProjectMentor>
    {
        public void Configure(EntityTypeBuilder<ProjectMentor> builder)
        {
            builder.ToTable("ProjectMentors");

            builder.HasKey(pm => pm.Id);

            builder.Property(pm => pm.Id)
                .ValueGeneratedOnAdd();

            builder.Property(pm => pm.Status)
                .HasConversion<int>();

            builder.Property(pm => pm.Notes)
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(pm => pm.ProjectId);
            builder.HasIndex(pm => pm.MentorId);
            builder.HasIndex(pm => pm.AssignedBy);
            builder.HasIndex(pm => new { pm.ProjectId, pm.MentorId, pm.Status });

            // Foreign key to User (Mentor)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(pm => pm.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to User (AssignedBy)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(pm => pm.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore computed property
            builder.Ignore(pm => pm.IsActive);
        }
    }
}
