using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.Seeds;

/// <summary>
/// Seeds development/testing data into the database.
/// Idempotent: checks for existing data before inserting.
/// </summary>
public static class DevelopmentDataSeeder
{
    // Department
    private const int DeptCNTT = 1; // Khoa Kỹ Thuật Phần Mềm

    // Majors (all belong to DeptCNTT)
    private const int MajorSE = 1; // Kỹ thuật phần mềm

    private static readonly DateTime SeedDate = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────────────────

    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        // Never reset data by default on app startup.
        // Opt-in reset only when explicitly requested via environment variable.
        // Example: set TEDF_RESET_DEVELOPMENT_TEST_ON_STARTUP=true
        var resetOnStartup =
            string.Equals(
                Environment.GetEnvironmentVariable("TEDF_RESET_DEVELOPMENT_TEST_ON_STARTUP"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        if (resetOnStartup)
        {
            logger?.LogWarning("TEDF_RESET_DEVELOPMENT_TEST_ON_STARTUP=true => resetting database before development-test seeding.");
            await ResetDatabaseAsync(context, logger);
        }

        var alreadySeeded = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Major WHERE Id = {0}", MajorSE)
            .SingleOrDefaultAsync();

        if (alreadySeeded > 0)
        {
            logger?.LogInformation("Load-test data already seeded, skipping.");
            return;
        }

        await SeedDepartmentsAsync(context);
        await SeedMajorsAsync(context);
        await SeedProjectArchivesAsync(context);
    }

    // ─── 1. Departments ──────────────────────────────────────────────────────

    private static async Task SeedDepartmentsAsync(AppDbContext context)
    {
        var sql = @"
            SET IDENTITY_INSERT Departments ON;
            INSERT INTO Departments (Id, Name, Code, Description, HeadOfDepartmentId, IsActive, CreatedAt, UpdatedAt)
            VALUES (@p0, N'Kỹ thuật phần mềm', 'SE', N'Bộ môn Kỹ thuật phần mềm', NULL, 1, @p1, NULL);
            SET IDENTITY_INSERT Departments OFF;";

        await context.Database.ExecuteSqlRawAsync(sql, DeptCNTT, SeedDate);
    }

    // ─── 2. Majors ───────────────────────────────────────────────────────────

    private static async Task SeedMajorsAsync(AppDbContext context)
    {
        var sql = @"
            SET IDENTITY_INSERT Majors ON;
            INSERT INTO Majors (Id, DepartmentId, Name, Code, Description, IsActive, CreatedAt, UpdatedAt)
            VALUES
            (@p0, @p8, N'Kỹ thuật phần mềm',              'SE',  N'Chuyên ngành Kỹ thuật phần mềm',              1, @p9, NULL)
            SET IDENTITY_INSERT Majors OFF;";

        await context.Database.ExecuteSqlRawAsync(sql, MajorSE, DeptCNTT, SeedDate);
    }

    // ─── 3. Project Archives (sample, for the admin "Old topic archives" panel) ──

    private static async Task SeedProjectArchivesAsync(AppDbContext context)
    {
        // A few completed projects per past academic year so the storage panel shows real data.
        var sql = @"
            INSERT INTO ProjectArchives (Id, ProjectName, StudentNames, MajorId, AcademicYear, Summary, DocumentUrl, Tags, FileSizeBytes, ViewCount, DownloadCount, CreatedAt)
            VALUES
            (@p0, N'Hệ thống quản lý thư viện thông minh', N'Nguyễn Văn A, Trần Thị B', @pMajor, N'2023-2024', N'Khóa luận tốt nghiệp', NULL, N'library,web', @pSize1, 12, 3, @pDate),
            (@p1, N'Ứng dụng đặt lịch khám bệnh',          N'Lê Văn C, Phạm Thị D',    @pMajor, N'2023-2024', N'Khóa luận tốt nghiệp', NULL, N'health,mobile', @pSize2, 8, 1, @pDate),
            (@p2, N'Nền tảng học trực tuyến',              N'Hoàng Văn E, Vũ Thị F',   @pMajor, N'2022-2023', N'Khóa luận tốt nghiệp', NULL, N'education,web', @pSize3, 20, 7, @pDate);";

        var parameters = new List<object>
        {
            Guid.Parse("D0000000-0000-0000-0000-000000000001"),
            Guid.Parse("D0000000-0000-0000-0000-000000000002"),
            Guid.Parse("D0000000-0000-0000-0000-000000000003"),
        };

        await context.Database.ExecuteSqlRawAsync(
            sql.Replace("@pMajor", "@p3").Replace("@pSize1", "@p4").Replace("@pSize2", "@p5").Replace("@pSize3", "@p6").Replace("@pDate", "@p7"),
            parameters[0], parameters[1], parameters[2],
            MajorSE,
            644245094L,   // ~0.6 GB
            322122547L,   // ~0.3 GB
            1288490189L,  // ~1.2 GB
            SeedDate);
    }

    // ════════════════════════════════════════════════
    //  RESET DATABASE (uncomment call in SeedAsync to use)
    //  Deletes ALL data in FK-safe order, then resets
    //  identity seeds so the next seeding starts fresh.
    // ════════════════════════════════════════════════
    // ReSharper disable once UnusedMember.Local
    private static async Task ResetDatabaseAsync(AppDbContext context, ILogger? logger)
    {
        logger?.LogWarning("Resetting database — deleting ALL data...");

        var originalTimeout = context.Database.GetCommandTimeout();
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));

        // Order matters: delete children before parents to respect FK constraints.
        var tables = new[]
        {
            "ProjectArchives",
            "Majors",
            "Departments"
        };

        try
        {
            foreach (var entry in tables)
            {
                await context.Database.ExecuteSqlRawAsync($"DELETE FROM [{entry}];");
            }

            // Only RESEED tables that actually use identity columns (int Id, auto-increment).
            // Tables with Guid PKs or ValueGeneratedNever do NOT have identity columns.
            var identityTables = new[]
            {
                "Departments", "Majors"
            };

            foreach (var table in identityTables)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync($"DBCC CHECKIDENT ('[{table}]', RESEED, 0);");
                }
                catch
                {
                    // Table might not exist or might be empty — ignore.
                }
            }

            logger?.LogWarning("Database reset complete. All data deleted.");
        }
        finally
        {
            context.Database.SetCommandTimeout(originalTimeout);
        }
    }

}
