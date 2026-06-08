namespace TEDF.Application.Features.Settings.DTOs;

/// <summary>A single admin-visible system setting.</summary>
public record SystemSettingDto(
    string Key,
    string Value,
    string DataType,
    string? Description,
    string? Category);
