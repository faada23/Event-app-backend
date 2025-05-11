public interface IUserService
{
    Task<Result<GetUserResponse>> GetUserById(Guid id);
    Task<Result<PagedResponse<GetUserResponse>>> GetAllUsers(PaginationParameters? pagParams);
    Task<Result<GetUserResponse>> UpdateUser(Guid id, UpdateUserRequest request);
    Task<Result<bool>> DeleteUser(Guid id);
    Task<Result<UserEventParticipationResponse>> ParticipateInEvent(Guid userId, Guid eventId);
    Task<Result<bool>> CancelEventParticipation(Guid userId, Guid eventId);
    Task<Result<PagedResponse<UserParticipatedEventResponse>>> GetUserParticipatedEvents(Guid id,PaginationParameters? pagParams);
}