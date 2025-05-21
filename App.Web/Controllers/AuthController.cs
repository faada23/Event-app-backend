using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append("JwtCookie", accessToken);
        Response.Cookies.Append("RefreshTokenCookie", refreshToken);
    }

    private void ClearTokenCookies()
    {
        Response.Cookies.Append("JwtCookie", "");
        Response.Cookies.Append("RefreshTokenCookie", "");
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
        SetTokenCookies(result.AccessToken, result.RefreshToken);
        return Ok();
    }

    [Authorize(Policy ="AuthenticatedUserPolicy", AuthenticationSchemes ="RefreshScheme")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshTokenFromCookie = Request.Cookies["RefreshTokenCookie"];
        if (string.IsNullOrEmpty(refreshTokenFromCookie))
        {
            return Unauthorized("Refresh token not found in cookie.");
        }

        var accessToken = await _authService.RefreshToken(refreshTokenFromCookie, cancellationToken);
        Response.Cookies.Append("JwtCookie", accessToken);
        return Ok();
    }

    [HttpDelete("logout")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshTokenFromCookie = Request.Cookies["RefreshTokenCookie"];
        await _authService.Logout(refreshTokenFromCookie ?? string.Empty, cancellationToken);

        ClearTokenCookies();
        return Ok();

    }

    [HttpDelete("logout-all")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue("Id");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("User ID claim not found or invalid");
        }
        await _authService.LogoutAll(userId, cancellationToken);

        ClearTokenCookies();
        return Ok();
    }

}