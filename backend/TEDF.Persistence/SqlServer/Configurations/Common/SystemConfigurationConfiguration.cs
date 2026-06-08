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
                new { Id = 5, Key = "ModificationDeadlineDays", Value = "14", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Days to modify after feedback", Category = "Evaluation", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                // ── Admin Settings page (Registration / Appearance / System / Notifications) ──
                new { Id = 6, Key = "MaxTopicsPerMentor", Value = "5", DataType = Domain.Enums.System.ConfigDataType.Int, Description = "Maximum active topics a mentor may propose per pool", Category = "Registration", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 7, Key = "AllowDirectRegistration", Value = "true", DataType = Domain.Enums.System.ConfigDataType.Bool, Description = "Allow students to propose their own topics (direct registration)", Category = "Registration", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 8, Key = "RequireOutlineApproval", Value = "true", DataType = Domain.Enums.System.ConfigDataType.Bool, Description = "Require mentor approval of the outline before implementation", Category = "Registration", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 9, Key = "PrimaryColor", Value = "#2c6090", DataType = Domain.Enums.System.ConfigDataType.String, Description = "System primary theme color (hex)", Category = "Appearance", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 10, Key = "HeaderName", Value = "TEDF", DataType = Domain.Enums.System.ConfigDataType.String, Description = "Header / brand display name", Category = "Appearance", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 11, Key = "LogoUrl", Value = "", DataType = Domain.Enums.System.ConfigDataType.String, Description = "System logo URL", Category = "Appearance", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 12, Key = "MaintenanceMode", Value = "false", DataType = Domain.Enums.System.ConfigDataType.Bool, Description = "When enabled, only Admins can access the system", Category = "System", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 13, Key = "EmailOnEvaluationResult", Value = "true", DataType = Domain.Enums.System.ConfigDataType.Bool, Description = "Email students when an evaluation result is finalized", Category = "Notifications", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new { Id = 14, Key = "NotifyMentorOnRegistration", Value = "true", DataType = Domain.Enums.System.ConfigDataType.Bool, Description = "Notify a mentor when a group registers for their topic", Category = "Notifications", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
