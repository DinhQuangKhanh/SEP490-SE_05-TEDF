/**
 * seed-firebase-auth.js
 * ─────────────────────────────────────────────────────────
 * Creates ALL test users in the Firebase Auth Emulator that
 * match what LoadTestDataSeeder.cs inserts into SQL Server.
 *
 * Distribution (mirrors C# seeder):
 *   250 Admins     – test-admin-0001 … test-admin-0250
 *   250 Lecturers  – test-lecturer-0001 … test-lecturer-0250
 *   510 Students   – test-student-0001 … test-student-0510
 *    62 Real Summer 2026 students (by roll number)
 *
 * Default password for ALL accounts: Test@123456
 *
 * Usage:
 *   node seed-firebase-auth.js
 *
 * The Firebase Auth Emulator must be running on 127.0.0.1:9099.
 * ─────────────────────────────────────────────────────────
 */

const admin = require('firebase-admin');
const { getAuth } = require('firebase-admin/auth');

process.env.FIREBASE_AUTH_EMULATOR_HOST = '127.0.0.1:9099';
admin.initializeApp({ projectId: 'unithesis-38c38' });

const auth = getAuth();
const DEFAULT_PASSWORD = 'Test@123456'; // NOSONAR: This is a dummy password for local emulator testing only

// ─── ID / naming helpers (must match LoadTestDataSeeder.cs) ───

function pad(n, len) { return String(n).padStart(len, '0'); }

function adminUid(i)    { return `test-admin-${pad(i, 4)}`; }
function adminEmail(i)  { return `admin${i}@fpt.edu.vn`; }

function lecturerUid(i)   { return `test-lecturer-${pad(i, 4)}`; }
function lecturerEmail(i) { return `lecturer${i}@fpt.edu.vn`; }

function studentUid(i)    { return `test-student-${pad(i, 4)}`; }
function studentEmail(i)  { return `student${i}@fpt.edu.vn`; }

function realStudentUid(roll)   { return `test-realstudent-${roll.toLowerCase()}`; }
function realStudentEmail(roll) { return `${roll.toLowerCase()}@fpt.edu.vn`; }

// ─── Real Summer 2026 students (mirrors C# Summer26RealGroups) ───

const realStudents = [
    // SE_01
    { roll: 'DE180484', name: 'Huỳnh Trần Văn Trọng' },
    { roll: 'DE180650', name: 'Nguyễn Văn Việt Hưng' },
    { roll: 'DE170287', name: 'Lê Quốc Ân' },
    { roll: 'DE181079', name: 'Lê Nguyên Hưng' },
    { roll: 'DE180881', name: 'Nguyễn Thành Sơn' },
    // SE_02
    { roll: 'DE170169', name: 'Lê Đức Minh' },
    { roll: 'DE180661', name: 'Đinh Bảo Hân' },
    { roll: 'DE170086', name: 'Nguyễn Thạc Tiến Dũng' },
    { roll: 'HE171706', name: 'Nguyễn Khánh Duy' },
    { roll: 'DE170488', name: 'Đặng Quang Huy' },
    // SE_03
    { roll: 'DE180972', name: 'Nguyễn Đức Trí' },
    { roll: 'DE170739', name: 'Nguyễn Hồ Bảo Khang' },
    { roll: 'DE180924', name: 'Ngô Anh Quân' },
    { roll: 'DE180524', name: 'Võ Xuân Thanh' },
    { roll: 'DE180896', name: 'Đoàn Nam Sơn' },
    // SE_04
    { roll: 'DE180074', name: 'Phạm Nguyễn Nam Khánh' },
    { roll: 'DE180395', name: 'Nguyễn Đức Tài' },
    { roll: 'DE180364', name: 'Nguyễn Lâm Hải' },
    { roll: 'DE180362', name: 'Nguyễn Văn Huân' },
    { roll: 'DE180411', name: 'Trịnh Quốc Trung' },
    // SE_05
    { roll: 'DE170745', name: 'Đinh Quang Khánh' },
    { roll: 'DE170559', name: 'Ngô Dương Hoàng Châu' },
    { roll: 'DE180791', name: 'Phan Xuân Hoàng' },
    { roll: 'DE170328', name: 'Trần Nguyễn Anh Hào' },
    { roll: 'DE170278', name: 'Phạm Tuấn Kiệt' },
    // SE_06
    { roll: 'DE180625', name: 'Nguyễn Xuân Linh' },
    { roll: 'DE180701', name: 'Võ Tuấn Kiệt' },
    { roll: 'DE170043', name: 'Nguyễn Phi Hùng' },
    { roll: 'DE170026', name: 'Lương Đình Quỳnh' },
    { roll: 'DE180745', name: 'Phạm Toàn Bách' },
    // SE_07
    { roll: 'DE181046', name: 'Nguyễn Nhật Minh' },
    { roll: 'DE170684', name: 'Nguyễn Trọng Trí' },
    { roll: 'DE180679', name: 'Trịnh Minh Hải' },
    { roll: 'DE180808', name: 'Tán Quang Triển' },
    { roll: 'DE180740', name: 'Nguyễn Trương Hoàng Vũ' },
    // SE_08
    { roll: 'DE170021', name: 'Đào Lưu Đức Sơn' },
    { roll: 'DE180343', name: 'Phan Đức Mạnh' },
    { roll: 'DE180611', name: 'Trần Lương Bình' },
    { roll: 'DE180492', name: 'Lê Văn Thiện' },
    { roll: 'DS180213', name: 'Đỗ Thị Thu Ngân' },
    // SE_09
    { roll: 'DE170319', name: 'Huỳnh Lê Đức Thọ' },
    { roll: 'DE180438', name: 'Trần Lê Trung Hiếu' },
    { roll: 'DE180554', name: 'Đỗ Phương Ánh' },
    { roll: 'DE160630', name: 'Trần Quốc Khánh' },
    { roll: 'DE180356', name: 'Trần Phước Huy' },
    // SE_10
    { roll: 'DE180468', name: 'Đoàn Xuân Sơn' },
    { roll: 'DE180848', name: 'Trần Duy Khang' },
    { roll: 'DE180378', name: 'Dương Công Minh' },
    { roll: 'DE181082', name: 'Nguyễn Viết Nguyên' },
    { roll: 'HE170231', name: 'Dương Quý Lợi' },
    // SE_11
    { roll: 'DE170549', name: 'Huỳnh Văn Minh' },
    { roll: 'DE170052', name: 'Nguyễn Đức Mạnh' },
    { roll: 'DE170438', name: 'Trần Thị Phương Hà' },
    { roll: 'DE170398', name: 'Ngô Văn Thuận' },
    { roll: 'DE170445', name: 'Nguyễn Ngọc Tuấn Hoàng' },
    // SE_12
    { roll: 'DE160257', name: 'Nguyễn Văn Tân' },
    { roll: 'DE180349', name: 'Phạm Đăng Phát' },
    { roll: 'DE180165', name: 'Trịnh Quang Tâm' },
    { roll: 'DE160156', name: 'Trần Nhân Chánh' },
    // SE_13
    { roll: 'HE153552', name: 'Nguyễn Phan Anh Minh' },
    { roll: 'DE180741', name: 'Đinh Hải Quân' },
    { roll: 'DE170780', name: 'Trần Quang Dũng' },
    { roll: 'DE170152', name: 'Nguyễn Anh Kiệt' },
];

