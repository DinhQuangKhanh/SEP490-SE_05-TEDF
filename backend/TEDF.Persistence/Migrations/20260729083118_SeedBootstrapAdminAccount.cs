using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <summary>
    /// Creates the bootstrap Admin account so a fresh deployment can be signed into at all.
    /// Without it there is no user row, every Firebase token is rejected, and there is no admin
    /// available to create the first one — a chicken-and-egg the app cannot resolve on its own.
    ///
    /// <para>
    /// The row is written with a placeholder <c>FirebaseUid</c> ("pending:"), not a real one:
    /// Google only issues the UID the first time the person signs in. On that first sign-in
    /// <c>OnTokenValidated</c> matches the row by verified email, re-points it to the real UID and
    /// pushes the role into Firebase custom claims — so no UID has to be copied by hand.
    /// </para>
    ///
    /// <para>Data-only migration: it changes no schema.</para>
    /// </summary>
    public partial class SeedBootstrapAdminAccount : Migration
    {
        // Fixed, not NEWID(): Down() must be able to find exactly this row, and re-running Up()
        // must not create a second admin.
        private const string AdminId = "A0000000-0000-0000-0000-000000000001";
        private const string AdminEmail = "khanhdqde170745@fpt.edu.vn";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded on both Id and Email: the account may already have been created by hand
            // before this migration existed, and Users.Email is unique.
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = '{AdminId}' OR [Email] = N'{AdminEmail}')
                BEGIN
                    INSERT INTO [Users]
                        ([Id], [Email], [FullName], [AvatarUrl], [PhoneNumber], [BirthDate],
                         [DepartmentId], [Status], [FirebaseUid], [CreatedAt], [UpdatedAt],
                         [LastLoginAt], [PrivacySettings])
                    VALUES
                        ('{AdminId}',
                         N'{AdminEmail}',
                         N'Đinh Quang Khánh',
                         NULL, NULL, NULL,
                         -- DepartmentId stays NULL: Departments has no seed data, so any fixed id
                         -- would break the foreign key on a fresh database.
                         NULL,
                         0,                       -- UserStatus.Active
                         N'pending:{AdminEmail}', -- replaced with the real UID on first sign-in
                         SYSUTCDATETIME(), NULL, NULL, NULL);
                END;");

            // RoleId 1 = Admin, seeded by RoleConfiguration.HasData, so it always exists by now.
            // Selected by email rather than by AdminId so it also attaches to a hand-created row.
            migrationBuilder.Sql($@"
                IF NOT EXISTS (
                    SELECT 1 FROM [UserRoles] ur
                    JOIN [Users] u ON u.[Id] = ur.[UserId]
                    WHERE u.[Email] = N'{AdminEmail}' AND ur.[RoleId] = 1)
                BEGIN
                    INSERT INTO [UserRoles] ([UserId], [RoleId], [AssignedAt], [AssignedBy], [IsActive])
                    SELECT u.[Id], 1, SYSUTCDATETIME(), NULL, 1
                    FROM [Users] u
                    WHERE u.[Email] = N'{AdminEmail}';
                END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes only the row this migration created (matched on its fixed Id). A row created
            // by hand under the same email is left alone.
            migrationBuilder.Sql($@"
                DELETE FROM [UserRoles] WHERE [UserId] = '{AdminId}';
                DELETE FROM [Users]     WHERE [Id]     = '{AdminId}';");
        }
    }
}
