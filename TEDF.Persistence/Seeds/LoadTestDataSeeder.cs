using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.Seeds;

/// <summary>
/// Seeds realistic load-test data with three real semesters (Fall 2025 / Spring 2026 / Summer 2026),
/// real project topics from FPT University, topic pools for every major, and
/// proper FK relationships throughout.
/// <para>Distribution:</para>
/// <list type="bullet">
///   <item>250 Admins (role: Admin)</item>
///   <item>250 Lecturers with dual roles (roles: Mentor + Evaluator)</item>
///   <item>420 Students (role: Student)</item>
/// </list>
/// Uses raw SQL to bypass domain validation. Idempotent.
/// </summary>
public static class LoadTestDataSeeder
{
    // ────────────────── Distribution ──────────────────
    public const int SeededAdminCount = 250;
    public const int SeededDualRoleCount = 250;
    // 50*5 (Fall) + 40*5 (Spring) + 15*4 (Summer) = 510
    public const int SeededStudentCount = 510;

    private const int AdminCount = SeededAdminCount;
    private const int DualRoleCount = SeededDualRoleCount;
    private const int StudentCount = SeededStudentCount;

    /// <summary>Number of members per group: 5 for Fall/Spring (full), 4 for Summer (open for requests).</summary>
    private static int MembersForGroup(int groupIndex) =>
        groupIndex <= Fall25GroupCount + Spring26GroupCount ? 5 : 4;

    /// <summary>1-based student index of the first member (leader) in the given group.</summary>
    private static int StudentStartIndex(int groupIndex)
    {
        if (groupIndex <= Fall25GroupCount)
            return (groupIndex - 1) * 5 + 1;
        if (groupIndex <= Fall25GroupCount + Spring26GroupCount)
            return Fall25GroupCount * 5 + (groupIndex - Fall25GroupCount - 1) * 5 + 1;
        return Fall25GroupCount * 5 + Spring26GroupCount * 5
             + (groupIndex - Fall25GroupCount - Spring26GroupCount - 1) * 4 + 1;
    }

    // Semester IDs (assigned, not auto-generated)
    private const int Fall2025Id = 100;
    private const int Spring2026Id = 101;
    private const int Summer2026Id = 102;

    private const int Fall25GroupCount = 50;
    private const int Spring26GroupCount = 40;
    private const int Summer26GroupCount = 15;

    private static readonly DateTime SeedDate = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    private const int BatchSize = 50;

    // Major IDs from DevelopmentDataSeeder
    private const int MajorSE = 1;
    private const int MajorAI = 2;
    private const int MajorDS = 3;
    private const int MajorIA = 4;
    private const int MajorIC = 5;
    private const int MajorAS = 6;
    private const int MajorIS = 7;
    private const int MajorGD = 8;

    private static readonly int[] AllMajorIds = [MajorSE, MajorAI, MajorDS, MajorIA, MajorIC, MajorAS, MajorIS, MajorGD];
    private static readonly string[] MajorCodes = ["SE", "AI", "DS", "IA", "IC", "AS", "IS", "GD"];
    private static readonly string[] MajorNames =
    [
        "Kỹ thuật phần mềm", "Trí tuệ nhân tạo", "Khoa học dữ liệu ứng dụng",
        "An toàn thông tin", "Vi mạch bán dẫn", "Công nghệ ô tô số",
        "Hệ thống thông tin", "Thiết kế đồ hoạ và mỹ thuật số"
    ];

    // ────────────────── ID helpers ──────────────────
    public static Guid AdminId(int i) => Guid.Parse($"10000000-0000-0000-0000-{i:D12}");
    public static Guid DualRoleId(int i) => Guid.Parse($"20000000-0000-0000-0000-{i:D12}");
    public static Guid StudentId(int i) => Guid.Parse($"40000000-0000-0000-0000-{i:D12}");
    private static Guid GroupId(int i) => Guid.Parse($"50000000-0000-0000-0000-{i:D12}");
    private static Guid ProjectId(int i) => Guid.Parse($"60000000-0000-0000-0000-{i:D12}");
    private static Guid AssignmentId(int i) => Guid.Parse($"70000000-0000-0000-0000-{i:D12}");
    private static Guid TopicPoolId(int majorIndex) => Guid.Parse($"80000000-0000-0000-0000-{majorIndex:D12}");
    private static Guid PoolProjectId(int majorIndex, int topicIndex) => Guid.Parse($"90{majorIndex:D2}0000-0000-0000-0000-{topicIndex:D12}");
    private static Guid RegistrationId(int i) => Guid.Parse($"A0000000-0000-0000-0000-{i:D12}");
    private static Guid SupportTicketId(int i) => Guid.Parse($"B0000000-0000-0000-0000-{i:D12}");
    private static Guid RejectedProjectId(int i) => Guid.Parse($"C0000000-0000-0000-0000-{i:D12}");

    // Rejected project count for Spring 2026
    private const int RejectedProjectCount = 10;
    private const int SupportTicketCount = 12;
    private const int TopicPoolProjectsPerMajor = 25;

    public static string AdminFirebaseUid(int i) => $"test-admin-{i:D4}";
    public static string DualRoleFirebaseUid(int i) => $"test-lecturer-{i:D4}";
    public static string StudentFirebaseUid(int i) => $"test-student-{i:D4}";

    public static string AdminEmail(int i) => $"admin{i}@fpt.edu.vn";
    public static string DualRoleEmail(int i) => $"lecturer{i}@fpt.edu.vn";
    public static string StudentEmail(int i) => $"student{i}@fpt.edu.vn";

    public const string DefaultPassword = "Test@123456";

