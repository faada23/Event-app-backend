public interface IUserService
{
    Task<GetUserResponse> GetUserById(Guid id);
    Task<PagedResponse<GetUserResponse>> GetAllUsers(PaginationParameters? pagParams);
    Task<GetUserResponse> UpdateUser(Guid id, UpdateUserRequest request);
    Task<bool> DeleteUser(Guid id);
    Task<UserEventParticipationResponse> ParticipateInEvent(Guid userId, Guid eventId);
    Task<bool> CancelEventParticipation(Guid userId, Guid eventId);
    Task<PagedResponse<UserParticipatedEventResponse>> GetUserParticipatedEvents(Guid id,PaginationParameters? pagParams);
}