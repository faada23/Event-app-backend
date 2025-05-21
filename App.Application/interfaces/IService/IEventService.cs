using Microsoft.AspNetCore.Http;

public interface IEventService
{
    Task<PagedResponse<GetEventResponse>> GetAllEvents(PaginationParameters? pagParams, EventFilterCriteriaRequest? criteria = null, CancellationToken cancellationToken = default);
    Task<GetEventResponse> GetEventById(Guid id, CancellationToken cancellationToken);
    Task<GetEventResponse> CreateEvent(CreateEventRequest request, CancellationToken cancellationToken);
    Task<GetEventResponse> UpdateEventDetails(Guid id, UpdateEventRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteEvent(Guid id, CancellationToken cancellationToken);
    Task<EventImageDetailsResponse> UploadEventImage(Guid eventId, IFormFile imageFile, CancellationToken cancellationToken);
    Task<bool> DeleteEventImage(Guid eventId, CancellationToken cancellationToken);
}