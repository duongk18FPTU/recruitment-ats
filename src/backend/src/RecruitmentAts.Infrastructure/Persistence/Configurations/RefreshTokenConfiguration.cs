using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Auth;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.TokenHash).HasColumnType("character(64)");
        builder.Property(x => x.CreatedAt).HasDbDefault("now()");
        builder.Property(x => x.UserAgent).HasMaxLength(300);

        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ReplacedById).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_refresh_tokens_user").HasFilter("revoked_at IS NULL");
        builder.HasIndex(x => x.FamilyId).HasDatabaseName("ix_refresh_tokens_family");
    }
}
