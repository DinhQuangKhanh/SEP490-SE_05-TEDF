using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Lecturer-specific profile data. Id is both PK and FK to Users.Id.
    /// </summary>
    public class Lecturer : Entity<Guid>
    {
        public string EmployeeCode { get; private set; } = string.Empty;
        public string? AcademicTitle { get; private set; }

        private Lecturer() { }

        public static Lecturer Create(Guid userId, string employeeCode, string? academicTitle = null)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                throw new ArgumentException("Employee code cannot be empty.", nameof(employeeCode));

            return new Lecturer
            {
                Id = userId,
                EmployeeCode = employeeCode,
                AcademicTitle = academicTitle
            };
        }

        public void UpdateAcademicTitle(string? academicTitle)
        {
            AcademicTitle = academicTitle;
        }
    }
}
