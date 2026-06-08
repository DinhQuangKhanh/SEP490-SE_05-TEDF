using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;

namespace TEDF.Persistence.Seeds;

/// <summary>
/// Creates matching user accounts in the Firebase Auth Emulator so that
/// every user seeded by <see cref="LoadTestDataSeeder"/> can actually sign in.
/// Also sets Firebase custom claims (dbUserId, roles) so the JWT token contains role info.
/// <para>
/// Requires <c>FIREBASE_AUTH_EMULATOR_HOST</c> to be set (done by Infrastructure DI).
/// Idempotent — silently skips users that already exist.
/// </para>
/// </summary>
public static class FirebaseEmulatorSeeder
{
    private const int ConcurrencyLimit = 20; // Avoid overwhelming the emulator

    public static async Task SeedAsync(ILogger? logger = null)
    {
        var auth = FirebaseAuth.DefaultInstance;
        if (auth == null)
        {
            logger?.LogWarning("FirebaseAuth not initialized. Skipping emulator user seeding.");
            return;
        }

        logger?.LogInformation("Seeding Firebase Emulator users...");

        var users = BuildUserList();
        var created = 0;
        var skipped = 0;

        // Process in batches to limit concurrency
        for (var i = 0; i < users.Count; i += ConcurrencyLimit)
        {
            var batch = users.Skip(i).Take(ConcurrencyLimit);
            var tasks = batch.Select(async u =>
            {
                var isNew = false;
                try
                {
                    await auth.CreateUserAsync(new UserRecordArgs
                    {
                        Uid = u.FirebaseUid,
                        Email = u.Email,
                        Password = LoadTestDataSeeder.DefaultPassword,
                        DisplayName = u.DisplayName,
                        EmailVerified = true
                    });
                    isNew = true;
                    Interlocked.Increment(ref created);
                }
                catch (FirebaseAuthException ex) when (ex.Message.Contains("DUPLICATE_LOCAL_ID") ||
                                                        ex.Message.Contains("EMAIL_EXISTS") ||
                                                        ex.AuthErrorCode == AuthErrorCode.UidAlreadyExists ||
                                                        ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
                {
                    Interlocked.Increment(ref skipped);
                }

                // Set custom claims (roles + dbUserId) regardless of whether user is new or existing
                try
                {
                    var claims = new Dictionary<string, object>
                    {
                        ["dbUserId"] = u.DbUserId,
                        ["roles"] = u.Roles
                    };
                    await auth.SetCustomUserClaimsAsync(u.FirebaseUid, claims);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex,
                        "Failed to set custom claims for {Email} ({FirebaseUid}). User {Status}.",
                        u.Email, u.FirebaseUid, isNew ? "was created" : "already existed");
                }
            });

            await Task.WhenAll(tasks);
        }

        logger?.LogInformation(
            "Firebase Emulator seeding complete: {Created} created, {Skipped} already existed. Custom claims set for all.",
            created, skipped);
    }

    private static List<(string FirebaseUid, string Email, string DisplayName, string DbUserId, string[] Roles)> BuildUserList()
    {
        var list = new List<(string, string, string, string, string[])>();

        for (var i = 1; i <= LoadTestDataSeeder.SeededAdminCount; i++) // Admins
            list.Add((
                LoadTestDataSeeder.AdminFirebaseUid(i),
                LoadTestDataSeeder.AdminEmail(i),
                $"Admin LoadTest {i}",
                LoadTestDataSeeder.AdminId(i).ToString(),
                new[] { "Admin" }
            ));

        for (var i = 1; i <= LoadTestDataSeeder.SeededDualRoleCount; i++) // DualRole (Mentor + Evaluator), lecturer 1 is also DepartmentHead
        {
            var roles = i == 1
                ? new[] { "Mentor", "Evaluator", "DepartmentHead" }
                : new[] { "Mentor", "Evaluator" };

            list.Add((
                LoadTestDataSeeder.DualRoleFirebaseUid(i),
                LoadTestDataSeeder.DualRoleEmail(i),
                $"Lecturer LoadTest {i}",
                LoadTestDataSeeder.DualRoleId(i).ToString(),
                roles
            ));
        }

        for (var i = 1; i <= LoadTestDataSeeder.SeededStudentCount; i++) // Students
            list.Add((
                LoadTestDataSeeder.StudentFirebaseUid(i),
                LoadTestDataSeeder.StudentEmail(i),
                $"Student LoadTest {i}",
                LoadTestDataSeeder.StudentId(i).ToString(),
                new[] { "Student" }
            ));

        // Real Summer 2026 students (from the registered thesis-topic list)
        list.AddRange(LoadTestDataSeeder.GetSummer26RealUserAccounts());

        return list;
    }
}
