public interface IUserService
{
    Task<GetUserResponse> GetUserById(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<GetUserResponse>> GetAllUsers(PaginationParameters? pagParams, CancellationToken cancellationToken);
    Task<GetUserResponse> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteUser(Guid id, CancellationToken cancellationToken);
    Task<UserEventParticipationResponse> ParticipateInEvent(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<bool> CancelEventParticipation(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<PagedResponse<UserParticipatedEventResponse>> GetUserParticipatedEvents(Guid id,PaginationParameters? pagParams, CancellationToken cancellationToken);
}