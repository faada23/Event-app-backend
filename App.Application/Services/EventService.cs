using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

public class EventService : IEventService
{
    private readonly IRepository<Event> _eventRepository;
    private readonly IRepository<Image> _imageRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDefaultMapper _mapper; 
    private readonly string _eventImagesPath = "images/eventImages";

    public EventService(
        IRepository<Event> eventRepository,
        IRepository<Image> imageRepository,
        IRepository<Category> categoryRepository,
        IFileStorageService fileStorageService,
        IDefaultMapper mapper) 
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper)); 
    }


    public async Task<PagedResponse<GetEventResponse>> GetAllEvents(PaginationParameters? pagParams, EventFilterCriteriaRequest? criteria = null, CancellationToken cancellationToken = default)
    {
        Expression<Func<Event, bool>>? filter = null;
        if (criteria != null)
        {
            var locationLower = criteria.Location?.ToLowerInvariant();
            var categoryNameLower = criteria.CategoryName?.ToLowerInvariant();
            var eventNameLower = criteria.EventName?.Trim().ToLowerInvariant();

            filter = e =>
                (!criteria.DateFrom.HasValue || e.DateTimeOfEvent >= criteria.DateFrom.Value) &&
                (!criteria.DateTo.HasValue || e.DateTimeOfEvent <= criteria.DateTo.Value) &&
                (string.IsNullOrEmpty(locationLower) || (e.Location != null && e.Location.ToLower().Contains(locationLower))) &&
                (string.IsNullOrEmpty(categoryNameLower) || (e.Category != null && e.Category.Name.ToLower() == categoryNameLower)) &&
                (string.IsNullOrEmpty(eventNameLower) || (e.Name != null && e.Name.ToLower() == eventNameLower));
        }

        var eventsResult = await _eventRepository.GetAll(
            filter: filter,
            pagParams: pagParams,
            orderBy: q => q.OrderByDescending(e => e.DateTimeOfEvent),
            includeProperties: "Category,Image",
            cancellationToken: cancellationToken
        );
    
        var pagedResponse = _mapper.Map<PagedList<Event>, PagedResponse<GetEventResponse>>(eventsResult);
        return pagedResponse;
    }

    public async Task<GetEventResponse> GetEventById(Guid id, CancellationToken cancellationToken)
    {
        var currentEvent = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == id,
            includeProperties: "Category,Image" ,
            cancellationToken: cancellationToken
        ) ?? throw new NotFoundException("Event", id);

        return _mapper.Map<Event, GetEventResponse>(currentEvent);
    }

    public async Task<GetEventResponse> CreateEvent(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetFirstOrDefault(
            filter: c => c.Id == request.CategoryId,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Category", request.CategoryId);

        var newEvent = _mapper.Map<CreateEventRequest, Event>(request);

        _eventRepository.Insert(newEvent);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        var createdEventWithDetails = await _eventRepository.GetFirstOrDefault(
            e => e.Id == newEvent.Id,
            includeProperties: "Category,Image",
            cancellationToken: cancellationToken) ?? throw new NotFoundException("Event", newEvent.Id);
        
        return _mapper.Map<Event, GetEventResponse>(createdEventWithDetails);
    }

    public async Task<GetEventResponse> UpdateEventDetails(Guid id, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var eventToUpdate = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == id,
            includeProperties: "Category,Image",
            cancellationToken: cancellationToken
        ) ?? throw new NotFoundException("Event", id);

        if (eventToUpdate.CategoryId != request.CategoryId)
        {
            var category = await _categoryRepository.GetFirstOrDefault(
                filter: x => x.Id == request.CategoryId,
                cancellationToken: cancellationToken)
                ?? throw new NotFoundException("Category", request.CategoryId);
        }   

        _mapper.Map(request, eventToUpdate);

        _eventRepository.Update(eventToUpdate);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<Event, GetEventResponse>(eventToUpdate);
    }

    public async Task<bool> DeleteEvent(Guid id, CancellationToken cancellationToken)
    {
        var eventToDelete = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == id,
            includeProperties: "Image",
            cancellationToken: cancellationToken
        ) ?? throw new NotFoundException("Event", id);

        if (eventToDelete.Image != null)
            _fileStorageService.DeleteFile(eventToDelete.Image.StoredPath);
        
        _eventRepository.Delete(eventToDelete); 
        await _eventRepository.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<EventImageDetailsResponse> UploadEventImage(Guid eventId, IFormFile imageFile,CancellationToken cancellationToken)
    {
        if (imageFile == null || imageFile.Length == 0)
            throw new BadRequestException("image file is null or empty");

        var currentEvent = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == eventId,
            includeProperties: "Image",
            cancellationToken: cancellationToken
        ) ?? throw new NotFoundException("Event", eventId);

        if (currentEvent.Image != null)
        {
            _fileStorageService.DeleteFile(currentEvent.Image.StoredPath);
            _imageRepository.Delete(currentEvent.Image);
        }

        var saveFileResult = await _fileStorageService.SaveFileAsync(imageFile, _eventImagesPath, cancellationToken);
        
        var relativePath = saveFileResult;
        
        var newImage = new Image
        {
            Id = Guid.NewGuid(),
            StoredPath = relativePath,
            ContentType = imageFile.ContentType,
            UploadedAt = DateTimeOffset.UtcNow,
            EventId = eventId
        };

        _imageRepository.Insert(newImage);
        currentEvent.Image = newImage;
        
        await _imageRepository.SaveChangesAsync(cancellationToken);
        return _mapper.Map<Image, EventImageDetailsResponse>(newImage);
    }

    public async Task<bool> DeleteEventImage(Guid eventId, CancellationToken cancellationToken)
    {
        var currentEvent = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == eventId,
            includeProperties: "Image",
            cancellationToken
        ) ?? throw new NotFoundException("Event", eventId);

        if (currentEvent.Image == null)
            return true;
        
        var imageToDelete = currentEvent.Image;
        var storedPath = imageToDelete.StoredPath;

        _imageRepository.Delete(imageToDelete);

        await _eventRepository.SaveChangesAsync(cancellationToken);
        _fileStorageService.DeleteFile(storedPath);

        return true;
    }
}