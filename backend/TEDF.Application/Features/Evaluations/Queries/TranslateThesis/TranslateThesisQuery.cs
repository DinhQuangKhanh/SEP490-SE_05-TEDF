using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.TranslateThesis;

/// <summary>Translates a matched topic's content to Vietnamese for the side-by-side comparison.</summary>
public record TranslateThesisQuery(Guid ThesisId) : IQuery<TranslatedThesisDto>;
