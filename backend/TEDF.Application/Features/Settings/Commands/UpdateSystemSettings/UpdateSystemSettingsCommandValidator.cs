using FluentValidation;
using TEDF.Application.Common;

namespace TEDF.Application.Features.Settings.Commands.UpdateSystemSettings;

public class UpdateSystemSettingsCommandValidator : AbstractValidator<UpdateSystemSettingsCommand>
{
    // Keys whose value must be an integer within an inclusive range.
    private static readonly Dictionary<string, (int Min, int Max)> IntRanges = new()
    {
        [SettingKeys.MaxGroupMembers] = (1, 20),
        [SettingKeys.MaxTopicsPerMentor] = (1, 100),
    };

    private static readonly HashSet<string> BoolKeys =
    [
        SettingKeys.RequireOutlineApproval,
        SettingKeys.MaintenanceMode,
        SettingKeys.EmailOnEvaluationResult,
        SettingKeys.NotifyMentorOnRegistration,
        SettingKeys.EmailOnGroupMembership,
        SettingKeys.EmailOnSupportTicket,
    ];

    public UpdateSystemSettingsCommandValidator()
    {
        RuleFor(x => x.Settings).NotNull();

        RuleFor(x => x.Settings).Custom((settings, ctx) =>
        {
            if (settings is null) return;

            foreach (var (key, value) in settings)
            {
                if (IntRanges.TryGetValue(key, out var range))
                {
                    if (!int.TryParse(value, out var n) || n < range.Min || n > range.Max)
                        ctx.AddFailure(key, $"'{key}' phải là số nguyên trong khoảng {range.Min}-{range.Max}.");
                }
                else if (BoolKeys.Contains(key))
                {
                    if (!bool.TryParse(value, out _))
                        ctx.AddFailure(key, $"'{key}' phải là true hoặc false.");
                }
                else if (key == SettingKeys.PrimaryColor)
                {
                    if (string.IsNullOrWhiteSpace(value) || !System.Text.RegularExpressions.Regex.IsMatch(value, "^#([0-9a-fA-F]{6})$"))
                        ctx.AddFailure(key, "Màu chủ đạo phải có dạng hex #RRGGBB.");
                }
                else if (key == SettingKeys.HeaderName)
                {
                    if (string.IsNullOrWhiteSpace(value) || value.Length > 50)
                        ctx.AddFailure(key, "Tên hiển thị không được rỗng và tối đa 50 ký tự.");
                }
            }
        });
    }
}
