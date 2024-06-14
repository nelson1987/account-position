using Aeon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aeon.Infrastructure.Contexts;

public class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("TB_PRODUTO");
        builder.HasKey(x => x.Id)
                .HasName("IDT_PRODUTO");
        //.IsRequired();
    }
}