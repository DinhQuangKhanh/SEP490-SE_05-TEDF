using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Chương trình đào tạo (curriculum program) — e.g. BIT_SE_K18C.
    /// Maps to the "Programs" table.
    /// </summary>
    public class MajorProgram : Entity<int>
    {
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public int TotalCredit { get; private set; }

        private MajorProgram() { }

        public static MajorProgram Create(string code, string name, int totalCredit, string? description = null)
        {
            return new MajorProgram
            {
                Code = code,
                Name = name,
                Description = description,
                TotalCredit = totalCredit
            };
        }
    }
}
