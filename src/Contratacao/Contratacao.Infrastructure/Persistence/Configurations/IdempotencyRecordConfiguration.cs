using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(r => r.Chave);

        builder.Property(r => r.Chave).HasColumnName("chave").HasMaxLength(200);

        builder.Property(r => r.ContratacaoId).HasColumnName("contratacao_id").IsRequired();

        builder.Property(r => r.HashRequisicao).HasColumnName("hash_requisicao").HasMaxLength(64).IsRequired();

        builder.Property(r => r.CriadoEm).HasColumnName("criado_em").IsRequired();
    }
}
