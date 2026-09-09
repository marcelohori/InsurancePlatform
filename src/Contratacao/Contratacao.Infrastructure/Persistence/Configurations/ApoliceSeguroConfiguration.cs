using Contratacao.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Infrastructure.Persistence.Configurations;

public sealed class ApoliceSeguroConfiguration : IEntityTypeConfiguration<ApoliceSeguro>
{
    public void Configure(EntityTypeBuilder<ApoliceSeguro> builder)
    {
        builder.ToTable("contratacoes", t =>
        {
            t.HasCheckConstraint("CK_contratacoes_valor_premio_positivo", "\"valor_premio\" >= 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PropostaId).HasColumnName("proposta_id").IsRequired();

        builder.Property(a => a.NumeroApolice).HasColumnName("numero_apolice").HasMaxLength(40).IsRequired();

        builder.Property(a => a.DataContratacao).HasColumnName("data_contratacao").IsRequired();

        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.OwnsOne(a => a.Vigencia, vigencia =>
        {
            vigencia.Property(v => v.DataInicio).HasColumnName("vigencia_inicio");
            vigencia.Property(v => v.DataFim).HasColumnName("vigencia_fim");
        });

        builder.OwnsOne(a => a.ValorPremio, valorPremio =>
        {
            valorPremio.Property(v => v.Quantia).HasColumnName("valor_premio").HasColumnType("numeric(18,2)");
            valorPremio.Property(v => v.Moeda).HasColumnName("valor_premio_moeda").HasMaxLength(3);
        });

        builder.Navigation(a => a.Vigencia).IsRequired();
        builder.Navigation(a => a.ValorPremio).IsRequired();

        builder.HasIndex(a => a.PropostaId);
        builder.HasIndex(a => a.NumeroApolice).IsUnique();
    }
}
