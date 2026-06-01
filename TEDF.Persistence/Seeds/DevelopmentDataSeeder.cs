using Microsoft.EntityFrameworkCore;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.Seeds;

/// <summary>
/// Seeds development/testing data into the database.
/// Idempotent: checks for existing data before inserting.
/// </summary>
public static class DevelopmentDataSeeder
{
    // Department
    private const int DeptCNTT = 1; // Khoa Công nghệ thông tin

    // Majors (all belong to DeptCNTT)
    private const int MajorSE = 1; // Kỹ thuật phần mềm
    private const int MajorAI = 2; // Trí tuệ nhân tạo
    private const int MajorDS = 3; // Khoa học dữ liệu ứng dụng
    private const int MajorIA = 4; // An toàn thông tin
    private const int MajorIC = 5; // Vi mạch bán dẫn
    private const int MajorAS = 6; // Công nghệ ô tô số
    private const int MajorIS = 7; // Hệ thống thông tin
    private const int MajorGD = 8; // Thiết kế đồ hoạ và mỹ thuật số

    private static readonly DateTime SeedDate = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────────────────

    public static async Task SeedAsync(AppDbContext context)
    {
        // Idempotent: skip entirely if the department row already exists
        var alreadySeeded = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Departments WHERE Id = {0}", DeptCNTT)
            .SingleOrDefaultAsync();

        if (alreadySeeded > 0)
            return;

        await SeedDepartmentsAsync(context);
        await SeedMajorsAsync(context);
    }

    // ─── 1. Departments ──────────────────────────────────────────────────────

    private static async Task SeedDepartmentsAsync(AppDbContext context)
    {
        var sql = @"
            SET IDENTITY_INSERT Departments ON;
            INSERT INTO Departments (Id, Name, Code, Description, HeadOfDepartmentId, IsActive, CreatedAt, UpdatedAt)
            VALUES (@p0, N'Công nghệ thông tin', 'CNTT', N'Khoa Công nghệ thông tin', NULL, 1, @p1, NULL);
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
            (@p0, @p8, N'Kỹ thuật phần mềm',              'SE',  N'Chuyên ngành Kỹ thuật phần mềm',              1, @p9, NULL),
            (@p1, @p8, N'Trí tuệ nhân tạo',               'AI',  N'Chuyên ngành Trí tuệ nhân tạo',               1, @p9, NULL),
            (@p2, @p8, N'Khoa học dữ liệu ứng dụng',      'DS',  N'Chuyên ngành Khoa học dữ liệu ứng dụng',      1, @p9, NULL),
            (@p3, @p8, N'An toàn thông tin',               'IA',  N'Chuyên ngành An toàn thông tin',              1, @p9, NULL),
            (@p4, @p8, N'Vi mạch bán dẫn',                'IC',  N'Chuyên ngành Vi mạch bán dẫn',                1, @p9, NULL),
            (@p5, @p8, N'Công nghệ ô tô số',              'AS', N'Chuyên ngành Công nghệ ô tô số',              1, @p9, NULL),
            (@p6, @p8, N'Hệ thống thông tin',             'IS',  N'Chuyên ngành Hệ thống thông tin',             1, @p9, NULL),
            (@p7, @p8, N'Thiết kế đồ hoạ và mỹ thuật số','GD',  N'Chuyên ngành Thiết kế đồ hoạ và mỹ thuật số', 1, @p9, NULL);
            SET IDENTITY_INSERT Majors OFF;";

        await context.Database.ExecuteSqlRawAsync(sql,
            MajorSE, MajorAI, MajorDS, MajorIA, MajorIC, MajorAS, MajorIS, MajorGD,
            DeptCNTT, SeedDate);
    }

}
