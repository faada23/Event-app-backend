public interface IAuthService
{
    Task<Result<bool>> Register(RegisterUserRequest registerDto);
    Task<Result<LoginUserResponse>> Login(LoginUserRequest loginDto);
    Task<Result<RefreshTokenResponse>> RefreshToken(string refreshToken);
    Task<Result<bool>> Logout(string refreshToken);
    Task<Result<bool>> LogoutAll(Guid userId);
}