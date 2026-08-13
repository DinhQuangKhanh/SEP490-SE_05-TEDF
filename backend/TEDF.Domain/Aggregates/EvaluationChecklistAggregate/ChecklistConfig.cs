using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// A versioned, per-semester checklist configuration managed by the Department Head (imported from Excel).
/// Owns its <see cref="ChecklistCriterion"/> collection. At most one Active config may exist per semester
/// (enforced by the application layer + a filtered unique index). The number of criteria is dynamic — it
/// comes entirely from the imported file, with no hard-coded maximum.
/// </summary>
public class ChecklistConfig : AggregateRoot<Guid>
{
    public int SemesterId { get; private set; }
    public int Version { get; private set; }
    public ChecklistConfigStatus Status { get; private set; }

    /// <summary>
    /// Minimum number of criteria an evaluator must pass to be allowed to approve a topic.
    /// Configured per checklist by the Department Head; must be in [1, criteria count].
    /// </summary>
    public int RequiredPassCount { get; private set; }

    /// <summary>Name of the Excel file this configuration was imported from (null for manual/legacy configs).</summary>
    public string? SourceFileName { get; private set; }

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
        IEnumerable<ChecklistCriterionSpec> criteria,
        int requiredPassCount,
        string? sourceFileName = null,
        Guid? createdBy = null)
    {
        var config = new ChecklistConfig
        {
            Id = Guid.NewGuid(),
            SemesterId = semesterId,
            Version = version,
            Status = ChecklistConfigStatus.Draft,
            SourceFileName = string.IsNullOrWhiteSpace(sourceFileName) ? null : sourceFileName.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        config.SetCriteriaInternal(criteria);
        config.SetRequiredPassCountInternal(requiredPassCount);
        config.RaiseDomainEvent(new ChecklistConfigCreatedEvent(config.Id, semesterId, version));
        return config;
    }

    /// <summary>Clones this config's criteria into a new Draft for <paramref name="targetSemesterId"/>.</summary>
    public ChecklistConfig CopyTo(int targetSemesterId, int version, Guid? createdBy = null)
    {
        var ordered = _criteria.OrderBy(c => c.Order)
            .Select(c => new ChecklistCriterionSpec(c.TitleVi, c.TitleEn, c.Description));
        return Create(targetSemesterId, version, ordered, RequiredPassCount, SourceFileName, createdBy);
    }

    /// <summary>
    /// Replaces all criteria and the required-pass count. Only allowed while Draft — an Active config that
    /// already has evaluation history must be forked into a new version instead.
    /// </summary>
    public void ReplaceCriteria(IEnumerable<ChecklistCriterionSpec> criteria, int requiredPassCount, Guid? updatedBy = null)
    {
        EnsureDraft();
        SetCriteriaInternal(criteria);
        SetRequiredPassCountInternal(requiredPassCount);
        Touch(updatedBy);
    }

    /// <summary>Activates the config, making it the checklist applied to the semester.</summary>
    public void Activate(Guid? activatedBy = null)
    {
        CheckRule(new ChecklistMustHaveCriteriaRule(_criteria.Count));
        CheckRule(new ChecklistRequiredPassCountRule(RequiredPassCount, _criteria.Count));

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

    private void SetCriteriaInternal(IEnumerable<ChecklistCriterionSpec> criteria)
    {
        var criteriaList = criteria.ToList();

        // Merge in place instead of clear-and-recreate so criterion ids survive an edit (result snapshots
        // reference them by id). EF loads the collection unordered, so pair the specs against the criteria
        // in Order — otherwise the merge would shuffle content across rows on every save.
        var existing = _criteria.OrderBy(c => c.Order).ToList();

        // Drop the surplus tail; EF cascade-deletes the orphans on SaveChanges.
        for (var i = existing.Count - 1; i >= criteriaList.Count; i--)
            _criteria.Remove(existing[i]);

        for (var i = 0; i < criteriaList.Count; i++)
        {
            var spec = criteriaList[i];
            if (i < existing.Count)
            {
                existing[i].Update(i + 1, spec.TitleVi, spec.TitleEn ?? string.Empty, spec.Description);
            }
            else
            {
                // ChecklistCriterion.Create validates the title and throws a business rule error.
                _criteria.Add(ChecklistCriterion.Create(
                    Id, i + 1, spec.TitleVi, spec.TitleEn ?? string.Empty, spec.Description));
            }
        }
    }

    private void SetRequiredPassCountInternal(int requiredPassCount)
    {
        // Allow saving a Draft with an out-of-range value only when there are no criteria yet is NOT needed:
        // the count is always validated against the current criteria so previews/imports fail fast.
        CheckRule(new ChecklistRequiredPassCountRule(requiredPassCount, _criteria.Count));
        RequiredPassCount = requiredPassCount;
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
