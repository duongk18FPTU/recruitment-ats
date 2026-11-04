using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Candidates;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates", table => table.HasCheckConstraint(
            "ck_candidates_contact", "email IS NOT NULL OR phone IS NOT NULL OR is_anonymized"));

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.FullName).HasMaxLength(120);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.EmailNormalized).HasMaxLength(254);
        builder.Property(x => x.EmailInvalid).HasDbDefault("false");
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.PhoneNormalized).HasMaxLength(15);
        builder.Property(x => x.Skills).HasDbDefault("'{}'");
        builder.Property(x => x.ConsentTokenVersion).HasDbDefault("1");
        builder.Property(x => x.LastActivityAt).HasDbDefault("now()");
        builder.Property(x => x.IsAnonymized).HasDbDefault("false");
        builder.Property(x => x.CreatedAt).HasDbDefault("now()");
        builder.Property(x => x.UpdatedAt).HasDbDefault("now()");

        // BR-06: one active candidate per normalized email or phone.
        builder.HasIndex(x => x.EmailNormalized).HasDatabaseName("ux_candidates_email").IsUnique()
            .HasFilter("email_normalized IS NOT NULL AND NOT is_anonymized");
        builder.HasIndex(x => x.PhoneNormalized).HasDatabaseName("ux_candidates_phone").IsUnique()
            .HasFilter("phone_normalized IS NOT NULL AND NOT is_anonymized");
        builder.HasIndex(x => x.PoolConsentExpiresAt).HasDatabaseName("ix_candidates_pool")
            .HasFilter("pool_consent_expires_at IS NOT NULL");
        builder.HasIndex(x => x.Skills).HasDatabaseName("ix_candidates_skills").HasMethod("gin");
    }
}
