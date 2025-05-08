using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<DatabaseContext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("EventDbConnection")));
builder.Services.AddScoped<IRepository<User>,Repository<User>>();
builder.Services.AddScoped<IRepository<Event>,Repository<Event>>();
builder.Services.AddScoped<IRepository<Category>,Repository<Category>>();

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