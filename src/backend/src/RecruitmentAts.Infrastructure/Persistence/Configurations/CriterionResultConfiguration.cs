using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class CriterionResultConfiguration : IEntityTypeConfiguration<CriterionResult>
{
    public void Configure(EntityTypeBuilder<CriterionResult> builder)
    {
        builder.ToTable("analysis_criterion_results", table =>
        {
            table.HasValues<CriterionOutcome>("ai_result");
            table.HasValues<CriterionOutcome>("final_result");
            table.HasCheckConstraint("ck_analysis_criterion_results_override_reason",
                "override_reason IS NULL OR char_length(override_reason) >= 10"); // BR-13
            table.HasCheckConstraint("ck_analysis_criterion_results_override",
                "(overridden_by IS NULL) = (override_reason IS NULL)");
        });

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.AiResult).HasMaxLength(10);
        builder.Property(x => x.AiQuote).HasMaxLength(1000);
        builder.Property(x => x.AiExplanation).HasMaxLength(1000);
        builder.Property(x => x.FinalResult).HasMaxLength(10);
        builder.Property(x => x.OverrideReason).HasMaxLength(500);

        builder.HasOne<PositionCriterion>().WithMany().HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.OverriddenBy).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.AnalysisId, x.CriterionId }).HasDatabaseName("ux_analysis_criterion").IsUnique();
    }
}
