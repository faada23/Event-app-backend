using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    private ActionResult ResultIdIsNull(string idName = "ID")
    {
        var badRequest = Result<object>.Failure($"{idName} cannot be empty.", ErrorType.InvalidInput);
        return badRequest.ToActionResult();
    }

    private void ClearTokenCookies()
    {
        Response.Cookies.Append("JwtCookie", "");
        Response.Cookies.Append("RefreshTokenCookie", "");
    }

    private Guid GetCurrentUserIdFromClaims()
    {
        var userIdClaim = User.FindFirstValue("Id");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
        }
        return userId;
    }

    [HttpGet("me")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult<GetUserResponse>> GetCurrentUser()
    {
        Guid currentUserId = GetCurrentUserIdFromClaims();

        return await GetUserById(currentUserId);
    }

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<PagedList<GetUserResponse>>> GetAllUsers([FromQuery] PaginationParameters? pagParams)
    {
        pagParams ??= new PaginationParameters();
        var result = await _userService.GetAllUsers(pagParams);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<GetUserResponse>> GetUserById(Guid id)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull("User ID");

        var result = await _userService.GetUserById(id);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOrSelfPolicy")]
    public async Task<ActionResult<GetUserResponse>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull("User ID");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await _userService.UpdateUser(id, request);
        return result.ToActionResult();
    }

    [HttpPut("me")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult<GetUserResponse>> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        Guid currentUserId = GetCurrentUserIdFromClaims();

        return await UpdateUser(currentUserId, request);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull("User ID");

        var result = await _userService.DeleteUser(id);
        if (result.IsSuccess)
        {
            return NoContent();
        }
        var errorResult = Result<object>.Failure(result.Message!, result.ErrorType!.Value);
        return errorResult.ToActionResult();
    }

    
    [HttpDelete("me")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<IActionResult> DeleteCurrentUser()
    {
        Guid currentUserId = GetCurrentUserIdFromClaims();
        ClearTokenCookies();
        return await DeleteUser(currentUserId);
    }

    [HttpPost("participate/{eventId:guid}")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult<UserEventParticipationResponse>> ParticipateInEvent(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return ResultIdIsNull("Event ID");

        Guid userId = GetCurrentUserIdFromClaims();

        var result = await _userService.ParticipateInEvent(userId, eventId);
        return result.ToActionResult();
    }

    [HttpDelete("participate/{eventId:guid}")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult<UserEventParticipationResponse>> CancelEventParticipation(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return ResultIdIsNull("Event ID");
               
        Guid userId = GetCurrentUserIdFromClaims();
 
        var result = await _userService.CancelEventParticipation(userId, eventId);
        return result.ToActionResult();
    }

    [HttpGet("participated-events")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<IActionResult> GetUserParticipatedEvents([FromQuery] PaginationParameters? pagParams)
    {   
        Guid userId = GetCurrentUserIdFromClaims();

        var result = await _userService.GetUserParticipatedEvents(userId, pagParams);
        return result.ToActionResult();     
    }
}