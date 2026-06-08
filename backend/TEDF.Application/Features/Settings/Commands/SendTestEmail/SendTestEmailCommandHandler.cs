using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Settings.Commands.SendTestEmail;

public class SendTestEmailCommandHandler : ICommandHandler<SendTestEmailCommand>
{
    private readonly ISettingsDomainService _settings;
    private readonly ICurrentUserService _currentUser;

    public SendTestEmailCommandHandler(ISettingsDomainService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUser.Email;
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleValidationException("Tài khoản hiện tại không có địa chỉ email để gửi thử.");

        await _settings.SendTestEmailAsync(email, _currentUser.FullName, cancellationToken);
        return Unit.Value;
    }
}
