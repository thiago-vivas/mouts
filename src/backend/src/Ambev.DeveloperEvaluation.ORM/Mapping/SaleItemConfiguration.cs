using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnType("uuid").ValueGeneratedNever();

        builder.Property(i => i.SaleId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.Discount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.IsCancelled);

        builder.OwnsOne(i => i.Product, p =>
        {
            p.Property(x => x.Id).HasColumnName("ProductId").IsRequired();
            p.Property(x => x.Name).HasColumnName("ProductName").HasMaxLength(100).IsRequired();
        });
        builder.Navigation(i => i.Product).IsRequired();
    }
}
