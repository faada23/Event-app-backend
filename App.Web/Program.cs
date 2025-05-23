using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();

ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en-US");

builder.Services.AddDbContext<DatabaseContext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("EventDbConnection")));
builder.Services.AddScoped<IRepository<User>,Repository<User>>();
builder.Services.AddScoped<IRepository<Event>,Repository<Event>>();
builder.Services.AddScoped<IRepository<Category>,Repository<Category>>();
builder.Services.AddScoped<IRepository<RefreshToken>,Repository<RefreshToken>>();
builder.Services.AddScoped<IRepository<Role>,Repository<Role>>();
builder.Services.AddScoped<IRepository<Image>,Repository<Image>>();
builder.Services.AddScoped<IRepository<EventParticipant>,Repository<EventParticipant>>();

builder.Services.AddScoped<IAuthService,AuthService>();
builder.Services.AddScoped<ICategoryService,CategoryService>();
builder.Services.AddScoped<IUserService,UserService>();
builder.Services.AddScoped<IEventService,EventService>();

builder.Services.AddScoped<IUserPasswordHasher,PasswordHasherAdapter>();
builder.Services.AddScoped<IPasswordHasher<User>,PasswordHasher<User>>();

builder.Services.AddScoped<ICookieAuthManager, CookieAuthManager>();

builder.Services.Configure<FileStorageOptions>(options =>
{
    options.BasePath = builder.Environment.WebRootPath;
});

builder.Services.AddScoped<IFileStorageService, FileStorageService>();

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

var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
typeAdapterConfig.RegisterMaps();
builder.Services.AddSingleton(typeAdapterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();
builder.Services.AddScoped<IDefaultMapper, MapsterAdapter>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseGlobalExceptionHandler();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    dbContext.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.Initialize(scope.ServiceProvider);
}

app.Run();