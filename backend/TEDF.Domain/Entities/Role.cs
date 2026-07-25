using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    public class Role : Entity<int>
    {
        public string Name { get; private set; } = string.Empty;

        private Role() { }

        public static Role Create(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty.", nameof(name));

            return new Role { Id = id, Name = name };
        }
    }
}
