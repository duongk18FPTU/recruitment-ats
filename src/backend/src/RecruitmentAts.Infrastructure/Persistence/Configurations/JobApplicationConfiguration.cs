using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("applications", table =>
        {
            table.HasValues<Stage>("stage");
            table.HasValues<Source>("source");
            table.HasValues<IntakeChannel>("intake_channel");
            table.HasValues<RejectionReason>("rejection_reason");
            table.HasCheckConstraint("ck_applications_fit_score", "fit_score BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_applications_source_note", "source <> 'OTHER' OR source_note IS NOT NULL"); // BR-18
            table.HasCheckConstraint("ck_applications_rejection",
                "(stage = 'REJECTED') = (rejection_reason IS NOT NULL)"); // BR-02
        });

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.Stage).HasMaxLength(10).HasDbDefault("'NEW'");
        builder.Property(x => x.Source).HasMaxLength(15);
        builder.Property(x => x.SourceNote).HasMaxLength(200);
        builder.Property(x => x.IntakeChannel).HasMaxLength(12);
        builder.Property(x => x.CoverMessage).HasMaxLength(2000);
        builder.Property(x => x.FitScore).HasConversion<short?>();
        builder.Property(x => x.MissingMustHave).HasDbDefault("false");
        builder.Property(x => x.RejectionReason).HasMaxLength(25);
        builder.Property(x => x.RejectionNote).HasMaxLength(500);
        builder.Property(x => x.AppliedAt).HasDbDefault("now()");
        builder.Property(x => x.StageChangedAt).HasDbDefault("now()");
        builder.Property(x => x.UpdatedAt).HasDbDefault("now()");

        builder.HasOne<Position>().WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Candidate>().WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<CvFile>().WithMany().HasForeignKey(x => x.CurrentCvFileId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<CvAnalysis>().WithMany().HasForeignKey(x => x.CurrentAnalysisId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
        // import_batch_id gets its foreign key with the import_batches table (US16, M4).

        builder.HasIndex(x => new { x.PositionId, x.CandidateId })
            .HasDatabaseName("ux_applications_position_candidate").IsUnique(); // BR-06
        builder.HasIndex(x => new { x.PositionId, x.Stage }).HasDatabaseName("ix_applications_position_stage");
        builder.HasIndex(x => x.CandidateId).HasDatabaseName("ix_applications_candidate");
        builder.HasIndex(x => x.FitScore).HasDatabaseName("ix_applications_score")
            .IsDescending().HasNullSortOrder(NullSortOrder.NullsLast);
        builder.HasIndex(x => x.AppliedAt).HasDatabaseName("ix_applications_applied").IsDescending();
    }
}
