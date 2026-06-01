using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Persistence.ValueConverters;

namespace TEDF.Persistence.SqlServer.Configurations.Support
{
    /// <summary>
    /// EF Core configuration for SupportTicket aggregate root.
    /// </summary>
    public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.ToTable("SupportTickets");

            builder.HasKey(t => t.Id);

            // Value Object conversions
            builder.Property(t => t.Code)
                .HasConversion<TicketCodeConverter>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(t => t.Category)
                .HasConversion<int>();

            builder.Property(t => t.Priority)
                .HasConversion<int>();

            builder.Property(t => t.Status)
                .HasConversion<int>();

            // Indexes
            builder.HasIndex(t => t.Code).IsUnique();
            builder.HasIndex(t => t.ReporterId);
            builder.HasIndex(t => t.AssigneeId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.Category);
            builder.HasIndex(t => t.Priority);
            builder.HasIndex(t => t.CreatedAt);

            // Foreign key to User (Reporter)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(t => t.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to User (Assignee)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Attachments Relationship - using Owned Types
            builder.OwnsMany(t => t.Attachments, a =>
            {
                a.ToTable("SupportTicketAttachments");
                a.WithOwner().HasForeignKey("SupportTicketId");
                a.Property<int>("Id").ValueGeneratedOnAdd();
                a.HasKey("Id");

                a.Property(att => att.FileName).HasMaxLength(255).IsRequired();
                a.Property(att => att.FilePath).HasMaxLength(1000).IsRequired();
                a.Property(att => att.ContentType).HasMaxLength(100).IsRequired();
                a.Property(att => att.FileSize).IsRequired();
            });

            // Messages Relationship – use backing field so EF can track adds via _messages
            builder.Navigation(t => t.Messages)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(t => t.Messages)
                .WithOne()
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(t => t.DomainEvents);
        }
    }

    /// <summary>
    /// EF Core configuration for TicketMessage entity.
    /// </summary>
    public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
    {
        public void Configure(EntityTypeBuilder<TicketMessage> builder)
        {
            builder.ToTable("TicketMessages");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Content)
                .HasMaxLength(4000)
                .IsRequired();

            builder.HasIndex(m => m.TicketId);
            builder.HasIndex(m => m.SenderId);
            builder.HasIndex(m => m.CreatedAt);

            // Foreign key to User (Sender)
            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Attachments Relationship - using Owned Types
            builder.OwnsMany(m => m.Attachments, a =>
            {
                a.ToTable("TicketMessageAttachments");
                a.WithOwner().HasForeignKey("TicketMessageId");
                a.Property<int>("Id").ValueGeneratedOnAdd();
                a.HasKey("Id");

                a.Property(att => att.FileName).HasMaxLength(255).IsRequired();
                a.Property(att => att.FilePath).HasMaxLength(1000).IsRequired();
                a.Property(att => att.ContentType).HasMaxLength(100).IsRequired();
                a.Property(att => att.FileSize).IsRequired();
            });
        }
    }
}
