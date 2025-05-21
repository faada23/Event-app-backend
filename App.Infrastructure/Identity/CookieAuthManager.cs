using Microsoft.AspNetCore.Http;

public class CookieAuthManager : ICookieAuthManager
    {
        private const string AccessTokenCookieName = "Access-Token"; 
        private const string RefreshTokenCookieName = "Refresh-Token";

        public void SetAuthCookies(HttpContext httpContext, LoginUserResponse authResponse)
        {
            ArgumentNullException.ThrowIfNull(httpContext); 
            ArgumentNullException.ThrowIfNull(authResponse); 

            httpContext.Response.Cookies.Append(AccessTokenCookieName, authResponse.AccessToken);
            httpContext.Response.Cookies.Append(RefreshTokenCookieName, authResponse.RefreshToken);
        }

        public void SetAccessTokenCookie(HttpContext httpContext, string accessToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext); 
            ArgumentException.ThrowIfNullOrEmpty(accessToken); 

            httpContext.Response.Cookies.Append(AccessTokenCookieName, accessToken);
        }

        public string? GetRefreshTokenFromCookie(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            return httpContext.Request.Cookies[RefreshTokenCookieName];
        }

        public void ClearAuthCookies(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            httpContext.Response.Cookies.Append(AccessTokenCookieName, "");
            httpContext.Response.Cookies.Append(RefreshTokenCookieName, "");
        }
    }