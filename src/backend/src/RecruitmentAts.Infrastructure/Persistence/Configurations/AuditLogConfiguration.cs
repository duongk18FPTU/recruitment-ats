using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", table =>
        {
            table.HasValues<AuditActorType>("actor_type");
            table.HasValues<AuditModule>("module");
            table.HasValues<AuditAction>("action");
        });

        builder.Property(x => x.Id).UseIdentityAlwaysColumn();
        builder.Property(x => x.OccurredAt).HasDbDefault("now()");
        builder.Property(x => x.ActorType).HasMaxLength(10);
        builder.Property(x => x.Module).HasMaxLength(15);
        builder.Property(x => x.Action).HasMaxLength(20);
        builder.Property(x => x.EntityType).HasMaxLength(30);
        builder.Property(x => x.Summary).HasMaxLength(300);
        builder.Property(x => x.Details).HasColumnType("jsonb"); // before/after values; never secrets or CV text
        builder.Property(x => x.TraceId).HasMaxLength(64);

        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_audit_logs_time").IsDescending();
        builder.HasIndex(x => new { x.Module, x.Action, x.OccurredAt }).HasDatabaseName("ix_audit_logs_filter")
            .IsDescending(false, false, true);
        builder.HasIndex(x => new { x.ActorId, x.OccurredAt }).HasDatabaseName("ix_audit_logs_actor")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("ix_audit_logs_entity");
    }
}
