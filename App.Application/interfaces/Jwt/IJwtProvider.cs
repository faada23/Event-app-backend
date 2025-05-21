

public interface IJwtProvider
{
    public Task<(string accessToken, string? refreshToken)> GenerateTokens(User user, bool generateRefreshToken);
    public Task<string> RefreshToken(string RefreshToken);
}