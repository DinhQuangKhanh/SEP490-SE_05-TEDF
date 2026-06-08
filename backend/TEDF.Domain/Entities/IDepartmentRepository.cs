namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Repository interface for Department entity.
    /// </summary>
    public interface IDepartmentRepository
    {
        /// <summary>
        /// Gets a department by its ID.
        /// </summary>
        Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a department by its code.
        /// </summary>
        Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all departments.
        /// </summary>
        Task<IEnumerable<Department>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active departments.
        /// </summary>
        Task<IEnumerable<Department>> GetActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new department.
        /// </summary>
        Task AddAsync(Department department, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing department.
        /// </summary>
        void Update(Department department);

        /// <summary>
        /// Removes a department.
        /// </summary>
        void Remove(Department department);

        /// <summary>
        /// Checks if a department with the given ID exists.
        /// </summary>
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a department with the given code exists.
        /// </summary>
        Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a major belongs to a department.
        /// Used for validating department scope (e.g., CNBM can only manage projects in their department).
        /// </summary>
        Task<bool> IsMajorInDepartmentAsync(int majorId, int departmentId, CancellationToken cancellationToken = default);
    }
}