// ─── Build the full user list ─────────────────────────────

const ADMIN_COUNT    = 250;
const LECTURER_COUNT = 250;
const STUDENT_COUNT  = 510;

function buildAllUsers() {
    const users = [];

    const addUsers = (count, uidFn, emailFn, namePrefix) => {
        for (let i = 1; i <= count; i++) {
            users.push({
                uid: uidFn(i),
                email: emailFn(i),
                password: DEFAULT_PASSWORD,
                displayName: `${namePrefix} ${i}`,
            });
        }
    };

    addUsers(ADMIN_COUNT, adminUid, adminEmail, 'Admin LoadTest');
    addUsers(LECTURER_COUNT, lecturerUid, lecturerEmail, 'Lecturer LoadTest');
    addUsers(STUDENT_COUNT, studentUid, studentEmail, 'Student LoadTest');

    for (const s of realStudents) {
        users.push({
            uid: realStudentUid(s.roll),
            email: realStudentEmail(s.roll),
            password: DEFAULT_PASSWORD,
            displayName: s.name,
        });
    }

    return users;
}

// ─── Seed with concurrency control ────────────────────────

async function seed() {
    const users = buildAllUsers();
    const total = users.length;
    let created = 0;
    let skipped = 0;
    let errors  = 0;

    console.log(`Seeding ${total} users into Firebase Auth Emulator...`);
    console.log(`Password for all accounts: ${DEFAULT_PASSWORD}`);
    console.log('');

    // Process in batches of 20 to avoid overwhelming the emulator
    const BATCH = 20;
    for (let i = 0; i < users.length; i += BATCH) {
        const batch = users.slice(i, i + BATCH);
        const results = await Promise.allSettled(
            batch.map(u => auth.createUser(u))
        );

        for (let j = 0; j < results.length; j++) {
            const r = results[j];
            if (r.status === 'fulfilled') {
                created++;
            } else {
                const code = r.reason?.code;
                if (code === 'auth/uid-already-exists' || code === 'auth/email-already-exists') {
                    skipped++;
                } else {
                    errors++;
                    console.error(`  Error: ${batch[j].email} – ${r.reason?.message || r.reason}`);
                }
            }
        }

        // Progress log every 100 users
        const done = Math.min(i + BATCH, total);
        if (done % 100 === 0 || done === total) {
            console.log(`  Progress: ${done}/${total}`);
        }
    }

    console.log('');
    console.log('═══════════════════════════════════════');
    console.log(`  Created : ${created}`);
    console.log(`  Skipped : ${skipped} (already existed)`);
    console.log(`  Errors  : ${errors}`);
    console.log('═══════════════════════════════════════');
    console.log('');
    console.log('Sample login accounts:');
    console.log('  Admin    : admin1@fpt.edu.vn');
    console.log('  Lecturer : lecturer1@fpt.edu.vn');
    console.log('  Student  : student1@fpt.edu.vn');
    console.log('  Real SE_05 (Đinh Quang Khánh): de170745@fpt.edu.vn');
    console.log(`  Password : ${DEFAULT_PASSWORD}`);
    console.log('');

    process.exit(errors > 0 ? 1 : 0);
}

seed();
