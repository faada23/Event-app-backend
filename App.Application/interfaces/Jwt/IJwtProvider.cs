public interface IJwtProvider
{
    public Task<(string accessToken, string? refreshToken)> GenerateTokens(User user, bool generateRefreshToken,CancellationToken cancellationToken);
    public Task<string> RefreshToken(string RefreshToken,CancellationToken cancellationToken);
}