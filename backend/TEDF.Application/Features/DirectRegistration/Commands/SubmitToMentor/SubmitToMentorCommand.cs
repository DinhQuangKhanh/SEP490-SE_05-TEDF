using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectRegistration.Commands.SubmitToMentor;

public record SubmitToMentorCommand(Guid ProjectId, Guid GroupId) : ICommand;
