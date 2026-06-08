using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.Settings.Commands.SendTestEmail;

/// <summary>Admin-only: sends a test email to the current admin's own address.</summary>
public record SendTestEmailCommand() : ICommand;
