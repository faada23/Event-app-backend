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
        var userResult = await _userRepository.GetFirstOrDefault(filter: u => u.Id == id)
            ?? throw new NotFoundException("User", id);

        return _mapper.Map<User, GetUserResponse>(userResult);
    }

    public async Task<GetUserResponse> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var userToUpdate = await _userRepository.GetFirstOrDefault(u => u.Id == id)
            ?? throw new NotFoundException("User", id);

        if (userToUpdate.Email != request.Email)
        {
            var existingEmail = await _userRepository.GetFirstOrDefault(u => u.Email == request.Email && u.Id != id);

            if(existingEmail != null)
                throw new AlreadyExistsException("User", request.Email);
        }

        _mapper.Map(request, userToUpdate);

        _userRepository.Update(userToUpdate);
        await _userRepository.SaveChangesAsync();

        return _mapper.Map<User, GetUserResponse>(userToUpdate);
    }

    public async Task<bool> DeleteUser(Guid id)
    {
        var userToDelete = await _userRepository.GetFirstOrDefault(filter: u => u.Id == id)
            ?? throw new NotFoundException("User", id);

        _userRepository.Delete(userToDelete);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<UserEventParticipationResponse> ParticipateInEvent(Guid userId, Guid eventId)
    {
        var user = await _userRepository.GetFirstOrDefault(u => u.Id == userId) 
            ?? throw new NotFoundException("User",userId);
        var currentEvent = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == eventId,
            includeProperties: "EventParticipants") 
            ?? throw new NotFoundException("Event",eventId);

        if(currentEvent.EventParticipants.Count >= currentEvent.MaxParticipants)
            throw new BadRequestException("The maximum number of participants has been reached.");
        
        var existingParticipation = currentEvent.EventParticipants.Where(x => x.UserId == userId);

        if(existingParticipation.Count() != 0)
            throw new AlreadyExistsException("EventParticipant",$"eventId: {eventId}, userId: {userId}");

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
        ) ?? throw new NotFoundException("EventParticipant", $"eventId: {eventId}, userId: {userId}");

        _eventParticipantRepository.Delete(participationResult);
        await _eventParticipantRepository.SaveChangesAsync();

        return true;
    }

    public async Task<PagedResponse<UserParticipatedEventResponse>> GetUserParticipatedEvents(Guid userId, PaginationParameters? pagParams)
    {
        var userExistsResult = await _userRepository.GetFirstOrDefault(u => u.Id == userId)
            ?? throw new NotFoundException("User",userId);

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