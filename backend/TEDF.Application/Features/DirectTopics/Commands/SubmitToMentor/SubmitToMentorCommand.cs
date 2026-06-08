using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectTopics.Commands.SubmitToMentor;

public record SubmitToMentorCommand(Guid ProjectId, Guid GroupId) : ICommand;
