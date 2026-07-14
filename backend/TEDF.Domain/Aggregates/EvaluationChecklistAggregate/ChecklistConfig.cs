using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// A versioned, per-semester checklist configuration managed by the Department Head.
/// Owns its <see cref="ChecklistCriterion"/> collection. At most one Active config may exist per
/// semester (enforced by the application layer + a filtered unique index).
/// </summary>
public class ChecklistConfig : AggregateRoot<Guid>
{
    /// <summary>A checklist must contain exactly this many criteria to be activated.</summary>
    public const int RequiredCriteriaCount = 10;

    /// <summary>Minimum passed criteria required for an evaluator to approve a topic.</summary>
    public const int DefaultPassThreshold = 7;

    public int SemesterId { get; private set; }
    public int Version { get; private set; }
    public ChecklistConfigStatus Status { get; private set; }
    public int PassThreshold { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private readonly List<ChecklistCriterion> _criteria = [];
    public IReadOnlyCollection<ChecklistCriterion> Criteria => _criteria.AsReadOnly();

    private ChecklistConfig() { }

    /// <summary>Creates a new Draft configuration with the supplied ordered criteria.</summary>
    public static ChecklistConfig Create(
        int semesterId,
        int version,
        IEnumerable<(string TitleVi, string TitleEn, string? Description)> criteria,
        int passThreshold = DefaultPassThreshold,
        Guid? createdBy = null)
    {
        var config = new ChecklistConfig
        {
            Id = Guid.NewGuid(),
            SemesterId = semesterId,
            Version = version,
            Status = ChecklistConfigStatus.Draft,
            PassThreshold = passThreshold,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        config.SetCriteriaInternal(criteria);
        config.RaiseDomainEvent(new ChecklistConfigCreatedEvent(config.Id, semesterId, version));
        return config;
    }

    /// <summary>Clones this config's criteria into a new Draft for <paramref name="targetSemesterId"/>.</summary>
    public ChecklistConfig CopyTo(int targetSemesterId, int version, Guid? createdBy = null)
    {
        var ordered = _criteria.OrderBy(c => c.Order)
            .Select(c => (c.TitleVi, c.TitleEn, (string?)c.Description));
        return Create(targetSemesterId, version, ordered, PassThreshold, createdBy);
    }

    /// <summary>
    /// Replaces all criteria (used for editing text and/or reordering). Only allowed while Draft —
    /// an Active config that already has evaluation history must be forked into a new version instead.
    /// </summary>
    public void ReplaceCriteria(IEnumerable<(string TitleVi, string TitleEn, string? Description)> criteria, Guid? updatedBy = null)
    {
        EnsureDraft();
        SetCriteriaInternal(criteria);
        Touch(updatedBy);
    }

    /// <summary>Activates the config, making it the checklist applied to the semester.</summary>
    public void Activate(Guid? activatedBy = null)
    {
        CheckRule(new ChecklistConfigMustHaveExactlyTenCriteriaRule(_criteria.Count));

        if (Status == ChecklistConfigStatus.Active)
            return;

        Status = ChecklistConfigStatus.Active;
        Touch(activatedBy);
        RaiseDomainEvent(new ChecklistConfigActivatedEvent(Id, SemesterId, Version));
    }

    /// <summary>Retires the config (kept for history; no longer applied to new evaluations).</summary>
    public void Deactivate(Guid? deactivatedBy = null)
    {
        if (Status == ChecklistConfigStatus.Inactive)
            return;

        Status = ChecklistConfigStatus.Inactive;
        Touch(deactivatedBy);
    }

    private void SetCriteriaInternal(IEnumerable<(string TitleVi, string TitleEn, string? Description)> criteria)
    {
        _criteria.Clear();
        var order = 1;
        foreach (var c in criteria)
        {
            if (string.IsNullOrWhiteSpace(c.TitleVi))
                throw new BusinessRuleValidationException("Tên tiêu chí (tiếng Việt) không được để trống.");
            _criteria.Add(ChecklistCriterion.Create(Id, order++, c.TitleVi, c.TitleEn ?? string.Empty, c.Description));
        }
    }

    private void EnsureDraft()
    {
        if (Status != ChecklistConfigStatus.Draft)
            throw new BusinessRuleValidationException(
                "Chỉ có thể chỉnh sửa checklist ở trạng thái Nháp. Hãy tạo phiên bản mới để thay đổi checklist đã áp dụng.");
    }

    private void Touch(Guid? by)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = by;
    }
}
