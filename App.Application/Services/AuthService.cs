public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository; 
    private readonly IRepository<Role> _roleRepository; 
    private readonly IJwtProvider _jwtProvider; 
    private readonly IUserPasswordHasher _passwordHasher;
    private readonly IDefaultMapper _mapper;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        IRepository<Role> roleRepository,
        IDefaultMapper mapper,
        IJwtProvider jwtProvider,
        IUserPasswordHasher userPasswordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _passwordHasher = userPasswordHasher ?? throw new ArgumentNullException(nameof(userPasswordHasher));
    }

    public async Task<bool> Register(RegisterUserRequest registerDto, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetFirstOrDefault(
            filter: u => u.Email == registerDto.Email,
            cancellationToken: cancellationToken);

        if (existingUser != null)
            throw new AlreadyExistsException("User", existingUser.Email);

        var user = _mapper.Map<RegisterUserRequest, User>(registerDto);
        user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

        var getRole = await _roleRepository.GetFirstOrDefault(
            filter: r => r.Name == RoleConstants.User,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException();
            
        user.Roles.Add(getRole);
    
        _userRepository.Insert(user);
        await _userRepository.SaveChangesAsync(cancellationToken); 

        return true;
    }
 
    public async Task<LoginUserResponse> Login(LoginUserRequest loginDto, CancellationToken cancellationToken)
    {
        var userResult = await _userRepository.GetFirstOrDefault(
            filter: u => u.Email == loginDto.Email,
            includeProperties: "Roles",
            cancellationToken: cancellationToken
        ) ?? throw new BadRequestException("Invalid username or password");

        var user = userResult;

        var passwordVerificationStatus = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (passwordVerificationStatus == PasswordVerificationStatus.Failed)
            throw new BadRequestException("Invalid username or password");

        var tokens = await _jwtProvider.GenerateTokens(user,true,cancellationToken); 
                                                        
        var response = _mapper.Map<(string accessToken, string refreshToken), LoginUserResponse>(tokens!);
        return response;
        
    }
    

    public async Task<string> RefreshToken(string oldRefreshToken, CancellationToken cancellationToken)
    {
        var refreshedToken = await _jwtProvider.RefreshToken(oldRefreshToken,cancellationToken);

        var response = refreshedToken;
        return response;
    }

    public async Task Logout(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.GetFirstOrDefault(
            filter: rt => rt.Token == refreshToken,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException();

        if (token == null || token.IsRevoked)
            throw new BadRequestException("Refresh token is null or revoked");

        token.IsRevoked = true;
        _refreshTokenRepository.Update(token);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken); 
        

        
    }
    
    public async Task LogoutAll(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokenRepository.GetAll(
            filter: rt => rt.UserId == userId && !rt.IsRevoked,
            cancellationToken: cancellationToken
        );

        foreach (var token in tokens) 
        {
            token.IsRevoked = true;
            _refreshTokenRepository.Update(token);
        }

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

}
        