using Microsoft.AspNetCore.Http;

public interface ICookieAuthManager
    {
        void SetAuthCookies(HttpContext httpContext, LoginUserResponse authResponse);

        void SetAccessTokenCookie(HttpContext httpContext, string accessToken);

        string? GetRefreshTokenFromCookie(HttpContext httpContext);

        void ClearAuthCookies(HttpContext httpContext);
    }