using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Repositories
{
    public class ProjectArchiveRepository : IProjectArchiveRepository
    {
        private readonly AppDbContext _context;

        public ProjectArchiveRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProjectArchive>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ProjectArchives
                .AsNoTracking()
                .OrderByDescending(a => a.AcademicYear)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProjectArchive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ProjectArchives.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public void Update(ProjectArchive archive) => _context.ProjectArchives.Update(archive);
    }
}
