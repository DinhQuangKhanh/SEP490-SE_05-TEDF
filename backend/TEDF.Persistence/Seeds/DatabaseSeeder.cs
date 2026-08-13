using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TEDF.Persistence.MongoDB;
using TEDF.Persistence.MongoDB.Indexes;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.Seeds;

/// <summary>
/// The two start-up recipes, one per environment. <c>InitializeDatabaseAsync</c> picks between them
/// and does nothing else, so "what happens on start-up" is answered by reading one method.
/// <para>
/// The recipes differ on purpose — Development owns a throwaway database, Production owns real data:
/// </para>
/// <list type="table">
///   <listheader><term>Step</term><description>Development / Production</description></listheader>
///   <item><term>Opt-in switch</term><description>always runs / only when <c>Seeding:RunOnStartup</c> is true</description></item>
///   <item><term>EF Core migrations</term><description>applied / never applied</description></item>
///   <item><term>Destructive reset switch</term><description>honoured / ignored</description></item>
///   <item><term>Firebase accounts</term><description>Auth Emulator only / never</description></item>
///   <item><term>On failure</term><description>throws (fail fast) / logged, API still starts</description></item>
/// </list>
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Config key (<c>Seeding__RunOnStartup</c> as an environment variable) that arms production
    /// seeding. Default false: with the switch off, a production start touches the database exactly
    /// as little as it did before seeding existed — which also makes it the recovery lever if a
    /// seeding run ever misbehaves. Turn it on, let one start seed, then turn it back off.
    /// </summary>
    public const string RunOnStartupKey = "Seeding:RunOnStartup";

    /// <summary>
    /// Development: apply pending migrations, seed the full dataset (the destructive
    /// <c>TEDF_RESET_LOADTEST_ON_STARTUP</c> switch is honoured here), create the matching Firebase
    /// Auth Emulator accounts when the emulator is on, then the checklists and Mongo indexes.
    /// <para>Failures propagate — a broken local database should be loud, not silent.</para>
    /// </summary>
    public static async Task SeedDevelopmentAsync(
        AppDbContext dbContext,
        MongoDbContext mongoContext,
        IConfiguration configuration,
        ILogger logger)
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("No pending EF Core migrations.");
        }
        else
        {
            logger.LogWarning("Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));
        }

        await dbContext.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations applied successfully.");

        await LoadTestDataSeeder.ResetIfRequestedAsync(dbContext, logger, allowDestructiveReset: true);

        if (await LoadTestDataSeeder.IsAlreadySeededAsync(dbContext))
            await LoadTestDataSeeder.BackfillProfileTablesAsync(dbContext, logger);
        else
            await LoadTestDataSeeder.SeedAllTablesAsync(dbContext, logger);

        // Sign-in accounts for the seeded users live in the Auth Emulator. Never run this against a
        // real Firebase project: it would create ~1000 usable accounts sharing one password that is
        // published in the source.
        if (configuration.GetValue<bool>("Firebase:UseEmulator"))
            await FirebaseEmulatorSeeder.SeedAsync(logger);

        await EvaluationChecklistSeeder.SeedAsync(dbContext, logger);
        await MongoIndexConfiguration.CreateIndexesAsync(mongoContext);

        logger.LogInformation("Development database initialization complete.");
    }

    /// <summary>
    /// Production: seed the same dataset, but only when <see cref="RunOnStartupKey"/> is on, without
    /// applying migrations, without honouring the destructive reset switch, and without creating any
    /// Firebase account.
    /// <para>
    /// Never throws. Seeding demo data is not a reason to refuse to start: a crash loop here takes
    /// the whole site down, which is strictly worse than a database missing its sample rows.
    /// </para>
    /// </summary>
    public static async Task SeedProductionAsync(
        AppDbContext dbContext,
        MongoDbContext mongoContext,
        IConfiguration configuration,
        ILogger logger)
    {
        if (!configuration.GetValue<bool>(RunOnStartupKey))
        {
            logger.LogInformation(
                "Seeding is off ({Key} is false) — starting without touching the database. " +
                "Set Seeding__RunOnStartup=true for one start to seed, then turn it back off.",
                RunOnStartupKey);
            return;
        }

        logger.LogWarning(
            "{Key} is ON: seeding a non-Development database. Migrations are NOT applied and no " +
            "Firebase account is created; the seeded users therefore cannot sign in.",
            RunOnStartupKey);

        try
        {
            if (!await LoadTestDataSeeder.IsSchemaReadyAsync(dbContext, logger))
                return;

            if (await LoadTestDataSeeder.IsAlreadySeededAsync(dbContext))
            {
                logger.LogInformation("Already seeded — nothing to do.");
                await LoadTestDataSeeder.BackfillProfileTablesAsync(dbContext, logger);
            }
            else
            {
                await LoadTestDataSeeder.SeedProductionTablesAsync(dbContext, logger);
            }

            await EvaluationChecklistSeeder.SeedAsync(dbContext, logger);
            await MongoIndexConfiguration.CreateIndexesAsync(mongoContext);

            logger.LogInformation("Production seeding complete. Turn {Key} back off.", RunOnStartupKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Production seeding FAILED. The API is starting anyway; the database is unchanged " +
                "beyond whatever had already been committed. Fix the cause, or set {Key}=false.",
                RunOnStartupKey);
        }
    }

}
