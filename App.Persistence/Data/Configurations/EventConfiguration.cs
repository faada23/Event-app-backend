using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventConfiguration : IEntityTypeConfiguration<Event>{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired();

        builder.Property(e => e.DateTimeOfEvent)
            .IsRequired();

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.MaxParticipants)
            .IsRequired(); 
                    
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_MaxParticipants_GreaterThanZero", "\"MaxParticipants\" > 0"));

        builder.HasOne(e => e.Category)        
                .WithMany(c => c.Events)        
                .HasForeignKey(e => e.CategoryId) 
                .IsRequired()                   
                .OnDelete(DeleteBehavior.Restrict); 

        builder.HasOne(e => e.Image)          
                .WithOne(i => i.Event)         
                .HasForeignKey<Image>(i => i.Id)                        
                .IsRequired(false)           
                .OnDelete(DeleteBehavior.Cascade); 

        builder.HasMany(e => e.EventParticipants) 
                .WithOne(ep => ep.Event)           
                .HasForeignKey(ep => ep.EventId)   
                .IsRequired()                      
                .OnDelete(DeleteBehavior.Cascade);  
    }

}