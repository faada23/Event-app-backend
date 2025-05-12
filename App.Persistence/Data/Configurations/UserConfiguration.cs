using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(254);

            builder.Property(u => u.DateOfBirth)
                .IsRequired(); 

            builder.Property(u => u.PasswordHash)
                .IsRequired();


            builder.Property(u => u.SystemRegistrationDate)
                .IsRequired();

            builder.HasMany(e => e.Roles)
                    .WithMany(e => e.Users)
                    .UsingEntity(
                        "UserRole",
                        l => l.HasOne(typeof(Role)).WithMany().HasForeignKey("RolesId").HasPrincipalKey(nameof(Role.Id)),
                        r => r.HasOne(typeof(User)).WithMany().HasForeignKey("UsersId").HasPrincipalKey(nameof(User.Id)),
                        j => j.HasKey("UsersId", "RolesId"));

            builder.HasMany(u => u.EventParticipations)
                   .WithOne(ep => ep.User)              
                   .HasForeignKey(ep => ep.UserId)      
                   .IsRequired()                        
                   .OnDelete(DeleteBehavior.Cascade);   

            builder.HasMany(u => u.RefreshTokens)       
                   .WithOne(rt => rt.User)              
                   .HasForeignKey(rt => rt.UserId)      
                   .IsRequired()                       
                   .OnDelete(DeleteBehavior.Cascade);   
        }

}