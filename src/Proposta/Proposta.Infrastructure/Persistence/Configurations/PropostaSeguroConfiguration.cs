using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposta.Domain;
using Proposta.Domain.ValueObjects;

namespace Proposta.Infrastructure.Persistence.Configurations;

public sealed class PropostaSeguroConfiguration : IEntityTypeConfiguration<PropostaSeguro>
{
    public void Configure(EntityTypeBuilder<PropostaSeguro> builder)
    {
        builder.ToTable("propostas", t =>
        {
            t.HasCheckConstraint("CK_propostas_valor_cobertura_positivo", "\"valor_cobertura\" >= 0");
            t.HasCheckConstraint("CK_propostas_valor_premio_positivo", "\"valor_premio\" >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CriadoPor)
            .HasColumnName("criado_por")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.NomeSegurado)
            .HasColumnName("nome_segurado")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.DocumentoSegurado)
            .HasColumnName("documento_segurado")
            .HasConversion(
                documento => documento.Numero,
                numero => DocumentoIdentificacao.Criar(numero))
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(p => p.TipoSeguro)
            .HasColumnName("tipo_seguro")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.DataCriacao)
            .HasColumnName("data_criacao")
            .IsRequired();

        builder.OwnsOne(p => p.ValorCobertura, valorCobertura =>
        {
            valorCobertura.Property(v => v.Quantia).HasColumnName("valor_cobertura").HasColumnType("numeric(18,2)");
            valorCobertura.Property(v => v.Moeda).HasColumnName("valor_cobertura_moeda").HasMaxLength(3);
        });

        builder.OwnsOne(p => p.ValorPremio, valorPremio =>
        {
            valorPremio.Property(v => v.Quantia).HasColumnName("valor_premio").HasColumnType("numeric(18,2)");
            valorPremio.Property(v => v.Moeda).HasColumnName("valor_premio_moeda").HasMaxLength(3);
        });

        builder.Navigation(p => p.ValorCobertura).IsRequired();
        builder.Navigation(p => p.ValorPremio).IsRequired();

        builder.HasIndex(p => p.DocumentoSegurado);
        builder.HasIndex(p => new { p.CriadoPor, p.DataCriacao });
    }
}
