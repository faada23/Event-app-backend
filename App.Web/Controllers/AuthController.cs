using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICookieAuthManager _cookieAuthManager;

    public AuthController(IAuthService authService, ICookieAuthManager cookieAuthManager)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _cookieAuthManager = cookieAuthManager ?? throw new ArgumentNullException(nameof(cookieAuthManager));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request,CancellationToken cancellationToken)
    {
        var result = await _authService.Register(request,cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginUserRequest request,CancellationToken cancellationToken)
    {
        var result = await _authService.Login(request,cancellationToken);
        _cookieAuthManager.SetAuthCookies(HttpContext, result);
        return Ok();
    }

    [Authorize(Policy ="AuthenticatedUserPolicy", AuthenticationSchemes ="RefreshScheme")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshTokenCookie = _cookieAuthManager.GetRefreshTokenFromCookie(HttpContext);
        var accessToken = await _authService.RefreshToken(refreshTokenCookie ?? string.Empty, cancellationToken);
        _cookieAuthManager.SetAccessTokenCookie(HttpContext, accessToken);

        return Ok();
    }

    [HttpDelete("logout")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshTokenCookie = _cookieAuthManager.GetRefreshTokenFromCookie(HttpContext);
        await _authService.Logout(refreshTokenCookie ?? string.Empty, cancellationToken);

        _cookieAuthManager.ClearAuthCookies(HttpContext);
        return Ok();

    }

    [HttpDelete("logout-all")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue("Id");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("User ID not found or invalid");
        }
        await _authService.LogoutAll(userId, cancellationToken);

        _cookieAuthManager.ClearAuthCookies(HttpContext);
        return Ok();
    }

}