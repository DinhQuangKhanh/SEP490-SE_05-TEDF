using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Student-specific profile data. Id is both PK and FK to Users.Id.
    /// </summary>
    public class Student : Entity<Guid>
    {
        public string StudentCode { get; private set; } = string.Empty;

        public int? ProgramId { get; private set; }

        /// <summary>Navigation property — populated by EF Core through the backing field.</summary>
        public MajorProgram? Program { get; }

        public int? ComboId { get; private set; }

        /// <summary>Navigation property — populated by EF Core through the backing field.</summary>
        public Combo? Combo { get; }

        private Student() { }

        public static Student Create(Guid userId, string studentCode, int? programId = null, int? comboId = null)
        {
            if (string.IsNullOrWhiteSpace(studentCode))
                throw new ArgumentException("Student code cannot be empty.", nameof(studentCode));

            return new Student
            {
                Id = userId,
                StudentCode = studentCode,
                ProgramId = programId,
                ComboId = comboId
            };
        }

        public void UpdateProgram(int? programId, int? comboId)
        {
            ProgramId = programId;
            ComboId = comboId;
        }
    }
}
