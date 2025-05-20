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
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedList<GetEventResponse>>> GetAllEvents(
        [FromQuery] PaginationParameters? pagParams,
        [FromQuery] EventFilterCriteriaRequest? criteria)
    {
        var result = await _eventService.GetAllEvents(pagParams, criteria);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] 
    public async Task<ActionResult<GetEventResponse>> GetEventById(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var result = await _eventService.GetEventById(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetEventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        var result = await _eventService.CreateEvent(request);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetEventResponse>> UpdateEventDetails(Guid id, [FromBody] UpdateEventRequest request)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var result = await _eventService.UpdateEventDetails(id, request);
        return Ok(result); 
    }


    [HttpDelete("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteEvent(Guid id) 
    {
        if (id == Guid.Empty)
            return BadRequest();

        var result = await _eventService.DeleteEvent(id);
        return Ok(result);
    }

    [HttpPost("{eventId:guid}/image")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> UploadEventImage(Guid eventId, IFormFile imageFile) 
    {
        if (imageFile == null || imageFile.Length == 0)
            return BadRequest();
        
        var result = await _eventService.UploadEventImage(eventId, imageFile);
        return Ok(result);

    }

    [HttpDelete("{eventId:guid}/image")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteEventImage(Guid eventId)
    {
        var result = await _eventService.DeleteEventImage(eventId);
        return Ok(result); 
    }

}