// Data/DatabaseInitializer.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class DatabaseInitializer
{
    public async static Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<DatabaseContext>();
        const string defaultAdminEmail = "tempMail@abc.com";

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedRoles(context);
            await SeedUsers(context,defaultAdminEmail);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {   
            await transaction.RollbackAsync();
            Console.WriteLine("Exception while Initializing Database: "+ex);
        }
    }

    private async static Task SeedRoles(DatabaseContext context)
    {
        if (!await context.Roles.AnyAsync())
        {   
            var adminRoleName = RoleConstants.Administrator;
            var userRoleName = RoleConstants.User;

            await context.Roles.AddRangeAsync(
                new Role{Name = adminRoleName},
                new Role{Name = userRoleName}
            );
            await context.SaveChangesAsync();
        }
    }

    private async static Task SeedUsers(DatabaseContext context, string defaultAdminEmail)
    {   
        
        if (!await context.Users.AnyAsync(u => u.Email == defaultAdminEmail))
        {
            var hasher = new PasswordHasher<User>();
            var dateTime = DateTime.UtcNow;

            var AP1 = Environment.GetEnvironmentVariable("EventAppAP1") 
                    ?? throw new ArgumentNullException("EventAppAP1", "Admin password is not set");

            var adminRole = await context.Roles.FirstAsync(r => r.Name == RoleConstants.Administrator);
            var userRole = await context.Roles.FirstAsync(r => r.Name == RoleConstants.User); 

            var user = new User{
                    Email = defaultAdminEmail, 
                    FirstName = "Admin",
                    LastName = "Admin",
                    DateOfBirth = DateOnly.Parse("01.01.2000"),
                    PasswordHash = AP1
                };

            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

            user.Roles = new List<Role> { adminRole, userRole };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }
    }
}