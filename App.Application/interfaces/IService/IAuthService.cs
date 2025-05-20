public interface IAuthService
{
    Task<bool> Register(RegisterUserRequest registerDto);
    Task<LoginUserResponse> Login(LoginUserRequest loginDto);
    Task<RefreshTokenResponse> RefreshToken(string refreshToken);
    Task Logout(string refreshToken);
    Task LogoutAll(Guid userId);
}