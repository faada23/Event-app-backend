using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Event> _eventRepository;
    private readonly IRepository<EventParticipant> _eventParticipantRepository;
    private readonly IDefaultMapper _mapper; 

    public UserService(
        IRepository<User> userRepository,
        IRepository<Event> eventRepository,
        IRepository<EventParticipant> eventParticipantRepository,
        IDefaultMapper mapper) 
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _eventParticipantRepository = eventParticipantRepository ?? throw new ArgumentNullException(nameof(eventParticipantRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper)); 
    }

    public async Task<Result<GetUserResponse>> GetUserById(Guid id)
    {
        var userResult = await _userRepository.GetFirstOrDefault(filter: u => u.Id == id);

        if (!userResult.IsSuccess)
        {
            return Result<GetUserResponse>.Failure(userResult.Message!, userResult.ErrorType!.Value);
        }
        if (userResult.Value == null)
        {
            return Result<GetUserResponse>.Failure($"User with ID {id} not found.", ErrorType.RecordNotFound);
        }

        return Result<GetUserResponse>.Success(_mapper.Map<User, GetUserResponse>(userResult.Value));
    }

    public async Task<Result<GetUserResponse>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var userResult = await _userRepository.GetFirstOrDefault(u => u.Id == id);

        if (!userResult.IsSuccess)
        {
            return Result<GetUserResponse>.Failure(userResult.Message!, userResult.ErrorType!.Value);
        }

        if (userResult.Value == null)
        {
            return Result<GetUserResponse>.Failure($"User with ID {id} not found.", ErrorType.RecordNotFound);
        }

        var userToUpdate = userResult.Value;

        if (userToUpdate.Email != request.Email)
        {
            var emailExistsResult = await _userRepository.GetFirstOrDefault(u => u.Email == request.Email && u.Id != id);
            if (!emailExistsResult.IsSuccess)
            {
                return Result<GetUserResponse>.Failure(emailExistsResult.Message!, emailExistsResult.ErrorType!.Value);
            }
            if (emailExistsResult.Value != null)
            {
                return Result<GetUserResponse>.Failure($"Email '{request.Email}' is already in use by another account.", ErrorType.AlreadyExists);
            }
        }

        _mapper.Map(request, userToUpdate);

        _userRepository.Update(userToUpdate);
        var saveResult = await _userRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<GetUserResponse>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

         return Result<GetUserResponse>.Success(_mapper.Map<User, GetUserResponse>(userToUpdate));
    }

    public async Task<Result<bool>> DeleteUser(Guid id)
    {
        var userResult = await _userRepository.GetFirstOrDefault(filter: u => u.Id == id);

        if (!userResult.IsSuccess)
        {
            return Result<bool>.Failure(userResult.Message!, userResult.ErrorType!.Value);
        }
        if (userResult.Value == null)
        {
            return Result<bool>.Failure($"User with ID {id} not found.", ErrorType.RecordNotFound);
        }

        var userToDelete = userResult.Value;

        _userRepository.Delete(userToDelete);
        var saveResult = await _userRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<UserEventParticipationResponse>> ParticipateInEvent(Guid userId, Guid eventId)
    {
        var userResult = await _userRepository.GetFirstOrDefault(u => u.Id == userId);
        if (!userResult.IsSuccess || userResult.Value == null)
        {
            return Result<UserEventParticipationResponse>.Failure(userResult.Message ?? $"User with ID {userId} not found.", userResult.ErrorType ?? ErrorType.RecordNotFound);
        }

        var eventResult = await _eventRepository.GetFirstOrDefault(e => e.Id == eventId);
        if (!eventResult.IsSuccess || eventResult.Value == null)
        {
            return Result<UserEventParticipationResponse>.Failure(eventResult.Message ?? $"Event with ID {eventId} not found.", eventResult.ErrorType ?? ErrorType.RecordNotFound);
        }

        var existingParticipation = await _eventParticipantRepository.GetFirstOrDefault(
            ep => ep.UserId == userId && ep.EventId == eventId
        );

        if (!existingParticipation.IsSuccess)
        {
            return Result<UserEventParticipationResponse>.Failure(existingParticipation.Message!, existingParticipation.ErrorType!.Value);
        }
        if (existingParticipation.Value != null)
        {
            return Result<UserEventParticipationResponse>.Failure("User is already participating in this event.", ErrorType.AlreadyExists);
        }

        var newParticipation = new EventParticipant
        {
            UserId = userId,
            EventId = eventId,
            EventRegistrationDate = DateTimeOffset.UtcNow
        };

        _eventParticipantRepository.Insert(newParticipation);
        var saveResult = await _eventParticipantRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess || saveResult.Value <= 0)
        {
            return Result<UserEventParticipationResponse>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        var response = _mapper.Map<EventParticipant, UserEventParticipationResponse>(newParticipation);

        return Result<UserEventParticipationResponse>.Success(response);
    }

    public async Task<Result<PagedResponse<GetUserResponse>>> GetAllUsers(PaginationParameters? pagParams)
    {
        var usersResult = await _userRepository.GetAll(
            pagParams: pagParams,
            orderBy: q => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
        );

        if (!usersResult.IsSuccess)
        {
            return Result<PagedResponse<GetUserResponse>>.Failure(usersResult.Message!, usersResult.ErrorType!.Value);
        }

        var pagedList = usersResult.Value!;
        var pagedResponse = _mapper.Map<PagedList<User>, PagedResponse<GetUserResponse>>(usersResult.Value!);

        return Result<PagedResponse<GetUserResponse>>.Success(pagedResponse);
    }

    public async Task<Result<bool>> CancelEventParticipation(Guid userId, Guid eventId)
    {
        var participationResult = await _eventParticipantRepository.GetFirstOrDefault(
            ep => ep.UserId == userId && ep.EventId == eventId
        );

        if (!participationResult.IsSuccess)
        {
            return Result<bool>.Failure(participationResult.Message!, participationResult.ErrorType!.Value);
        }
        if (participationResult.Value == null)
        {
            return Result<bool>.Failure("User is not participating in this event.", ErrorType.RecordNotFound);
        }

        _eventParticipantRepository.Delete(participationResult.Value);
        var saveResult = await _eventParticipantRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResponse<UserParticipatedEventResponse>>> GetUserParticipatedEvents(Guid userId, PaginationParameters? pagParams)
    {
        var userExistsResult = await _userRepository.GetFirstOrDefault(u => u.Id == userId);
        if (!userExistsResult.IsSuccess)
        {
            return Result<PagedResponse<UserParticipatedEventResponse>>.Failure(userExistsResult.Message!, userExistsResult.ErrorType!.Value);
        }
        if (userExistsResult.Value == null)
        {
            return Result<PagedResponse<UserParticipatedEventResponse>>.Failure($"User with ID {userId} not found.", ErrorType.RecordNotFound);
        }

        var participationsPageResult = await _eventParticipantRepository.GetAll(
            pagParams: pagParams,
            filter: ep => ep.UserId == userId,
            includeProperties: "Event",
            orderBy: q => q.OrderByDescending(ep => ep.EventRegistrationDate)
        );

        if (!participationsPageResult.IsSuccess)
        {
            return Result<PagedResponse<UserParticipatedEventResponse>>.Failure(participationsPageResult.Message!, participationsPageResult.ErrorType!.Value);
        }

        var pagedParticipations = participationsPageResult.Value;

        var pagedResponse = _mapper.Map<PagedList<EventParticipant>, PagedResponse<UserParticipatedEventResponse>>(pagedParticipations);

        return Result<PagedResponse<UserParticipatedEventResponse>>.Success(pagedResponse);
    }
}