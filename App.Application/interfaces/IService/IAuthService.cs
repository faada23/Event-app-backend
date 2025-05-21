public interface IAuthService
{
    Task<bool> Register(RegisterUserRequest registerDto,CancellationToken cancellationToken);
    Task<LoginUserResponse> Login(LoginUserRequest loginDto,CancellationToken cancellationToken);
    Task<string> RefreshToken(string refreshToken, CancellationToken cancellationToken);
    Task Logout(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAll(Guid userId, CancellationToken cancellationToken);
}