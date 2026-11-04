using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class CvAnalysisConfiguration : IEntityTypeConfiguration<CvAnalysis>
{
    public void Configure(EntityTypeBuilder<CvAnalysis> builder)
    {
        builder.ToTable("cv_analyses", table =>
        {
            table.HasValues<AnalysisStatus>("status");
            table.HasCheckConstraint("ck_cv_analyses_ai_score", "ai_score BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_cv_analyses_final_score", "final_score BETWEEN 0 AND 100"); // BR-12, BR-13
        });

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.Status).HasMaxLength(10).HasDbDefault("'PENDING'");
        builder.Property(x => x.AttemptCount).HasConversion<short>().HasDbDefault("0");
        builder.Property(x => x.AvailableAt).HasDbDefault("now()");
        builder.Property(x => x.RequestedAt).HasDbDefault("now()");
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.Extracted).HasColumnType("jsonb");
        builder.Property(x => x.AiScore).HasConversion<short?>();
        builder.Property(x => x.FinalScore).HasConversion<short?>();
        builder.Property(x => x.IsOverridden).HasDbDefault("false");

        builder.HasOne<JobApplication>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CvFile>().WithMany().HasForeignKey(x => x.CvFileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Results).WithOne().HasForeignKey(x => x.AnalysisId).OnDelete(DeleteBehavior.Cascade);

        // The Worker claims jobs from this index (AD-02).
        builder.HasIndex(x => x.AvailableAt).HasDatabaseName("ix_cv_analyses_queue")
            .HasFilter("status IN ('PENDING', 'PROCESSING')");
        builder.HasIndex(x => new { x.ApplicationId, x.RequestedAt }).HasDatabaseName("ix_cv_analyses_application")
            .IsDescending(false, true);
    }
}
