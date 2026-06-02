using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    public class Department : Entity<int>
    {
        public string Name { get; private set; } = string.Empty;
        public string Code { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public Guid? HeadOfDepartmentId { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Department() { }

        public static Department Create(int id, string name, string code, string? description = null)
        {
            return new Department
            {
                Id = id,
                Name = name,
                Code = code.ToUpperInvariant(),
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Update(string name, string code, string? description = null)
        {
            Name = name;
            Code = code.ToUpperInvariant();
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetHeadOfDepartment(Guid? userId)
        {
            HeadOfDepartmentId = userId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
        public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    }
}
