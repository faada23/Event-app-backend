public interface IAuthService
{
    Task<bool> Register(RegisterUserRequest registerDto);
    Task<LoginUserResponse> Login(LoginUserRequest loginDto);
    Task<RefreshTokenResponse> RefreshToken(string refreshToken);
    Task<bool> Logout(string refreshToken);
    Task<bool> LogoutAll(Guid userId);
}