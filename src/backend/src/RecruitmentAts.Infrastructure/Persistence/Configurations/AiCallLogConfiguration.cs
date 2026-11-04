using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class AiCallLogConfiguration : IEntityTypeConfiguration<AiCallLog>
{
    public void Configure(EntityTypeBuilder<AiCallLog> builder)
    {
        builder.ToTable("ai_call_logs", table =>
        {
            table.HasValues<AiCallPurpose>("purpose");
            table.HasValues<AiCallOutcome>("outcome");
            table.HasCheckConstraint("ck_ai_call_logs_analysis", "(purpose = 'ANALYSIS') = (analysis_id IS NOT NULL)");
        });

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.Purpose).HasMaxLength(20).HasDbDefault("'ANALYSIS'");
        builder.Property(x => x.Attempt).HasConversion<short>().HasDbDefault("1");
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.Outcome).HasMaxLength(10);
        builder.Property(x => x.HttpStatus).HasConversion<short?>();
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);

        builder.HasOne<CvAnalysis>().WithMany().HasForeignKey(x => x.AnalysisId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.StartedAt).HasDatabaseName("ix_ai_call_logs_started").IsDescending();
        // BR-26: suggestion calls per user in the last hour.
        builder.HasIndex(x => new { x.ActorId, x.StartedAt }).HasDatabaseName("ix_ai_call_logs_actor")
            .IsDescending(false, true).HasFilter("actor_id IS NOT NULL");
    }
}
