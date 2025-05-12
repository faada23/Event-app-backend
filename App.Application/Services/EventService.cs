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


    public async Task<Result<PagedResponse<GetEventResponse>>> GetAllEvents(PaginationParameters? pagParams, EventFilterCriteriaRequest? criteria = null)
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
            includeProperties: "Category,Image"
        );

        if (!eventsResult.IsSuccess || eventsResult.Value == null)
        {
            return Result<PagedResponse<GetEventResponse>>.Failure(
                eventsResult.Message ?? "Unknown error retrieving event.",
                eventsResult.ErrorType ?? ErrorType.DatabaseError);
        }
        
        var pagedResponse = _mapper.Map<PagedList<Event>, PagedResponse<GetEventResponse>>(eventsResult.Value!);

        return Result<PagedResponse<GetEventResponse>>.Success(pagedResponse);
    }

    public async Task<Result<GetEventResponse>> GetEventById(Guid id)
    {
        var eventResult = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == id,
            includeProperties: "Category,Image" 
        );

        if (!eventResult.IsSuccess)
        {
            return Result<GetEventResponse>.Failure(eventResult.Message!, eventResult.ErrorType!.Value);
        }
        if (eventResult.Value == null)
        {
            return Result<GetEventResponse>.Failure($"Event with ID {id} not found.", ErrorType.RecordNotFound);
        }

        return Result<GetEventResponse>.Success(_mapper.Map<Event, GetEventResponse>(eventResult.Value));
    }

    public async Task<Result<GetEventResponse>> CreateEvent(CreateEventRequest request)
    {
        var categoryResult = await _categoryRepository.GetFirstOrDefault(c => c.Id == request.CategoryId);
        if (!categoryResult.IsSuccess || categoryResult.Value == null)
        {
            return Result<GetEventResponse>.Failure(
                categoryResult.Message ?? "Category was not found",
                categoryResult.ErrorType ?? ErrorType.RecordNotFound);
        }

        var newEvent = _mapper.Map<CreateEventRequest, Event>(request);

        _eventRepository.Insert(newEvent);
        var saveResult = await _eventRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<GetEventResponse>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        var createdEventWithDetails = await _eventRepository.GetFirstOrDefault(
            e => e.Id == newEvent.Id,
            includeProperties: "Category,Image");

        if (!createdEventWithDetails.IsSuccess || createdEventWithDetails.Value == null)
        {
            return Result<GetEventResponse>.Failure("Failed to retrieve newly created event with details.", ErrorType.DatabaseError);
        }
        
        return Result<GetEventResponse>.Success(_mapper.Map<Event, GetEventResponse>(createdEventWithDetails.Value));
    }

    public async Task<Result<GetEventResponse>> UpdateEventDetails(Guid id, UpdateEventRequest request)
    {
        var eventResult = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == id,
            includeProperties: "Category,Image"
        );

        if (!eventResult.IsSuccess)
        {
            return Result<GetEventResponse>.Failure(
                eventResult.Message ?? "Unknown error retrieving event.",
                eventResult.ErrorType ?? ErrorType.DatabaseError);
        }

        var eventToUpdate = eventResult.Value;

        if (eventToUpdate.CategoryId != request.CategoryId)
        {
            var categoryResult = await _categoryRepository.GetFirstOrDefault(x => x.Id == request.CategoryId);
            if (!categoryResult.IsSuccess || categoryResult.Value == null)
            {
                return Result<GetEventResponse>.Failure(
                    categoryResult.Message ?? "Unknown error retrieving category.",
                    categoryResult.ErrorType ?? ErrorType.DatabaseError);
            }
        }

        _mapper.Map(request, eventToUpdate);

        _eventRepository.Update(eventToUpdate);
        var saveResult = await _eventRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<GetEventResponse>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        return Result<GetEventResponse>.Success(_mapper.Map<Event, GetEventResponse>(eventToUpdate));
    }

    public async Task<Result<bool>> DeleteEvent(Guid id)
    {
        var eventResult = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == id,
            includeProperties: "Image" 
        );

        if (!eventResult.IsSuccess || eventResult.Value == null)
        {
            return Result<bool>.Failure(
                eventResult.Message ?? "Unknown error retrieving event.",
                eventResult.ErrorType ?? ErrorType.DatabaseError);
        }

        var eventToDelete = eventResult.Value;

        if (eventToDelete.Image != null)
        {
            _fileStorageService.DeleteFile(eventToDelete.Image.StoredPath);
        }

        _eventRepository.Delete(eventToDelete); 
        var saveResult = await _eventRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }
        
        return Result<bool>.Success(true);
    }

    public async Task<Result<EventImageDetailsResponse>> UploadEventImage(Guid eventId, IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return Result<EventImageDetailsResponse>.Failure("Image file is required.", ErrorType.InvalidInput);
        }

        var eventResult = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == eventId,
            includeProperties: "Image"
        );

        if (!eventResult.IsSuccess || eventResult.Value == null)
        {
            return Result<EventImageDetailsResponse>.Failure(
                eventResult.Message ?? "Unknown error retrieving event.",
                eventResult.ErrorType ?? ErrorType.DatabaseError);
        }
        var currentEvent = eventResult.Value;

        if (currentEvent.Image != null)
        {
            _fileStorageService.DeleteFile(currentEvent.Image.StoredPath);
            _imageRepository.Delete(currentEvent.Image);
        }

        var saveFileResult = await _fileStorageService.SaveFileAsync(imageFile, _eventImagesPath);
        if (!saveFileResult.IsSuccess)
        {
            return Result<EventImageDetailsResponse>.Failure(saveFileResult.Message!, saveFileResult.ErrorType ?? ErrorType.FileSystemError);
        }
        var relativePath = saveFileResult.Value!;
        
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
        
        var dbSaveResult = await _imageRepository.SaveChangesAsync();

        if (!dbSaveResult.IsSuccess)
        {
            _fileStorageService.DeleteFile(relativePath); 
            return Result<EventImageDetailsResponse>.Failure(dbSaveResult.Message!, dbSaveResult.ErrorType!.Value);
        }
        
        return Result<EventImageDetailsResponse>.Success(_mapper.Map<Image, EventImageDetailsResponse>(newImage));
    }

    public async Task<Result<bool>> DeleteEventImage(Guid eventId)
    {
        var eventResult = await _eventRepository.GetFirstOrDefault(
            filter: e => e.Id == eventId,
            includeProperties: "Image"
        );

        if (!eventResult.IsSuccess || eventResult.Value == null)
        {
            return Result<bool>.Failure(
                eventResult.Message ?? "Unknown error retrieving event.",
                eventResult.ErrorType ?? ErrorType.DatabaseError);
        }
        if (eventResult.Value.Image == null)
        {
            return Result<bool>.Success(true); 
        }

        var imageToDelete = eventResult.Value.Image;
        var storedPath = imageToDelete.StoredPath;

        _imageRepository.Delete(imageToDelete);

        var dbSaveResult = await _eventRepository.SaveChangesAsync();
        if (!dbSaveResult.IsSuccess)
        {
            return Result<bool>.Failure(dbSaveResult.Message!, dbSaveResult.ErrorType!.Value);
        }

        _fileStorageService.DeleteFile(storedPath);

        return Result<bool>.Success(true);
    }
}