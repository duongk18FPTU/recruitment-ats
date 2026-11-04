using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class StageHistoryEntryConfiguration : IEntityTypeConfiguration<StageHistoryEntry>
{
    public void Configure(EntityTypeBuilder<StageHistoryEntry> builder)
    {
        builder.ToTable("stage_history");

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.FromStage).HasMaxLength(10);
        builder.Property(x => x.ToStage).HasMaxLength(10);
        builder.Property(x => x.RejectionReason).HasMaxLength(25);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDbDefault("now()");

        builder.HasOne<JobApplication>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ApplicationId, x.CreatedAt }).HasDatabaseName("ix_stage_history_application");
        builder.HasIndex(x => new { x.ToStage, x.CreatedAt }).HasDatabaseName("ix_stage_history_report");
    }
}