    // ────────────────── Entry point ──────────────────
    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        // Never reset data by default on app startup.
        // Opt-in reset only when explicitly requested via environment variable.
        // Example: set TEDF_RESET_LOADTEST_ON_STARTUP=true
        var resetOnStartup =
            string.Equals(
                Environment.GetEnvironmentVariable("TEDF_RESET_LOADTEST_ON_STARTUP"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        if (resetOnStartup)
        {
            logger?.LogWarning("TEDF_RESET_LOADTEST_ON_STARTUP=true => resetting database before load-test seeding.");
            await ResetDatabaseAsync(context, logger);
        }

        var alreadySeeded = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Users WHERE Id = {0}", AdminId(1))
            .SingleOrDefaultAsync();

        if (alreadySeeded > 0)
        {
            logger?.LogInformation("Load-test data already seeded, skipping.");
            return;
        }

        logger?.LogInformation("Seeding load-test data (Fall 2025 + Spring 2026 + Summer 2026)...");

        await SeedSemestersAsync(context, logger);
        await SeedUsersAsync(context, logger);
        await SeedUserRolesAsync(context, logger);
        await SeedTopicPoolsAsync(context, logger);
        await SeedTopicPoolProjectsAsync(context, logger);
        await SeedTopicPoolProjectMentorsAsync(context, logger);
        await SeedGroupsAsync(context, logger);
        await SeedGroupMembersAsync(context, logger);
        await SeedFall25ProjectsAsync(context, logger);
        await SeedSpring26ProjectsAsync(context, logger);
        await SeedProjectMentorsAsync(context, logger);
        await SeedProjectEvaluatorAssignmentsAsync(context, logger);
        await SeedTopicRegistrationsAsync(context, logger);
        await SeedSpring26RejectedProjectsAsync(context, logger);
        await SeedSpring26TopicRegistrationsAsync(context, logger);
        await SeedSupportTicketsAsync(context, logger);
        await SetDepartmentHeadAsync(context, logger);

        logger?.LogInformation("Load-test data seeding complete.");
    }

    // ════════════════════════════════════════════════
    //  SEMESTERS + PHASES
    // ════════════════════════════════════════════════
    private static async Task SeedSemestersAsync(AppDbContext context, ILogger? logger)
    {
        // Semesters.Id uses ValueGeneratedNever (no identity column)
        // Status column was removed – it is now a computed property based on StartDate/EndDate
        var sql = @"
            INSERT INTO Semesters (Id, Name, Code, AcademicYear, StartDate, EndDate, Description, CreatedAt, UpdatedAt)
            VALUES
            (@p0, N'Học kỳ Fall 2025', 'FALL2025', '2025-2026', @p2, @p3, N'Học kỳ đồ án tốt nghiệp Fall 2025 - Đã kết thúc', @p9, NULL),
            (@p1, N'Học kỳ Spring 2026', 'SPRING2026', '2025-2026', @p4, @p5, N'Học kỳ đồ án tốt nghiệp Spring 2026 - Đang triển khai', @p9, NULL),
            (@p6, N'Học kỳ Summer 2026', 'SUMMER2026', '2025-2026', @p7, @p8, N'Học kỳ đồ án tốt nghiệp Summer 2026 - Sắp triển khai', @p9, NULL);";

        await context.Database.ExecuteSqlRawAsync(sql,
            Fall2025Id,
            Spring2026Id,
            new DateTime(2025, 9, 8, 0, 0, 0, DateTimeKind.Utc),   // Fall25 start
            new DateTime(2025, 12, 28, 0, 0, 0, DateTimeKind.Utc),  // Fall25 end
            new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),    // SP26 start
            new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),   // SP26 end
            Summer2026Id,
            new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),   // SU26 start
            new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),   // SU26 end
            SeedDate);

        // Fall 2025 phases (all Completed)
        var phaseSql = @"
            IF NOT EXISTS (SELECT 1 FROM SemesterPhases WHERE SemesterId = @p0)
            BEGIN
                INSERT INTO SemesterPhases (SemesterId, Name, Type, StartDate, EndDate, [Order], Status)
                VALUES
                (@p0, N'Đăng ký đề tài',    0, @p1, @p2, 1, 2),
                (@p0, N'Thẩm định đề tài',  1, @p3, @p4, 2, 2),
                (@p0, N'Triển khai',         2, @p5, @p6, 3, 2),
                (@p0, N'Bảo vệ đồ án',      3, @p7, @p8, 4, 2);
            END";

        await context.Database.ExecuteSqlRawAsync(phaseSql,
            Fall2025Id,
            new DateTime(2025, 7, 7), new DateTime(2025, 7, 27),     // Registration
            new DateTime(2025, 7, 28), new DateTime(2025, 8, 17),    // Evaluation
            new DateTime(2025, 9, 8), new DateTime(2025, 12, 22),    // Implementation
            new DateTime(2025, 12, 23), new DateTime(2025, 12, 27)); // Defense

        // Spring 2026 phases (Implementation in progress, Defense not started)
        var phaseSql2 = @"
            IF NOT EXISTS (SELECT 1 FROM SemesterPhases WHERE SemesterId = @p0)
            BEGIN
                INSERT INTO SemesterPhases (SemesterId, Name, Type, StartDate, EndDate, [Order], Status)
                VALUES
                (@p0, N'Đăng ký đề tài',    0, @p1, @p2, 1, 2),
                (@p0, N'Thẩm định đề tài',  1, @p3, @p4, 2, 2),
                (@p0, N'Triển khai',         2, @p5, @p6, 3, 1);
            END";

        await context.Database.ExecuteSqlRawAsync(phaseSql2,
            Spring2026Id,
            new DateTime(2025, 11, 3), new DateTime(2025, 11, 23),   // Registration
            new DateTime(2025, 11, 24), new DateTime(2025, 12, 14),  // Evaluation
            new DateTime(2026, 1, 5), new DateTime(2026, 5, 4));     // Implementation (15 weeks + 2 weeks Tet)

        // Summer 2026 phases (Registration upcoming, rest not started)
        var phaseSql3 = @"
            INSERT INTO SemesterPhases (SemesterId, Name, Type, StartDate, EndDate, [Order], Status)
            VALUES
            (@p0, N'Đăng ký đề tài',    0, @p1, @p2, 1, 0),
            (@p0, N'Thẩm định đề tài',  1, @p3, @p4, 2, 0),
            (@p0, N'Triển khai',         2, @p5, @p6, 3, 0);";

        await context.Database.ExecuteSqlRawAsync(phaseSql3,
            Summer2026Id,
            new DateTime(2026, 3, 16), new DateTime(2026, 4, 5),    // Registration
            new DateTime(2026, 4, 6), new DateTime(2026, 4, 26),    // Evaluation
            new DateTime(2026, 5, 11), new DateTime(2026, 8, 24));   // Implementation

        logger?.LogInformation("Seeded 3 semesters with phases.");
    }

    // ════════════════════════════════════════════════
    //  USERS
    // ════════════════════════════════════════════════
    private static async Task SeedUsersAsync(AppDbContext context, ILogger? logger)
    {
        var users = new List<(Guid Id, string Email, string FullName, string? StudentCode, string? EmployeeCode, string FirebaseUid)>();

        for (var i = 1; i <= AdminCount; i++)
            users.Add((AdminId(i), AdminEmail(i), $"Admin LoadTest {i}", null, $"LT-EMP-A{i:D4}", AdminFirebaseUid(i)));

        for (var i = 1; i <= DualRoleCount; i++)
            users.Add((DualRoleId(i), DualRoleEmail(i), $"Lecturer LoadTest {i}", null, $"LT-EMP-L{i:D4}", DualRoleFirebaseUid(i)));

        for (var i = 1; i <= StudentCount; i++)
            users.Add((StudentId(i), StudentEmail(i), $"Student LoadTest {i}", $"LT-{i:D6}", null, StudentFirebaseUid(i)));

        for (var batch = 0; batch < users.Count; batch += BatchSize)
        {
            var chunk = users.Skip(batch).Take(BatchSize).ToList();
            var valueClauses = new List<string>();
            var parameters = new List<object>();
            var paramIndex = 0;

            foreach (var u in chunk)
            {
                var pId = $"@p{paramIndex++}";
                var pEmail = $"@p{paramIndex++}";
                var pName = $"@p{paramIndex++}";
                var pStudentCode = $"@p{paramIndex++}";
                var pEmployeeCode = $"@p{paramIndex++}";
                var pFirebaseUid = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";

                valueClauses.Add($"({pId}, {pEmail}, {pName}, NULL, {pStudentCode}, {pEmployeeCode}, NULL, 1, 0, {pFirebaseUid}, {pDate}, NULL, NULL)");

                parameters.Add(u.Id);
                parameters.Add(u.Email);
                parameters.Add(u.FullName);
                parameters.Add((object?)u.StudentCode);
                parameters.Add((object?)u.EmployeeCode);
                parameters.Add(u.FirebaseUid);
                parameters.Add(SeedDate);
            }

            var sql = $@"
                INSERT INTO Users (Id, Email, FullName, AvatarUrl, StudentCode, EmployeeCode, AcademicTitle, DepartmentId, Status, FirebaseUid, CreatedAt, UpdatedAt, LastLoginAt)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        logger?.LogInformation("Seeded {Count} load-test users.", users.Count);
    }

    // ════════════════════════════════════════════════
    //  USER ROLES
    // ════════════════════════════════════════════════
    private static async Task SeedUserRolesAsync(AppDbContext context, ILogger? logger)
    {
        var roles = new List<(Guid UserId, string RoleName)>();

        for (var i = 1; i <= AdminCount; i++)
            roles.Add((AdminId(i), "Admin"));

        for (var i = 1; i <= DualRoleCount; i++)
        {
            roles.Add((DualRoleId(i), "Mentor"));
            roles.Add((DualRoleId(i), "Evaluator"));
        }

        roles.Add((DualRoleId(1), "DepartmentHead"));

        for (var i = 1; i <= StudentCount; i++)
            roles.Add((StudentId(i), "Student"));

        for (var batch = 0; batch < roles.Count; batch += BatchSize)
        {
            var chunk = roles.Skip(batch).Take(BatchSize).ToList();
            var valueClauses = new List<string>();
            var parameters = new List<object>();
            var paramIndex = 0;

            foreach (var r in chunk)
            {
                var pUserId = $"@p{paramIndex++}";
                var pRole = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";

                valueClauses.Add($"({pUserId}, {pRole}, {pDate}, NULL, 1)");
                parameters.Add(r.UserId);
                parameters.Add(r.RoleName);
                parameters.Add(SeedDate);
            }

            var sql = $@"
                INSERT INTO UserRoles (UserId, RoleName, AssignedAt, AssignedBy, IsActive)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        logger?.LogInformation("Seeded {Count} load-test user roles.", roles.Count);
    }

    // ════════════════════════════════════════════════
    //  TOPIC POOLS (8 pools, one per major)
    // ════════════════════════════════════════════════
    private static async Task SeedTopicPoolsAsync(AppDbContext context, ILogger? logger)
    {
        var valueClauses = new List<string>();
        var parameters = new List<object>();
        var paramIndex = 0;

        for (var m = 0; m < AllMajorIds.Length; m++)
        {
            var pId = $"@p{paramIndex++}";
            var pCode = $"@p{paramIndex++}";
            var pName = $"@p{paramIndex++}";
            var pDesc = $"@p{paramIndex++}";
            var pMajor = $"@p{paramIndex++}";
            var pDate = $"@p{paramIndex++}";

            valueClauses.Add($"({pId}, {pCode}, {pName}, {pDesc}, {pMajor}, 'Active', 5, 2, {pDate}, NULL, NULL, NULL)");

            parameters.Add(TopicPoolId(m));
            parameters.Add($"KHO-{MajorCodes[m]}");
            parameters.Add($"Kho đề tài {MajorNames[m]}");
            parameters.Add($"Kho đề tài chuyên ngành {MajorNames[m]} - Khoa CNTT");
            parameters.Add(AllMajorIds[m]);
            parameters.Add(SeedDate);
        }

        var sql = $@"
            INSERT INTO TopicPools (Id, Code, Name, Description, MajorId, Status, MaxActiveTopicsPerMentor, ExpirationSemesters, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
            VALUES {string.Join(",\n                   ", valueClauses)};";

        await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());

        logger?.LogInformation("Seeded 8 topic pools.");
    }

    // ════════════════════════════════════════════════
    //  TOPIC POOL PROJECTS (~25 per major = 200 total)
    //  SourceType=FromPool, no GroupId
    // ════════════════════════════════════════════════
    private static async Task SeedTopicPoolProjectsAsync(AppDbContext context, ILogger? logger)
    {
        var totalCount = 0;

        for (var m = 0; m < AllMajorIds.Length; m++)
        {
            var majorId = AllMajorIds[m];
            var majorCode = MajorCodes[m];
            var poolId = TopicPoolId(m);
            var topicNames = GetGeneratedTopicNames(m);
            var topicsPerMajor = Math.Min(topicNames.Length, TopicPoolProjectsPerMajor);
            var availableCount = (int)Math.Floor(topicsPerMajor * 0.6);
            var expiredCount = (int)Math.Floor(topicsPerMajor * 0.2);
            var reservedCount = (int)Math.Floor(topicsPerMajor * 0.1);
            var reservedStart = availableCount + expiredCount;
            var assignedStart = reservedStart + reservedCount;

            for (var batch = 0; batch < topicsPerMajor; batch += BatchSize)
            {
                var end = Math.Min(batch + BatchSize, topicsPerMajor);
                var valueClauses = new List<string>();
                var parameters = new List<object?>();
                var paramIndex = 0;

                for (var t = batch; t < end; t++)
                {
                    totalCount++;
                    var (nameEn, nameVi) = topicNames[t];

                    // Distribution: 60% Available, 20% Expired, 10% Reserved, remainder Assigned.
                    string poolStatus;
                    int projectStatus;
                    int? createdInSemester;
                    int? expirationSemester;
                    int semesterId;

                    if (t < availableCount)
                    {
                        poolStatus = "Available";
                        projectStatus = 3; // Approved
                        semesterId = Spring2026Id;
                        createdInSemester = Spring2026Id;
                        expirationSemester = null;
                    }
                    else if (t < reservedStart)
                    {
                        poolStatus = "Expired";
                        projectStatus = 3; // Approved (but expired in pool)
                        semesterId = Fall2025Id;
                        createdInSemester = Fall2025Id;
                        expirationSemester = Spring2026Id;
                    }
                    else if (t < assignedStart)
                    {
                        poolStatus = "Reserved";
                        projectStatus = 3; // Approved
                        semesterId = Spring2026Id;
                        createdInSemester = Spring2026Id;
                        expirationSemester = null;
                    }
                    else
                    {
                        poolStatus = "Assigned";
                        projectStatus = 5; // InProgress
                        semesterId = Spring2026Id;
                        createdInSemester = Spring2026Id;
                        expirationSemester = null;
                    }

                    var mentorId = DualRoleId((t % DualRoleCount) + 1);

                    var pId = $"@p{paramIndex++}";
                    var pCode = $"@p{paramIndex++}";
                    var pNameVi = $"@p{paramIndex++}";
                    var pNameEn = $"@p{paramIndex++}";
                    var pNameAbbr = $"@p{paramIndex++}";
                    var pDesc = $"@p{paramIndex++}";
                    var pObj = $"@p{paramIndex++}";
                    var pMajor = $"@p{paramIndex++}";
                    var pSemester = $"@p{paramIndex++}";
                    var pPool = $"@p{paramIndex++}";
                    var pStatus = $"@p{paramIndex++}";
                    var pPoolStatus = $"@p{paramIndex++}";
                    var pSubmittedBy = $"@p{paramIndex++}";
                    var pSubmittedAt = $"@p{paramIndex++}";
                    var pCreatedIn = $"@p{paramIndex++}";
                    var pExpiration = $"@p{paramIndex++}";
                    var pDate = $"@p{paramIndex++}";

                    valueClauses.Add($@"({pId}, {pCode}, {pNameVi}, {pNameEn}, {pNameAbbr},
                        {pDesc}, {pObj}, NULL, NULL, NULL,
                        {pMajor}, {pSemester}, NULL, {pPool}, 5, 0, 0, {pStatus}, 0,
                        {pSubmittedAt}, {pSubmittedBy}, NULL, NULL, NULL, 0, NULL,
                        {pPoolStatus}, {pCreatedIn}, {pExpiration}, {pDate}, NULL)");

                    parameters.Add(PoolProjectId(m, t + 1));
                    parameters.Add($"POOL-{majorCode}-{t + 1:D3}");
                    parameters.Add(nameVi);
                    parameters.Add(nameEn);
                    parameters.Add($"P-{majorCode}-{t + 1:D3}");
                    parameters.Add($"Mô tả đề tài: {nameVi}");
                    parameters.Add($"Mục tiêu: Xây dựng và triển khai {nameVi}");
                    parameters.Add(majorId);
                    parameters.Add(semesterId);
                    parameters.Add(poolId);
                    parameters.Add(projectStatus);
                    parameters.Add(poolStatus);
                    parameters.Add(mentorId);
                    parameters.Add(SeedDate.AddDays(-30));
                    parameters.Add((object?)createdInSemester);
                    parameters.Add((object?)expirationSemester);
                    parameters.Add(SeedDate);
                }

                var sql = $@"
                    INSERT INTO Projects (Id, Code, NameVi, NameEn, NameAbbr,
                        Description, Objectives, Scope, Technologies, ExpectedResults,
                        MajorId, SemesterId, GroupId, TopicPoolId, MaxStudents, SourceType, RegistrationType, Status, Priority,
                        SubmittedAt, SubmittedBy, ApprovedAt, StartDate, Deadline, EvaluationCount, LastEvaluationResult,
                        PoolStatus, CreatedInSemesterId, ExpirationSemesterId, CreatedAt, UpdatedAt)
                    VALUES {string.Join(",\n                           ", valueClauses)};";

                await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);
            }
        }

        logger?.LogInformation("Seeded {Count} topic pool projects.", totalCount);
    }

    // ════════════════════════════════════════════════
    //  TOPIC POOL PROJECT MENTORS (1 per pool project, matches SubmittedBy)
    // ════════════════════════════════════════════════
    private static async Task SeedTopicPoolProjectMentorsAsync(AppDbContext context, ILogger? logger)
    {
        var totalCount = 0;

        for (var m = 0; m < AllMajorIds.Length; m++)
        {
            var topicNames = GetGeneratedTopicNames(m);
            var topicsPerMajor = Math.Min(topicNames.Length, TopicPoolProjectsPerMajor);

            for (var batch = 0; batch < topicsPerMajor; batch += BatchSize)
            {
                var end = Math.Min(batch + BatchSize, topicsPerMajor);
                var valueClauses = new List<string>();
                var parameters = new List<object>();
                var paramIndex = 0;

                for (var t = batch; t < end; t++)
                {
                    totalCount++;
                    // Must match the SubmittedBy logic in SeedTopicPoolProjectsAsync
                    var mentorId = DualRoleId((t % DualRoleCount) + 1);

                    var pProject = $"@p{paramIndex++}";
                    var pMentor = $"@p{paramIndex++}";
                    var pDate = $"@p{paramIndex++}";

                    valueClauses.Add($"({pProject}, {pMentor}, 0, {pDate}, NULL, NULL)");
                    parameters.Add(PoolProjectId(m, t + 1));
                    parameters.Add(mentorId);
                    parameters.Add(SeedDate);
                }

                if (valueClauses.Count > 0)
                {
                    var sql = $@"
                        INSERT INTO ProjectMentors (ProjectId, MentorId, Status, AssignedAt, AssignedBy, Notes)
                        VALUES {string.Join(",\n                               ", valueClauses)};";

                    await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
                }
            }
        }

        logger?.LogInformation("Seeded {Count} topic pool project mentors.", totalCount);
    }

    // ════════════════════════════════════════════════
    //  GROUPS (50 Fall25 + 40 Spring26 + 15 Summer26 = 105 groups)
    // ════════════════════════════════════════════════
    private static async Task SeedGroupsAsync(AppDbContext context, ILogger? logger)
    {
        var totalGroups = Fall25GroupCount + Spring26GroupCount + Summer26GroupCount;

        for (var batch = 0; batch < totalGroups; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, totalGroups);
            var valueClauses = new List<string>();
            var parameters = new List<object>();
            var paramIndex = 0;

            for (var i = batch + 1; i <= end; i++)
            {
                var isFall = i <= Fall25GroupCount;
                var isSpring = !isFall && i <= Fall25GroupCount + Spring26GroupCount;
                // isSummer = everything else (i > Fall25GroupCount + Spring26GroupCount)

                int semesterId;
                int groupStatus;
                string code;
                string name;

                if (isFall)
                {
                    semesterId = Fall2025Id;
                    groupStatus = 1; // Completed
                    code = $"FA25-G-{i:D3}";
                    name = $"SE_{i:D2}";
                }
                else if (isSpring)
                {
                    var springIdx = i - Fall25GroupCount;
                    semesterId = Spring2026Id;
                    groupStatus = 0; // Active
                    code = $"SP26-G-{springIdx:D3}";
                    name = $"SE_{springIdx:D2}";
                }
                else
                {
                    var summerIndex = i - Fall25GroupCount - Spring26GroupCount;
                    semesterId = Summer2026Id;
                    groupStatus = 0; // Active
                    code = $"SU26-G-{summerIndex:D3}";
                    name = $"SE_{summerIndex:D2}";
                }

                var leaderId = StudentId(StudentStartIndex(i));

                var pId = $"@p{paramIndex++}";
                var pCode = $"@p{paramIndex++}";
                var pName = $"@p{paramIndex++}";
                var pSemester = $"@p{paramIndex++}";
                var pLeader = $"@p{paramIndex++}";
                var pStatus = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";
                var pIsOpen = $"@p{paramIndex++}";

                valueClauses.Add($"({pId}, {pCode}, {pName}, NULL, {pSemester}, {pLeader}, {pStatus}, 5, {pIsOpen}, {pDate}, NULL)");
                parameters.Add(GroupId(i));
                parameters.Add(code);
                parameters.Add(name);
                parameters.Add(semesterId);
                parameters.Add(leaderId);
                parameters.Add(groupStatus);
                parameters.Add(SeedDate);
                parameters.Add(!isFall && !isSpring); // Only Summer groups are open for requests
            }

            var sql = $@"
                INSERT INTO Groups (Id, Code, Name, ProjectId, SemesterId, LeaderId, Status, MaxMembers, IsOpenForRequests, CreatedAt, UpdatedAt)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        logger?.LogInformation("Seeded {Count} load-test groups.", totalGroups);
    }

    // ════════════════════════════════════════════════
    //  GROUP MEMBERS (510 students: 90 groups × 5 + 15 groups × 4)
    // ════════════════════════════════════════════════
    private static async Task SeedGroupMembersAsync(AppDbContext context, ILogger? logger)
    {
        var totalGroups = Fall25GroupCount + Spring26GroupCount + Summer26GroupCount;
        var members = new List<(Guid GroupId, Guid StudentId, int Role)>();

        for (var g = 1; g <= totalGroups; g++)
        {
            var count = MembersForGroup(g);
            var start = StudentStartIndex(g);
            for (var s = 0; s < count; s++)
            {
                members.Add((GroupId(g), StudentId(start + s), s == 0 ? 0 : 1));
            }
        }

        for (var batch = 0; batch < members.Count; batch += BatchSize)
        {
            var chunk = members.Skip(batch).Take(BatchSize).ToList();
            var valueClauses = new List<string>();
            var parameters = new List<object>();
            var paramIndex = 0;

            foreach (var m in chunk)
            {
                var pGroup = $"@p{paramIndex++}";
                var pStudent = $"@p{paramIndex++}";
                var pRole = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";

                valueClauses.Add($"({pGroup}, {pStudent}, {pRole}, 0, {pDate}, NULL)");
                parameters.Add(m.GroupId);
                parameters.Add(m.StudentId);
                parameters.Add(m.Role);
                parameters.Add(SeedDate);
            }

            var sql = $@"
                INSERT INTO GroupMembers (GroupId, StudentId, Role, Status, JoinedAt, LeftAt)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        logger?.LogInformation("Seeded {Count} load-test group members.", members.Count);
    }

    // ════════════════════════════════════════════════
    //  FALL 2025 PROJECTS (50 real SE topics, Completed)
    // ════════════════════════════════════════════════
    private static async Task SeedFall25ProjectsAsync(AppDbContext context, ILogger? logger)
    {
        var poolId = TopicPoolId(0); // SE pool (majorIndex=0)

        for (var batch = 0; batch < Fall25Topics.Length; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, Fall25Topics.Length);
            var valueClauses = new List<string>();
            var parameters = new List<object?>();
            var paramIndex = 0;

            for (var i = batch; i < end; i++)
            {
                var projectIndex = i + 1; // 1-based
                var topic = Fall25Topics[i];

                var pId = $"@p{paramIndex++}";
                var pCode = $"@p{paramIndex++}";
                var pNameVi = $"@p{paramIndex++}";
                var pNameEn = $"@p{paramIndex++}";
                var pNameAbbr = $"@p{paramIndex++}";
                var pDesc = $"@p{paramIndex++}";
                var pObj = $"@p{paramIndex++}";
                var pMajor = $"@p{paramIndex++}";
                var pSemester = $"@p{paramIndex++}";
                var pGroup = $"@p{paramIndex++}";
                var pPool = $"@p{paramIndex++}";
                var pSubmittedBy = $"@p{paramIndex++}";
                var pSubmittedAt = $"@p{paramIndex++}";
                var pApprovedAt = $"@p{paramIndex++}";
                var pStartDate = $"@p{paramIndex++}";
                var pDeadline = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";

                valueClauses.Add($@"({pId}, {pCode}, {pNameVi}, {pNameEn}, {pNameAbbr},
                    {pDesc}, {pObj}, NULL, NULL, NULL,
                    {pMajor}, {pSemester}, {pGroup}, {pPool}, 5, 0, 0, 6, 0,
                    {pSubmittedAt}, {pSubmittedBy}, {pApprovedAt}, {pStartDate}, {pDeadline}, 3, 1,
                    'Assigned', @p{paramIndex++}, NULL, {pDate}, NULL)");

                parameters.Add(ProjectId(projectIndex));
                parameters.Add(topic.Code);
                parameters.Add(topic.NameVi);
                parameters.Add(topic.NameEn);
                parameters.Add(topic.Code);
                parameters.Add($"Mô tả đề tài: {topic.NameVi}");
                parameters.Add($"Mục tiêu: {topic.NameEn}");
                parameters.Add(MajorSE);
                parameters.Add(Fall2025Id);
                parameters.Add(GroupId(projectIndex));
                parameters.Add(poolId);
                parameters.Add(DualRoleId((i % DualRoleCount) + 1));
                parameters.Add(new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc));
                parameters.Add(new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc));
                parameters.Add(new DateTime(2025, 9, 8, 0, 0, 0, DateTimeKind.Utc));
                parameters.Add(new DateTime(2025, 12, 22, 0, 0, 0, DateTimeKind.Utc));
                parameters.Add(SeedDate);
                parameters.Add(Fall2025Id); // CreatedInSemesterId
            }

            var sql = $@"
                INSERT INTO Projects (Id, Code, NameVi, NameEn, NameAbbr,
                    Description, Objectives, Scope, Technologies, ExpectedResults,
                    MajorId, SemesterId, GroupId, TopicPoolId, MaxStudents, SourceType, RegistrationType, Status, Priority,
                    SubmittedAt, SubmittedBy, ApprovedAt, StartDate, Deadline, EvaluationCount, LastEvaluationResult,
                    PoolStatus, CreatedInSemesterId, ExpirationSemesterId, CreatedAt, UpdatedAt)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);
        }

        // Update groups to reference their projects
        for (var i = 1; i <= Fall25GroupCount; i++)
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Groups SET ProjectId = @p0 WHERE Id = @p1;",
                ProjectId(i), GroupId(i));
        }

        logger?.LogInformation("Seeded {Count} Fall 2025 projects.", Fall25Topics.Length);
    }

    // ════════════════════════════════════════════════
    //  SPRING 2026 PROJECTS (40 real SE topics)
    //  First 20: InProgress (evaluated, approved, group assigned)
    //  Last  20: PendingEvaluation (awaiting evaluator review, no group)
    // ════════════════════════════════════════════════
    private const int Spring26EvaluatedCount = 20;

    private static async Task SeedSpring26ProjectsAsync(AppDbContext context, ILogger? logger)
    {
        var projectOffset = Fall25GroupCount; // Projects start at index 51

        for (var batch = 0; batch < Spring26Topics.Length; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, Spring26Topics.Length);
            var valueClauses = new List<string>();
            var parameters = new List<object?>();
            var paramIndex = 0;

            for (var i = batch; i < end; i++)
            {
                var projectIndex = projectOffset + i + 1; // 51..90
                var groupIndex = projectIndex; // Groups 51..90
                var topic = Spring26Topics[i];
                var isEvaluated = i < Spring26EvaluatedCount; // First 20 are evaluated & approved

                var pId = $"@p{paramIndex++}";
                var pCode = $"@p{paramIndex++}";
                var pNameVi = $"@p{paramIndex++}";
                var pNameEn = $"@p{paramIndex++}";
                var pNameAbbr = $"@p{paramIndex++}";
                var pDesc = $"@p{paramIndex++}";
                var pObj = $"@p{paramIndex++}";
                var pMajor = $"@p{paramIndex++}";
                var pSemester = $"@p{paramIndex++}";
                var pSubmittedBy = $"@p{paramIndex++}";
                var pSubmittedAt = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";

                // Common parameters (order matches @p indices above)
                parameters.Add(ProjectId(projectIndex));
                parameters.Add(topic.Code);
                parameters.Add(topic.NameVi);
                parameters.Add(topic.NameEn);
                parameters.Add(topic.Code);
                parameters.Add($"Mô tả đề tài: {topic.NameVi}");
                parameters.Add($"Mục tiêu: {topic.NameEn}");
                parameters.Add(MajorSE);
                parameters.Add(Spring2026Id);
                parameters.Add(DualRoleId(((projectOffset + i) % DualRoleCount) + 1));
                parameters.Add(new DateTime(2025, 11, 10, 0, 0, 0, DateTimeKind.Utc));
                parameters.Add(SeedDate);

                if (isEvaluated)
                {
                    // InProgress: has group, approved, evaluation completed
                    var pGroup = $"@p{paramIndex++}";
                    var pApprovedAt = $"@p{paramIndex++}";
                    var pStartDate = $"@p{paramIndex++}";
                    var pDeadline = $"@p{paramIndex++}";

                    valueClauses.Add($@"({pId}, {pCode}, {pNameVi}, {pNameEn}, {pNameAbbr},
                    {pDesc}, {pObj}, NULL, NULL, NULL,
                    {pMajor}, {pSemester}, {pGroup}, NULL, 5, 1, 0, 5, 0,
                    {pSubmittedAt}, {pSubmittedBy}, {pApprovedAt}, {pStartDate}, {pDeadline}, 3, 1,
                    NULL, NULL, NULL, {pDate}, NULL)");

                    parameters.Add(GroupId(groupIndex));
                    parameters.Add(new DateTime(2025, 12, 10, 0, 0, 0, DateTimeKind.Utc));
                    parameters.Add(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
                    parameters.Add(new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc));
                }
                else
                {
                    // PendingEvaluation: no group, no approval, awaiting evaluator review
                    valueClauses.Add($@"({pId}, {pCode}, {pNameVi}, {pNameEn}, {pNameAbbr},
                    {pDesc}, {pObj}, NULL, NULL, NULL,
                    {pMajor}, {pSemester}, NULL, NULL, 5, 1, 0, 1, 0,
                    {pSubmittedAt}, {pSubmittedBy}, NULL, NULL, NULL, 1, NULL,
                    NULL, NULL, NULL, {pDate}, NULL)");
                }
            }

            var sql = $@"
                INSERT INTO Projects (Id, Code, NameVi, NameEn, NameAbbr,
                    Description, Objectives, Scope, Technologies, ExpectedResults,
                    MajorId, SemesterId, GroupId, TopicPoolId, MaxStudents, SourceType, RegistrationType, Status, Priority,
                    SubmittedAt, SubmittedBy, ApprovedAt, StartDate, Deadline, EvaluationCount, LastEvaluationResult,
                    PoolStatus, CreatedInSemesterId, ExpirationSemesterId, CreatedAt, UpdatedAt)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);
        }

        // Update groups to reference their projects (only evaluated projects have groups)
        for (var i = 1; i <= Spring26EvaluatedCount; i++)
        {
            var groupIndex = Fall25GroupCount + i;
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Groups SET ProjectId = @p0 WHERE Id = @p1;",
                ProjectId(groupIndex), GroupId(groupIndex));
        }

        logger?.LogInformation("Seeded {Count} Spring 2026 projects ({Evaluated} evaluated, {Pending} pending).",
            Spring26Topics.Length, Spring26EvaluatedCount, Spring26Topics.Length - Spring26EvaluatedCount);
    }

    // ════════════════════════════════════════════════
    //  PROJECT MENTORS (1 per project, round-robin)
    // ════════════════════════════════════════════════
    private static async Task SeedProjectMentorsAsync(AppDbContext context, ILogger? logger)
    {
        var totalProjects = Fall25GroupCount + Spring26GroupCount;

        for (var batch = 0; batch < totalProjects; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, totalProjects);
            var valueClauses = new List<string>();
            var parameters = new List<object>();
            var paramIndex = 0;

            for (var i = batch + 1; i <= end; i++)
            {
                var mentorIndex = ((i - 1) % DualRoleCount) + 1;
                var pProject = $"@p{paramIndex++}";
                var pMentor = $"@p{paramIndex++}";
                var pDate = $"@p{paramIndex++}";

                valueClauses.Add($"({pProject}, {pMentor}, 0, {pDate}, NULL, NULL)");
                parameters.Add(ProjectId(i));
                parameters.Add(DualRoleId(mentorIndex));
                parameters.Add(SeedDate);
            }

            var sql = $@"
                INSERT INTO ProjectMentors (ProjectId, MentorId, Status, AssignedAt, AssignedBy, Notes)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        logger?.LogInformation("Seeded {Count} load-test project mentors.", totalProjects);
    }

    // ════════════════════════════════════════════════
    //  PROJECT EVALUATOR ASSIGNMENTS
    //  Fall25: 3 evaluators each (completed), Spring26: 3 each (pending)
    // ════════════════════════════════════════════════
    private static async Task SeedProjectEvaluatorAssignmentsAsync(AppDbContext context, ILogger? logger)
    {
        var totalProjects = Fall25GroupCount + Spring26GroupCount;
        var assignmentIndex = 0;

        for (var batch = 0; batch < totalProjects; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, totalProjects);
            var valueClauses = new List<string>();
            var parameters = new List<object?>();
            var paramIndex = 0;

            for (var i = batch + 1; i <= end; i++)
            {
                var isFall = i <= Fall25GroupCount;
                var mentorIndex = ((i - 1) % DualRoleCount) + 1;

                var evaluatorOffset = 0;
                for (var order = 1; order <= 2; order++)
                {
                    assignmentIndex++;

                    int evaluatorIndex;
                    do
                    {
                        evaluatorIndex = ((i - 1) * 2 + order - 1 + evaluatorOffset) % DualRoleCount + 1;
                        if (evaluatorIndex == mentorIndex)
                            evaluatorOffset++;
                    } while (evaluatorIndex == mentorIndex);

                    // Fall25: all evaluated as Approved
                    // Spring26 first 20 (i=51..70): evaluated as Approved
                    // Spring26 last 20 (i=71..90): pending evaluation
                    var hasResult = isFall || (!isFall && i <= Fall25GroupCount + Spring26EvaluatedCount);
                    var resultValue = hasResult ? (object?)1 : null; // 1=Approved
                    var evaluatedAt = hasResult
                        ? (object?)(isFall
                            ? new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc)
                            : new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc))
                        : null;
                    var feedback = hasResult
                        ? (object?)EvaluationFeedbacks[assignmentIndex % EvaluationFeedbacks.Length]
                        : null;

                    var pId = $"@p{paramIndex++}";
                    var pProject = $"@p{paramIndex++}";
                    var pEvaluator = $"@p{paramIndex++}";
                    var pOrder = $"@p{paramIndex++}";
                    var pAssignedAt = $"@p{paramIndex++}";
                    var pAssignedBy = $"@p{paramIndex++}";
                    var pIsActive = $"@p{paramIndex++}";
                    var pResult = $"@p{paramIndex++}";
                    var pEvaluatedAt = $"@p{paramIndex++}";
                    var pFeedback = $"@p{paramIndex++}";

                    valueClauses.Add($"({pId}, {pProject}, {pEvaluator}, {pOrder}, {pAssignedAt}, {pAssignedBy}, {pIsActive}, {pResult}, {pEvaluatedAt}, {pFeedback})");

                    parameters.Add(AssignmentId(assignmentIndex));
                    parameters.Add(ProjectId(i));
                    parameters.Add(DualRoleId(evaluatorIndex));
                    parameters.Add(order);
                    parameters.Add(SeedDate.AddDays(-14));
                    parameters.Add(AdminId(1));
                    parameters.Add(true);
                    parameters.Add(resultValue);
                    parameters.Add(evaluatedAt);
                    parameters.Add(feedback);
                }
            }

            if (valueClauses.Count > 0)
            {
                var sql = $@"
                    INSERT INTO ProjectEvaluatorAssignments (Id, ProjectId, EvaluatorId, EvaluatorOrder, AssignedAt, AssignedBy, IsActive, IndividualResult, EvaluatedAt, Feedback)
                    VALUES {string.Join(",\n                           ", valueClauses)};";

                await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);
            }
        }

        logger?.LogInformation("Seeded {Count} load-test evaluator assignments.", assignmentIndex);
    }

    // ════════════════════════════════════════════════
    //  TOPIC REGISTRATIONS (Fall25 only, Confirmed)
    // ════════════════════════════════════════════════
    private static async Task SeedTopicRegistrationsAsync(AppDbContext context, ILogger? logger)
    {
        for (var batch = 0; batch < Fall25GroupCount; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, Fall25GroupCount);
            var valueClauses = new List<string>();
            var parameters = new List<object?>();
            var paramIndex = 0;

            for (var i = batch + 1; i <= end; i++)
            {
                var leaderId = StudentId(StudentStartIndex(i));
                var mentorIndex = ((i - 1) % DualRoleCount) + 1;

                var pId = $"@p{paramIndex++}";
                var pProject = $"@p{paramIndex++}";
                var pGroup = $"@p{paramIndex++}";
                var pRegisteredBy = $"@p{paramIndex++}";
                var pRegisteredAt = $"@p{paramIndex++}";
                var pProcessedBy = $"@p{paramIndex++}";
                var pProcessedAt = $"@p{paramIndex++}";

                valueClauses.Add($"({pId}, {pProject}, {pGroup}, {pRegisteredBy}, {pRegisteredAt}, 'Confirmed', 1, NULL, {pProcessedBy}, {pProcessedAt}, NULL)");

                parameters.Add(RegistrationId(i));
                parameters.Add(ProjectId(i));
                parameters.Add(GroupId(i));
                parameters.Add(leaderId);
                parameters.Add(new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc));
                parameters.Add(DualRoleId(mentorIndex));
                parameters.Add(new DateTime(2025, 8, 5, 0, 0, 0, DateTimeKind.Utc));
            }

            var sql = $@"
                INSERT INTO TopicRegistrations (Id, ProjectId, GroupId, RegisteredBy, RegisteredAt, Status, Priority, Note, ProcessedBy, ProcessedAt, RejectReason)
                VALUES {string.Join(",\n                       ", valueClauses)};";

            await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);
        }

        logger?.LogInformation("Seeded {Count} topic registrations.", Fall25GroupCount);
    }

    // ════════════════════════════════════════════════
    //  SPRING 2026 REJECTED PROJECTS (10 projects, Rejected status)
    //  These represent topics rejected during the evaluation phase
    // ════════════════════════════════════════════════
    private static async Task SeedSpring26RejectedProjectsAsync(AppDbContext context, ILogger? logger)
    {
        // Rejected topics — diverse majors, no Group, have Mentor
        var rejectedTopics = new (string Code, string NameEn, string NameVi, int MajorId)[]
        {
            ("RJ_01", "Smart Greenhouse Monitoring System using IoT and Machine Learning", "Hệ thống giám sát nhà kính thông minh sử dụng IoT và Machine Learning", MajorAI),
            ("RJ_02", "Blockchain-based Supply Chain Tracking Platform for Agricultural Products", "Nền tảng theo dõi chuỗi cung ứng nông sản dựa trên Blockchain", MajorSE),
            ("RJ_03", "Real-time Network Intrusion Detection using Deep Learning", "Hệ thống phát hiện xâm nhập mạng thời gian thực sử dụng Deep Learning", MajorIA),
            ("RJ_04", "Automated Resume Screening System with NLP and Sentiment Analysis", "Hệ thống sàng lọc hồ sơ tự động sử dụng NLP và phân tích cảm xúc", MajorDS),
            ("RJ_05", "FPGA-based Hardware Accelerator for Convolutional Neural Networks", "Thiết kế bộ tăng tốc phần cứng dựa trên FPGA cho mạng CNN", MajorIC),
            ("RJ_06", "Autonomous Vehicle Parking System using Computer Vision and Sensors", "Hệ thống đỗ xe tự động sử dụng thị giác máy tính và cảm biến", MajorAS),
            ("RJ_07", "Enterprise Resource Planning System for Small Healthcare Clinics", "Hệ thống ERP cho phòng khám y tế quy mô nhỏ", MajorIS),
            ("RJ_08", "3D Product Configurator with AR Preview for E-commerce", "Công cụ cấu hình sản phẩm 3D với xem trước AR cho thương mại điện tử", MajorGD),
            ("RJ_09", "Predictive Maintenance Platform for Industrial Equipment using AI", "Nền tảng bảo trì dự đoán cho thiết bị công nghiệp sử dụng AI", MajorAI),
            ("RJ_10", "Decentralized Voting System using Zero-Knowledge Proofs", "Hệ thống bỏ phiếu phi tập trung sử dụng Zero-Knowledge Proofs", MajorIA),
        };

        var valueClauses = new List<string>();
        var parameters = new List<object?>();
        var paramIndex = 0;

        for (var i = 0; i < rejectedTopics.Length; i++)
        {
            var topic = rejectedTopics[i];
            var mentorIndex = ((i + 100) % DualRoleCount) + 1; // Offset to avoid clash with existing mentors

            var pId = $"@p{paramIndex++}";
            var pCode = $"@p{paramIndex++}";
            var pNameVi = $"@p{paramIndex++}";
            var pNameEn = $"@p{paramIndex++}";
            var pNameAbbr = $"@p{paramIndex++}";
            var pDesc = $"@p{paramIndex++}";
            var pObj = $"@p{paramIndex++}";
            var pMajor = $"@p{paramIndex++}";
            var pSemester = $"@p{paramIndex++}";
            var pSubmittedBy = $"@p{paramIndex++}";
            var pSubmittedAt = $"@p{paramIndex++}";
            var pDate = $"@p{paramIndex++}";

            // Status=4 (Rejected), no GroupId, no TopicPoolId, no ApprovedAt/StartDate/Deadline
            valueClauses.Add($@"({pId}, {pCode}, {pNameVi}, {pNameEn}, {pNameAbbr},
                    {pDesc}, {pObj}, NULL, NULL, NULL,
                    {pMajor}, {pSemester}, NULL, NULL, 5, 1, 0, 4, 0,
                    {pSubmittedAt}, {pSubmittedBy}, NULL, NULL, NULL, 3, 0,
                    NULL, NULL, NULL, {pDate}, NULL)");

            parameters.Add(RejectedProjectId(i + 1));
            parameters.Add(topic.Code);
            parameters.Add(topic.NameVi);
            parameters.Add(topic.NameEn);
            parameters.Add(topic.Code);
            parameters.Add($"Mô tả đề tài: {topic.NameVi}");
            parameters.Add($"Mục tiêu: {topic.NameEn}");
            parameters.Add(topic.MajorId);
            parameters.Add(Spring2026Id);
            parameters.Add(DualRoleId(mentorIndex));
            parameters.Add(new DateTime(2025, 11, 10, 0, 0, 0, DateTimeKind.Utc));
            parameters.Add(SeedDate);
        }

        var sql = $@"
            INSERT INTO Projects (Id, Code, NameVi, NameEn, NameAbbr,
                Description, Objectives, Scope, Technologies, ExpectedResults,
                MajorId, SemesterId, GroupId, TopicPoolId, MaxStudents, SourceType, RegistrationType, Status, Priority,
                SubmittedAt, SubmittedBy, ApprovedAt, StartDate, Deadline, EvaluationCount, LastEvaluationResult,
                PoolStatus, CreatedInSemesterId, ExpirationSemesterId, CreatedAt, UpdatedAt)
            VALUES {string.Join(",\n                   ", valueClauses)};";

        await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);

        // Add ProjectMentors for rejected projects
        var mentorClauses = new List<string>();
        var mentorParams = new List<object>();
        var mParamIndex = 0;

        for (var i = 0; i < rejectedTopics.Length; i++)
        {
            var mentorIndex = ((i + 100) % DualRoleCount) + 1;
            var pProject = $"@p{mParamIndex++}";
            var pMentor = $"@p{mParamIndex++}";
            var pDate = $"@p{mParamIndex++}";

            mentorClauses.Add($"({pProject}, {pMentor}, 0, {pDate}, NULL, NULL)");
            mentorParams.Add(RejectedProjectId(i + 1));
            mentorParams.Add(DualRoleId(mentorIndex));
            mentorParams.Add(SeedDate);
        }

        var mentorSql = $@"
            INSERT INTO ProjectMentors (ProjectId, MentorId, Status, AssignedAt, AssignedBy, Notes)
            VALUES {string.Join(",\n                   ", mentorClauses)};";

        await context.Database.ExecuteSqlRawAsync(mentorSql, mentorParams.ToArray());

        logger?.LogInformation("Seeded {Count} rejected Spring 2026 projects with mentors.", rejectedTopics.Length);
    }

    // ════════════════════════════════════════════════
    //  SPRING 2026 TOPIC REGISTRATIONS
    //  20 Confirmed (evaluated InProgress projects) + 10 Rejected (rejected projects)
    //  Last 20 Spring26 projects are PendingEvaluation — no registrations yet
    // ════════════════════════════════════════════════
    private static async Task SeedSpring26TopicRegistrationsAsync(AppDbContext context, ILogger? logger)
    {
        var registrationOffset = Fall25GroupCount; // Fall25 registrations use IDs 1..50
        var valueClauses = new List<string>();
        var parameters = new List<object?>();
        var paramIndex = 0;

        // 20 Confirmed registrations for evaluated Spring 2026 InProgress projects (ProjectId 51..70, GroupId 51..70)
        for (var i = 1; i <= Spring26EvaluatedCount; i++)
        {
            var projectIndex = Fall25GroupCount + i; // 51..90
            var groupIndex = projectIndex;
            var leaderId = StudentId(StudentStartIndex(projectIndex));
            var mentorIndex = ((projectIndex - 1) % DualRoleCount) + 1;

            var pId = $"@p{paramIndex++}";
            var pProject = $"@p{paramIndex++}";
            var pGroup = $"@p{paramIndex++}";
            var pRegisteredBy = $"@p{paramIndex++}";
            var pRegisteredAt = $"@p{paramIndex++}";
            var pProcessedBy = $"@p{paramIndex++}";
            var pProcessedAt = $"@p{paramIndex++}";

            valueClauses.Add($"({pId}, {pProject}, {pGroup}, {pRegisteredBy}, {pRegisteredAt}, 'Confirmed', 1, NULL, {pProcessedBy}, {pProcessedAt}, NULL)");

            parameters.Add(RegistrationId(registrationOffset + i));
            parameters.Add(ProjectId(projectIndex));
            parameters.Add(GroupId(groupIndex));
            parameters.Add(leaderId);
            parameters.Add(new DateTime(2025, 11, 10, 0, 0, 0, DateTimeKind.Utc));
            parameters.Add(DualRoleId(mentorIndex));
            parameters.Add(new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc));
        }

        var sql = $@"
            INSERT INTO TopicRegistrations (Id, ProjectId, GroupId, RegisteredBy, RegisteredAt, Status, Priority, Note, ProcessedBy, ProcessedAt, RejectReason)
            VALUES {string.Join(",\n                   ", valueClauses)};";

        await context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray()!);

        // 10 Rejected registrations for rejected projects
        // These need existing groups that tried to register for rejected topics
        // Use groups from the 361..400 student range (students 361*4=1441.. which doesn't exist)
        // Instead, reuse some groups that already have confirmed registrations — but that would be duplicate.
        // Better: create virtual registrations from random students
        var rejClauses = new List<string>();
        var rejParams = new List<object?>();
        var rejParamIndex = 0;

        for (var i = 1; i <= RejectedProjectCount; i++)
        {
            // Use students not assigned to any project group (students 361+)
            var studentIndex = Fall25GroupCount * 5 + Spring26GroupCount * 5 + i; // 450+1
            if (studentIndex > StudentCount) studentIndex = (i % StudentCount) + 1;

            var registrationIndex = registrationOffset + Spring26GroupCount + i; // 90 + i
            var mentorIndex = ((i + 100 - 1) % DualRoleCount) + 1;

            // Rejected registrations don't need a real group - use an existing group
            // But TopicRegistration requires GroupId. Use the group that corresponds to the student range
            // Actually, use the same groups from Fall25 (they already finished, so they could have tried to register again)
            var groupIndex = i; // Groups 1..10 (Fall25 groups, they're done with Fall25)

            var pId = $"@p{rejParamIndex++}";
            var pProject = $"@p{rejParamIndex++}";
            var pGroup = $"@p{rejParamIndex++}";
            var pRegisteredBy = $"@p{rejParamIndex++}";
            var pRegisteredAt = $"@p{rejParamIndex++}";
            var pProcessedBy = $"@p{rejParamIndex++}";
            var pProcessedAt = $"@p{rejParamIndex++}";
            var pRejectReason = $"@p{rejParamIndex++}";

            rejClauses.Add($"({pId}, {pProject}, {pGroup}, {pRegisteredBy}, {pRegisteredAt}, 'Rejected', 1, NULL, {pProcessedBy}, {pProcessedAt}, {pRejectReason})");

            rejParams.Add(RegistrationId(registrationIndex));
            rejParams.Add(RejectedProjectId(i));
            rejParams.Add(GroupId(groupIndex));
            rejParams.Add(StudentId(StudentStartIndex(groupIndex)));
            rejParams.Add(new DateTime(2025, 11, 12, 0, 0, 0, DateTimeKind.Utc));
            rejParams.Add(DualRoleId(mentorIndex));
            rejParams.Add(new DateTime(2025, 12, 10, 0, 0, 0, DateTimeKind.Utc));
            rejParams.Add($"Đề tài không đáp ứng yêu cầu chất lượng của hội đồng thẩm định kỳ Spring 2026.");
        }

        var rejSql = $@"
            INSERT INTO TopicRegistrations (Id, ProjectId, GroupId, RegisteredBy, RegisteredAt, Status, Priority, Note, ProcessedBy, ProcessedAt, RejectReason)
            VALUES {string.Join(",\n                   ", rejClauses)};";

        await context.Database.ExecuteSqlRawAsync(rejSql, rejParams.ToArray()!);

        logger?.LogInformation("Seeded {Confirmed} confirmed + {Rejected} rejected Spring 2026 topic registrations.",
            Spring26GroupCount, RejectedProjectCount);
    }

    // ════════════════════════════════════════════════
    //  SUPPORT TICKETS (50 tickets, mixed statuses)
    // ════════════════════════════════════════════════
    private static async Task SeedSupportTicketsAsync(AppDbContext context, ILogger? logger)
    {
        // Support ticket templates: (Title, Description, Category, Priority)
        // Category: Technical=0, Academic=1, Account=2, Other=3
        // Priority: Low=0, Medium=1, High=2, Urgent=3
        var ticketTemplates = new (string Title, string Description, int Category, int Priority)[]
        {
            // Technical issues (Category=0)
            ("Lỗi upload file PDF đồ án", "Khi upload file PDF lớn hơn 10MB, hệ thống báo lỗi timeout. Đã thử nhiều lần nhưng không thành công.", 0, 3),
            ("Không thể đăng nhập vào hệ thống", "Sau khi đổi mật khẩu, tài khoản bị khóa và không thể đăng nhập lại.", 0, 2),
            ("Trang quản lý nhóm bị lỗi hiển thị", "Danh sách thành viên nhóm không hiển thị đúng, một số thành viên bị trùng lặp.", 0, 1),
            ("Lỗi khi xuất báo cáo Excel", "Chức năng xuất báo cáo ra file Excel bị crash khi có quá nhiều dữ liệu.", 0, 2),
            ("Hệ thống phản hồi chậm vào giờ cao điểm", "Trong khoảng 8h-10h sáng, hệ thống load rất chậm, ảnh hưởng đến việc đăng ký đề tài.", 0, 2),
            ("Lỗi hiển thị tiếng Việt trên mobile", "Các ký tự tiếng Việt có dấu bị lỗi font trên trình duyệt mobile Safari.", 0, 1),
            ("Chức năng tìm kiếm không hoạt động", "Thanh tìm kiếm đề tài không trả về kết quả khi nhập từ khóa tiếng Việt có dấu.", 0, 2),
            ("Lỗi 500 khi xem chi tiết đồ án", "Click vào xem chi tiết một số đồ án trả về lỗi Internal Server Error.", 0, 3),
            ("Notification email không gửi được", "Hệ thống thông báo qua email đã ngừng hoạt động từ 2 ngày trước.", 0, 2),
            ("Lỗi phân quyền truy cập trang admin", "Giảng viên thường có thể truy cập một số trang admin restricted.", 0, 3),
            ("Database connection timeout", "High Server Load detected - database connection pool exhausted during peak hours.", 0, 3),
            ("SSL certificate sắp hết hạn", "SSL certificate của domain chính sẽ hết hạn trong 7 ngày, cần renew gấp.", 0, 2),

            // Academic issues (Category=1)
            ("Yêu cầu đổi tên đề tài", "Nhóm đã thống nhất với GVHD đổi tên đề tài nhưng hệ thống không cho phép chỉnh sửa vì đã quá hạn.", 1, 1),
            ("Xin gia hạn nộp báo cáo tiến độ", "Do thành viên nhóm bị ốm, xin gia hạn nộp báo cáo tiến độ tuần 8 thêm 3 ngày.", 1, 1),
            ("Yêu cầu đổi GVHD", "GVHD hiện tại không phù hợp với hướng nghiên cứu, xin đổi sang giảng viên khác.", 1, 2),
            ("Xin phép bảo vệ đồ án sớm", "Nhóm đã hoàn thành đồ án trước hạn, xin được bảo vệ sớm hơn lịch dự kiến.", 1, 0),
            ("Khiếu nại kết quả thẩm định", "Kết quả thẩm định đề tài bị đánh giá sai, phản biện không đúng chuyên ngành.", 1, 2),
            ("Yêu cầu bổ sung thành viên nhóm", "Nhóm hiện có 3 thành viên, xin bổ sung thêm 1 thành viên để hoàn thành đồ án.", 1, 1),
            ("Đề tài trùng lặp với nhóm khác", "Phát hiện đề tài nhóm mình có nội dung gần giống với nhóm PROJ-239, cần xem xét.", 1, 2),
            ("Yêu cầu thay đổi phạm vi đề tài", "Sau khi triển khai, phạm vi ban đầu quá rộng, xin thu hẹp phạm vi đề tài.", 1, 1),
            ("Xin xác nhận hoàn thành đồ án", "Nhóm đã hoàn thành tất cả yêu cầu, xin admin xác nhận để được bảo vệ.", 1, 0),
            ("Hỏi về quy trình đăng ký đề tài", "Sinh viên mới chưa nắm rõ quy trình đăng ký đề tài cho kỳ tới, cần hướng dẫn chi tiết.", 1, 0),

            // Account issues (Category=2)
            ("Yêu cầu reset mật khẩu", "Quên mật khẩu tài khoản sinh viên, email xác nhận không nhận được.", 2, 1),
            ("Tài khoản bị khóa không rõ lý do", "Tài khoản sinh viên bị khóa đột ngột mà không nhận được thông báo.", 2, 2),
            ("Yêu cầu cập nhật thông tin cá nhân", "Cần cập nhật email và số điện thoại trong hệ thống nhưng không có quyền chỉnh sửa.", 2, 0),
            ("Không thể liên kết tài khoản Google", "Chức năng đăng nhập bằng Google không hoạt động, báo lỗi OAuth.", 2, 1),
            ("Yêu cầu cấp tài khoản cho giảng viên mới", "Giảng viên mới chuyển về khoa CNTT, cần được cấp tài khoản hệ thống.", 2, 1),
            ("Tài khoản hiển thị sai vai trò", "Tài khoản giảng viên đang hiển thị vai trò sinh viên, không truy cập được chức năng mentor.", 2, 2),
            ("Yêu cầu xóa tài khoản cũ", "Sinh viên đã tốt nghiệp, yêu cầu xóa tài khoản theo quy định bảo mật.", 2, 0),
            ("Không nhận được email kích hoạt", "Đã đăng ký tài khoản 3 ngày nhưng vẫn chưa nhận được email kích hoạt.", 2, 1),

            // Other issues (Category=3)
            ("Góp ý cải thiện giao diện", "Giao diện trang dashboard khó sử dụng trên tablet, cần responsive design tốt hơn.", 3, 0),
            ("Đề xuất thêm tính năng thống kê", "Cần thêm biểu đồ thống kê tiến độ đồ án theo tuần cho giảng viên.", 3, 0),
            ("Báo lỗi tài liệu hướng dẫn", "Tài liệu hướng dẫn sử dụng trên trang help có một số link bị hỏng.", 3, 0),
            ("Yêu cầu hỗ trợ tích hợp API", "Cần hỗ trợ tích hợp API hệ thống với LMS của trường.", 3, 1),
            ("Phản hồi về chính sách đề tài", "Chính sách giới hạn số lượng đề tài mỗi giảng viên quá ít, cần xem xét lại.", 3, 1),
            ("Câu hỏi về bảo mật dữ liệu", "Sinh viên thắc mắc về chính sách bảo mật dữ liệu đồ án trên hệ thống.", 3, 0),
            ("Yêu cầu export dữ liệu cá nhân", "Theo quy định GDPR, yêu cầu xuất toàn bộ dữ liệu cá nhân trên hệ thống.", 3, 1),
            ("Đề xuất dark mode cho hệ thống", "Nhiều sinh viên yêu cầu thêm chế độ dark mode để dễ sử dụng ban đêm.", 3, 0),
            ("Hỗ trợ cài đặt VPN truy cập hệ thống", "Sinh viên thực tập ở nước ngoài không truy cập được hệ thống, cần VPN.", 3, 1),
            ("Báo cáo spam trong hệ thống tin nhắn", "Có tài khoản gửi tin nhắn spam đến nhiều sinh viên qua hệ thống.", 3, 2),

            // Additional technical
            ("Lỗi sync dữ liệu giữa mobile và web", "Dữ liệu cập nhật trên mobile không đồng bộ sang phiên bản web.", 0, 1),
            ("API response time quá chậm", "Endpoint /api/projects trả về response time > 5 giây khi có filter phức tạp.", 0, 2),
            ("Lỗi cache không invalidate", "Sau khi cập nhật thông tin đề tài, dữ liệu cũ vẫn hiển thị do cache không được xóa.", 0, 1),
            ("Memory leak trên server production", "RAM usage tăng liên tục, cần restart server mỗi 48 giờ.", 0, 3),
            ("Backup database thất bại", "Scheduled backup đêm qua failed, cần kiểm tra disk space và retry.", 0, 3),
            ("Lỗi CORS khi gọi API từ subdomain", "Frontend ở subdomain mới không gọi được API do CORS policy.", 0, 1),
            ("Yêu cầu nâng cấp storage", "Dung lượng lưu trữ file đồ án đã đạt 85%, cần mở rộng trước khi hết.", 0, 2),
            ("Log monitoring alert: nhiều request 404", "Hệ thống monitor phát hiện 500+ request 404 trong 1 giờ qua.", 0, 1),
            ("Yêu cầu tăng kích thước upload file", "Giới hạn upload 10MB quá nhỏ, nhiều file báo cáo vượt quá giới hạn.", 1, 1),
            ("Hỏi về lịch bảo vệ đồ án", "Sinh viên hỏi lịch bảo vệ đồ án kỳ Spring 2026 đã được công bố chưa.", 1, 0),
        };

        var baseDate = new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc);

        for (var batch = 0; batch < SupportTicketCount; batch += BatchSize)
        {
            var end = Math.Min(batch + BatchSize, SupportTicketCount);
            var valueClauses2 = new List<string>();
            var parameters2 = new List<object?>();
            var pIdx = 0;

            for (var i = batch; i < end; i++)
            {
                var template = ticketTemplates[i % ticketTemplates.Length];
                var ratio = SupportTicketCount == 0 ? 0d : (double)i / SupportTicketCount;
                var status = ratio switch
                {
                    < 0.30 => 0, // Open
                    < 0.55 => 1, // InProgress
                    < 0.85 => 2, // Resolved
                    _ => 3       // Closed
                };
                var ticketCode = $"TK-2026-{(i + 1):D4}";
                var createdAt = baseDate.AddDays(-(SupportTicketCount - i)).AddHours(i % 12).AddMinutes(i * 7 % 60);

                // Reporter: alternate between students and lecturers
                var reporterId = i % 3 == 0
                    ? DualRoleId((i % DualRoleCount) + 1) // lecturer
                    : StudentId((i % StudentCount) + 1);  // student

                // Assignee: admin for InProgress/Resolved/Closed tickets
                Guid? assigneeId = status >= 1 ? AdminId((i % 10) + 1) : null;

                DateTime? resolvedAt = status >= 2 ? createdAt.AddDays(1).AddHours(3) : null;
                DateTime? closedAt = status == 3 ? createdAt.AddDays(2).AddHours(5) : null;
                DateTime? updatedAt = status >= 1 ? createdAt.AddHours(2) : null;

                var pId = $"@p{pIdx++}";
                var pCode = $"@p{pIdx++}";
                var pTitle = $"@p{pIdx++}";
                var pDesc = $"@p{pIdx++}";
                var pReporter = $"@p{pIdx++}";
                var pAssignee = $"@p{pIdx++}";
                var pCategory = $"@p{pIdx++}";
                var pPriority = $"@p{pIdx++}";
                var pStatus = $"@p{pIdx++}";
                var pCreatedAt = $"@p{pIdx++}";
                var pUpdatedAt = $"@p{pIdx++}";
                var pResolvedAt = $"@p{pIdx++}";
                var pClosedAt = $"@p{pIdx++}";

                valueClauses2.Add($"({pId}, {pCode}, {pTitle}, {pDesc}, {pReporter}, {pAssignee}, {pCategory}, {pPriority}, {pStatus}, {pCreatedAt}, {pUpdatedAt}, {pResolvedAt}, {pClosedAt})");

                parameters2.Add(SupportTicketId(i + 1));
                parameters2.Add(ticketCode);
                parameters2.Add(template.Title);
                parameters2.Add(template.Description);
                parameters2.Add(reporterId);
                parameters2.Add(assigneeId.HasValue ? (object)assigneeId.Value : null);
                parameters2.Add(template.Category);
                parameters2.Add(template.Priority);
                parameters2.Add(status);
                parameters2.Add(createdAt);
                parameters2.Add(updatedAt.HasValue ? (object)updatedAt.Value : null);
                parameters2.Add(resolvedAt.HasValue ? (object)resolvedAt.Value : null);
                parameters2.Add(closedAt.HasValue ? (object)closedAt.Value : null);
            }

            var sql2 = $@"
                INSERT INTO SupportTickets (Id, Code, Title, Description, ReporterId, AssigneeId, Category, Priority, Status, CreatedAt, UpdatedAt, ResolvedAt, ClosedAt)
                VALUES {string.Join(",\n                       ", valueClauses2)};";

            await context.Database.ExecuteSqlRawAsync(sql2, parameters2.ToArray()!);
        }

        logger?.LogInformation("Seeded {Count} support tickets.", SupportTicketCount);
    }

    // ════════════════════════════════════════════════
    //  DEPARTMENT HEAD
    // ════════════════════════════════════════════════
    private static async Task SetDepartmentHeadAsync(AppDbContext context, ILogger? logger)
    {
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE Departments SET HeadOfDepartmentId = @p0, UpdatedAt = @p1 WHERE Id = 1;",
            DualRoleId(1), SeedDate);

        logger?.LogInformation("Set DepartmentHead: Lecturer 1 ({UserId}) for Department CNTT.", DualRoleId(1));
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
            // Leaf tables (no dependents)
            "SupportTickets",
            "TopicRegistrations",
            "ProjectEvaluatorAssignments",
            "ProjectMentors",
            "Documents",
            "GroupMembers",
            "EvaluationSubmissions",

            // Projects reference Groups & TopicPools; Groups reference Projects (circular via ProjectId)
            // Break the cycle: NULL out the FK first, then delete.
            "UPDATE Groups SET ProjectId = NULL;",

            "Projects",
            "Groups",
            "TopicPools",
            "UserRoles",
            "Users",
            "SemesterPhases",

            // Reset DepartmentHead FK before deleting semesters
            "UPDATE Departments SET HeadOfDepartmentId = NULL;",

            "Semesters",
        };

        try
        {
            foreach (var entry in tables)
            {
                if (entry.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    await context.Database.ExecuteSqlRawAsync(entry);
                }
                else
                {
                    await context.Database.ExecuteSqlRawAsync($"DELETE FROM [{entry}];");
                }
            }

            // Only RESEED tables that actually use identity columns (int Id, auto-increment).
            // Tables with Guid PKs or ValueGeneratedNever do NOT have identity columns.
            var identityTables = new[]
            {
                "SemesterPhases", "GroupMembers", "UserRoles", "ProjectMentors",
                "Departments", "Majors"
            };

            foreach (var table in identityTables)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync(
                        $"DBCC CHECKIDENT ('[{table}]', RESEED, 0);");
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

    // ════════════════════════════════════════════════
    //  GENERATED TOPIC NAMES (for TopicPool, ~100 per major)
    // ════════════════════════════════════════════════
    private static (string NameEn, string NameVi)[] GetGeneratedTopicNames(int majorIndex)
    {
        return majorIndex switch
        {
            0 => GenerateSeTopics(),
            1 => GenerateAiTopics(),
            2 => GenerateDsTopics(),
            3 => GenerateIaTopics(),
            4 => GenerateIcTopics(),
            5 => GenerateAsTopics(),
            6 => GenerateIsTopics(),
            7 => GenerateGdTopics(),
            _ => throw new ArgumentOutOfRangeException(nameof(majorIndex))
        };
    }

    private static (string NameEn, string NameVi)[] GenerateSeTopics()
    {
        var templates = new (string En, string Vi)[]
        {
            ("Building a {0} Management System using ReactJS, ASP.NET Core and SQL Server", "Xây dựng hệ thống quản lý {0} sử dụng ReactJS, ASP.NET Core và SQL Server"),
            ("Developing a {0} Platform using Next.js, Node.js and PostgreSQL", "Phát triển nền tảng {0} sử dụng Next.js, Node.js và PostgreSQL"),
            ("Building a {0} Application using React Native, Spring Boot and MySQL", "Xây dựng ứng dụng {0} sử dụng React Native, Spring Boot và MySQL"),
            ("Building a Web-based {0} System using Vue.js, NestJS and MongoDB", "Xây dựng hệ thống {0} trên nền web sử dụng Vue.js, NestJS và MongoDB"),
            ("Developing an {0} E-Commerce Platform using React, .NET Core and Redis", "Phát triển nền tảng thương mại điện tử {0} sử dụng React, .NET Core và Redis"),
        };
        var subjects = new[]
        {
            ("Hospital Appointment Booking", "Đặt lịch khám bệnh viện"),
            ("Online Bookstore", "Nhà sách trực tuyến"),
            ("Pet Care Service", "Dịch vụ chăm sóc thú cưng"),
            ("Warehouse Inventory", "Quản lý kho hàng"),
            ("Food Delivery", "Giao đồ ăn"),
            ("Car Rental", "Cho thuê ô tô"),
            ("Real Estate Listing", "Bất động sản"),
            ("Hotel Reservation", "Đặt phòng khách sạn"),
            ("Pharmacy Chain", "Chuỗi nhà thuốc"),
            ("Fitness Tracking", "Theo dõi sức khỏe"),
            ("Student Attendance", "Điểm danh sinh viên"),
            ("Online Auction", "Đấu giá trực tuyến"),
            ("Wedding Planning", "Lên kế hoạch đám cưới"),
            ("Music Streaming", "Phát nhạc trực tuyến"),
            ("Smart Parking", "Bãi đỗ xe thông minh"),
            ("Agriculture Supply Chain", "Chuỗi cung ứng nông nghiệp"),
            ("Freelance Marketplace", "Sàn việc làm tự do"),
            ("Dental Clinic", "Phòng khám nha khoa"),
            ("Laundry Service", "Dịch vụ giặt ủi"),
            ("Blood Donation", "Hiến máu"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var t = templates[i % templates.Length];
            var s = subjects[i % subjects.Length];
            result[i] = (string.Format(t.En, s.Item1), string.Format(t.Vi, s.Item2));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateAiTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Building an AI-powered Chatbot for Customer Service using Transformers and FastAPI", "Xây dựng Chatbot AI hỗ trợ khách hàng sử dụng Transformers và FastAPI"),
            ("Developing a Deep Learning Model for Vietnamese Handwriting Recognition", "Phát triển mô hình Deep Learning nhận dạng chữ viết tay tiếng Việt"),
            ("Building a Face Recognition Attendance System using OpenCV and TensorFlow", "Xây dựng hệ thống điểm danh nhận diện khuôn mặt sử dụng OpenCV và TensorFlow"),
            ("Developing an AI-based Medical Image Diagnosis System using CNN", "Phát triển hệ thống chẩn đoán hình ảnh y tế bằng AI sử dụng CNN"),
            ("Building a Sentiment Analysis System for Vietnamese Social Media", "Xây dựng hệ thống phân tích cảm xúc mạng xã hội tiếng Việt"),
            ("Developing an AI-powered Plant Disease Detection Mobile App", "Phát triển ứng dụng phát hiện bệnh cây trồng bằng AI"),
            ("Building an Intelligent Traffic Monitoring System using YOLOv8", "Xây dựng hệ thống giám sát giao thông thông minh sử dụng YOLOv8"),
            ("Developing a Vietnamese Speech-to-Text System using Wav2Vec", "Phát triển hệ thống nhận dạng giọng nói tiếng Việt sử dụng Wav2Vec"),
            ("Building an AI-based Recommendation Engine for E-Learning", "Xây dựng hệ thống gợi ý học tập bằng AI"),
            ("Developing a Predictive Maintenance System using Machine Learning", "Phát triển hệ thống bảo trì dự đoán sử dụng Machine Learning"),
            ("Building a GAN-based Image Generation System for Fashion Design", "Xây dựng hệ thống tạo ảnh thời trang bằng GAN"),
            ("Developing an NLP-based Legal Document Analysis System", "Phát triển hệ thống phân tích văn bản pháp luật bằng NLP"),
            ("Building an AI-powered Fraud Detection System for Banking", "Xây dựng hệ thống phát hiện gian lận ngân hàng bằng AI"),
            ("Developing an Autonomous Drone Navigation System using Reinforcement Learning", "Phát triển hệ thống điều hướng drone tự động bằng Reinforcement Learning"),
            ("Building a Vietnamese Language Translation System using Seq2Seq", "Xây dựng hệ thống dịch thuật tiếng Việt sử dụng Seq2Seq"),
            ("Developing an AI-based Resume Screening System", "Phát triển hệ thống sàng lọc CV bằng AI"),
            ("Building a Smart Agriculture Monitoring System using Computer Vision", "Xây dựng hệ thống giám sát nông nghiệp thông minh bằng Computer Vision"),
            ("Developing an AI-powered Music Composition Tool", "Phát triển công cụ sáng tác nhạc bằng AI"),
            ("Building an Emotion Detection System from Voice using Deep Learning", "Xây dựng hệ thống nhận diện cảm xúc qua giọng nói bằng Deep Learning"),
            ("Developing an AI-based Real-time Object Tracking System", "Phát triển hệ thống theo dõi đối tượng thời gian thực bằng AI"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateDsTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Building a Real-time Data Dashboard for Business Intelligence using Power BI and Python", "Xây dựng Dashboard dữ liệu thời gian thực cho Business Intelligence sử dụng Power BI và Python"),
            ("Developing a Customer Churn Prediction Model using Machine Learning", "Phát triển mô hình dự đoán khách hàng rời bỏ sử dụng Machine Learning"),
            ("Building a Big Data Processing Pipeline using Apache Spark and Kafka", "Xây dựng pipeline xử lý dữ liệu lớn sử dụng Apache Spark và Kafka"),
            ("Developing a Sales Forecasting System using Time Series Analysis", "Phát triển hệ thống dự báo doanh thu sử dụng phân tích chuỗi thời gian"),
            ("Building a Data Lake Architecture for Healthcare Analytics", "Xây dựng kiến trúc Data Lake cho phân tích y tế"),
            ("Developing a Social Media Analytics Platform using NLP and Graph DB", "Phát triển nền tảng phân tích mạng xã hội sử dụng NLP và Graph DB"),
            ("Building a Credit Scoring Model using Ensemble Methods", "Xây dựng mô hình chấm điểm tín dụng sử dụng Ensemble Methods"),
            ("Developing a Real-time Fraud Detection System using Stream Processing", "Phát triển hệ thống phát hiện gian lận thời gian thực sử dụng Stream Processing"),
            ("Building a Recommendation System for Movie Streaming using Collaborative Filtering", "Xây dựng hệ thống gợi ý phim sử dụng Collaborative Filtering"),
            ("Developing a Market Basket Analysis Tool for Retail", "Phát triển công cụ phân tích giỏ hàng cho bán lẻ"),
            ("Building a Geospatial Data Analysis Platform for Urban Planning", "Xây dựng nền tảng phân tích dữ liệu không gian cho quy hoạch đô thị"),
            ("Developing a Student Performance Prediction System using Data Mining", "Phát triển hệ thống dự đoán kết quả học tập sử dụng Data Mining"),
            ("Building an ETL Pipeline for E-Commerce Data Warehouse", "Xây dựng pipeline ETL cho Data Warehouse thương mại điện tử"),
            ("Developing a Supply Chain Optimization Model using Linear Programming", "Phát triển mô hình tối ưu chuỗi cung ứng sử dụng Linear Programming"),
            ("Building a COVID-19 Data Visualization and Prediction Platform", "Xây dựng nền tảng trực quan hóa và dự đoán dữ liệu COVID-19"),
            ("Developing a Text Mining System for Vietnamese News Classification", "Phát triển hệ thống Text Mining phân loại tin tức tiếng Việt"),
            ("Building a Data Quality Monitoring Dashboard using Great Expectations", "Xây dựng Dashboard giám sát chất lượng dữ liệu sử dụng Great Expectations"),
            ("Developing an Energy Consumption Prediction System using IoT Data", "Phát triển hệ thống dự đoán tiêu thụ năng lượng từ dữ liệu IoT"),
            ("Building a Web Scraping and Data Analysis Pipeline for Price Comparison", "Xây dựng pipeline thu thập và phân tích dữ liệu so sánh giá"),
            ("Developing a Customer Segmentation Model using Clustering Algorithms", "Phát triển mô hình phân khúc khách hàng sử dụng thuật toán Clustering"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateIaTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Building a Network Intrusion Detection System using Machine Learning", "Xây dựng hệ thống phát hiện xâm nhập mạng sử dụng Machine Learning"),
            ("Developing a Vulnerability Scanner for Web Applications using Python", "Phát triển công cụ quét lỗ hổng bảo mật ứng dụng web sử dụng Python"),
            ("Building a SIEM Dashboard for Security Operations Center", "Xây dựng Dashboard SIEM cho Trung tâm điều hành an ninh"),
            ("Developing a Phishing Email Detection System using NLP", "Phát triển hệ thống phát hiện email lừa đảo sử dụng NLP"),
            ("Building a Secure File Sharing Platform with End-to-End Encryption", "Xây dựng nền tảng chia sẻ tệp bảo mật với mã hóa đầu cuối"),
            ("Developing a Digital Forensics Investigation Tool", "Phát triển công cụ điều tra pháp y kỹ thuật số"),
            ("Building a PKI-based Certificate Management System", "Xây dựng hệ thống quản lý chứng chỉ dựa trên PKI"),
            ("Developing a Malware Analysis Sandbox Environment", "Phát triển môi trường Sandbox phân tích mã độc"),
            ("Building a Zero-Trust Network Architecture Prototype", "Xây dựng nguyên mẫu kiến trúc mạng Zero-Trust"),
            ("Developing an IoT Security Monitoring Platform", "Phát triển nền tảng giám sát bảo mật IoT"),
            ("Building a Blockchain-based Identity Verification System", "Xây dựng hệ thống xác minh danh tính dựa trên Blockchain"),
            ("Developing a Web Application Firewall using ModSecurity", "Phát triển tường lửa ứng dụng web sử dụng ModSecurity"),
            ("Building a Password Security Assessment Tool", "Xây dựng công cụ đánh giá bảo mật mật khẩu"),
            ("Developing a DDoS Attack Detection and Mitigation System", "Phát triển hệ thống phát hiện và giảm thiểu tấn công DDoS"),
            ("Building a Secure Authentication System using FIDO2/WebAuthn", "Xây dựng hệ thống xác thực bảo mật sử dụng FIDO2/WebAuthn"),
            ("Developing a Data Loss Prevention System for Enterprises", "Phát triển hệ thống ngăn chặn mất dữ liệu cho doanh nghiệp"),
            ("Building a Security Awareness Training Platform", "Xây dựng nền tảng đào tạo nhận thức an ninh mạng"),
            ("Developing a Threat Intelligence Aggregation Platform", "Phát triển nền tảng tổng hợp thông tin mối đe dọa"),
            ("Building a Secure API Gateway with Rate Limiting and WAF", "Xây dựng API Gateway bảo mật với Rate Limiting và WAF"),
            ("Developing a Compliance Monitoring System for GDPR/PDPA", "Phát triển hệ thống giám sát tuân thủ GDPR/PDPA"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateIcTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Designing a RISC-V Processor Core using Verilog HDL", "Thiết kế lõi vi xử lý RISC-V sử dụng Verilog HDL"),
            ("Building an FPGA-based Image Processing Accelerator", "Xây dựng bộ tăng tốc xử lý ảnh trên FPGA"),
            ("Developing a Low-Power IoT SoC Design using Cadence Tools", "Phát triển thiết kế SoC IoT tiết kiệm năng lượng sử dụng Cadence"),
            ("Designing a Digital Signal Processing Unit on FPGA", "Thiết kế đơn vị xử lý tín hiệu số trên FPGA"),
            ("Building an AI Inference Accelerator Chip Architecture", "Xây dựng kiến trúc chip tăng tốc suy luận AI"),
            ("Developing a Custom ASIC for Edge Computing Applications", "Phát triển ASIC tùy chỉnh cho ứng dụng Edge Computing"),
            ("Designing a Memory Controller for DDR4 SDRAM", "Thiết kế bộ điều khiển bộ nhớ cho DDR4 SDRAM"),
            ("Building a Hardware Security Module using FPGA", "Xây dựng module bảo mật phần cứng sử dụng FPGA"),
            ("Developing a UART/SPI/I2C Communication IP Core", "Phát triển IP Core giao tiếp UART/SPI/I2C"),
            ("Designing a Neural Network Accelerator on Zynq FPGA", "Thiết kế bộ tăng tốc mạng neural trên Zynq FPGA"),
            ("Building a USB 3.0 Controller IP Core", "Xây dựng IP Core điều khiển USB 3.0"),
            ("Developing a Power Management IC Design", "Phát triển thiết kế IC quản lý năng lượng"),
            ("Designing a Bluetooth Low Energy SoC", "Thiết kế SoC Bluetooth Low Energy"),
            ("Building an AES Encryption Engine on FPGA", "Xây dựng engine mã hóa AES trên FPGA"),
            ("Developing a Camera Interface Module using MIPI CSI-2", "Phát triển module giao tiếp camera sử dụng MIPI CSI-2"),
            ("Designing a Multi-core Processor Architecture for Embedded Systems", "Thiết kế kiến trúc vi xử lý đa nhân cho hệ thống nhúng"),
            ("Building a PCIe Controller IP for High-Speed Data Transfer", "Xây dựng IP điều khiển PCIe cho truyền dữ liệu tốc độ cao"),
            ("Developing an ADC/DAC Interface for Mixed-Signal Systems", "Phát triển giao diện ADC/DAC cho hệ thống tín hiệu hỗn hợp"),
            ("Designing a Chip Layout for 28nm CMOS Technology", "Thiết kế layout chip cho công nghệ CMOS 28nm"),
            ("Building a MEMS Sensor Interface ASIC", "Xây dựng ASIC giao tiếp cảm biến MEMS"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateAsTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Building an ADAS Lane Detection System using Computer Vision", "Xây dựng hệ thống phát hiện làn đường ADAS sử dụng Computer Vision"),
            ("Developing an OBD-II Diagnostic Application for Vehicles", "Phát triển ứng dụng chẩn đoán OBD-II cho xe hơi"),
            ("Building an Autonomous Parking System using Ultrasonic Sensors", "Xây dựng hệ thống đỗ xe tự động sử dụng cảm biến siêu âm"),
            ("Developing a Vehicle Fleet Management System using GPS and IoT", "Phát triển hệ thống quản lý đội xe sử dụng GPS và IoT"),
            ("Building a Driver Drowsiness Detection System using AI", "Xây dựng hệ thống phát hiện tài xế buồn ngủ bằng AI"),
            ("Developing a CAN Bus Communication Simulator", "Phát triển trình mô phỏng giao tiếp CAN Bus"),
            ("Building a Battery Management System for Electric Vehicles", "Xây dựng hệ thống quản lý pin cho xe điện"),
            ("Developing a V2X Communication Prototype using DSRC", "Phát triển nguyên mẫu giao tiếp V2X sử dụng DSRC"),
            ("Building a Vehicle Infotainment System on Embedded Linux", "Xây dựng hệ thống giải trí trên xe sử dụng Embedded Linux"),
            ("Developing a Tire Pressure Monitoring System using BLE", "Phát triển hệ thống giám sát áp suất lốp sử dụng BLE"),
            ("Building a Traffic Sign Recognition System using Deep Learning", "Xây dựng hệ thống nhận dạng biển báo giao thông bằng Deep Learning"),
            ("Developing an Electric Vehicle Charging Station Management System", "Phát triển hệ thống quản lý trạm sạc xe điện"),
            ("Building a Vehicle Telematics Platform using AWS IoT", "Xây dựng nền tảng Telematics cho xe sử dụng AWS IoT"),
            ("Developing a Pedestrian Detection System using LiDAR", "Phát triển hệ thống phát hiện người đi bộ sử dụng LiDAR"),
            ("Building a Digital Twin for Vehicle Performance Monitoring", "Xây dựng Digital Twin giám sát hiệu suất xe"),
            ("Developing a Smart Dashboard for Connected Vehicles", "Phát triển bảng điều khiển thông minh cho xe kết nối"),
            ("Building an Autonomous Vehicle Path Planning System", "Xây dựng hệ thống lập kế hoạch đường đi xe tự lái"),
            ("Developing an AUTOSAR-based ECU Software Architecture", "Phát triển kiến trúc phần mềm ECU dựa trên AUTOSAR"),
            ("Building a Vehicle Cybersecurity Monitoring System", "Xây dựng hệ thống giám sát an ninh mạng cho xe"),
            ("Developing a Predictive Maintenance System for Vehicle Engines", "Phát triển hệ thống bảo trì dự đoán cho động cơ xe"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateIsTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Building an ERP System for Small and Medium Enterprises using SAP HANA", "Xây dựng hệ thống ERP cho doanh nghiệp vừa và nhỏ sử dụng SAP HANA"),
            ("Developing a CRM System with Salesforce Integration", "Phát triển hệ thống CRM tích hợp Salesforce"),
            ("Building a Business Process Management Platform using Camunda", "Xây dựng nền tảng quản lý quy trình kinh doanh sử dụng Camunda"),
            ("Developing a Document Management System for Government Agencies", "Phát triển hệ thống quản lý văn bản cho cơ quan nhà nước"),
            ("Building a Knowledge Management Portal for Enterprises", "Xây dựng cổng quản lý tri thức cho doanh nghiệp"),
            ("Developing an IT Service Management System using ITIL Framework", "Phát triển hệ thống quản lý dịch vụ CNTT theo ITIL"),
            ("Building a Supply Chain Management System using Blockchain", "Xây dựng hệ thống quản lý chuỗi cung ứng sử dụng Blockchain"),
            ("Developing a Human Resource Information System", "Phát triển hệ thống thông tin nhân sự"),
            ("Building an E-Government Service Portal for Da Nang City", "Xây dựng cổng dịch vụ chính phủ điện tử cho TP Đà Nẵng"),
            ("Developing a Workflow Automation System using Low-Code Platform", "Phát triển hệ thống tự động hóa quy trình sử dụng nền tảng Low-Code"),
            ("Building a Healthcare Information System for Clinics", "Xây dựng hệ thống thông tin y tế cho phòng khám"),
            ("Developing a School Management Information System", "Phát triển hệ thống thông tin quản lý trường học"),
            ("Building a Digital Asset Management System", "Xây dựng hệ thống quản lý tài sản số"),
            ("Developing a Financial Reporting and Analytics System", "Phát triển hệ thống báo cáo và phân tích tài chính"),
            ("Building an Inventory Management System with Barcode/RFID", "Xây dựng hệ thống quản lý tồn kho tích hợp Barcode/RFID"),
            ("Developing a Customer Support Ticketing System", "Phát triển hệ thống quản lý ticket hỗ trợ khách hàng"),
            ("Building a Project Portfolio Management System", "Xây dựng hệ thống quản lý danh mục dự án"),
            ("Developing a Compliance Management System for Banking", "Phát triển hệ thống quản lý tuân thủ cho ngân hàng"),
            ("Building a Learning Management System for Corporate Training", "Xây dựng hệ thống quản lý học tập cho đào tạo doanh nghiệp"),
            ("Developing a Quality Management System using Six Sigma", "Phát triển hệ thống quản lý chất lượng theo Six Sigma"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    private static (string NameEn, string NameVi)[] GenerateGdTopics()
    {
        var subjects = new (string En, string Vi)[]
        {
            ("Building a 3D Character Modeling Pipeline using Blender and Unity", "Xây dựng pipeline tạo mô hình nhân vật 3D sử dụng Blender và Unity"),
            ("Developing a Brand Identity Design System for Digital Platforms", "Phát triển hệ thống thiết kế nhận diện thương hiệu cho nền tảng số"),
            ("Building an Interactive Motion Graphics Portfolio Website", "Xây dựng website portfolio Motion Graphics tương tác"),
            ("Developing a UI/UX Design System for Mobile Banking Applications", "Phát triển Design System UI/UX cho ứng dụng ngân hàng di động"),
            ("Building a Digital Illustration Marketplace Platform", "Xây dựng nền tảng sàn giao dịch minh họa kỹ thuật số"),
            ("Developing an Augmented Reality Product Visualization App", "Phát triển ứng dụng trực quan hóa sản phẩm bằng AR"),
            ("Building a Responsive Web Design Template System", "Xây dựng hệ thống template thiết kế web responsive"),
            ("Developing a Social Media Content Creation Tool", "Phát triển công cụ tạo nội dung mạng xã hội"),
            ("Building a 2D Game Art Asset Pipeline using Photoshop and Spine", "Xây dựng pipeline tạo asset game 2D sử dụng Photoshop và Spine"),
            ("Developing an Infographic Generator for Data Storytelling", "Phát triển công cụ tạo Infographic cho Data Storytelling"),
            ("Building a Virtual Exhibition Gallery using Three.js", "Xây dựng phòng triển lãm ảo sử dụng Three.js"),
            ("Developing a Logo Design Tool with AI-Assisted Suggestions", "Phát triển công cụ thiết kế logo tích hợp gợi ý AI"),
            ("Building a Typography Showcase Platform for Vietnamese Fonts", "Xây dựng nền tảng trưng bày Typography cho font chữ Việt"),
            ("Developing a Packaging Design Mockup Generator", "Phát triển công cụ tạo mockup thiết kế bao bì"),
            ("Building a Color Palette Generator for Design Projects", "Xây dựng công cụ tạo bảng màu cho dự án thiết kế"),
            ("Developing a Video Editing Platform for Short-Form Content", "Phát triển nền tảng chỉnh sửa video cho nội dung ngắn"),
            ("Building an Animated Sticker Creator for Messaging Apps", "Xây dựng công cụ tạo sticker hoạt hình cho ứng dụng nhắn tin"),
            ("Developing a Print-on-Demand Design Studio", "Phát triển studio thiết kế in theo yêu cầu"),
            ("Building a Collaborative Whiteboard for Design Teams", "Xây dựng bảng trắng cộng tác cho nhóm thiết kế"),
            ("Developing a Photo Retouching and Enhancement Web App", "Phát triển ứng dụng web chỉnh sửa và nâng cao chất lượng ảnh"),
        };
        var result = new (string, string)[100];
        for (var i = 0; i < 100; i++)
        {
            var s = subjects[i % subjects.Length];
            result[i] = (s.En + (i >= subjects.Length ? $" - Version {i / subjects.Length + 1}" : ""),
                         s.Vi + (i >= subjects.Length ? $" - Phiên bản {i / subjects.Length + 1}" : ""));
        }
        return result;
    }

    // ════════════════════════════════════════════════
    //  EVALUATION FEEDBACKS
    // ════════════════════════════════════════════════
    private static readonly string[] EvaluationFeedbacks =
    [
        "Đề tài có tính ứng dụng cao, nhóm triển khai tốt.",
        "Phương pháp luận tốt, triển khai đầy đủ các tính năng yêu cầu.",
        "Kiến trúc hệ thống hợp lý, code chất lượng cao.",
        "Đề tài sáng tạo, phần demo ấn tượng, đáp ứng yêu cầu đề ra.",
        "Nhóm hoàn thành tốt, tài liệu đầy đủ và rõ ràng.",
        "Hệ thống hoạt động ổn định, UI/UX thân thiện với người dùng.",
        "Ứng dụng thực tiễn cao, có tiềm năng phát triển thêm.",
        "Giải pháp kỹ thuật phù hợp, đáp ứng đúng nghiệp vụ.",
    ];

    // ════════════════════════════════════════════════
    //  REAL TOPIC DATA FROM EXCEL FILES
    // ════════════════════════════════════════════════

    // 50 Fall 2025 SE topics from Fall25.xlsx
    private static readonly (string Code, string NameEn, string NameVi)[] Fall25Topics =
    [
        ("SE_01", "Building a School Bus Management System for educational institutions using ReactJS, and ASP.NET", "Xây dựng hệ thống quản lý xe đưa đón học sinh cho trường học, sử dụng ReactJS, ASP.NET"),
        ("SE_02", "Building digital platform that enables transactions between flower farms and flower shops using ReactJS, Spring Boot and MySQL", "Xây dựng nền tảng số giao dịch giữa các trang trại hoa với tiệm hoa sử dụng ReactJS, Spring Boot và MySQL"),
        ("SE_03", "Building a Fashion E-commerce Website for a Brand with Integrated AI to Optimize User Experience", "Xây dựng Website Bán Áo Quần cho Brand Tích Hợp AI để tối ưu hóa trải nghiệm người dùng"),
        ("SE_04", "FamTree - Family Tree Management System", "FamTree - Hệ thống quản lý gia phả trực tuyến"),
        ("SE_05", "Building a Tour Booking Management System for Korean Tourists in Da Nang Using Spring Boot, ReactJS, Android, and MySQL", "Xây dựng hệ thống quản lý đặt tour du lịch Đà Nẵng dành cho du khách Hàn Quốc sử dụng công nghệ Spring Boot, ReactJS, Android, MySQL"),
        ("SE_06", "GaragePro-Building a digital garage management system to optimize the process of receiving, repairing, supporting incidents and delivering vehicles integrated on Web and Mobile platforms using ASP.NET WEB CORE API, Android, NextJS, SQL Server technology", "Xây dựng hệ thống quản lý Gara ô tô số hóa nhằm tối ưu hóa quy trình tiếp nhận, sửa chữa, hỗ trợ sự cố và giao xe tích hợp trên nền tảng Web và Mobile sử dụng công nghệ ASP.NET WEB CORE API, Android, NextJS, SQL Server"),
        ("SE_07", "Building a web-based for renting travel equipment and essentials such as suitcases, cameras, and camping gear using ReactJS, NodeJS, and MongoDB", "Xây dựng hệ thống cho phép khách hàng đặt thuê các thiết bị và đồ dùng cần thiết cho chuyến đi như vali, máy ảnh, thiết bị dã ngoại sử dụng ReactJS, NodeJS và MongoDB"),
        ("SE_08", "EduMeal: Building A Web-based School Lunch Meal Management System using NextJs, ASP.NET Core Web API and SQL Server", "EduMeal: Xây dựng hệ thống quản lý bữa ăn trưa cho học sinh sử dụng NextJs, ASP.NET Core Web API và SQL Server"),
        ("SE_09", "Building a System for Class Enrollment and Tracking at an English Center using ReactJS and ASP.NET Core Web API", "Xây dựng hệ thống đăng ký và theo dõi lớp học tại trung tâm Anh Ngữ sử dụng ReactJS, ASP.NET Core Web API"),
        ("SE_10", "Building the Book Platform: Combining Book Commerce and AI Text-to-Speech Using ASP.NET Core API, React JS, and SQL Server Database", "Xây dựng nền tảng Book: Kết hợp thương mại sách và AI chuyển đổi văn bản thành giọng nói sử dụng công nghệ ASP.NET Core API, React JS và cơ sở dữ liệu SQL Server"),
        ("SE_11", "Dozu - Personalized Learning Roadmaps Platform with Multi-Method Learning and Integrated Class Management System using Next.js, Node.js, PostgreSQL, Redis", "Dozu - Nền tảng tạo lộ trình học tập cá nhân hóa với phương pháp học đa dạng và tích hợp hệ thống quản lý lớp học sử dụng Nextjs, Nodejs, PostgreSQL, Redis"),
        ("SE_12", "Building a system to connect, support, monitor the health and psychology of the elderly integrated on Web and Mobile platforms using NodeJS, ReactJS, MongoDB and React Native", "Xây dựng hệ thống kết nối, hỗ trợ, theo dõi sức khỏe và tâm lí của người cao tuổi tích hợp trên nền tảng Web và Moblie sử dụng NodeJS, ReactJS, MongoDB và React Native"),
        ("SE_13", "Building a system to support management of scientific research and articles in universities using ReactJS, Spring Boot and Mysql", "Xây dựng hệ thống hỗ trợ quản lý nghiên cứu khoa học và bài báo trong trường đại học sử dụng ReactJS, Spring Boot và Mysql"),
        ("SE_14", "Smart Gym Management System Using NodeJS and React", "Hệ thống quản lý phòng tập thông minh sử dụng NodeJS và React"),
        ("SE_15", "Building a web-based workspace safety management and operation system for construction sites using Node.js, React Js and MongoDB", "Xây dựng hệ thống quản lý và điều hành an toàn lao động tại các công trình xây dựng sử dụng Node.js, React Js và MongoDB"),
        ("SE_16", "An online tutoring platform connecting tutors and students using AI for lecture content moderation and analysis using .NET and ReactJS", "Nền tảng gia sư trực tuyến kết nối gia sư và học viên, ứng dụng AI để kiểm duyệt và phân tích nội dung bài giảng sử dụng .NET và ReactJS"),
        ("SE_17", "HomeCareDN - Building a Construction and Repair Service Management System in Da Nang using ASP.NET Core API,ReactJS and SQL Server technology", "HomeCareDN - Xây dựng Hệ Thống Quản Lý Dịch Vụ Xây Nhà và Sửa Chữa tại Đà Nẵng sử dụng công nghệ ASP.NET Core API, ReactJS và SQL Server"),
        ("SE_18", "Building Event Management System in FPT University using ASP.NET Core Web API, ReactJS and SQL Server", "Xây dựng hệ thống quản lý sự kiện tại trường đại học FPT sử dụng ASP.NET Core Web API, ReactJS và SQL Server"),
        ("SE_19", "Building a comprehensive learning management and monitoring platform for training centers with React, ReactNative, Node.js and MongoDB", "Xây dựng nền tảng quản lý và giám sát học tập toàn diện cho các trung tâm đào tạo với React, ReactNative, Node.js và MongoDB"),
        ("SE_20", "Online Construction Supervision Platform for Residential Projects in Da Nang using Next.js, ASP.NET Core, PostgreSQL and Langchain", "Nền tảng giám sát công trình xây dựng dân dụng trực tuyến tại Đà Nẵng sử dụng Next.js, ASP.NET Core, PostgreSQL và Langchain"),
        ("SE_21", "Building a Smart Tutor-Student Matching and Learning Support Platform using REACT JS, EXPRESS JS, MONGODB", "Xây dựng nền tảng kết nối học sinh và gia sư thông minh hỗ trợ học tập sử dụng công nghệ REACT JS, EXPRESS JS, MONGODB"),
        ("SE_22", "Building a Microservices-Based Website for Managing the Supply Chain of Electronic Components and Microchip Devices using Spring Boot REST API, MySQL", "Xây dựng trang web quản lý chuỗi linh kiện và thiết bị vi mạch tại thành phố Đà Nẵng sử dụng công nghệ Spring boot REST API, MySQL theo kiến trúc Microservices"),
        ("SE_23", "Roommate Finder Management System for Students in Da Nang City using NextJS, Java Spring Boot, PostgreSQL, and MongoDB", "Hệ Thống Quản Lý - Tìm Người Ở Ghép - cho Sinh viên tại thành phố Đà Nẵng sử dụng NextJS, Java Spring boot, PostgreSQL và MongoDB"),
        ("SE_24", "Building an Online Platform for Student Connection and School Activity Management using ASP.NET Core Web API, ReactJS, and SQL Server", "Xây dựng nền tảng trực tuyến kết nối học sinh và quản lý hoạt động học đường sử dụng ASP.NET Core Web API, ReactJS, SQL Server"),
        ("SE_25", "Building Rentzy - Self-driving Vehicle Rental Platform using Node.js, React.js, and MySQL", "Xây dựng Rentzy - Nền tảng cho thuê xe tự lái sử dụng công nghệ NodeJs, ReactJs và MySQL"),
        ("SE_26", "Building a website to support IT lecturers in organizing and managing course projects at FPT University Danang using microservice pattern", "Xây dựng website hỗ trợ giảng viên ngành CNTT tổ chức và quản lý các đồ án môn học tại trường Đại học FPT Đà Nẵng sử dụng mô hình microservice"),
        ("SE_27", "Online platform for buying and selling smart homes integrated with IoT/ICT technology", "Nền tảng trực tuyến mua bán nhà thông minh kết hợp công nghệ Internet vạn vật IoT/ICT"),
        ("SE_28", "Faise Paper Trading - Real-Time Stock Trading Platform for Web and Mobile using Node.js, Python, React Native, MongoDB, MySQL and Redis", "Faise Paper Trading - Nền tảng giao dịch chứng khoán thời gian thực cho Web và Mobile, sử dụng Node.js, Python, React Native, MongoDB, MySQL và Redis"),
        ("SE_29", "Building ALLEN - A Platform supporting English learning using NextJS, ASP.NET Core API and SQL Server", "Xây dựng ALLEN - Nền tảng hỗ trợ học tiếng Anh sử dụng NextJS, ASP.NET Core API và SQL Server"),
        ("SE_30", "Building a Real-Time Public Waste Monitoring System Using IoT-Based Fill-Level Sensors Integrated with Web and Mobile Platforms via IoT Sensors, Spring Boot MVC, SQL Server and Android", "Xây dựng ứng dụng giám sát rác thải công cộng thông qua hệ thống giám sát toàn diện từ thời gian thực tế trên nền tảng Web và Mobile sử dụng cảm biến IoT, Spring Boot MVC, SQL Server và Android"),
        ("SE_31", "Build a luxury outfit rental and sales system, using Razor Pages, ASP.NET Core Web API and SQL Server", "Xây dựng hệ thống cho thuê và bán trang phục sang trọng, sử dụng Razor Pages, ASP.NET Core Web API và SQL Server"),
        ("SE_32", "Building a Co-working Space Booking System using ReactJS, ASP.NET Core Web API and SQL Server", "Xây dựng hệ thống đặt lịch thuê không gian làm việc theo giờ sử dụng ReactJS, ASP.NET Core Web API và SQL Server"),
        ("SE_33", "Building an online medical appointment booking platform using ASP.NET Core API, ReactTS, SQL Server technology", "Xây dựng nền tảng đặt lịch hẹn khám bệnh trực tuyến sử dụng công nghệ ASP.NET Core API, ReactTS, SQL Server"),
        ("SE_34", "Building a Web Application for Managing Seafood Supply and Consumption in Da Nang using ASP.NET Core API,ReactJS and SQL Server technology", "Xây dựng trang web quản lý nguồn cung cấp và tiêu thụ hải sản tại thành phố Đà Nẵng sử dụng công nghệ ASP.NET Core Web API, ReactJS và SQL Server"),
        ("SE_35", "Building a data visualization website for helpful insight using ReactJs, NestJs and PostgreSQL", "Xây dựng 1 website trực quan hóa dữ liệu để mang lại các thông tin hữu ích sử dụng ReactJs, NestJs và PostgreSQL"),
        ("SE_36", "Building an Apartment/House Rental Management System (Web and Mobile) using Spring Boot MVC, ReactJS, Firebase Database, Android", "Xây dựng hệ thống web và app quản lý cho thuê trọ/chung cư sử dụng mô hình Spring Boot, ReactJS, Firebase Database, Android"),
        ("SE_37", "FlexJob Connect: A platform that connects students and freelancers with job opportunities through bidding and contest-based mechanisms, built with Java Spring MVC, RESTful API and PostgreSQL", "FlexJob Connect: Xây dựng ứng dụng kết nối sinh viên và freelancer với việc làm thông qua cơ chế đấu thầu và thi tuyển sử dụng Java Spring MVC, RESTful API và PostgreSQL"),
        ("SE_38", "Build an online website to book sports fields and find coaches using React, Nest Js, MongoDB", "Xây dựng ứng dụng đặt sân thể thao, tìm huấn luyện viên sử dụng React, Nest Js, MongoDB"),
        ("SE_39", "Building a personal financial management system and dividing bills multi-platform multi-language using NextJS, React Native, Java Spring Boot technology", "Xây dựng hệ thống quản lý tài chính cá nhân và chia hóa đơn đa nền tảng đa ngôn ngữ sử dụng công nghệ NextJS, ReactNative, Java Spring Boot"),
        ("SE_40", "Building EduXtend - A Student Club and Training Point Management System for FPT University, using ASP.NET Core Web API, ReactJS and SQL Server", "Xây dựng EduXtend - Hệ thống quản lí câu lạc bộ và điểm rèn luyện cho trường Đại học FPT, sử dụng công nghệ ASP.NET Core Web API, ReactJS, MS SQL Server"),
        ("SE_41", "Building an E-Commerce Website for Second-Hand Products with Price Prediction AI using ReactJS, SQL Server and .NET", "Xây dựng website thương mại điện tử cho sản phẩm cũ tích hợp AI dự đoán giá sử dụng ReactJS, SQL Server và .NET"),
        ("SE_42", "Building a website system to support finding domestic helpers using NodeJS, SQL Server, ReactJS technology", "Xây dựng hệ thống website hỗ trợ tìm kiếm người giúp việc sử dụng công nghệ NodeJS, SQL Server, ReactJS"),
        ("SE_43", "Building a Comprehensive Project Management and Music Collaboration Platform for Music Producers using Java Spring RESTful API, ReactJS and MySQL", "Xây dựng hệ thống quản lý dự án và hợp tác âm nhạc toàn diện cho Music Producer sử dụng Java Spring RESTful API, ReactJS và MySQL"),
        ("SE_44", "Building a Management System for Eco-Tourism Service Chain in Da Nang City using .NET, React, SQL Server Technology", "Xây dựng hệ thống quản lý chuỗi dịch vụ du lịch sinh thái tại thành phố Đà Nẵng sử dụng công nghệ .NET, React, SQL Server"),
        ("SE_45", "Build a Personalized Learning Website Using the FSRS Algorithm with NodeJS, ReactJS and MongoDB", "Xây dựng Website cá nhân hoá học tập áp dụng thuật toán FSRS sử dụng công nghệ NodeJS, ReactJS và MongoDB"),
        ("SE_46", "Build an AI-powered job board management system using NextJS, .NET Core and SQL Server", "Xây dựng hệ thống quản lý tìm kiếm việc làm tích hợp trí tuệ nhân tạo bằng NextJS, .NET Core và SQL Server"),
        ("SE_47", "Building an Event Ticketing Platform with React, Node.js, PostgreSQL and MongoDB", "Xây dựng Nền tảng Bán Vé Sự kiện với React, Node.js, PostgreSQL và MongoDB"),
        ("SE_48", "Build a digital data portal on Vietnamese traditional festivals and beliefs using ASP.NET Core Web API, ReactJS and SQL Server", "Xây dựng cổng dữ liệu số về lễ hội và tín ngưỡng truyền thống Việt Nam sử dụng công nghệ ASP.NET Core Web API, ReactJS và SQL Server"),
        ("SE_49", "Build LittleEdu - A Preschool Management System using React.js, ASP.NET RESTful API and PostgreSQL", "Xây dựng LittleEdu - Hệ Thống quản lý trường mầm non sử dụng công nghệ React js, Asp.Net RESTful API và PostgreSQL"),
        ("SE_50", "Building a Free Smart Online Learning System using React, Node.js and MySQL", "Xây dựng hệ thống học trực tuyến thông minh miễn phí sử dụng React, Node.js và MySQL"),
    ];

    // 40 Spring 2026 topics from sp26.xlsx
    private static readonly (string Code, string NameEn, string NameVi)[] Spring26Topics =
    [
        ("SP_01", "Build a system to provide mass transportation services, connecting pickup truck drivers to users, using ReactJS, NodeJs, ReactNative, MongoDB", "Xây dựng hệ thống cung cấp dịch vụ vận tải, kết nối tài xế đến người dùng, sử dụng ReactJS, NodeJs, ReactNative, MongoDB"),
        ("SP_02", "Building a website to manage rescues and volunteering work using REACT JS, NODE JS, MONGODB technology", "Xây dựng hệ thống quản lý cứu trợ và thiện nguyện sử dụng công nghệ REACT JS, NODE JS, MONGODB"),
        ("SP_03", "GearXpert - An Online Smart Platform for Personal Electronics Rental, Automated Maintenance, and Intelligent Management using ReactJS, NodeJS, and MongoDB", "GearXpert - Nền tảng cho thuê thiết bị điện tử cá nhân trực tuyến, quản lý bảo trì tự động sử dụng ReactJS, NodeJS và MongoDB"),
        ("SP_04", "Building a restaurant management website system using REACT, ASP.NET and SQL Server", "Xây dựng hệ thống website quản lý nhà hàng sử dụng REACT, ASP.NET và SQL Server"),
        ("SP_05", "FPT University Project Management System Using ReactJS, ASP.NET CORE WEB API, SQL Server, Firebase", "Hệ thống quản lý quá trình làm dự án tại Đại học FPT sử dụng công nghệ ReactJS, ASP.NET CORE WEB API, SQL Server, Firebase"),
        ("SP_06", "Building a system to support studying and practicing for Korean certificate exams for Vitamin Korean Language Center, using React, Spring Boot and MySQL", "Xây dựng hệ thống hỗ trợ học tập và ôn thi các chứng chỉ tiếng Hàn cho Trung tâm Hàn ngữ Vitamin, sử dụng React, Spring Boot và MySQL"),
        ("SP_07", "Developing TikoSmart - a frozen food warehouse management system for TIKOVIA Trading and Service Co., Ltd. using ReactJS, React Native and NodeJS", "Xây dựng TikoSmart - Hệ thống quản lý kho hàng thực phẩm đông lạnh cho Công ty TNHH Thương mại và Dịch vụ Tikovia sử dụng ReactJS, React Native và NodeJS"),
        ("SP_08", "Building a web platform for purchasing, exchanging and selling bicycles and bicycle accessories using Next.JS technology and Java Spring Boot, PostgreSQL", "Xây dựng nền tảng Web thu mua, trao đổi và bán xe đạp và phụ kiện xe đạp sử dụng công nghệ Next.JS và Java Spring Boot, PostgreSQL"),
        ("SP_09", "Building a website to promote and manage the Robotics, Chips and Emerging Technologies Lab of FPT University - Danang Campus, using React, Node.js (Express), and SQL Server", "Xây dựng website quảng bá và quản lý Phòng Lab về Rô-bốt, Chíp và Công nghệ mới nổi của Trường Đại học FPT cơ sở Đà Nẵng, sử dụng React, Node.js (Express) và SQL Server"),
        ("SP_10", "Develop FigiCore - A retail and operational management system for collectible models using React.js, Nest.js and PostgreSQL", "Xây dựng FigiCore - Hệ thống bán lẻ và quản lý vận hành mô hình sưu tầm sử dụng React.js, Nest.js và PostgreSQL"),
        ("SP_11", "Building a Household Furniture Moving Management System using ReactJS, NodeJS, and MongoDB", "Xây dựng Hệ thống Quản lý Vận chuyển Đồ đạc cho Hộ gia đình bằng ReactJS, NodeJS và MongoDB"),
        ("SP_12", "Building a Small and Medium Enterprise Resource Planning System using ReactJS, ASP.NET Core Web API and Microsoft SQL Server", "Xây dựng Hệ thống Quản lý Nguồn lực Doanh nghiệp vừa và nhỏ sử dụng ReactJS, ASP.NET Core Web API và Microsoft SQL Server"),
        ("SP_13", "Developing the EduConnect System - An AI-Integrated Educational Ecosystem for Learning, Testing, and Academic Discussion using VueJS, ASP.NET, and MySQL", "Xây dựng Hệ thống EduConnect - Hệ sinh thái học tập, kiểm tra và thảo luận học thuật tích hợp AI sử dụng VueJS, ASP.Net và MySQL"),
        ("SP_14", "Building Online interview practice support System using ASP.NET Core Web API, ReactJS, PostgreSQL", "Xây dựng Hệ thống hỗ trợ luyện tập phỏng vấn trực tuyến sử dụng ASP.NET Core Web API, ReactJS, PostgreSQL"),
        ("SP_15", "Petties: Veterinary Appointment Booking and AI-Powered Pet Disease Diagnosis System", "Petties: Hệ thống đặt lịch bác sĩ thú y và chẩn đoán bệnh bằng AI cho thú cưng"),
        ("SP_16", "GZMart: An AI-Powered E-Commerce and Mini-ERP Platform for Fashion Retailers Using ReactJS, NodeJS, and MongoDB", "GZMart: Nền tảng thương mại điện tử tích hợp Mini-ERP và Trí tuệ nhân tạo cho người bán đồ thời trang sử dụng ReactJS, NodeJs và MongoDB"),
        ("SP_17", "Building an RPG game using Unity with NPC interaction through AI and player support based on local data, utilizing Unity UI, ASP.NET APIs, and SQLServer technologies", "Xây dựng game RPG bằng Unity với tương tác NPC qua AI và hỗ trợ người chơi dựa trên dữ liệu cục bộ sử dụng công nghệ Unity UI, ASP.NET APIs, SQLServer"),
        ("SP_18", "Building a management system for driving training centers in Da Nang city using NextJS, .NET C#, SQLServer, and MongoDB technologies", "Xây dựng hệ thống quản lý các trung tâm đào tạo lái xe tại thành phố Đà Nẵng sử dụng công nghệ NextJS, .NET C#, SQLServer, MongoDB"),
        ("SP_19", "Online dual-mode roguelite game design with AI assistance and Photon Fusion 2", "Thiết kế trò chơi roguelite trực tuyến hai chế độ với sự hỗ trợ của AI và Photon Fusion 2"),
        ("SP_20", "Developing a Nutrition and Exercise Tracking Mobile Application Using React Native (Expo), Express.js, and MongoDB", "Xây dựng ứng dụng theo dõi chế độ dinh dưỡng và luyện tập sử dụng React Native (Expo), Express.js và MongoDB Atlas"),
        ("SP_21", "Building a Student Dormitory Management System at FPT University Danang using ReactJS, NodeJS and MongoDB", "Xây dựng Hệ thống Quản lý Ký túc xá Sinh viên tại Đại Học FPT Đà Nẵng sử dụng ReactJS, NodeJS và MongoDB"),
        ("SP_22", "Building an AI-integrated recruitment platform for CV analysis and job recommendations using .NET 8 Web API, ReactJS, and MongoDB", "Xây dựng nền tảng tuyển dụng tích hợp AI cho phân tích CV và gợi ý việc làm sử dụng công nghệ .NET 8 Web API, ReactJS, MongoDB"),
        ("SP_23", "Developing the RestX System: A Restaurant Business Management Platform Using ReactJS, ASP.NET Core, and SQL Server", "Xây dựng Hệ thống RestX hỗ trợ quản lý hoạt động kinh doanh cho các Nhà hàng sử dụng ReactJS, ASP.NET Core và SQL Server"),
        ("SP_24", "An AI-integrated sports field booking and management system for venue owners on web and mobile platforms", "Nền tảng đặt sân thể thao và quản lý cho chủ sân doanh nghiệp tích hợp AI trên nền tảng web và ứng dụng mobile"),
        ("SP_25", "DOCIMAL AI - AI Agent Chatbot and Automation Platform - SaaS Product using Next.js, NestJS, FastAPI Microservices and RAG-LLM Technology", "Nền tảng AI Agent Chatbot và tự động hoá quy trình - Sản phẩm SaaS sử dụng công nghệ NextJS, NestJS, FastAPI microservices và công nghệ RAG, LLM"),
        ("SP_26", "Building a Virtual Try-On Platform for Student Uniform E-Commerce with ASP.NET Core Web API, Razor Pages, and SQL Server", "Xây dựng hệ thống thử đồ ảo cho mua sắm đồng phục học sinh sử dụng công nghệ ASP.NET Core Web API, ASP.NET Razor Pages và SQL Server"),
        ("SP_27", "Building the StudySense - an AI-based system for learning style analysis and personalized self-study optimization using .NET, MySQL, Next.js", "Xây dựng StudySense - hệ thống phân tích thói quen học tập và tối ưu kế hoạch tự học cá nhân hóa bằng trí tuệ nhân tạo, sử dụng .NET, MySQL, Next.js"),
        ("SP_28", "ThemisOnlineJudge: Building a Web-based Online Programming Judge and Evaluation using NextJs, ASP.NET Core WebAPI and PostgreSQL", "ThemisOnlineJudge: Xây dựng hệ thống chấm bài làm lập trình trực tuyến sử dụng NextJs, ASP.NET Core WebAPI và PostgreSQL"),
        ("SP_29", "Building a Cultural Experience and Craft Village Tourism Ecosystem in Ngu Hanh Son Ward Using React, Spring Boot and MySQL", "Xây dựng Hệ sinh thái Du lịch Trải nghiệm Văn hóa - Làng nghề tại phường Ngũ Hành Sơn sử dụng công nghệ React, Spring Boot, MySQL"),
        ("SP_30", "Building a smart library ecosystem SmartLib with HCE application, smart booking, reputation score and AI analysis using Flutter, PostgreSQL, Spring Boot", "Xây dựng hệ sinh thái thư viện thông minh SmartLib ứng dụng HCE, đặt chỗ thông minh, điểm uy tín và phân tích AI sử dụng công nghệ Flutter, PostgreSQL, Spring Boot"),
        ("SP_31", "Academic Management System at FPT University using Spring Boot, ReactJS, Flutter, and Python along with AI", "Hệ thống Quản lý Học vụ tại Đại học FPT sử dụng công nghệ Spring Boot, ReactJS, Flutter, Python ứng dụng AI"),
        ("SP_32", "Developing an Intelligent Examination Room Management System Using NestJS and AI at FPT University", "Xây dựng hệ thống quản lý phòng thi thông minh sử dụng NestJS và AI tại FPT University"),
        ("SP_33", "Building a Personalized Travel Planning Platform using ReactJS, ASP.NET Core API, and PostgreSQL", "Xây dựng nền tảng website hỗ trợ lên kế hoạch du lịch cá nhân hóa sử dụng công nghệ ReactJS và ASP.NET Core API, PostgreSQL"),
        ("SP_34", "Developing a Web/App Platform to Support Interview Practice and Career Preparation by Industry using Next JS, .Net Core, PostgreSQL", "Xây dựng nền tảng Web/App hỗ trợ ôn luyện phỏng vấn và chuẩn bị nghề nghiệp theo ngành sử dụng Next JS, .Net Core, PostgreSQL"),
        ("SP_35", "Building a Intelligent School Management System using React JS, Tailwind, Spring Boot, and PostgreSQL", "Xây dựng hệ thống quản lý trường học thông minh sử dụng công nghệ React JS, Tailwind, Spring Boot và PostgreSQL"),
        ("SP_36", "Website to diagnose Hand, Foot and Mouth Disease in Young Children through Images and Symptoms using AI, Machine Learning, ASP.NET Core, RESTful API and SQL Server", "Website chẩn đoán bệnh Tay Chân Miệng ở trẻ nhỏ thông qua hình ảnh và triệu chứng sử dụng AI, Machine Learning, ASP.NET Core, RESTful API và SQL Server"),
        ("SP_37", "Building an Integrated Lab Management System with Scheduling and Usage Tracking for FPT University Da Nang using ASP.NET API, ReactJS, and PostgreSQL", "Xây dựng Hệ thống quản lý phòng Lab tích hợp đặt lịch và theo dõi sử dụng tại Đại học FPT Đà Nẵng sử dụng công nghệ ASP.NET API, ReactJS, PostgreSQL"),
        ("SP_38", "Real-time Flood Monitoring and Safe Route Suggestion System using NextJS, React Native, ASP.NET Core, PostgreSQL", "Hệ thống giám sát ngập lụt và gợi ý lộ trình an toàn sử dụng NextJS, React Native, ASP.NET Core, PostgreSQL"),
        ("SP_39", "Developing a Web Platform for Gym Management with Franchise and Shared Trainer Model using NodeJs and React", "Xây dựng nền tảng Web quản lý phòng Gym kết hợp mô hình nhượng quyền và chia sẻ huấn luyện viên sử dụng NodeJs và React"),
        ("SP_40", "Building a used car sales system in Da Nang city, using React, NextJS, Java Spring framework, SQL Server", "Xây dựng hệ thống bán xe ô tô đã qua sử dụng tại thành phố Đà Nẵng, sử dụng React, NextJS, Java Spring framework, SQL Server"),
    ];
}
