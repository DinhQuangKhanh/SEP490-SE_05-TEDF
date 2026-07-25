using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    public class MajorProgramConfiguration : IEntityTypeConfiguration<MajorProgram>
    {
        // Seeded program names. Both spellings exist in the source curriculum data and are kept
        // verbatim — changing either value would produce a migration diff on the Programs table.
        private const string SeName = "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)";
        private const string SeNameThe = "The " + SeName;

        private const string ViDesc = "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.";
        private const string EnDesc = "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.";

        public void Configure(EntityTypeBuilder<MajorProgram> builder)
        {
            builder.ToTable("Programs");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(p => p.Name)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasMaxLength(2000);

            builder.Property(p => p.TotalCredit)
                .IsRequired();

            builder.HasIndex(p => p.Code).IsUnique();

            builder.HasData(
                new { Id = 1,  Code = "BIT_SE_K20B",      Name = SeName,    Description = ViDesc, TotalCredit = 145 },
                new { Id = 2,  Code = "BIT_SE_K20C",      Name = SeName,    Description = ViDesc, TotalCredit = 145 },
                new { Id = 3,  Code = "BIT_SE_K20D_K21A", Name = SeName,    Description = ViDesc, TotalCredit = 145 },
                new { Id = 4,  Code = "BIT_SE_K21B",      Name = SeName,    Description = ViDesc, TotalCredit = 145 },
                new { Id = 5,  Code = "BIT_SE_K21C",      Name = SeName,    Description = ViDesc, TotalCredit = 145 },
                new { Id = 6,  Code = "BIT_SE_K19D_K20A", Name = SeNameThe, Description = ViDesc, TotalCredit = 145 },
                new { Id = 7,  Code = "BIT_SE_K19B",      Name = SeNameThe, Description = ViDesc, TotalCredit = 145 },
                new { Id = 8,  Code = "BIT_SE_K19C",      Name = SeNameThe, Description = ViDesc, TotalCredit = 145 },
                new { Id = 9,  Code = "BIT_SE_K18D_19A",  Name = SeNameThe, Description = ViDesc, TotalCredit = 145 },
                new { Id = 10, Code = "BIT_SE_K18C",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 11, Code = "BIT_SE_K18B",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 12, Code = "BIT_SE_K17D_18A",  Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 13, Code = "BIT_SE_K17C",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 14, Code = "BIT_SE_K17B",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 15, Code = "BIT_SE_K16D_K17A", Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 16, Code = "BIT_SE_K16C",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 17, Code = "BIT_SE_K16B",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 18, Code = "BIT_SE_K16D,K17A", Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 19, Code = "BIT_SE_K15C",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 20, Code = "BIT_SE_K15D,K16A", Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 21, Code = "BIT_SE_K17D,K18A", Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 22, Code = "BIT_SE_K15A",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 23, Code = "BIT_SE_K15B",      Name = SeName,    Description = EnDesc, TotalCredit = 145 },
                new { Id = 24, Code = "BIT_SE_tuK16B",    Name = SeName,    Description = EnDesc, TotalCredit = 145 }
            );
        }
    }
}
