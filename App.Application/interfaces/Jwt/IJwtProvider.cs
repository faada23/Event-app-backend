

public interface IJwtProvider
{
    public Task<(string accessToken, string refreshToken)> GenerateTokens(User user);
    public Task<(string accessToken,string refreshToken)> RefreshTokens(string oldRefreshToken);
}