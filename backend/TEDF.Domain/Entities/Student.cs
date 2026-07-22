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
        public MajorProgram? Program { get; private set; }

        public int? ComboId { get; private set; }
        public Combo? Combo { get; private set; }

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
