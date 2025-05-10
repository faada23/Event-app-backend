using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<DatabaseContext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("EventDbConnection")));
builder.Services.AddScoped<IRepository<User>,Repository<User>>();
builder.Services.AddScoped<IRepository<Event>,Repository<Event>>();
builder.Services.AddScoped<IRepository<Category>,Repository<Category>>();
builder.Services.AddScoped<IRepository<RefreshToken>,Repository<RefreshToken>>();
builder.Services.AddScoped<IRepository<Role>,Repository<Role>>();

builder.Services.AddScoped<IAuthService,AuthService>();

builder.Services.AddScoped<IJwtProvider,JwtProvider>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUserPolicy", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin")); 

    //пример, не планируется использование
    options.AddPolicy("HasEmailPolicy", policy =>
        policy.RequireClaim(JwtRegisteredClaimNames.Email)); 
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JWTOptions"));
builder.Services.AddAuth(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict,
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.Initialize(scope.ServiceProvider);
}

app.Run();