using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Aggregates.UserAggregate.ValueObjects;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository implementation for User aggregate.
    /// </summary>
    public class UserRepository : BaseRepository<User, Guid>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        /// <inheritdoc/>
        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalizedEmail = email.ToLowerInvariant();
            return await _dbSet
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => EF.Property<string>(u, "Email") == normalizedEmail, ct);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, ct);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetByRoleAsync(string roleName, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.IsActive &&
                    _context.Roles.Any(dbRole => dbRole.Id == r.RoleId && dbRole.Name == roleName)))
                .ToListAsync(ct);
        }

        /// <inheritdoc/>
        public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Roles)
                .Include(u => u.Student).ThenInclude(s => s!.Program)
                .Include(u => u.Student).ThenInclude(s => s!.Combo)
                .Include(u => u.Lecturer)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            var idList = ids.ToList();
            if (!idList.Any()) return Enumerable.Empty<User>();
            return await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Lecturer)
                .Where(u => idList.Contains(u.Id))
                .ToListAsync(ct);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Synchronous EF operation. Returns completed task without async state machine overhead.
        /// </remarks>
        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _dbSet.Update(user);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByStudentCodeAsync(string studentCode, CancellationToken ct = default)
        {
            var userId = await _context.Students.AsNoTracking()
                .Where(s => s.StudentCode == studentCode)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            return userId.HasValue
                ? await _dbSet.FirstOrDefaultAsync(u => u.Id == userId.Value, ct)
                : null;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default)
        {
            var userId = await _context.Lecturers.AsNoTracking()
                .Where(l => l.EmployeeCode == employeeCode)
                .Select(l => (Guid?)l.Id)
                .FirstOrDefaultAsync(ct);

            return userId.HasValue
                ? await _dbSet.FirstOrDefaultAsync(u => u.Id == userId.Value, ct)
                : null;
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalizedEmail = Email.Create(email.Trim().ToLowerInvariant());
            return await _dbSet.AnyAsync(u => u.Email == normalizedEmail, ct);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(u => u.FirebaseUid == firebaseUid, ct);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
            string? role, string? search, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking()
                .Include(u => u.Roles)
                .Include(u => u.Student)
                .Include(u => u.Lecturer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Roles.Any(r => r.IsActive &&
                    _context.Roles.Any(dbRole => dbRole.Id == r.RoleId && dbRole.Name == role)));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(term) ||
                    EF.Property<string>(u, "Email").Contains(term) ||
                    _context.Students.Any(s => s.Id == u.Id && s.StudentCode.Contains(term)) ||
                    _context.Lecturers.Any(l => l.Id == u.Id && l.EmployeeCode.Contains(term)));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }
}
