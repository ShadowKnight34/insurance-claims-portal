using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimsPortal.Claims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClaimantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IncidentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IncidentLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IncidentDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    EstimatedLossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedSettlementAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AssignedOfficerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claims_users_AssignedOfficerId",
                        column: x => x.AssignedOfficerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_claims_users_ClaimantId",
                        column: x => x.ClaimantId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claim_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claim_assessments_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_assessments_users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claim_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claim_assignments_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_assignments_users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_claim_assignments_users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claim_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claim_documents_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_documents_users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claim_information_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Response = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_information_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claim_information_requests_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_information_requests_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claim_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claim_status_history_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_status_history_users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_claim_assessments_ClaimId",
                table: "claim_assessments",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_claim_assessments_OfficerId",
                table: "claim_assessments",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_claim_assignments_AssignedById",
                table: "claim_assignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_claim_assignments_ClaimId",
                table: "claim_assignments",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_claim_assignments_OfficerId",
                table: "claim_assignments",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_claim_documents_ClaimId",
                table: "claim_documents",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_claim_documents_UploadedById",
                table: "claim_documents",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_claim_information_requests_ClaimId",
                table: "claim_information_requests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_claim_information_requests_RequestedById",
                table: "claim_information_requests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_claim_status_history_ChangedById",
                table: "claim_status_history",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_claim_status_history_ClaimId",
                table: "claim_status_history",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_AssignedOfficerId",
                table: "claims",
                column: "AssignedOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_ClaimantId",
                table: "claims",
                column: "ClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_CreatedAt",
                table: "claims",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_claims_ReferenceNumber",
                table: "claims",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_claims_Status",
                table: "claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "claim_assessments");

            migrationBuilder.DropTable(
                name: "claim_assignments");

            migrationBuilder.DropTable(
                name: "claim_documents");

            migrationBuilder.DropTable(
                name: "claim_information_requests");

            migrationBuilder.DropTable(
                name: "claim_status_history");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
