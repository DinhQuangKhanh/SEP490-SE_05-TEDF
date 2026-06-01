using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.LockUser;

public class LockUserCommandHandler : ICommandHandler<LockUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthAccountService _authAccount;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public LockUserCommandHandler(
        IUserRepository userRepository,
        IAuthAccountService authAccount,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _authAccount = authAccount;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        // Prevent admin from locking themselves
        if (_currentUser.UserId == request.UserId)
            throw new BusinessRuleValidationException("Cannot lock your own account.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.UserId);

        user.Lock();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Disable auth account before committing DB changes
        // If this fails, DB transaction won't commit
        await _authAccount.DisableAccountAsync(user.FirebaseUid, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
