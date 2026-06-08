namespace TEDF.Infrastructure.Services.Email.Templates
{
    public static class EmailTemplates
    {
        public const string ProjectSubmitted = "ProjectSubmitted";
        public const string ProjectApproved = "ProjectApproved";
        public const string ProjectRejected = "ProjectRejected";
        public const string ProjectNeedsModification = "ProjectNeedsModification";
        public const string EvaluationAssigned = "EvaluationAssigned";
        public const string MeetingRequested = "MeetingRequested";
        public const string MeetingApproved = "MeetingApproved";
        public const string DefenseScheduled = "DefenseScheduled";
        public const string TopicExpiring = "TopicExpiring";
        public const string PasswordReset = "PasswordReset";
        public const string WelcomeEmail = "WelcomeEmail";

        /// <summary>
        /// Lazily-initialized, thread-safe singleton dictionary. Avoids re-creating on every call.
        /// </summary>
        private static readonly Lazy<IReadOnlyDictionary<string, string>> AllTemplates = new(() => new Dictionary<string, string>
        {
            [ProjectSubmitted] = GetProjectSubmittedTemplate(),
            [ProjectApproved] = GetProjectApprovedTemplate(),
            [ProjectRejected] = GetProjectRejectedTemplate(),
            [ProjectNeedsModification] = GetProjectNeedsModificationTemplate(),
            [EvaluationAssigned] = GetEvaluationAssignedTemplate(),
            [MeetingRequested] = GetMeetingRequestedTemplate(),
            [MeetingApproved] = GetMeetingApprovedTemplate(),
            [DefenseScheduled] = GetDefenseScheduledTemplate(),
            [TopicExpiring] = GetTopicExpiringTemplate(),
            [PasswordReset] = GetPasswordResetTemplate(),
            [WelcomeEmail] = GetWelcomeEmailTemplate()
        });

        /// <summary>
        /// Returns the cached template dictionary (created once, reused forever).
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetAllTemplates() => AllTemplates.Value;

        private static string GetBaseTemplate(string title, string content) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f57c00; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .btn {{ display: inline-block; padding: 10px 20px; background: #f57c00; color: white; text-decoration: none; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>{title}</h1></div>
        <div class='content'>{content}</div>
        <div class='footer'>
            <p>TEDF - Hệ thống quản lý đồ án tốt nghiệp</p>
            <p>FPT University Đà Nẵng</p>
        </div>
    </div>
</body>
</html>";

        private static string GetProjectSubmittedTemplate() => GetBaseTemplate(
            "Đề tài đã được nộp",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Đề tài <strong>{{ProjectName}}</strong> (Mã: {{ProjectCode}}) đã được nộp thành công vào lúc {{SubmittedAt}}.</p>
        <p>Đề tài của bạn đang chờ được thẩm định. Bạn sẽ nhận được thông báo khi có kết quả.</p>
        <p><a href='{{ProjectUrl}}' class='btn'>Xem chi tiết đề tài</a></p>");

        private static string GetProjectApprovedTemplate() => GetBaseTemplate(
            "Đề tài đã được duyệt",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Chúc mừng! Đề tài <strong>{{ProjectName}}</strong> (Mã: {{ProjectCode}}) đã được <strong>DUYỆT</strong>.</p>
        <p>Bạn có thể bắt đầu thực hiện đề tài ngay bây giờ.</p>
        <p><a href='{{ProjectUrl}}' class='btn'>Xem chi tiết đề tài</a></p>");

        private static string GetProjectRejectedTemplate() => GetBaseTemplate(
            "Đề tài bị từ chối",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Rất tiếc, đề tài <strong>{{ProjectName}}</strong> (Mã: {{ProjectCode}}) đã bị <strong>TỪ CHỐI</strong>.</p>
        <p><strong>Lý do:</strong> {{Reason}}</p>
        <p>Vui lòng liên hệ giảng viên hướng dẫn để được hỗ trợ.</p>");

        private static string GetProjectNeedsModificationTemplate() => GetBaseTemplate(
            "Đề tài cần chỉnh sửa",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Đề tài <strong>{{ProjectName}}</strong> (Mã: {{ProjectCode}}) cần được <strong>CHỈNH SỬA</strong> trước khi được duyệt.</p>
        <p><strong>Góp ý:</strong></p>
        <p>{{Feedback}}</p>
        <p><strong>Hạn chỉnh sửa:</strong> {{Deadline}}</p>
        <p><a href='{{ProjectUrl}}' class='btn'>Chỉnh sửa đề tài</a></p>");

        private static string GetEvaluationAssignedTemplate() => GetBaseTemplate(
            "Phân công thẩm định đề tài",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Bạn đã được phân công thẩm định đề tài <strong>{{ProjectName}}</strong> (Mã: {{ProjectCode}}).</p>
        <p><strong>Nhóm thực hiện:</strong> {{GroupName}}</p>
        <p><strong>Thời hạn thẩm định:</strong> {{Deadline}}</p>
        <p><a href='{{EvaluationUrl}}' class='btn'>Bắt đầu thẩm định</a></p>");

        private static string GetMeetingRequestedTemplate() => GetBaseTemplate(
            "Yêu cầu lịch hẹn mới",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Bạn có một yêu cầu lịch hẹn mới từ nhóm <strong>{{GroupName}}</strong>.</p>
        <p><strong>Tiêu đề:</strong> {{MeetingTitle}}</p>
        <p><strong>Thời gian đề xuất:</strong> {{ProposedTime}}</p>
        <p><strong>Địa điểm:</strong> {{Location}}</p>
        <p><a href='{{MeetingUrl}}' class='btn'>Xem và phản hồi</a></p>");

        private static string GetMeetingApprovedTemplate() => GetBaseTemplate(
            "Lịch hẹn đã được duyệt",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Lịch hẹn của bạn đã được <strong>DUYỆT</strong>.</p>
        <p><strong>Tiêu đề:</strong> {{MeetingTitle}}</p>
        <p><strong>Thời gian:</strong> {{MeetingTime}}</p>
        <p><strong>Địa điểm:</strong> {{Location}}</p>
        <p>Vui lòng đến đúng giờ.</p>");

        private static string GetDefenseScheduledTemplate() => GetBaseTemplate(
            "Lịch bảo vệ đồ án",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Lịch bảo vệ đồ án của bạn đã được xếp:</p>
        <p><strong>Đề tài:</strong> {{ProjectName}}</p>
        <p><strong>Thời gian:</strong> {{DefenseTime}}</p>
        <p><strong>Địa điểm:</strong> {{Location}}</p>
        <p><strong>Hội đồng:</strong> {{CouncilName}}</p>
        <p>Chúc bạn bảo vệ thành công!</p>");

        private static string GetTopicExpiringTemplate() => GetBaseTemplate(
            "Đề tài sắp hết hạn",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Đề tài <strong>{{TopicName}}</strong> (Mã: {{TopicCode}}) của bạn sẽ <strong>HẾT HẠN</strong> vào cuối học kỳ này nếu không có sinh viên đăng ký.</p>
        <p>Vui lòng cập nhật hoặc gia hạn đề tài nếu cần.</p>
        <p><a href='{{TopicUrl}}' class='btn'>Xem đề tài</a></p>");

        private static string GetPasswordResetTemplate() => GetBaseTemplate(
            "Đặt lại mật khẩu",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản TEDF.</p>
        <p>Click vào nút bên dưới để đặt lại mật khẩu:</p>
        <p><a href='{{ResetUrl}}' class='btn'>Đặt lại mật khẩu</a></p>
        <p>Link này sẽ hết hạn sau {{ExpirationHours}} giờ.</p>
        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>");

        private static string GetWelcomeEmailTemplate() => GetBaseTemplate(
            "Chào mừng đến với TEDF",
            @"<p>Xin chào {{RecipientName}},</p>
        <p>Chào mừng bạn đến với <strong>TEDF</strong> - Hệ thống quản lý đồ án tốt nghiệp của FPT University Đà Nẵng.</p>
        <p><strong>Thông tin tài khoản:</strong></p>
        <ul>
            <li>Email: {{Email}}</li>
            <li>Vai trò: {{Role}}</li>
        </ul>
        <p><a href='{{LoginUrl}}' class='btn'>Đăng nhập ngay</a></p>");
    }
}
