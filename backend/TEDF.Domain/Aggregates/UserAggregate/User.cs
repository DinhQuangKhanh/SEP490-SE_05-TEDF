using TEDF.Domain.Aggregates.UserAggregate.Entities;
using TEDF.Domain.Aggregates.UserAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate.ValueObjects;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.User;

namespace TEDF.Domain.Aggregates.UserAggregate
{
    /// <summary>
    /// User aggregate root representing a system user authenticated via Firebase.
    /// </summary>
    public class User : AggregateRoot<Guid>
    {
        public Email Email { get; private set; } = null!;
        public string FullName { get; private set; } = string.Empty;
        public string? AvatarUrl { get; private set; }
        public string? StudentCode { get; private set; }
        public string? EmployeeCode { get; private set; }
        public string? PhoneNumber { get; private set; }
        public DateOnly? BirthDate { get; private set; }
        public string? AcademicTitle { get; private set; }
        public int? DepartmentId { get; private set; }
        /// <summary>Chuyên ngành (Major) the user studies — set for students.</summary>
        public int? MajorId { get; private set; }
        /// <summary>Chuyên ngành hẹp (MajorProgram) the student studies.</summary>
        public int? MajorProgramId { get; private set; }
        /// <summary>Bộ môn đang giảng dạy (SE/CF/AI/IA/IC) — set for lecturers. Display-only snapshot, mirrors EligibleMentor.Division.</summary>
        public string? Division { get; private set; }
        public string? PrivacySettings { get; private set; }
        public UserStatus Status { get; private set; }
        public string FirebaseUid { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        private readonly List<UserRole> _roles = [];
        public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

        private User() { }

        /// <summary>
        /// Creates a new User aggregate.
        /// </summary>
        public static User Create(
            string firebaseUid,
            string email,
            string fullName,
            string? avatarUrl = null,
            string? studentCode = null,
            string? employeeCode = null,
            string? academicTitle = null,
            int? departmentId = null,
            string? phoneNumber = null,
            DateOnly? birthDate = null)
        {
            var emailValueObject = Email.Create(email);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirebaseUid = firebaseUid,
                Email = emailValueObject,
                FullName = fullName,
                AvatarUrl = avatarUrl,
                StudentCode = studentCode,
                EmployeeCode = employeeCode,
                PhoneNumber = phoneNumber,
                BirthDate = birthDate,
                AcademicTitle = academicTitle,
                DepartmentId = departmentId,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email, fullName, firebaseUid));

            return user;
        }

        /// <summary>
        /// Records a user login event.
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Liên kết tài khoản "chờ" (tạo lúc import) với FirebaseUid thật do Google cấp khi đăng nhập lần đầu.
        /// </summary>
        public void LinkFirebaseAccount(string firebaseUid)
        {
            if (string.IsNullOrWhiteSpace(firebaseUid))
                throw new ArgumentException("FirebaseUid không hợp lệ.", nameof(firebaseUid));

            FirebaseUid = firebaseUid;
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates user profile information.
        /// </summary>
        public void UpdateProfile(
            string? fullName = null,
            string? avatarUrl = null,
            string? studentCode = null,
            string? employeeCode = null,
            string? academicTitle = null,
            int? departmentId = null,
            string? phoneNumber = null,
            DateOnly? birthDate = null,
            string? privacySettings = null)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
                FullName = fullName;

            if (avatarUrl != null)
                AvatarUrl = avatarUrl;

            if (studentCode != null)
                StudentCode = studentCode;

            if (employeeCode != null)
                EmployeeCode = employeeCode;

            if (academicTitle != null)
                AcademicTitle = academicTitle;

            if (departmentId.HasValue)
                DepartmentId = departmentId;

            if (!string.IsNullOrWhiteSpace(phoneNumber))
                PhoneNumber = phoneNumber;

            if (birthDate.HasValue)
                BirthDate = birthDate;

            if (privacySettings != null)
                PrivacySettings = privacySettings;

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Assigns a role to the user.
        /// </summary>
        public void AssignRole(string roleName, Guid? assignedBy = null)
        {
            if (_roles.Any(r => r.RoleName == roleName && r.IsActive))
                return;

            var existingRole = _roles.FirstOrDefault(r => r.RoleName == roleName);
            if (existingRole != null)
            {
                existingRole.Reactivate();
            }
            else
            {
                var role = UserRole.Create(Id, roleName, assignedBy);
                _roles.Add(role);
            }

            RaiseDomainEvent(new UserRoleAssignedEvent(Id, roleName, assignedBy));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a role from the user.
        /// </summary>
        public void RemoveRole(string roleName)
        {
            var role = _roles.FirstOrDefault(r => r.RoleName == roleName && r.IsActive);
            if (role != null)
            {
                role.Deactivate();
                UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Checks if the user has a specific role.
        /// </summary>
        public bool HasRole(string roleName)
        {
            return _roles.Any(r => r.RoleName == roleName && r.IsActive);
        }

        /// <summary>
        /// Gets all active role names for this user.
        /// </summary>
        public IEnumerable<string> GetActiveRoles()
        {
            return _roles.Where(r => r.IsActive).Select(r => r.RoleName);
        }

        /// <summary>
        /// Activates the user account.
        /// </summary>
        public void Activate()
        {
            Status = UserStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Locks the user account.
        /// </summary>
        public void Lock()
        {
            Status = UserStatus.Locked;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the user account.
        /// </summary>
        public void Deactivate()
        {
            Status = UserStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Tiền tố FirebaseUid tạm cho tài khoản tạo lúc import, chờ liên kết khi đăng nhập lần đầu.</summary>
        public const string PendingUidPrefix = "pending:";

        /// <summary>Tài khoản được tạo lúc import nhưng chưa đăng nhập Google lần đầu (chưa có FirebaseUid thật).</summary>
        public bool IsPendingActivation => FirebaseUid.StartsWith(PendingUidPrefix, StringComparison.Ordinal);

        /// <summary>
        /// Checks if the user is a student.
        /// </summary>
        public bool IsStudent => !string.IsNullOrEmpty(StudentCode);

        /// <summary>
        /// Checks if the user is a staff member (mentor/admin).
        /// </summary>
        public bool IsStaff => !string.IsNullOrEmpty(EmployeeCode);

        /// <summary>
        /// Gets the display name (with academic title if available).
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(AcademicTitle)
            ? FullName
            : $"{AcademicTitle} {FullName}";
    }
}
