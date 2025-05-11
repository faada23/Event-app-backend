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
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var result = await _authService.Register(request);
        if (result.IsSuccess)
        {
            return Created();
        }
        var objectResult = Result<object>.Failure(result.Message!, result.ErrorType!.Value);
        return objectResult.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginUserRequest request)
    {
        var result = await _authService.Login(request);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToActionResult();
        }
        SetTokenCookies(result.Value.AccessToken, result.Value.RefreshToken);
        return Ok(new { Message = "Login successful." });
    }

    [Authorize(Policy ="AuthenticatedUserPolicy", AuthenticationSchemes ="RefreshScheme")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshTokenFromCookie = Request.Cookies["RefreshTokenCookie"];
        if (string.IsNullOrEmpty(refreshTokenFromCookie))
        {
            return Unauthorized("Refresh token not found in cookie.");
        }
        var result = await _authService.RefreshToken(refreshTokenFromCookie);
        if (!result.IsSuccess || result.Value == null)
        {
            ClearTokenCookies();
            return result.ToActionResult();
        }
        SetTokenCookies(result.Value.AccessToken, result.Value.RefreshToken);
        return Ok(new { Message = "Tokens refreshed successfully." });
    }

    [HttpDelete("logout")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult> Logout()
    {
        var refreshTokenFromCookie = Request.Cookies["RefreshTokenCookie"];
        var result = await _authService.Logout(refreshTokenFromCookie ?? string.Empty);

        ClearTokenCookies();

        if (result.IsSuccess)
        {
            return Ok(new { Message = "Logout successful." });
        }
        var objectResult = Result<string>.Failure(result.Message!, result.ErrorType!.Value);
        return objectResult.ToActionResult();
    }

    [HttpDelete("logout-all")]
    [Authorize(Policy ="AuthenticatedUserPolicy")]
    public async Task<ActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirstValue("Id");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("User ID claim not found or invalid");
        }
        var result = await _authService.LogoutAll(userId);
        ClearTokenCookies();
        if (result.IsSuccess)
        {
            return Ok(new { Message = "Logged out from all devices successfully." });
        }
        var objectResult = Result<string>.Failure(result.Message!, result.ErrorType!.Value);
        return objectResult.ToActionResult();
    }

}