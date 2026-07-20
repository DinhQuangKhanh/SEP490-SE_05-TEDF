using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// An evaluator's checklist result for a project, for a given evaluation round (SubmissionNumber).
/// Snapshots the criteria of the <see cref="ChecklistConfig"/> version used (title + scoring bounds) so
/// later config edits never change stored history. <see cref="PassedCount"/> and each item's pass state
/// are always recomputed by the domain from the evaluator's scores — never taken from client input.
/// </summary>
public class ProjectEvaluationChecklist : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid EvaluatorId { get; private set; }
    public int SemesterId { get; private set; }

    /// <summary>The checklist configuration version used (immutable link for auditing).</summary>
    public Guid ChecklistConfigId { get; private set; }

    /// <summary>Evaluation round this result belongs to (increments on resubmission).</summary>
    public int SubmissionNumber { get; private set; }

    /// <summary>Snapshot of the config's RequiredPassCount at creation time.</summary>
    public int RequiredPassCount { get; private set; }

    /// <summary>Number of criteria that passed — computed by the domain, not by the client.</summary>
    public int PassedCount { get; private set; }

    public string? EvaluatorNote { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Set when the evaluator's approval was accepted with this checklist.</summary>
    public DateTime? ApprovedAt { get; private set; }

    private readonly List<ChecklistResultItem> _items = [];
    public IReadOnlyCollection<ChecklistResultItem> Items => _items.AsReadOnly();

    /// <summary>True when the evaluator has passed at least <see cref="RequiredPassCount"/> criteria.</summary>
    public bool MeetsApprovalThreshold => PassedCount >= RequiredPassCount;

    private ProjectEvaluationChecklist() { }

    /// <summary>Builds an initial (unscored) result snapshot from the given active config.</summary>
    public static ProjectEvaluationChecklist CreateFromConfig(
        ChecklistConfig config, Guid projectId, Guid evaluatorId, int submissionNumber)
    {
        var result = new ProjectEvaluationChecklist
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EvaluatorId = evaluatorId,
            SemesterId = config.SemesterId,
            ChecklistConfigId = config.Id,
            SubmissionNumber = submissionNumber,
            RequiredPassCount = config.RequiredPassCount,
            PassedCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var criterion in config.Criteria.OrderBy(c => c.Order))
        {
            result._items.Add(ChecklistResultItem.Create(
                result.Id, criterion.Id, criterion.Order, criterion.TitleVi, criterion.MaxScore, criterion.PassScore));
        }

        return result;
    }

    /// <summary>
    /// Applies the evaluator's scores + per-criterion comments. Entries whose criterion id is not part of
    /// this snapshot are ignored; each score is validated against its snapshot bounds. Each item's pass
    /// state and <see cref="PassedCount"/> are then recomputed from the scores so a client can never inflate
    /// the count or the pass flags.
    /// </summary>
    public void ApplyScores(IEnumerable<ChecklistScoreEntry> entries, string? note)
    {
        var byCriterion = entries
            .GroupBy(e => e.CriterionId)
            .ToDictionary(g => g.Key, g => g.Last());

        foreach (var item in _items)
        {
            if (byCriterion.TryGetValue(item.CriterionId, out var entry))
                item.ApplyScore(entry.Score, entry.Comment);
        }

        PassedCount = _items.Count(i => i.IsPassed);
        EvaluatorNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ProjectChecklistSavedEvent(ProjectId, EvaluatorId, SubmissionNumber));
    }

    /// <summary>Records that this checklist backed a successful approval.</summary>
    public void MarkApproved()
    {
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
