using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Common.Exceptions;

namespace TEDF.Application.Features.Settings.Commands.SendTestEmail;

public class SendTestEmailCommandHandler : ICommandHandler<SendTestEmailCommand>
{
    private readonly IEmailSender _emailSender;
    private readonly ICurrentUserService _currentUser;

    public SendTestEmailCommandHandler(IEmailSender emailSender, ICurrentUserService currentUser)
    {
        _emailSender = emailSender;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUser.Email;
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleValidationException("Tài khoản hiện tại không có địa chỉ email để gửi thử.");

        const string subject = "TEDF — Email kiểm tra cấu hình";
        var body = $"<p>Xin chào {_currentUser.FullName ?? "Quản trị viên"},</p>"
                 + "<p>Đây là email kiểm tra được gửi từ trang Cấu hình hệ thống của TEDF. "
                 + "Nếu bạn nhận được email này, cấu hình gửi email đang hoạt động bình thường.</p>"
                 + $"<p><small>Thời điểm gửi: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</small></p>";

        await _emailSender.SendAsync(email, subject, body, cancellationToken);
        return Unit.Value;
    }
}
