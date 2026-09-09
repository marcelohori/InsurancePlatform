using Analise.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analise.Infrastructure.Persistence.Configurations;

public sealed class AnaliseRiscoConfiguration : IEntityTypeConfiguration<AnaliseRisco>
{
    public void Configure(EntityTypeBuilder<AnaliseRisco> builder)
    {
        builder.ToTable("analises", t =>
        {
            t.HasCheckConstraint("CK_analises_score_risco_range", "\"score_risco\" IS NULL OR (\"score_risco\" >= 0 AND \"score_risco\" <= 100)");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PropostaId).HasColumnName("proposta_id").IsRequired();

        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(a => a.ScoreRisco).HasColumnName("score_risco");

        builder.Property(a => a.Recomendacao).HasColumnName("recomendacao").HasConversion<string>().HasMaxLength(20);

        builder.Property(a => a.Justificativa).HasColumnName("justificativa").HasMaxLength(2000);

        builder.Property(a => a.DataCriacao).HasColumnName("data_criacao").IsRequired();

        builder.Property(a => a.DataConclusao).HasColumnName("data_conclusao");

        builder.HasIndex(a => a.PropostaId).IsUnique();
    }
}
