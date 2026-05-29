using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    /// <summary>
    /// EF Core configuration for SystemConfiguration entity.
    /// </summary>
    public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
    {
        public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
        {
            builder.ToTable("SystemConfigurations");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.Key)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Value)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(c => c.DataType)
                .HasConversion<int>();

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.Property(c => c.Category)
                .HasMaxLength(100);

            // Indexes
            builder.HasIndex(c => c.Key).IsUnique();
            builder.HasIndex(c => c.Category);

            // Seed data
            builder.HasData(
                new { Id = 1, Key = "MaxProjectMentors", Value = "2", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Maximum mentors per project", Category = "Project", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 2, Key = "MaxGroupMembers", Value = "5", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Maximum members per group", Category = "Group", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 3, Key = "TopicExpirationSemesters", Value = "2", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Semesters until topic expires", Category = "TopicPool", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 4, Key = "MaxResubmissions", Value = "3", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Maximum resubmissions allowed", Category = "Evaluation", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 5, Key = "ModificationDeadlineDays", Value = "14", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Days to modify after feedback", Category = "Evaluation", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
