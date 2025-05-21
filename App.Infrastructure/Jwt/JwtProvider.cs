using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

public class JwtProvider : IJwtProvider
{   
    private readonly IOptions<JwtOptions> _options;
    private readonly IRepository<RefreshToken> _refreshTokenRepository; 
    private readonly string? _jwtSecretKey;
    public JwtProvider(IOptions<JwtOptions> options, IRepository<RefreshToken> refreshTokenRepository)
    {
        _options = options;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSecretKey = Environment.GetEnvironmentVariable("JwtSecretKey");

        if (string.IsNullOrEmpty(_jwtSecretKey))
        {
            throw new InvalidOperationException("JwtSecretKey is not configured.");
        }
    }

    public async Task<(string accessToken,string? refreshToken)> GenerateTokens(User user, bool generateRefreshToken, CancellationToken cancellationToken)
    {   
        var claims = new List<Claim>{
            new Claim("Id", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email), 
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, role.Name));
        }

        var signingCred = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSecretKey!)),
                SecurityAlgorithms.HmacSha256);

        var accessTokenExpireTime = DateTime.UtcNow.Add(_options.Value.AccessTokenExpires);

        var jwtAccessToken = new JwtSecurityToken(
            expires : accessTokenExpireTime,
            claims: claims,
            signingCredentials: signingCred
        );
        
        var accessTokenString = new JwtSecurityTokenHandler().WriteToken(jwtAccessToken);

        //-------------------------------------------------------------------------------
        string? refreshToken = null;

        if(generateRefreshToken){
            refreshToken = GenerateRefreshTokenString();

            var refreshTokenEntity = new RefreshToken{
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow.Add(_options.Value.RefreshTokenExpires),
                AddedDate = DateTimeOffset.UtcNow
            };

            _refreshTokenRepository.Insert(refreshTokenEntity);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        return (accessTokenString, refreshToken);
    }

    public async Task<string> RefreshToken(string RefreshToken,CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetFirstOrDefault(
            filter: x => x.Token == RefreshToken,
            includeProperties: "User.Roles",
            cancellationToken: cancellationToken)
                ?? throw new NotFoundException("Refresh Token", RefreshToken);
        
        if(refreshToken.IsRevoked == true)
            throw new BadRequestException("Refresh Token is revoked");

        if(refreshToken.ExpiryDate < DateTimeOffset.UtcNow){
            refreshToken.IsRevoked = true;
            throw new BadRequestException("Refresh Token is expired");
        }
        
        refreshToken.IsRevoked = true;

        var user = refreshToken.User ?? throw new NotFoundException("User",refreshToken.UserId);
        var newTokensResult = await GenerateTokens(user,false,cancellationToken);
        return newTokensResult.accessToken;
    }

    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}