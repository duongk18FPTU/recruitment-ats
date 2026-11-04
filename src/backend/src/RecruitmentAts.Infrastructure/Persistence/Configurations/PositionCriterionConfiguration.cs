using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Positions;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class PositionCriterionConfiguration : IEntityTypeConfiguration<PositionCriterion>
{
    public void Configure(EntityTypeBuilder<PositionCriterion> builder)
    {
        builder.ToTable("position_criteria", table =>
        {
            table.HasValues<CriterionType>("type");
            table.HasCheckConstraint("ck_position_criteria_weight", "weight BETWEEN 1 AND 5");
        });

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Type).HasMaxLength(15);
        builder.Property(x => x.Weight).HasConversion<short>();
        builder.Property(x => x.SortOrder).HasDbDefault("0");

        builder.HasIndex(x => new { x.PositionId, x.Version }).HasDatabaseName("ix_position_criteria_version");
    }
}
