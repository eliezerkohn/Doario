using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentTypes",
                columns: table => new
                {
                    AssignmentTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentTypes", x => x.AssignmentTypeId);
                });

            migrationBuilder.CreateTable(
                name: "DocumentStatuses",
                columns: table => new
                {
                    DocumentStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStatuses", x => x.DocumentStatusId);
                });

            migrationBuilder.CreateTable(
                name: "MessageTypes",
                columns: table => new
                {
                    MessageTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTypes", x => x.MessageTypeId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTypes",
                columns: table => new
                {
                    NotificationTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTypes", x => x.NotificationTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SourceTypes",
                columns: table => new
                {
                    SourceTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceTypes", x => x.SourceTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SystemStatuses",
                columns: table => new
                {
                    SystemStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemStatuses", x => x.SystemStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SharePointSiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SharePointSiteId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AzureTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AzureClientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnknownSenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MailboxAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsHipaaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BAAReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BAASignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BAAExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScanInboxAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "DocumentUsages",
                columns: table => new
                {
                    DocumentUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemStatusId = table.Column<int>(type: "int", nullable: false),
                    BillingMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalDocuments = table.Column<int>(type: "int", nullable: false),
                    IncludedDocuments = table.Column<int>(type: "int", nullable: false),
                    ExtraDocuments = table.Column<int>(type: "int", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExtraCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCharged = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StripeInvoiceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActiveStaffCount = table.Column<int>(type: "int", nullable: false),
                    BilledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentUsages", x => x.DocumentUsageId);
                    table.ForeignKey(
                        name: "FK_DocumentUsages_SystemStatuses_SystemStatusId",
                        column: x => x.SystemStatusId,
                        principalTable: "SystemStatuses",
                        principalColumn: "SystemStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentUsages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportedStaff",
                columns: table => new
                {
                    ImportedStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTypeId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficeLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsAdminOverridden = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedStaff", x => x.ImportedStaffId);
                    table.ForeignKey(
                        name: "FK_ImportedStaff_SourceTypes_SourceTypeId",
                        column: x => x.SourceTypeId,
                        principalTable: "SourceTypes",
                        principalColumn: "SourceTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedStaff_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SenderTypes",
                columns: table => new
                {
                    SenderTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayNameColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    EmailContentLevel = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SenderTypes", x => x.SenderTypeId);
                    table.ForeignKey(
                        name: "FK_SenderTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffSyncLogs",
                columns: table => new
                {
                    StaffSyncLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTypeId = table.Column<int>(type: "int", nullable: false),
                    SystemStatusId = table.Column<int>(type: "int", nullable: false),
                    SyncStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordsSynced = table.Column<int>(type: "int", nullable: false),
                    RecordsAdded = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffSyncLogs", x => x.StaffSyncLogId);
                    table.ForeignKey(
                        name: "FK_StaffSyncLogs_SourceTypes_SourceTypeId",
                        column: x => x.SourceTypeId,
                        principalTable: "SourceTypes",
                        principalColumn: "SourceTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffSyncLogs_SystemStatuses_SystemStatusId",
                        column: x => x.SystemStatusId,
                        principalTable: "SystemStatuses",
                        principalColumn: "SystemStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffSyncLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantConnections",
                columns: table => new
                {
                    TenantConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTypeId = table.Column<int>(type: "int", nullable: false),
                    SystemStatusId = table.Column<int>(type: "int", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConnections", x => x.TenantConnectionId);
                    table.ForeignKey(
                        name: "FK_TenantConnections_SourceTypes_SourceTypeId",
                        column: x => x.SourceTypeId,
                        principalTable: "SourceTypes",
                        principalColumn: "SourceTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantConnections_SystemStatuses_SystemStatusId",
                        column: x => x.SystemStatusId,
                        principalTable: "SystemStatuses",
                        principalColumn: "SystemStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantConnections_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantConnectorConfigs",
                columns: table => new
                {
                    TenantConnectorConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTypeId = table.Column<int>(type: "int", nullable: false),
                    SystemStatusId = table.Column<int>(type: "int", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StaffTableName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffIdColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffNameColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffEmailColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffJobTitleColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffDepartmentColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderTableName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderIdColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderNameColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderEmailColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderTypeIdColumn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastTestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConnectorConfigs", x => x.TenantConnectorConfigId);
                    table.ForeignKey(
                        name: "FK_TenantConnectorConfigs_SourceTypes_SourceTypeId",
                        column: x => x.SourceTypeId,
                        principalTable: "SourceTypes",
                        principalColumn: "SourceTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantConnectorConfigs_SystemStatuses_SystemStatusId",
                        column: x => x.SystemStatusId,
                        principalTable: "SystemStatuses",
                        principalColumn: "SystemStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantConnectorConfigs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                columns: table => new
                {
                    TenantSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludedDocuments = table.Column<int>(type: "int", nullable: false),
                    ExtraDocumentPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PricePerStaff = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DocsPerStaff = table.Column<int>(type: "int", nullable: false),
                    StripePlanId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.TenantSubscriptionId);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportedSenders",
                columns: table => new
                {
                    ImportedSenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderTypeId = table.Column<int>(type: "int", nullable: false),
                    SenderTypeId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceTypeId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedSenders", x => x.ImportedSenderId);
                    table.ForeignKey(
                        name: "FK_ImportedSenders_SenderTypes_SenderTypeId1",
                        column: x => x.SenderTypeId1,
                        principalTable: "SenderTypes",
                        principalColumn: "SenderTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedSenders_SourceTypes_SourceTypeId",
                        column: x => x.SourceTypeId,
                        principalTable: "SourceTypes",
                        principalColumn: "SourceTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedSenders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Senders",
                columns: table => new
                {
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senders", x => x.SenderId);
                    table.ForeignKey(
                        name: "FK_Senders_SenderTypes_SenderTypeId",
                        column: x => x.SenderTypeId,
                        principalTable: "SenderTypes",
                        principalColumn: "SenderTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Senders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentStatusId = table.Column<int>(type: "int", nullable: false),
                    SenderTypeId = table.Column<int>(type: "int", nullable: false),
                    SenderTypeId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharePointUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OcrText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderMatchConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentStatuses_DocumentStatusId",
                        column: x => x.DocumentStatusId,
                        principalTable: "DocumentStatuses",
                        principalColumn: "DocumentStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_ImportedStaff_UploadedByStaffId",
                        column: x => x.UploadedByStaffId,
                        principalTable: "ImportedStaff",
                        principalColumn: "ImportedStaffId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_SenderTypes_SenderTypeId1",
                        column: x => x.SenderTypeId1,
                        principalTable: "SenderTypes",
                        principalColumn: "SenderTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Senders_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Senders",
                        principalColumn: "SenderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAssignments",
                columns: table => new
                {
                    DocumentAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentTypeId = table.Column<int>(type: "int", nullable: false),
                    AssignedToStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedByAI = table.Column<bool>(type: "bit", nullable: false),
                    AIConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AIConfirmedByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    AISuggestedEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StaffAccessToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StaffAccessTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminAccessToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdminAccessTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAssignments", x => x.DocumentAssignmentId);
                    table.ForeignKey(
                        name: "FK_DocumentAssignments_AssignmentTypes_AssignmentTypeId",
                        column: x => x.AssignmentTypeId,
                        principalTable: "AssignmentTypes",
                        principalColumn: "AssignmentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAssignments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAssignments_ImportedStaff_AssignedToStaffId",
                        column: x => x.AssignedToStaffId,
                        principalTable: "ImportedStaff",
                        principalColumn: "ImportedStaffId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentMessages",
                columns: table => new
                {
                    DocumentMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageTypeId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentMessages", x => x.DocumentMessageId);
                    table.ForeignKey(
                        name: "FK_DocumentMessages_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentMessages_ImportedStaff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "ImportedStaff",
                        principalColumn: "ImportedStaffId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentMessages_MessageTypes_MessageTypeId",
                        column: x => x.MessageTypeId,
                        principalTable: "MessageTypes",
                        principalColumn: "MessageTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentMessages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentDeliveries",
                columns: table => new
                {
                    DocumentDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemStatusId = table.Column<int>(type: "int", nullable: false),
                    SentToEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDeliveries", x => x.DocumentDeliveryId);
                    table.ForeignKey(
                        name: "FK_DocumentDeliveries_DocumentAssignments_DocumentAssignmentId",
                        column: x => x.DocumentAssignmentId,
                        principalTable: "DocumentAssignments",
                        principalColumn: "DocumentAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentDeliveries_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentDeliveries_SystemStatuses_SystemStatusId",
                        column: x => x.SystemStatusId,
                        principalTable: "SystemStatuses",
                        principalColumn: "SystemStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentDeliveries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AssignmentTypes",
                columns: new[] { "AssignmentTypeId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Primary", 100 },
                    { 2, "CC", 200 }
                });

            migrationBuilder.InsertData(
                table: "DocumentStatuses",
                columns: new[] { "DocumentStatusId", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[,]
                {
                    { 1, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Unassigned", 100, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Assigned", 200, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Read", 300, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Actioned", 400, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "MessageTypes",
                columns: new[] { "MessageTypeId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Note", 100 },
                    { 2, "ActionedViaOutlook", 200 },
                    { 3, "ActionedViaPortal", 300 },
                    { 4, "Forwarded", 400 },
                    { 5, "AdminConfirmedViaOutlook", 500 },
                    { 6, "AdminReassignedViaOutlook", 600 },
                    { 7, "AdminMarkedUrgent", 700 },
                    { 8, "ViewedDocument", 800 }
                });

            migrationBuilder.InsertData(
                table: "NotificationTypes",
                columns: new[] { "NotificationTypeId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "NewDocument", 100 },
                    { 2, "Urgent", 200 },
                    { 3, "Forwarded", 300 },
                    { 4, "MessageReceived", 400 }
                });

            migrationBuilder.InsertData(
                table: "SourceTypes",
                columns: new[] { "SourceTypeId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "CSV", 100, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "MicrosoftGraph", 200, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "GoogleDirectory", 300, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Connector", 400, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "SalesforceAPI", 500, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "PowerSchoolAPI", 600, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "CustomDatabase", 700, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "CustomAPI", 800, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "VirtualMailroom", 900, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "SystemStatuses",
                columns: new[] { "SystemStatusId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Active", 100, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Expired", 200, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Revoked", 300, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Success", 400, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Failed", 500, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "PartialSuccess", 600, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 700, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Sent", 800, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_AssignedToStaffId",
                table: "DocumentAssignments",
                column: "AssignedToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_AssignmentTypeId",
                table: "DocumentAssignments",
                column: "AssignmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_DocumentId",
                table: "DocumentAssignments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_TenantId",
                table: "DocumentAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDeliveries_DocumentAssignmentId",
                table: "DocumentDeliveries",
                column: "DocumentAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDeliveries_DocumentId",
                table: "DocumentDeliveries",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDeliveries_SystemStatusId",
                table: "DocumentDeliveries",
                column: "SystemStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDeliveries_TenantId",
                table: "DocumentDeliveries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMessages_DocumentId",
                table: "DocumentMessages",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMessages_MessageTypeId",
                table: "DocumentMessages",
                column: "MessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMessages_StaffId",
                table: "DocumentMessages",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMessages_TenantId",
                table: "DocumentMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentStatusId",
                table: "Documents",
                column: "DocumentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SenderId",
                table: "Documents",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SenderTypeId1",
                table: "Documents",
                column: "SenderTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId",
                table: "Documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByStaffId",
                table: "Documents",
                column: "UploadedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUsages_SystemStatusId",
                table: "DocumentUsages",
                column: "SystemStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUsages_TenantId",
                table: "DocumentUsages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSenders_SenderTypeId1",
                table: "ImportedSenders",
                column: "SenderTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSenders_SourceTypeId",
                table: "ImportedSenders",
                column: "SourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSenders_TenantId",
                table: "ImportedSenders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedStaff_SourceTypeId",
                table: "ImportedStaff",
                column: "SourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedStaff_TenantId",
                table: "ImportedStaff",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Senders_SenderTypeId",
                table: "Senders",
                column: "SenderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Senders_TenantId",
                table: "Senders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SenderTypes_TenantId",
                table: "SenderTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSyncLogs_SourceTypeId",
                table: "StaffSyncLogs",
                column: "SourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSyncLogs_SystemStatusId",
                table: "StaffSyncLogs",
                column: "SystemStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSyncLogs_TenantId",
                table: "StaffSyncLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConnections_SourceTypeId",
                table: "TenantConnections",
                column: "SourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConnections_SystemStatusId",
                table: "TenantConnections",
                column: "SystemStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConnections_TenantId",
                table: "TenantConnections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConnectorConfigs_SourceTypeId",
                table: "TenantConnectorConfigs",
                column: "SourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConnectorConfigs_SystemStatusId",
                table: "TenantConnectorConfigs",
                column: "SystemStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConnectorConfigs_TenantId",
                table: "TenantConnectorConfigs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Domain",
                table: "Tenants",
                column: "Domain",
                unique: true,
                filter: "[Domain] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentDeliveries");

            migrationBuilder.DropTable(
                name: "DocumentMessages");

            migrationBuilder.DropTable(
                name: "DocumentUsages");

            migrationBuilder.DropTable(
                name: "ImportedSenders");

            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "StaffSyncLogs");

            migrationBuilder.DropTable(
                name: "TenantConnections");

            migrationBuilder.DropTable(
                name: "TenantConnectorConfigs");

            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "DocumentAssignments");

            migrationBuilder.DropTable(
                name: "MessageTypes");

            migrationBuilder.DropTable(
                name: "SystemStatuses");

            migrationBuilder.DropTable(
                name: "AssignmentTypes");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentStatuses");

            migrationBuilder.DropTable(
                name: "ImportedStaff");

            migrationBuilder.DropTable(
                name: "Senders");

            migrationBuilder.DropTable(
                name: "SourceTypes");

            migrationBuilder.DropTable(
                name: "SenderTypes");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
