using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RecruitmentAts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M1CoreScreening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "position_code_seq");

            migrationBuilder.CreateTable(
                name: "candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    email_normalized = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    email_invalid = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_normalized = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    skills = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    pool_consent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pool_consent_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pool_renewal_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_token_version = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "1"),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_anonymized = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidates", x => x.id);
                    table.CheckConstraint("ck_candidates_contact", "email IS NOT NULL OR phone IS NOT NULL OR is_anonymized");
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValueSql: "'Asia/Ho_Chi_Minh'"),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true"),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_role", "role IN ('ADMIN', 'HR_MANAGER', 'RECRUITER', 'INTERVIEWER')");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actor_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    module = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.CheckConstraint("ck_audit_logs_action", "action IN ('LOGIN', 'LOGIN_FAILED', 'LOGOUT', 'CREATE', 'UPDATE', 'STATUS_CHANGE', 'STAGE_CHANGE', 'AI_OVERRIDE', 'PUBLISH', 'UNPUBLISH', 'EXPORT', 'IMPORT', 'ASSIGN', 'DISMISS', 'CANCEL', 'RESEND', 'SUBMIT', 'CONSENT_CHANGE', 'CV_DELETE', 'ANONYMIZE')");
                    table.CheckConstraint("ck_audit_logs_actor_type", "actor_type IN ('USER', 'SYSTEM', 'CANDIDATE')");
                    table.CheckConstraint("ck_audit_logs_module", "module IN ('AUTH', 'POSITION', 'JOB_POST', 'APPLICATION', 'CANDIDATE', 'AI', 'INTERVIEW', 'EMAIL', 'INBOX', 'IMPORT', 'TALENT_POOL', 'REPORT', 'USER', 'SETTINGS', 'CONSENT')");
                    table.ForeignKey(
                        name: "fk_audit_logs_user_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cv_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<int>(type: "integer", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", nullable: false),
                    extracted_text = table.Column<string>(type: "text", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cv_files", x => x.id);
                    table.CheckConstraint("ck_cv_files_content_type", "content_type IN ('application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document')");
                    table.CheckConstraint("ck_cv_files_size_bytes", "size_bytes > 0 AND size_bytes <= 10485760");
                    table.ForeignKey(
                        name: "fk_cv_files_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cv_files_user_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false, defaultValueSql: "'JOB-' || lpad(nextval('position_code_seq')::text, 3, '0')"),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    headcount = table.Column<int>(type: "integer", nullable: false),
                    target_hire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'DRAFT'"),
                    criteria_version = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "1"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.CheckConstraint("ck_positions_employment_type", "employment_type IN ('FULL_TIME', 'PART_TIME', 'CONTRACT', 'INTERNSHIP')");
                    table.CheckConstraint("ck_positions_headcount", "headcount >= 1");
                    table.CheckConstraint("ck_positions_level", "level IN ('INTERN', 'FRESHER', 'JUNIOR', 'MIDDLE', 'SENIOR', 'LEAD', 'MANAGER')");
                    table.CheckConstraint("ck_positions_status", "status IN ('DRAFT', 'OPEN', 'CLOSED')");
                    table.ForeignKey(
                        name: "fk_positions_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_user_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_user_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_refresh_tokens_replaced_by_id",
                        column: x => x.replaced_by_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "position_criteria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    weight = table.Column<short>(type: "smallint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_position_criteria", x => x.id);
                    table.CheckConstraint("ck_position_criteria_type", "type IN ('MUST_HAVE', 'NICE_TO_HAVE')");
                    table.CheckConstraint("ck_position_criteria_weight", "weight BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_position_criteria_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_call_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'ANALYSIS'"),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "1"),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    http_status = table.Column<short>(type: "smallint", nullable: true),
                    input_chars = table.Column<int>(type: "integer", nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_call_logs", x => x.id);
                    table.CheckConstraint("ck_ai_call_logs_analysis", "(purpose = 'ANALYSIS') = (analysis_id IS NOT NULL)");
                    table.CheckConstraint("ck_ai_call_logs_outcome", "outcome IN ('SUCCESS', 'TIMEOUT', 'RATE_LIMIT', 'ERROR', 'INVALID')");
                    table.CheckConstraint("ck_ai_call_logs_purpose", "purpose IN ('ANALYSIS', 'CRITERIA_SUGGESTION', 'INTERVIEW_QUESTIONS', 'CV_EMBEDDING', 'POSITION_EMBEDDING')");
                    table.ForeignKey(
                        name: "fk_ai_call_logs_user_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "analysis_criterion_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criterion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_result = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ai_quote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ai_explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    final_result = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    overridden_by = table.Column<Guid>(type: "uuid", nullable: true),
                    overridden_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_criterion_results", x => x.id);
                    table.CheckConstraint("ck_analysis_criterion_results_ai_result", "ai_result IN ('MET', 'PARTIAL', 'NOT_MET', 'UNVERIFIED')");
                    table.CheckConstraint("ck_analysis_criterion_results_final_result", "final_result IN ('MET', 'PARTIAL', 'NOT_MET', 'UNVERIFIED')");
                    table.CheckConstraint("ck_analysis_criterion_results_override", "(overridden_by IS NULL) = (override_reason IS NULL)");
                    table.CheckConstraint("ck_analysis_criterion_results_override_reason", "override_reason IS NULL OR char_length(override_reason) >= 10");
                    table.ForeignKey(
                        name: "fk_analysis_criterion_results_position_criterion_criterion_id",
                        column: x => x.criterion_id,
                        principalTable: "position_criteria",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_analysis_criterion_results_user_overridden_by",
                        column: x => x.overridden_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'NEW'"),
                    source = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    source_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    intake_channel = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    cover_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    current_cv_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fit_score = table.Column<short>(type: "smallint", nullable: true),
                    missing_must_have = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false"),
                    rejection_reason = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    rejection_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    stage_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_applications", x => x.id);
                    table.CheckConstraint("ck_applications_fit_score", "fit_score BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_applications_intake_channel", "intake_channel IN ('CAREER_SITE', 'MAILBOX', 'MANUAL', 'IMPORT', 'TALENT_POOL')");
                    table.CheckConstraint("ck_applications_rejection", "(stage = 'REJECTED') = (rejection_reason IS NOT NULL)");
                    table.CheckConstraint("ck_applications_rejection_reason", "rejection_reason IN ('SKILLS_MISMATCH', 'EXPERIENCE_MISMATCH', 'SALARY_MISMATCH', 'FAILED_INTERVIEW', 'CULTURE_FIT', 'POSITION_FILLED', 'NO_RESPONSE', 'CANDIDATE_WITHDREW', 'OTHER')");
                    table.CheckConstraint("ck_applications_source", "source IN ('CAREER_SITE', 'LINKEDIN', 'FACEBOOK', 'TOPCV', 'VIETNAMWORKS', 'EMAIL', 'REFERRAL', 'OTHER')");
                    table.CheckConstraint("ck_applications_source_note", "source <> 'OTHER' OR source_note IS NOT NULL");
                    table.CheckConstraint("ck_applications_stage", "stage IN ('NEW', 'SCREENING', 'INTERVIEW', 'OFFER', 'HIRED', 'REJECTED')");
                    table.ForeignKey(
                        name: "fk_applications_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_applications_cv_files_current_cv_file_id",
                        column: x => x.current_cv_file_id,
                        principalTable: "cv_files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_applications_position_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_applications_user_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cv_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criteria_version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'PENDING'"),
                    attempt_count = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "0"),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    extracted = table.Column<string>(type: "jsonb", nullable: true),
                    ai_score = table.Column<short>(type: "smallint", nullable: true),
                    final_score = table.Column<short>(type: "smallint", nullable: true),
                    missing_must_have = table.Column<bool>(type: "boolean", nullable: true),
                    is_overridden = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cv_analyses", x => x.id);
                    table.CheckConstraint("ck_cv_analyses_ai_score", "ai_score BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_cv_analyses_final_score", "final_score BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_cv_analyses_status", "status IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED', 'OUTDATED')");
                    table.ForeignKey(
                        name: "fk_cv_analyses_cv_file_cv_file_id",
                        column: x => x.cv_file_id,
                        principalTable: "cv_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cv_analyses_job_application_application_id",
                        column: x => x.application_id,
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cv_analyses_user_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stage_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    to_stage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stage_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_stage_history_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stage_history_user_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_call_logs_actor",
                table: "ai_call_logs",
                columns: new[] { "actor_id", "started_at" },
                descending: new[] { false, true },
                filter: "actor_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ai_call_logs_started",
                table: "ai_call_logs",
                column: "started_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ux_analysis_criterion",
                table: "analysis_criterion_results",
                columns: new[] { "analysis_id", "criterion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_applications_applied",
                table: "applications",
                column: "applied_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_applications_candidate",
                table: "applications",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_position_stage",
                table: "applications",
                columns: new[] { "position_id", "stage" });

            migrationBuilder.CreateIndex(
                name: "ix_applications_score",
                table: "applications",
                column: "fit_score",
                descending: new bool[0])
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "ux_applications_position_candidate",
                table: "applications",
                columns: new[] { "position_id", "candidate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor",
                table: "audit_logs",
                columns: new[] { "actor_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_filter",
                table: "audit_logs",
                columns: new[] { "module", "action", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_time",
                table: "audit_logs",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_candidates_pool",
                table: "candidates",
                column: "pool_consent_expires_at",
                filter: "pool_consent_expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_candidates_skills",
                table: "candidates",
                column: "skills")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ux_candidates_email",
                table: "candidates",
                column: "email_normalized",
                unique: true,
                filter: "email_normalized IS NOT NULL AND NOT is_anonymized");

            migrationBuilder.CreateIndex(
                name: "ux_candidates_phone",
                table: "candidates",
                column: "phone_normalized",
                unique: true,
                filter: "phone_normalized IS NOT NULL AND NOT is_anonymized");

            migrationBuilder.CreateIndex(
                name: "ix_cv_analyses_application",
                table: "cv_analyses",
                columns: new[] { "application_id", "requested_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_cv_analyses_queue",
                table: "cv_analyses",
                column: "available_at",
                filter: "status IN ('PENDING', 'PROCESSING')");

            migrationBuilder.CreateIndex(
                name: "ix_cv_files_candidate",
                table: "cv_files",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_cv_files_storage_key",
                table: "cv_files",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_name",
                table: "departments",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_position_criteria_version",
                table: "position_criteria",
                columns: new[] { "position_id", "version" });

            migrationBuilder.CreateIndex(
                name: "ix_positions_code",
                table: "positions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_positions_owner",
                table: "positions",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_status",
                table: "positions",
                columns: new[] { "status", "department_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user",
                table: "refresh_tokens",
                column: "user_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stage_history_application",
                table: "stage_history",
                columns: new[] { "application_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stage_history_report",
                table: "stage_history",
                columns: new[] { "to_stage", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_ai_call_logs_cv_analysis_analysis_id",
                table: "ai_call_logs",
                column: "analysis_id",
                principalTable: "cv_analyses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_analysis_criterion_results_cv_analyses_analysis_id",
                table: "analysis_criterion_results",
                column: "analysis_id",
                principalTable: "cv_analyses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_applications_cv_analyses_current_analysis_id",
                table: "applications",
                column: "current_analysis_id",
                principalTable: "cv_analyses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Expression index: EF Core cannot model it (schema.sql, BR-06 for users).
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_users_email ON users (lower(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ux_users_email;");

            migrationBuilder.DropForeignKey(
                name: "fk_applications_cv_analyses_current_analysis_id",
                table: "applications");

            migrationBuilder.DropTable(
                name: "ai_call_logs");

            migrationBuilder.DropTable(
                name: "analysis_criterion_results");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "stage_history");

            migrationBuilder.DropTable(
                name: "position_criteria");

            migrationBuilder.DropTable(
                name: "cv_analyses");

            migrationBuilder.DropTable(
                name: "applications");

            migrationBuilder.DropTable(
                name: "cv_files");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "candidates");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropSequence(
                name: "position_code_seq");
        }
    }
}
