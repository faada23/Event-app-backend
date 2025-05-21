using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class AuthExtension
{
    public static IServiceCollection AddAuth(this IServiceCollection serviceCollection, IConfiguration Configuration)
    {   
        var authSettings = Configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

        var secretKeyString = Environment.GetEnvironmentVariable("JwtSecretKey");
        if (string.IsNullOrEmpty(secretKeyString))
        {
            throw new InvalidOperationException("Jwt SecretKey is not configured in JwtOptions or Environment Variable 'JwtSecretKey'.");
        }

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKeyString));

        serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            { 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = secretKey

                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context => {
                        context.Token = context.Request.Cookies["Access-token"];

                        return Task.CompletedTask;
                    }    
                };
            })
            .AddJwtBearer("RefreshScheme", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false, 
                    ValidateLifetime = false, 
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = secretKey,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context => {
                        context.Token = context.Request.Cookies["Access-Token"];
                        return Task.CompletedTask;
                    }
                };
    });
        
        serviceCollection.AddAuthorization();

        return serviceCollection;    
    }
}