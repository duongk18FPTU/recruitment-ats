using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal sealed class CvFileConfiguration : IEntityTypeConfiguration<CvFile>
{
    public void Configure(EntityTypeBuilder<CvFile> builder)
    {
        builder.ToTable("cv_files", table =>
        {
            table.HasCheckConstraint("ck_cv_files_content_type",
                "content_type IN ('application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document')");
            table.HasCheckConstraint("ck_cv_files_size_bytes", "size_bytes > 0 AND size_bytes <= 10485760"); // BR-15
        });

        builder.Property(x => x.Id).HasDbDefault("gen_random_uuid()");
        builder.Property(x => x.StorageKey).HasMaxLength(200);
        builder.Property(x => x.OriginalName).HasMaxLength(255);
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.Property(x => x.Sha256).HasColumnType("character(64)");
        builder.Property(x => x.UploadedAt).HasDbDefault("now()");

        builder.HasOne<Candidate>().WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => x.CandidateId).HasDatabaseName("ix_cv_files_candidate");
    }
}
