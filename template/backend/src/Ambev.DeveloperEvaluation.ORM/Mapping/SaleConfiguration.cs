using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(s => s.SaleNumber).IsUnique();

        builder.Property(s => s.SaleDate).IsRequired();
        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.IsCancelled);
        builder.Property(s => s.CreatedAt);
        builder.Property(s => s.UpdatedAt);

        builder.OwnsOne(s => s.Customer, c =>
        {
            c.Property(p => p.Id).HasColumnName("CustomerId").IsRequired();
            c.Property(p => p.Name).HasColumnName("CustomerName").HasMaxLength(100).IsRequired();
        });
        builder.Navigation(s => s.Customer).IsRequired();

        builder.OwnsOne(s => s.Branch, b =>
        {
            b.Property(p => p.Id).HasColumnName("BranchId").IsRequired();
            b.Property(p => p.Name).HasColumnName("BranchName").HasMaxLength(100).IsRequired();
        });
        builder.Navigation(s => s.Branch).IsRequired();

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Sale.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
