using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Lookups;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.IsActive).HasDbDefault("true");
        builder.Property(x => x.SortOrder).HasDbDefault("0");

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
