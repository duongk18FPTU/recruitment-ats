using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // The unique index on lower(email) is an expression index, created in the migration.
        builder.ToTable("users", table => table.HasValues<Role>("role"));

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.FullName).HasMaxLength(120);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.Timezone).HasMaxLength(64).HasDbDefault("'Asia/Ho_Chi_Minh'");
        builder.Property(x => x.Role).HasMaxLength(20);
        builder.Property(x => x.IsActive).HasDbDefault("true");
        builder.Property(x => x.MustChangePassword).HasDbDefault("false");
        builder.Property(x => x.FailedLoginCount).HasDbDefault("0");
        builder.Property(x => x.CreatedAt).HasDbDefault("now()");
        builder.Property(x => x.UpdatedAt).HasDbDefault("now()");
    }
}
