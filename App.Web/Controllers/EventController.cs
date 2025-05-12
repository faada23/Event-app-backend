using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")] 
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    private ActionResult ResultIdIsNull(){
        var badRequest = Result<object>.Failure("Event ID cannot be empty.", ErrorType.InvalidInput);
        return badRequest.ToActionResult();
    }
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedList<GetEventResponse>>> GetAllEvents(
        [FromQuery] PaginationParameters? pagParams,
        [FromQuery] EventFilterCriteriaRequest? criteria)
    {
        var result = await _eventService.GetAllEvents(pagParams, criteria);
        return result.ToActionResult(); 
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] 
    public async Task<ActionResult<GetEventResponse>> GetEventById(Guid id)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull();

        var result = await _eventService.GetEventById(id);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetEventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        var result = await _eventService.CreateEvent(request);

        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetEventResponse>> UpdateEventDetails(Guid id, [FromBody] UpdateEventRequest request)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull();

        var result = await _eventService.UpdateEventDetails(id, request);
        return result.ToActionResult(); 
    }


    [HttpDelete("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteEvent(Guid id) 
    {
        if (id == Guid.Empty)
            return ResultIdIsNull();

        var result = await _eventService.DeleteEvent(id);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/image")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> UploadEventImage(Guid eventId, IFormFile imageFile) 
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return BadRequest();
        }

        const long maxFileSize = 10 * 1024 * 1024; 
        if (imageFile.Length > maxFileSize)
        {
             return BadRequest("Max file size is 10 MB");
        }

        var result = await _eventService.UploadEventImage(eventId, imageFile);
         return result.ToActionResult();

    }

    [HttpDelete("{eventId:guid}/image")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteEventImage(Guid eventId)
    {
        var result = await _eventService.DeleteEventImage(eventId);

        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        return NoContent(); 
    }

}