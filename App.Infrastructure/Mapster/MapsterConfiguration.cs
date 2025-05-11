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
        
        config.NewConfig<Category, GetCategoryResponse>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name);

        config.NewConfig<CreateUpdateCategoryRequest, Category>()
            .Map(dest => dest.Name, src => src.Name)
            .Ignore(dest => dest.Id);

         config.NewConfig<PagedList<Category>, PagedResponse<GetCategoryResponse>>()
                .Map(dest => dest.Data, src => src)
                .Map(dest => dest.CurrentPage, src => src.CurrentPage)
                .Map(dest => dest.PageSize, src => src.PageSize)
                .Map(dest => dest.TotalItems, src => src.TotalItems)
                .Map(dest => dest.TotalPages, src => src.TotalPages);
    }
}