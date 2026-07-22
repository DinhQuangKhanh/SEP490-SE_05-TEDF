using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Configurations.Common
{
    public class ComboConfiguration : IEntityTypeConfiguration<Combo>
    {
        public void Configure(EntityTypeBuilder<Combo> builder)
        {
            builder.ToTable("Combos");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.Name)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(c => c.Abbr)
                .HasMaxLength(20)
                .IsRequired();

            builder.HasData(
                new { Id = 340,  Name = "SE_COM5.2: Topic on Japanese Bridge Engineer_Chủ đề Kỹ sư cầu nối Nhật Bản (Định hướng Tiếng Nhật nâng cao cho kỹ sư CNTT) BIT_SE_K15A",                                                                                               Abbr = "JBE"   },
                new { Id = 402,  Name = "SE_COM6: Topic on Information Technology - Korean Language_Chủ đề Công nghệ thông tin - tiếng Hàn BIT_SE_K15C",                                                                                                                         Abbr = "KOR"   },
                new { Id = 1469, Name = "SE_COM5.1.1:Topic on Japanese Bridge Engineer_Chủ đề Kỹ sư cầu nối Nhật Bản (Định hướng Tiếng Nhật CNTT: Lựa chọn JFE301 và 1 trong 2 học phần JIS401, JIT401 để triển khai ở kỳ 8) BIT_SE_K15C",                                    Abbr = "JFE"   },
                new { Id = 2497, Name = "SE_COM4.1: Topic on React/NodeJS_Chủ đề React/NodeJS",                                                                                                                                                                                 Abbr = "React" },
                new { Id = 2566, Name = "SE_COM7.1:Topic on AI_Chủ đề AI",                                                                                                                                                                                                      Abbr = "AI"    },
                new { Id = 2605, Name = "SE_COM11: Topic on IC design_Chủ đề Thiết kế vi mạch",                                                                                                                                                                                 Abbr = "IC"    },
                new { Id = 2628, Name = "SE_COM12: Topic on Game Development_Phát triển game",                                                                                                                                                                                   Abbr = "Game"  },
                new { Id = 2640, Name = "SE_COM10.2: Topic on Intensive Java_Chủ đề Java chuyên sâu_K19A",                                                                                                                                                                      Abbr = "Java"  },
                new { Id = 2675, Name = "SE_COM14: Topic on Applied Data Science_Chủ đề Khoa học dữ liệu (KHDL) ứng dụng_K19B",                                                                                                                                                Abbr = "DS"    },
                new { Id = 2686, Name = "SE_COM3.3: Topic on .NET Programming_Chủ đề lập trình .NET BIT_SE_From_K18C",                                                                                                                                                          Abbr = ".NET"  }
            );
        }
    }
}
