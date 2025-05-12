using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
    {
        public void Configure(EntityTypeBuilder<EventParticipant> builder)
        {
            builder.ToTable("EventParticipants");

            builder.HasKey(ep => new { ep.EventId, ep.UserId });

            builder.Property(ep => ep.EventRegistrationDate)
                .IsRequired();
        }

}