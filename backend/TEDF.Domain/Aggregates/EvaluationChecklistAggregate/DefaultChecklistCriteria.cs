namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// The 10 default topic-evaluation criteria (Capstone / Kỹ thuật phần mềm).
/// Sourced here (backend) so the content is never hard-coded in the frontend — the Department Head's
/// "create checklist" form and the initial seed both read from this single source.
/// </summary>
public static class DefaultChecklistCriteria
{
    public sealed record CriterionSeed(string TitleVi, string TitleEn, string Description);

    public static IReadOnlyList<CriterionSeed> Items { get; } = new List<CriterionSeed>
    {
        new("Tên đề tài", "Project Title",
            "Tên đề tài có phản ánh được định hướng thực hiện nghiên cứu và phát triển sản phẩm của nhóm sinh viên không?"),
        new("Ngữ cảnh", "Context",
            "Ngữ cảnh nơi sản phẩm được triển khai có được xác định cụ thể không?"),
        new("Vấn đề cần giải quyết", "Problem Statement",
            "Vấn đề cần giải quyết có được mô tả rõ ràng và thể hiện được động lực cho việc ra đời của sản phẩm không?"),
        new("Người dùng chính", "Main Actors",
            "Người dùng chính của sản phẩm có được xác định rõ ràng trong đề tài không?"),
        new("Luồng xử lý và chức năng chính", "Main Flows and Use Cases",
            "Các luồng xử lý chính và các chức năng chính của người dùng có được mô tả đầy đủ không?"),
        new("Khách hàng hoặc nhà tài trợ", "Customers/Sponsors",
            "Khách hàng hoặc nhà tài trợ của đề tài có được xác định không?"),
        new("Hướng tiếp cận", "Approach",
            "Hướng tiếp cận về lý thuyết, công nghệ áp dụng và các sản phẩm chính cần tạo ra có được xác định rõ ràng và phù hợp không?"),
        new("Phạm vi và quy mô sản phẩm", "Scope and Product Size",
            "Phạm vi đề tài và quy mô sản phẩm có khả thi, phù hợp để nhóm 5 sinh viên thực hiện trong 15 tuần không? Công việc có được phân chia thành các gói để đánh giá không?"),
        new("Độ phức tạp và tính kỹ thuật", "Complexity/Technicality",
            "Độ phức tạp và tính kỹ thuật của đề tài có phù hợp với yêu cầu năng lực của một Capstone Project ngành Kỹ thuật phần mềm không?"),
        new("Khả năng ứng dụng và tính khả thi", "Applicability and Technological Feasibility",
            "Đề tài có hướng đến giải quyết vấn đề thực tế và khả thi về mặt công nghệ trong giới hạn thời gian của dự án không?"),
    };
}
