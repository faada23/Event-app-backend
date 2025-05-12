

public interface IJwtProvider
{
    public Task<Result<(string accessToken, string refreshToken)>> GenerateTokens(User user);
    public Task<Result<(string accessToken,string refreshToken)>> RefreshTokens(string oldRefreshToken);
}