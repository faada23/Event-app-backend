using Microsoft.AspNetCore.Identity;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository; 
    private readonly IRepository<Role> _roleRepository; 
    private readonly IJwtProvider _jwtProvider; 
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        IRepository<Role> roleRepository,
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<Result<bool>> Register(RegisterUserRequest registerDto)
    {
        var existingUserResult = await _userRepository.GetFirstOrDefault(u => u.Email == registerDto.Email);
        if (!existingUserResult.IsSuccess)
        {
            return Result<bool>.Failure(existingUserResult.Message!, existingUserResult.ErrorType!.Value);
        }
        if (existingUserResult.Value != null)
        {
            return Result<bool>.Failure("User with this email already exists.", ErrorType.AlreadyExists);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

        var getRoleResult = await _roleRepository.GetFirstOrDefault(r => r.Name == "User");

        if (getRoleResult.IsSuccess && getRoleResult.Value != null)
        {
            user.Roles.Add(getRoleResult.Value);
        }
        else
        {
            return Result<bool>.Failure("Default role 'User' not found. Please configure roles.", ErrorType.DatabaseError);
        }

        _userRepository.Insert(user);

        var saveResult = await _userRepository.SaveChangesAsync(); 

        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(saveResult.Message!,ErrorType.DatabaseError);
        }

        return Result<bool>.Success(true);
    }
 
    public async Task<Result<LoginUserResponse>> Login(LoginUserRequest loginDto)
    {
        var userResult = await _userRepository.GetFirstOrDefault(
            filter: u => u.Email == loginDto.Email,
            includeProperties: "Roles"
        );

        if (!userResult.IsSuccess)
        {
            return Result<LoginUserResponse>.Failure(userResult.Message!, ErrorType.DatabaseError);
        }
        if (userResult.Value == null)
        {
            return Result<LoginUserResponse>.Failure("Invalid email or password.", ErrorType.InvalidInput);
        }

        var user = userResult.Value;


        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
            return Result<LoginUserResponse>.Failure("Invalid email or password.", ErrorType.InvalidInput);
        }

        var tokensResult = await _jwtProvider.GenerateTokens(user); 
                                                            
        if (!tokensResult.IsSuccess || tokensResult.Value == default)
        {
            return Result<LoginUserResponse>.Failure(tokensResult.Message!, tokensResult.ErrorType!.Value);
        }

        var response = new LoginUserResponse
        (
            tokensResult.Value.accessToken,
            tokensResult.Value.refreshToken
        );

        return Result<LoginUserResponse>.Success(response);
    }
    

    public async Task<Result<RefreshTokenResponse>> RefreshToken(string oldRefreshToken)
    {
        if (string.IsNullOrEmpty(oldRefreshToken))
            return Result<RefreshTokenResponse>.Failure("Refresh token is required.", ErrorType.InvalidInput); 

        var refreshResult = await _jwtProvider.RefreshTokens(oldRefreshToken);

        if (!refreshResult.IsSuccess || refreshResult.Value == default)
        {
            return Result<RefreshTokenResponse>.Failure(refreshResult.Message!,refreshResult.ErrorType!.Value);
        }

        var response = new RefreshTokenResponse
        (
            refreshResult.Value.accessToken,
            refreshResult.Value.refreshToken
        );

        return Result<RefreshTokenResponse>.Success(response);
    }

    public async Task<Result<bool>> Logout(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {  
            return Result<bool>.Success(true);
        }

        var tokenResult = await _refreshTokenRepository.GetFirstOrDefault(rt => rt.Token == refreshToken);

        if (!tokenResult.IsSuccess)
        {
            return Result<bool>.Failure(tokenResult.Message!, ErrorType.DatabaseError);
        }

        if (tokenResult.Value != null && !tokenResult.Value.IsRevoked)
        {
            tokenResult.Value.IsRevoked = true;
            _refreshTokenRepository.Update(tokenResult.Value);
            var saveResult = await _refreshTokenRepository.SaveChangesAsync(); 
            if (!saveResult.IsSuccess)
            {
                return Result<bool>.Failure(saveResult.Message!, ErrorType.DatabaseError);
            }
        }
        return Result<bool>.Success(true);
    }
    
    public async Task<Result<bool>> LogoutAll(Guid userId)
    {
         var tokensResult = await _refreshTokenRepository.GetAll(
            filter: rt => rt.UserId == userId && !rt.IsRevoked
        );

        if (!tokensResult.IsSuccess || tokensResult.Value == null)
        {
            return Result<bool>.Failure(tokensResult.Message!, ErrorType.DatabaseError);
        }

        foreach (var token in tokensResult.Value) 
        {
            token.IsRevoked = true;
            _refreshTokenRepository.Update(token);
        }

        var saveResult = await _refreshTokenRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(saveResult.Message!, ErrorType.DatabaseError);
        }

        return Result<bool>.Success(true);
    }

}
        

   