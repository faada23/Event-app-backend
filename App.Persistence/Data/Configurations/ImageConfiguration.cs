using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.ToTable("Images");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                   .ValueGeneratedNever();

            builder.Property(i => i.StoredPath)
                .IsRequired()
                .HasMaxLength(1024); 

            builder.Property(i => i.ContentType)
                .IsRequired()
                .HasMaxLength(100); 

            builder.Property(i => i.UploadedAt)
                .IsRequired();
        }

}