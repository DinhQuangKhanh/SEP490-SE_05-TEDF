using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.User
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Students");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.StudentCode)
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(s => s.StudentCode).IsUnique();

            builder.HasOne<Domain.Aggregates.UserAggregate.User>()
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Program)
                .WithMany()
                .HasForeignKey(s => s.ProgramId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(s => s.Combo)
                .WithMany()
                .HasForeignKey(s => s.ComboId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
