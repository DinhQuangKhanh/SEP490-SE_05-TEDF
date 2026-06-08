using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    public class Major : Entity<int>
    {
        public int DepartmentId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Code { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Major() { }

        public static Major Create(int id, int departmentId, string name, string code, string? description = null)
        {
            return new Major
            {
                Id = id,
                DepartmentId = departmentId,
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

        public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
        public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    }
}
