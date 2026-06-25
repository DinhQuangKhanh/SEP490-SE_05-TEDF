using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandler : ICommandHandler<UpdateMyProfileCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMyProfileCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<MediatR.Unit> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.UpdateProfile(
            phoneNumber: request.PhoneNumber,
            birthDate: request.BirthDate,
            privacySettings: request.PrivacySettings);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MediatR.Unit.Value;
    }
}
