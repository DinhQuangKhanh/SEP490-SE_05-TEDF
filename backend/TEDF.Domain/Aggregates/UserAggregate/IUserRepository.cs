namespace TEDF.Domain.Aggregates.UserAggregate
{
    /// <summary>
    /// Repository interface for User aggregate.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by their ID.
        /// </summary>
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Gets a list of users by their IDs.
        /// </summary>
        Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Gets a user by their Firebase UID.
        /// </summary>
        Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);

        /// <summary>
        /// Gets all users with a specific role.
        /// </summary>
        Task<IEnumerable<User>> GetByRoleAsync(string roleName, CancellationToken ct = default);

        /// <summary>
        /// Adds a new user.
        /// </summary>
        Task AddAsync(User user, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        Task UpdateAsync(User user, CancellationToken ct = default);

        /// <summary>
        /// Checks if a user with the given email exists.
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Checks if a user with the given Firebase UID exists.
        /// </summary>
        Task<bool> ExistsByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);

        /// <summary>
        /// Gets a user by their student code.
        /// </summary>
        Task<User?> GetByStudentCodeAsync(string studentCode, CancellationToken ct = default);

        /// <summary>
        /// Gets a user (mentor/staff) by their employee code.
        /// </summary>
        Task<User?> GetByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default);

        /// <summary>
        /// Gets a paginated list of users, optionally filtered by role and search term.
        /// Search matches against FullName, Email, StudentCode, and EmployeeCode.
        /// </summary>
        Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
            string? role, string? search, int page, int pageSize, CancellationToken ct = default);
    }
}
