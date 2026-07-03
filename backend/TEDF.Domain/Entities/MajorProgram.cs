using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Chuyên ngành hẹp — a narrow specialization belonging to a <see cref="Major"/> (chuyên ngành).
    /// </summary>
    public class MajorProgram : Entity<int>
    {
        /// <summary>FK to the owning Major (chuyên ngành).</summary>
        public int MajorId { get; private set; }

        /// <summary>CurriculumCode + "_" + ComboCode, e.g. "BIT_SE_18C_NodeJS" or "BIT_SE_18B_COM3.2".</summary>
        public string ProgramCode { get; private set; } = string.Empty;

        /// <summary>Combo name (ComboName) — the descriptive name of the program/combo.</summary>
        public string? ProgramDescription { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private MajorProgram() { }

        public static MajorProgram Create(int id, int majorId, string programCode, string? programDescription = null)
        {
            return new MajorProgram
            {
                Id = id,
                MajorId = majorId,
                ProgramCode = programCode,
                ProgramDescription = programDescription,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Update(string programCode, string? programDescription = null)
        {
            ProgramCode = programCode;
            ProgramDescription = programDescription;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
        public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    }
}
