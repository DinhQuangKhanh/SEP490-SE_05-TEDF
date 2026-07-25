using TEDF.Domain.Aggregates.UserAggregate.Entities;
using TEDF.Domain.Aggregates.UserAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate.ValueObjects;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.User;

namespace TEDF.Domain.Aggregates.UserAggregate
{
    public class User : AggregateRoot<Guid>
    {
        public Email Email { get; private set; } = null!;
        public string FullName { get; private set; } = string.Empty;
        public string? AvatarUrl { get; private set; }
        public string? PhoneNumber { get; private set; }
        public DateOnly? BirthDate { get; private set; }
        public int? DepartmentId { get; private set; }
        public string? PrivacySettings { get; private set; }
        public UserStatus Status { get; private set; }
        public string FirebaseUid { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        public Student? Student { get; private set; }
        public Lecturer? Lecturer { get; private set; }

        private readonly List<UserRole> _roles = [];
        public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

        private User() { }

        public static User Create(
            string firebaseUid,
            string email,
            string fullName,
            string? avatarUrl = null,
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
                PhoneNumber = phoneNumber,
                BirthDate = birthDate,
                DepartmentId = departmentId,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email, fullName, firebaseUid));

            return user;
        }

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

        public void UpdateProfile(
            string? fullName = null,
            string? avatarUrl = null,
            int? departmentId = null,
            string? phoneNumber = null,
            DateOnly? birthDate = null,
            string? privacySettings = null)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
                FullName = fullName;

            if (avatarUrl != null)
                AvatarUrl = avatarUrl;

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
        /// Assigns a role to the user by roleId. roleName is used for the domain event only.
        /// Requires UserRole.Role navigation to be loaded for HasRole/GetActiveRoles to work.
        /// </summary>
        public void AssignRole(int roleId, string roleName, Guid? assignedBy = null)
        {
            if (_roles.Any(r => r.RoleId == roleId && r.IsActive))
                return;

            var existingRole = _roles.FirstOrDefault(r => r.RoleId == roleId);
            if (existingRole != null)
                existingRole.Reactivate();
            else
                _roles.Add(UserRole.Create(Id, roleId, assignedBy));

            RaiseDomainEvent(new UserRoleAssignedEvent(Id, roleName, assignedBy));
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveRole(int roleId)
        {
            var role = _roles.FirstOrDefault(r => r.RoleId == roleId && r.IsActive);
            if (role != null)
            {
                role.Deactivate();
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void InitializeStudentProfile(string studentCode)
        {
            Student = TEDF.Domain.Entities.Student.Create(Id, studentCode);
        }

        public void InitializeStaffProfile(string employeeCode, string? academicTitle = null)
        {
            Lecturer = TEDF.Domain.Entities.Lecturer.Create(Id, employeeCode, academicTitle);
        }

        /// <summary>Requires UserRole.Role navigation to be eagerly loaded.</summary>
        public bool HasRole(string roleName)
        {
            return _roles.Any(r => r.RoleName == roleName && r.IsActive);
        }

        public bool HasRole(int roleId)
        {
            return _roles.Any(r => r.RoleId == roleId && r.IsActive);
        }

        /// <summary>Requires UserRole.Role navigation to be eagerly loaded.</summary>
        public IEnumerable<string> GetActiveRoles()
        {
            return _roles.Where(r => r.IsActive).Select(r => r.RoleName);
        }

        public void Activate()
        {
            Status = UserStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Lock()
        {
            Status = UserStatus.Locked;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            Status = UserStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;
        }

        public const string PendingUidPrefix = "pending:";

        public bool IsPendingActivation => FirebaseUid.StartsWith(PendingUidPrefix, StringComparison.Ordinal);
    }
}
