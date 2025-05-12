using Microsoft.AspNetCore.Http;

public interface IEventService
{
    Task<Result<PagedResponse<GetEventResponse>>> GetAllEvents(PaginationParameters? pagParams, EventFilterCriteriaRequest? criteria = null);
    Task<Result<GetEventResponse>> GetEventById(Guid id);
    Task<Result<GetEventResponse>> CreateEvent(CreateEventRequest request);
    Task<Result<GetEventResponse>> UpdateEventDetails(Guid id, UpdateEventRequest request);
    Task<Result<bool>> DeleteEvent(Guid id);
    Task<Result<EventImageDetailsResponse>> UploadEventImage(Guid eventId, IFormFile imageFile);
    Task<Result<bool>> DeleteEventImage(Guid eventId);
}