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

    public async Task<Result<(string accessToken,string refreshToken)>> GenerateTokens(User user)
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

        var refreshToken = GenerateRefreshTokenString();

        var refreshTokenEntity = new RefreshToken{
            Token = refreshToken,
            UserId = user.Id,
            ExpiryDate = DateTimeOffset.UtcNow.Add(_options.Value.RefreshTokenExpires),
            AddedDate = DateTimeOffset.UtcNow
        };

        _refreshTokenRepository.Insert(refreshTokenEntity);
        var result = await _refreshTokenRepository.SaveChangesAsync();

        if(result.IsSuccess) 
            return Result<(string, string)>.Success((accessTokenString, refreshToken));
        else
            return Result<(string,string)>.Failure(result.Message!,ErrorType.DatabaseError);
    }

    public async Task<Result<(string accessToken, string refreshToken)>> RefreshTokens(string oldRefreshToken)
    {
        var result = await _refreshTokenRepository.GetFirstOrDefault(
            filter: x => x.Token == oldRefreshToken,
            includeProperties: "User.Roles");

        if(!result.IsSuccess)
            return Result<(string,string)>.Failure("Error while seraching refresh token",ErrorType.DatabaseError);
        
        if(result.Value!.Token == null)
            return Result<(string,string)>.Failure("Invalid refresh token",ErrorType.RecordNotFound);
        
        if(result.Value.IsRevoked == true)
            return Result<(string,string)>.Failure("Refresh token has been revoked",ErrorType.Forbidden);

        if(result.Value.ExpiryDate < DateTimeOffset.UtcNow){
            result.Value.IsRevoked = true;
            return Result<(string,string)>.Failure("Refresh token is Expired",ErrorType.Forbidden);
        }
        
        result.Value.IsRevoked = true;

        var user = result.Value.User;
        if(user == null)
            return Result<(string,string)>.Failure("User was not found for the refresh token",ErrorType.RecordNotFound);
        
        var newTokensResult = await GenerateTokens(user);

        if(!newTokensResult.IsSuccess)
            return Result<(string,string)>.Failure(result.Message!,result.ErrorType!.Value);

        return newTokensResult;
    }

    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}