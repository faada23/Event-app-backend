using Mapster;

public static class MapsterConfiguration
{
    public static void RegisterMaps(this TypeAdapterConfig config)
    {
        config.NewConfig<RegisterUserRequest, User>()
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PasswordHash)
            .Ignore(dest => dest.Roles)
            .Ignore(dest => dest.RefreshTokens);

        config.NewConfig<(string accessToken, string refreshToken), LoginUserResponse>()
            .Map(dest => dest.AccessToken, src => src.accessToken)
            .Map(dest => dest.RefreshToken, src => src.refreshToken);

        config.NewConfig<(string accessToken, string refreshToken), RefreshTokenResponse>()
            .Map(dest => dest.AccessToken, src => src.accessToken)
            .Map(dest => dest.RefreshToken, src => src.refreshToken);
    }
}