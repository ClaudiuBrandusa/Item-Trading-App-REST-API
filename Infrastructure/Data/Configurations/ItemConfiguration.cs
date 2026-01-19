using Domain.Entities.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(x => x.ItemId);

        builder.Property(x => x.Name).IsRequired();

        builder.ToTable(tb => tb.HasCheckConstraint("CK_Item_Name_MinLength", $"LEN(Name) >= {Item.MinimumNameLength}"));
    }
}
