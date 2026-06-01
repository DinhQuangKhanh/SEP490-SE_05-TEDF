namespace TEDF.Application.Features.Evaluations.DTOs;

public record FilterOptionItemDto(int Value, string Label);

public record EvaluatorFilterOptionsDto(
    List<FilterOptionItemDto> Semesters,
    List<FilterOptionItemDto> Majors
);
