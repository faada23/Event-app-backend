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

    public async Task<GetUserResponse> GetUserById(Guid id)
    {
        var userResult = await _userRepository.GetFirstOrDefault(filter: u => u.Id == id) ?? throw new Exception();

        return _mapper.Map<User, GetUserResponse>(userResult);
    }

    public async Task<GetUserResponse> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var userToUpdate = await _userRepository.GetFirstOrDefault(u => u.Id == id) ?? throw new Exception();

        if (userToUpdate.Email != request.Email)
        {
            var existingEmail = await _userRepository.GetFirstOrDefault(u => u.Email == request.Email && u.Id != id)
                ?? throw new Exception();
        }

        _mapper.Map(request, userToUpdate);

        _userRepository.Update(userToUpdate);
        await _userRepository.SaveChangesAsync();

        return _mapper.Map<User, GetUserResponse>(userToUpdate);
    }

    public async Task<bool> DeleteUser(Guid id)
    {
        var userToDelete = await _userRepository.GetFirstOrDefault(filter: u => u.Id == id) ?? throw new Exception();

        _userRepository.Delete(userToDelete);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<UserEventParticipationResponse> ParticipateInEvent(Guid userId, Guid eventId)
    {
        var user = await _userRepository.GetFirstOrDefault(u => u.Id == userId) ?? throw new Exception();
        var currentEvent = await _eventRepository.GetFirstOrDefault(e => e.Id == eventId) ?? throw new Exception();

        var existingParticipation = await _eventParticipantRepository.GetFirstOrDefault(
            ep => ep.UserId == userId && ep.EventId == eventId
        );

        if(existingParticipation != null)
            throw new Exception();

        var newParticipation = new EventParticipant
        {
            UserId = userId,
            EventId = eventId,
            EventRegistrationDate = DateTimeOffset.UtcNow
        };

        _eventParticipantRepository.Insert(newParticipation);
        await _eventParticipantRepository.SaveChangesAsync();

        var response = _mapper.Map<EventParticipant, UserEventParticipationResponse>(newParticipation);
        return response;
    }

    public async Task<PagedResponse<GetUserResponse>> GetAllUsers(PaginationParameters? pagParams)
    {
        var users = await _userRepository.GetAll(
            pagParams: pagParams,
            orderBy: q => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
        );

        var pagedResponse = _mapper.Map<PagedList<User>, PagedResponse<GetUserResponse>>(users);
        return pagedResponse;
    }

    public async Task<bool> CancelEventParticipation(Guid userId, Guid eventId)
    {
        var participationResult = await _eventParticipantRepository.GetFirstOrDefault(
            ep => ep.UserId == userId && ep.EventId == eventId
        ) ?? throw new Exception();

        _eventParticipantRepository.Delete(participationResult);
        await _eventParticipantRepository.SaveChangesAsync();

        return true;
    }

    public async Task<PagedResponse<UserParticipatedEventResponse>> GetUserParticipatedEvents(Guid userId, PaginationParameters? pagParams)
    {
        var userExistsResult = await _userRepository.GetFirstOrDefault(u => u.Id == userId) ?? throw new Exception();

        var participations = await _eventParticipantRepository.GetAll(
            pagParams: pagParams,
            filter: ep => ep.UserId == userId,
            includeProperties: "Event",
            orderBy: q => q.OrderByDescending(ep => ep.EventRegistrationDate)
        );

        var pagedResponse = _mapper.Map<PagedList<EventParticipant>, PagedResponse<UserParticipatedEventResponse>>(participations);
        return pagedResponse;
    }
}