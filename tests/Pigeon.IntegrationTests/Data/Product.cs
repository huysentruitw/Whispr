using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pigeon.IntegrationTests.Tests.Data;

public sealed record Product
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required decimal Price { get; init; }
}

public sealed class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product", "Application");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Price).IsRequired().HasColumnType("decimal(18, 2)");
    }
}
