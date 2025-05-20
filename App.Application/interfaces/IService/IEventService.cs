using Microsoft.AspNetCore.Http;

public interface IEventService
{
    Task<PagedResponse<GetEventResponse>> GetAllEvents(PaginationParameters? pagParams, EventFilterCriteriaRequest? criteria = null);
    Task<GetEventResponse> GetEventById(Guid id);
    Task<GetEventResponse> CreateEvent(CreateEventRequest request);
    Task<GetEventResponse> UpdateEventDetails(Guid id, UpdateEventRequest request);
    Task<bool> DeleteEvent(Guid id);
    Task<EventImageDetailsResponse> UploadEventImage(Guid eventId, IFormFile imageFile);
    Task<bool> DeleteEventImage(Guid eventId);
}