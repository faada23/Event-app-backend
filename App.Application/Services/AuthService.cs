using Microsoft.AspNetCore.Identity;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository; 
    private readonly IRepository<Role> _roleRepository; 
    private readonly IJwtProvider _jwtProvider; 
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly IDefaultMapper _mapper;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        IRepository<Role> roleRepository,
        IDefaultMapper mapper,
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<bool> Register(RegisterUserRequest registerDto)
    {
        var existingUser = await _userRepository.GetFirstOrDefault(u => u.Email == registerDto.Email);

        if (existingUser != null)
            throw new Exception();

        var user = _mapper.Map<RegisterUserRequest, User>(registerDto);
        user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

        var getRole = await _roleRepository.GetFirstOrDefault(r => r.Name == RoleConstants.User);
        if( getRole == null) 
            throw new Exception();

        user.Roles.Add(getRole);
    
        _userRepository.Insert(user);
        await _userRepository.SaveChangesAsync(); 

        return true;
    }
 
    public async Task<LoginUserResponse> Login(LoginUserRequest loginDto)
    {
        var userResult = await _userRepository.GetFirstOrDefault(
            filter: u => u.Email == loginDto.Email,
            includeProperties: "Roles"
        );

        var user = userResult;

        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
            throw new Exception();
        
        var tokens = await _jwtProvider.GenerateTokens(user); 
                                                        
        var response = _mapper.Map<(string accessToken, string refreshToken), LoginUserResponse>(tokens);
        return response;
        
    }
    

    public async Task<RefreshTokenResponse> RefreshToken(string oldRefreshToken)
    {
        var refreshedTokens = await _jwtProvider.RefreshTokens(oldRefreshToken);

        var response = _mapper.Map<(string accessToken, string refreshToken), RefreshTokenResponse>(refreshedTokens);

        return response;
    }

    public async Task<bool> Logout(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetFirstOrDefault(rt => rt.Token == refreshToken);

        if (token!= null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            _refreshTokenRepository.Update(token);
            await _refreshTokenRepository.SaveChangesAsync(); 
            return true;
        }

        return false;
    }
    
    public async Task<bool> LogoutAll(Guid userId)
    {
        var tokens = await _refreshTokenRepository.GetAll(
            filter: rt => rt.UserId == userId && !rt.IsRevoked
        );

        foreach (var token in tokens) 
        {
            token.IsRevoked = true;
            _refreshTokenRepository.Update(token);
        }

        await _refreshTokenRepository.SaveChangesAsync();
        return true;
    }

}
        

   