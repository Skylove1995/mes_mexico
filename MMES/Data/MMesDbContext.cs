using System;
using System.Collections.Generic;
using MMES.Models;
using Microsoft.EntityFrameworkCore;

namespace MMES.Data;

public partial class MMesDbContext : DbContext
{
    public MMesDbContext(DbContextOptions<MMesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbAlarmList> TbAlarmLists { get; set; }

    public virtual DbSet<TbAoiNg> TbAoiNgs { get; set; }

    public virtual DbSet<TbAoiPid> TbAoiPids { get; set; }

    public virtual DbSet<TbAuditActionHi> TbAuditActionHis { get; set; }

    public virtual DbSet<TbAuditChecksheet> TbAuditChecksheets { get; set; }

    public virtual DbSet<TbAuditEvidenceHi> TbAuditEvidenceHis { get; set; }

    public virtual DbSet<TbAuditResult> TbAuditResults { get; set; }

    public virtual DbSet<TbAuditStatus> TbAuditStatuses { get; set; }

    public virtual DbSet<TbAuditType> TbAuditTypes { get; set; }

    public virtual DbSet<TbBlock> TbBlocks { get; set; }

    public virtual DbSet<TbDept> TbDepts { get; set; }

    public virtual DbSet<TbDeptGroup> TbDeptGroups { get; set; }

    public virtual DbSet<TbDocApprovalHistory> TbDocApprovalHistories { get; set; }

    public virtual DbSet<TbDocList> TbDocLists { get; set; }

    public virtual DbSet<TbDocType> TbDocTypes { get; set; }

    public virtual DbSet<TbFaDefectDatum> TbFaDefectData { get; set; }

    public virtual DbSet<TbFgIn> TbFgIns { get; set; }

    public virtual DbSet<TbFgOut> TbFgOuts { get; set; }

    public virtual DbSet<TbImportCursor> TbImportCursors { get; set; }

    public virtual DbSet<TbInternalDefectDatum> TbInternalDefectData { get; set; }

    public virtual DbSet<TbKla> TbKlas { get; set; }

    public virtual DbSet<TbLine> TbLines { get; set; }

    public virtual DbSet<TbMcChecksheet> TbMcChecksheets { get; set; }

    public virtual DbSet<TbMcEvidence> TbMcEvidences { get; set; }

    public virtual DbSet<TbMcLog> TbMcLogs { get; set; }

    public virtual DbSet<TbMcRequest> TbMcRequests { get; set; }

    public virtual DbSet<TbModel> TbModels { get; set; }

    public virtual DbSet<TbModelDict> TbModelDicts { get; set; }

    public virtual DbSet<TbProductionBlock> TbProductionBlocks { get; set; }

    public virtual DbSet<TbProductionFullView> TbProductionFullViews { get; set; }

    public virtual DbSet<TbProductionMachine> TbProductionMachines { get; set; }

    public virtual DbSet<TbProductionPlan> TbProductionPlans { get; set; }

    public virtual DbSet<TbProductionResult> TbProductionResults { get; set; }

    public virtual DbSet<TbProductionWorkOrder> TbProductionWorkOrders { get; set; }

    public virtual DbSet<TbRescan> TbRescans { get; set; }

    public virtual DbSet<TbRomColor> TbRomColors { get; set; }

    public virtual DbSet<TbRomDatum> TbRomData { get; set; }

    public virtual DbSet<TbRomHistory> TbRomHistories { get; set; }

    public virtual DbSet<TbScanOut> TbScanOuts { get; set; }

    public virtual DbSet<TbScanoutLine> TbScanoutLines { get; set; }

    public virtual DbSet<TbSpecBlockJig> TbSpecBlockJigs { get; set; }

    public virtual DbSet<TbSpecSolder> TbSpecSolders { get; set; }

    public virtual DbSet<TbSpecSqueegee> TbSpecSqueegees { get; set; }

    public virtual DbSet<TbSpecStencil> TbSpecStencils { get; set; }

    public virtual DbSet<TbStationMonitoring> TbStationMonitorings { get; set; }

    public virtual DbSet<TbUser> TbUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<TbAlarmList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_alarm_list");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("active");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
        });

        modelBuilder.Entity<TbAoiNg>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_aoi_ng");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ArrayId)
                .HasMaxLength(45)
                .HasColumnName("ARRAY_ID");
            entity.Property(e => e.ArrayIndex).HasColumnName("ARRAY_INDEX");
            entity.Property(e => e.Component)
                .HasMaxLength(22)
                .HasColumnName("COMPONENT");
            entity.Property(e => e.EndDate).HasColumnName("END_DATE");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("END_TIME");
            entity.Property(e => e.InspType)
                .HasMaxLength(45)
                .HasColumnName("INSP_TYPE");
            entity.Property(e => e.Pid)
                .HasMaxLength(22)
                .HasColumnName("PID");
            entity.Property(e => e.UserResult)
                .HasMaxLength(45)
                .HasColumnName("USER_RESULT");
        });

        modelBuilder.Entity<TbAoiPid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_aoi_pid");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArrayId)
                .HasMaxLength(22)
                .HasColumnName("ARRAY_ID");
            entity.Property(e => e.EndDate).HasColumnName("END_DATE");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("END_TIME");
            entity.Property(e => e.Line)
                .HasMaxLength(10)
                .HasColumnName("LINE");
            entity.Property(e => e.MachineResult)
                .HasMaxLength(45)
                .HasColumnName("MACHINE_RESULT");
            entity.Property(e => e.Model)
                .HasMaxLength(45)
                .HasColumnName("MODEL");
            entity.Property(e => e.Pid)
                .HasMaxLength(22)
                .HasColumnName("PID");
            entity.Property(e => e.ProgramName)
                .HasMaxLength(255)
                .HasColumnName("PROGRAM_NAME");
            entity.Property(e => e.UserResult)
                .HasMaxLength(45)
                .HasColumnName("USER_RESULT");
            entity.Property(e => e.WorkFace)
                .HasMaxLength(3)
                .HasColumnName("WORK_FACE");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");
        });

        modelBuilder.Entity<TbAuditActionHi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_audit_action_his");

            entity.HasIndex(e => e.AuditResultId, "fk_tbActionHis_ID_idx");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AuditResultId).HasColumnName("AUDIT_RESULT_ID");
            entity.Property(e => e.MimeType)
                .HasMaxLength(255)
                .HasColumnName("MIME_TYPE");
            entity.Property(e => e.Url)
                .HasColumnType("text")
                .HasColumnName("URL");

            entity.HasOne(d => d.AuditResult).WithMany(p => p.TbAuditActionHis)
                .HasForeignKey(d => d.AuditResultId)
                .HasConstraintName("fk_tbActionHis_ID");
        });

        modelBuilder.Entity<TbAuditChecksheet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_audit_checksheet");

            entity.HasIndex(e => e.AuditTypeId, "fk_Checksheet_auditType_idx");

            entity.HasIndex(e => e.ForDept, "fk_Checksheet_forDept_idx");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.AuditItem)
                .HasColumnType("text")
                .HasColumnName("AUDIT_ITEM");
            entity.Property(e => e.AuditTypeId).HasColumnName("AUDIT_TYPE_ID");
            entity.Property(e => e.Category)
                .HasMaxLength(255)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.Criteria)
                .HasColumnType("text")
                .HasColumnName("CRITERIA");
            entity.Property(e => e.Critical)
                .HasMaxLength(1)
                .HasColumnName("CRITICAL");
            entity.Property(e => e.ForDept).HasColumnName("FOR_DEPT");
            entity.Property(e => e.MaxScore).HasColumnName("MAX_SCORE");

            entity.HasOne(d => d.AuditType).WithMany(p => p.TbAuditChecksheets)
                .HasForeignKey(d => d.AuditTypeId)
                .HasConstraintName("fk_Checksheet_auditType");

            entity.HasOne(d => d.ForDeptNavigation).WithMany(p => p.TbAuditChecksheets)
                .HasForeignKey(d => d.ForDept)
                .HasConstraintName("fk_Checksheet_forDept");
        });

        modelBuilder.Entity<TbAuditEvidenceHi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_audit_evidence_his");

            entity.HasIndex(e => e.AuditResultId, "fk_tbEvidenceHis_ID_idx");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AuditResultId).HasColumnName("AUDIT_RESULT_ID");
            entity.Property(e => e.MimeType)
                .HasMaxLength(255)
                .HasColumnName("MIME_TYPE");
            entity.Property(e => e.Url)
                .HasColumnType("text")
                .HasColumnName("URL");

            entity.HasOne(d => d.AuditResult).WithMany(p => p.TbAuditEvidenceHis)
                .HasForeignKey(d => d.AuditResultId)
                .HasConstraintName("fk_tbEvidenceHis_ID");
        });

        modelBuilder.Entity<TbAuditResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_audit_result");

            entity.HasIndex(e => e.AuditById, "fk_auditResult_AuditBy_idx");

            entity.HasIndex(e => e.AuditItemId, "fk_auditResult_CheckItem_idx");

            entity.HasIndex(e => e.IssueOwnerId, "fk_auditResult_IssueOwner_idx");

            entity.HasIndex(e => e.LineId, "fk_auditResult_LineId_idx");

            entity.HasIndex(e => e.ModelId, "fk_auditResult_Model_idx");

            entity.HasIndex(e => e.StatusId, "fk_auditResult_Status_idx");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ActionContent)
                .HasColumnType("text")
                .HasColumnName("ACTION_CONTENT");
            entity.Property(e => e.ActionDate)
                .HasColumnType("datetime")
                .HasColumnName("ACTION_DATE");
            entity.Property(e => e.AuditById).HasColumnName("AUDIT_BY_ID");
            entity.Property(e => e.AuditDate)
                .HasColumnType("datetime")
                .HasColumnName("AUDIT_DATE");
            entity.Property(e => e.AuditItemId).HasColumnName("AUDIT_ITEM_ID");
            entity.Property(e => e.IssueOwnerId).HasColumnName("ISSUE_OWNER_ID");
            entity.Property(e => e.Judge)
                .HasMaxLength(2)
                .HasColumnName("JUDGE");
            entity.Property(e => e.LastComment)
                .HasColumnType("text")
                .HasColumnName("LAST_COMMENT");
            entity.Property(e => e.LineId).HasColumnName("LINE_ID");
            entity.Property(e => e.ModelId).HasColumnName("MODEL_ID");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("NOTE");
            entity.Property(e => e.Score).HasColumnName("SCORE");
            entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");

            entity.HasOne(d => d.AuditBy).WithMany(p => p.TbAuditResults)
                .HasForeignKey(d => d.AuditById)
                .HasConstraintName("fk_auditResult_AuditBy");

            entity.HasOne(d => d.AuditItem).WithMany(p => p.TbAuditResults)
                .HasForeignKey(d => d.AuditItemId)
                .HasConstraintName("fk_auditResult_CheckItem");

            entity.HasOne(d => d.IssueOwner).WithMany(p => p.TbAuditResults)
                .HasForeignKey(d => d.IssueOwnerId)
                .HasConstraintName("fk_auditResult_IssueOwner");

            entity.HasOne(d => d.Line).WithMany(p => p.TbAuditResults)
                .HasForeignKey(d => d.LineId)
                .HasConstraintName("fk_auditResult_LineId");

            entity.HasOne(d => d.Model).WithMany(p => p.TbAuditResults)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("fk_auditResult_Model");

            entity.HasOne(d => d.Status).WithMany(p => p.TbAuditResults)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("fk_auditResult_Status");
        });

        modelBuilder.Entity<TbAuditStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_audit_status");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Status)
                .HasMaxLength(45)
                .HasColumnName("STATUS");
        });

        modelBuilder.Entity<TbAuditType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_audit_type");

            entity.HasIndex(e => e.AuditType, "AUDIT_TYPE_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AuditType).HasColumnName("AUDIT_TYPE");
        });

        modelBuilder.Entity<TbBlock>(entity =>
        {
            entity.HasKey(e => e.Pid).HasName("PRIMARY");

            entity.ToTable("tb_block");

            entity.Property(e => e.Pid)
                .HasMaxLength(22)
                .HasColumnName("pid");
            entity.Property(e => e.BlockAt)
                .HasColumnType("datetime")
                .HasColumnName("BLOCK_AT");
            entity.Property(e => e.History).HasColumnType("text");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pba)
                .HasMaxLength(45)
                .HasColumnName("PBA");
            entity.Property(e => e.ReleaseAt)
                .HasColumnType("datetime")
                .HasColumnName("RELEASE_AT");
            entity.Property(e => e.Status)
                .HasMaxLength(1)
                .HasComment("B: Block - R: Release")
                .HasColumnName("STATUS");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");
        });

        modelBuilder.Entity<TbDept>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_dept");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Authority)
                .HasMaxLength(45)
                .HasColumnName("AUTHORITY");
            entity.Property(e => e.Dept)
                .HasMaxLength(45)
                .HasColumnName("DEPT");
        });

        modelBuilder.Entity<TbDeptGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_dept_group");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChildDept)
                .HasMaxLength(255)
                .HasColumnName("child_dept");
            entity.Property(e => e.ParrentDept)
                .HasMaxLength(45)
                .HasColumnName("parrent_dept");
        });

        modelBuilder.Entity<TbDocApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_doc_approval_history");

            entity.HasIndex(e => e.ApproveBy, "fk_approvalHis_ApprovalBy_idx");

            entity.HasIndex(e => e.DocId, "fk_approvalHis_docId_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApproveBy).HasColumnName("APPROVE_BY");
            entity.Property(e => e.ApproveTime)
                .HasColumnType("datetime")
                .HasColumnName("APPROVE_TIME");
            entity.Property(e => e.Comment)
                .HasColumnType("text")
                .HasColumnName("COMMENT");
            entity.Property(e => e.DocId).HasColumnName("DOC_ID");
            entity.Property(e => e.LineNo).HasColumnName("LINE_NO");
            entity.Property(e => e.Status)
                .HasComment("0: draft\\n1: approved\\n2: reject")
                .HasColumnName("STATUS");

            entity.HasOne(d => d.ApproveByNavigation).WithMany(p => p.TbDocApprovalHistories)
                .HasForeignKey(d => d.ApproveBy)
                .HasConstraintName("fk_approvalHis_ApprovalBy");

            entity.HasOne(d => d.Doc).WithMany(p => p.TbDocApprovalHistories)
                .HasForeignKey(d => d.DocId)
                .HasConstraintName("fk_approvalHis_docId");
        });

        modelBuilder.Entity<TbDocList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_doc_list");

            entity.HasIndex(e => e.DocType, "fk_docList_docType_idx");

            entity.HasIndex(e => e.Owner, "fk_docList_owner_idx");

            entity.HasIndex(e => e.UploadBy, "fk_docList_uploadBy_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(45)
                .HasColumnName("CODE");
            entity.Property(e => e.DocType).HasColumnName("DOC_TYPE");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("NOTE");
            entity.Property(e => e.Owner).HasColumnName("OWNER");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("TITLE");
            entity.Property(e => e.UploadAt)
                .HasColumnType("datetime")
                .HasColumnName("UPLOAD_AT");
            entity.Property(e => e.UploadBy).HasColumnName("UPLOAD_BY");
            entity.Property(e => e.Url)
                .HasMaxLength(255)
                .HasColumnName("URL");
            entity.Property(e => e.Version)
                .HasMaxLength(10)
                .HasColumnName("VERSION");

            entity.HasOne(d => d.DocTypeNavigation).WithMany(p => p.TbDocLists)
                .HasForeignKey(d => d.DocType)
                .HasConstraintName("fk_docList_docType");

            entity.HasOne(d => d.OwnerNavigation).WithMany(p => p.TbDocLists)
                .HasForeignKey(d => d.Owner)
                .HasConstraintName("fk_docList_owner");

            entity.HasOne(d => d.UploadByNavigation).WithMany(p => p.TbDocLists)
                .HasForeignKey(d => d.UploadBy)
                .HasConstraintName("fk_docList_uploadBy");
        });

        modelBuilder.Entity<TbDocType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_doc_type");

            entity.HasIndex(e => e.DocType, "DOC_TYPE_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocType)
                .HasMaxLength(45)
                .HasColumnName("DOC_TYPE");
        });

        modelBuilder.Entity<TbFaDefectDatum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_fa_defect_data");

            entity.HasIndex(e => e.Pic, "fk_internalDefect_repairMan_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionDate).HasColumnName("ACTION_DATE");
            entity.Property(e => e.ActionType)
                .HasMaxLength(45)
                .HasColumnName("ACTION_TYPE");
            entity.Property(e => e.AoiHistory)
                .HasMaxLength(45)
                .HasColumnName("AOI_HISTORY");
            entity.Property(e => e.Category1)
                .HasMaxLength(45)
                .HasColumnName("CATEGORY1");
            entity.Property(e => e.Category2)
                .HasMaxLength(45)
                .HasColumnName("CATEGORY2");
            entity.Property(e => e.Cause)
                .HasMaxLength(255)
                .HasColumnName("CAUSE");
            entity.Property(e => e.DefectFrom)
                .HasMaxLength(45)
                .HasComment("ISSUE OWNER")
                .HasColumnName("DEFECT_FROM");
            entity.Property(e => e.DefectImageUrl)
                .HasMaxLength(255)
                .HasColumnName("DEFECT_IMAGE_URL");
            entity.Property(e => e.DefectName)
                .HasMaxLength(255)
                .HasColumnName("DEFECT_NAME");
            entity.Property(e => e.FctHistory)
                .HasMaxLength(45)
                .HasColumnName("FCT_HISTORY");
            entity.Property(e => e.FctNote)
                .HasMaxLength(255)
                .HasColumnName("FCT_NOTE");
            entity.Property(e => e.FctRetest)
                .HasMaxLength(45)
                .HasColumnName("FCT_RETEST");
            entity.Property(e => e.HseReportUrl)
                .HasMaxLength(255)
                .HasColumnName("HSE_REPORT_URL");
            entity.Property(e => e.Location)
                .HasMaxLength(45)
                .HasColumnName("LOCATION");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.NcrCloseDate).HasColumnName("NCR_CLOSE_DATE");
            entity.Property(e => e.NcrDate).HasColumnName("NCR_DATE");
            entity.Property(e => e.NcrNo)
                .HasMaxLength(45)
                .HasColumnName("NCR_NO");
            entity.Property(e => e.NcrStatus)
                .HasMaxLength(45)
                .HasColumnName("NCR_STATUS");
            entity.Property(e => e.NcrUrl)
                .HasMaxLength(255)
                .HasColumnName("NCR_URL");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pic).HasColumnName("PIC");
            entity.Property(e => e.Pid)
                .HasMaxLength(45)
                .HasColumnName("PID");
            entity.Property(e => e.Remark)
                .HasMaxLength(255)
                .HasColumnName("REMARK");
            entity.Property(e => e.SampleLocation)
                .HasMaxLength(100)
                .HasColumnName("SAMPLE_LOCATION");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");

            entity.HasOne(d => d.PicNavigation).WithMany(p => p.TbFaDefectData)
                .HasForeignKey(d => d.Pic)
                .HasConstraintName("fk_falDefect_repairMan");
        });

        modelBuilder.Entity<TbFgIn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_fg_in");

            entity.HasIndex(e => e.Pba, "PBA_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Locator)
                .HasMaxLength(45)
                .HasColumnName("LOCATOR");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pba)
                .HasMaxLength(45)
                .HasColumnName("PBA");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.ScanDate).HasColumnName("SCAN_DATE");
            entity.Property(e => e.ScanTime)
                .HasColumnType("datetime")
                .HasColumnName("SCAN_TIME");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");
        });

        modelBuilder.Entity<TbFgOut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_fg_out");

            entity.HasIndex(e => e.Pba, "PBA_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Locator)
                .HasMaxLength(45)
                .HasColumnName("LOCATOR");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pba)
                .HasMaxLength(45)
                .HasColumnName("PBA");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.ScanDate).HasColumnName("SCAN_DATE");
            entity.Property(e => e.ScanTime)
                .HasColumnType("datetime")
                .HasColumnName("SCAN_TIME");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");
        });

        modelBuilder.Entity<TbImportCursor>(entity =>
        {
            entity.HasKey(e => e.LineKey).HasName("PRIMARY");

            entity.ToTable("tb_import_cursor");

            entity.Property(e => e.LineKey).HasMaxLength(45);
            entity.Property(e => e.LastFileName).HasMaxLength(255);
            entity.Property(e => e.LastStampUtc).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbInternalDefectDatum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_internal_defect_data");

            entity.HasIndex(e => e.RepairMan, "fk_internalDefect_repairMan_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionContent)
                .HasMaxLength(255)
                .HasColumnName("ACTION_CONTENT");
            entity.Property(e => e.ActionDate).HasColumnName("ACTION_DATE");
            entity.Property(e => e.ActionType)
                .HasMaxLength(45)
                .HasColumnName("ACTION_TYPE");
            entity.Property(e => e.Cause)
                .HasMaxLength(255)
                .HasColumnName("CAUSE");
            entity.Property(e => e.DefectDate).HasColumnName("DEFECT_DATE");
            entity.Property(e => e.DefectFrom)
                .HasMaxLength(45)
                .HasColumnName("DEFECT_FROM");
            entity.Property(e => e.DefectName)
                .HasMaxLength(255)
                .HasColumnName("DEFECT_NAME");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("IMAGE_URL");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("LOCATION");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pid)
                .HasMaxLength(45)
                .HasColumnName("PID");
            entity.Property(e => e.Process)
                .HasMaxLength(45)
                .HasColumnName("PROCESS");
            entity.Property(e => e.RepairMan).HasColumnName("REPAIR_MAN");
            entity.Property(e => e.Side)
                .HasMaxLength(4)
                .HasColumnName("SIDE");
            entity.Property(e => e.UploadTime)
                .HasColumnType("datetime")
                .HasColumnName("UPLOAD_TIME");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");

            entity.HasOne(d => d.RepairManNavigation).WithMany(p => p.TbInternalDefectData)
                .HasForeignKey(d => d.RepairMan)
                .HasConstraintName("fk_internalDefect_repairMan");
        });

        modelBuilder.Entity<TbKla>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_klas");

            entity.HasIndex(e => e.Wo, "WO_UNIQUE").IsUnique();

            entity.HasIndex(e => new { e.Ebr, e.StartSn, e.EndSn }, "ix_klas_ebr_sn");

            entity.HasIndex(e => new { e.SerialPrefix5, e.StartSerial, e.EndSerial }, "ix_klas_serial11");

            entity.HasIndex(e => new { e.Ebr, e.SerialPrefix5, e.StartSerial, e.EndSerial }, "ix_klas_serial22fb");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ebr)
                .HasMaxLength(11)
                .IsFixedLength()
                .HasColumnName("EBR")
                .UseCollation("utf8mb4_bin");
            entity.Property(e => e.EndSerial)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("END_SERIAL")
                .UseCollation("utf8mb4_bin");
            entity.Property(e => e.EndSn)
                .HasMaxLength(22)
                .IsFixedLength()
                .HasColumnName("END_SN")
                .UseCollation("utf8mb4_bin");
            entity.Property(e => e.SerialPrefix5)
                .HasMaxLength(5)
                .HasComputedColumnSql("substr(`START_SN`,12,5)", true)
                .HasColumnName("SERIAL_PREFIX5");
            entity.Property(e => e.StartSerial)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("START_SERIAL")
                .UseCollation("utf8mb4_bin");
            entity.Property(e => e.StartSn)
                .HasMaxLength(22)
                .IsFixedLength()
                .HasColumnName("START_SN")
                .UseCollation("utf8mb4_bin");
            entity.Property(e => e.Wo)
                .HasMaxLength(45)
                .HasColumnName("WO");
        });

        modelBuilder.Entity<TbLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_line");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LineName)
                .HasMaxLength(45)
                .HasColumnName("LINE_NAME");
        });

        modelBuilder.Entity<TbMcChecksheet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_mc_checksheets");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasComment("Y/N")
                .HasColumnName("ACTIVE");
            entity.Property(e => e.CheckItem)
                .HasMaxLength(255)
                .HasColumnName("CHECK_ITEM");
            entity.Property(e => e.EvidenceReq)
                .HasMaxLength(1)
                .HasColumnName("EVIDENCE_REQ");
            entity.Property(e => e.McFor)
                .HasMaxLength(45)
                .HasColumnName("MC_FOR");
            entity.Property(e => e.Pic)
                .HasMaxLength(45)
                .HasColumnName("PIC");
            entity.Property(e => e.Process)
                .HasMaxLength(45)
                .HasColumnName("PROCESS");
            entity.Property(e => e.Spec)
                .HasMaxLength(255)
                .HasColumnName("SPEC");
            entity.Property(e => e.SpecType)
                .HasMaxLength(45)
                .HasComment("DYNAMIC / FIX")
                .HasColumnName("SPEC_TYPE");
        });

        modelBuilder.Entity<TbMcEvidence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_mc_evidences");

            entity.HasIndex(e => e.LogId, "index2");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LogId).HasColumnName("logId");
            entity.Property(e => e.MimeType)
                .HasMaxLength(255)
                .HasColumnName("MIME_TYPE");
            entity.Property(e => e.Url)
                .HasColumnType("text")
                .HasColumnName("url");

            entity.HasOne(d => d.Log).WithMany(p => p.TbMcEvidences)
                .HasForeignKey(d => d.LogId)
                .HasConstraintName("fk_logId");
        });

        modelBuilder.Entity<TbMcLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_mc_logs");

            entity.HasIndex(e => e.ReqId, "fk_MCrequest_idx");

            entity.HasIndex(e => e.ItemId, "fk_checkItem_idx");

            entity.HasIndex(e => e.CheckBy, "fk_checkby_idx");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CheckAt)
                .HasColumnType("datetime")
                .HasColumnName("CHECK_AT");
            entity.Property(e => e.CheckBy).HasColumnName("CHECK_BY");
            entity.Property(e => e.CheckResult)
                .HasMaxLength(3)
                .HasComment("0: Processing\\n1: OK\\n2: NG")
                .HasColumnName("CHECK_RESULT");
            entity.Property(e => e.ItemId).HasColumnName("ITEM_ID");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("NOTE");
            entity.Property(e => e.ReqId).HasColumnName("REQ_ID");

            entity.HasOne(d => d.CheckByNavigation).WithMany(p => p.TbMcLogs)
                .HasForeignKey(d => d.CheckBy)
                .HasConstraintName("fk_checkby");

            entity.HasOne(d => d.Item).WithMany(p => p.TbMcLogs)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("fk_checkItem");

            entity.HasOne(d => d.Req).WithMany(p => p.TbMcLogs)
                .HasForeignKey(d => d.ReqId)
                .HasConstraintName("fk_MCrequest");
        });

        modelBuilder.Entity<TbMcRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_mc_requests");

            entity.HasIndex(e => e.LineId, "fk_lineId_idx");

            entity.HasIndex(e => e.WoAfterId, "fk_mcReq_wo_after_idx");

            entity.HasIndex(e => e.WoBeforeId, "fk_mcReq_wo_before_idx");

            entity.HasIndex(e => e.ReqBy, "fk_user_idx");

            entity.HasIndex(e => e.Status, "index8");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DoneAt)
                .HasColumnType("datetime")
                .HasColumnName("DONE_AT");
            entity.Property(e => e.LineId).HasColumnName("LINE_ID");
            entity.Property(e => e.McFor)
                .HasMaxLength(45)
                .HasColumnName("MC_FOR");
            entity.Property(e => e.ReqAt)
                .HasColumnType("datetime")
                .HasColumnName("REQ_AT");
            entity.Property(e => e.ReqBy).HasColumnName("REQ_BY");
            entity.Property(e => e.Status)
                .HasMaxLength(45)
                .HasComment("0: Prrocessing\\\\n1: Done\\\\n2: Cancel")
                .HasColumnName("STATUS");
            entity.Property(e => e.WoAfterId).HasColumnName("WO_AFTER_ID");
            entity.Property(e => e.WoBeforeId).HasColumnName("WO_BEFORE_ID");
            entity.Property(e => e.WorkFace)
                .HasMaxLength(45)
                .HasColumnName("WORK_FACE");

            entity.HasOne(d => d.Line).WithMany(p => p.TbMcRequests)
                .HasForeignKey(d => d.LineId)
                .HasConstraintName("fk_lineId");

            entity.HasOne(d => d.ReqByNavigation).WithMany(p => p.TbMcRequests)
                .HasForeignKey(d => d.ReqBy)
                .HasConstraintName("fk_user");

            entity.HasOne(d => d.WoAfter).WithMany(p => p.TbMcRequestWoAfters)
                .HasForeignKey(d => d.WoAfterId)
                .HasConstraintName("fk_mcReq_wo_after");

            entity.HasOne(d => d.WoBefore).WithMany(p => p.TbMcRequestWoBefores)
                .HasForeignKey(d => d.WoBeforeId)
                .HasConstraintName("fk_mcReq_wo_before");
        });

        modelBuilder.Entity<TbModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_model");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModelName)
                .HasMaxLength(255)
                .HasColumnName("MODEL_NAME");
        });

        modelBuilder.Entity<TbModelDict>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_model_dict");

            entity.HasIndex(e => e.PartNo, "PART_NO_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Board)
                .HasMaxLength(45)
                .HasColumnName("BOARD");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
        });

        modelBuilder.Entity<TbProductionBlock>(entity =>
        {
            entity.HasKey(e => e.Pid).HasName("PRIMARY");

            entity.ToTable("tb_production_block");

            entity.HasIndex(e => new { e.Pid, e.Status }, "idx_pid_status");

            entity.Property(e => e.Pid)
                .HasMaxLength(30)
                .HasComment("Mã Barcode vỉ mạch")
                .HasColumnName("pid");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Thời điểm Block lần đầu")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.McId)
                .HasMaxLength(50)
                .HasComment("Mã máy phát hiện lỗi đầu tiên")
                .HasColumnName("mc_id");
            entity.Property(e => e.McType)
                .HasMaxLength(30)
                .HasComment("Loại trạm phát hiện đầu tiên (SPI, MOI, AOI)")
                .HasColumnName("mc_type");
            entity.Property(e => e.Reason)
                .HasMaxLength(100)
                .HasComment("Ví dụ: Blocked at SPI issue USERPASS")
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = Blocked (Chặn), 0 = Released (QA mở khóa)")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Thời điểm QA Release")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(30)
                .HasComment("Mã người thao tác cho Pass (Operator ID)")
                .HasColumnName("user_id");
            entity.Property(e => e.WoName)
                .HasMaxLength(50)
                .HasComment("Mã đơn hàng W/O")
                .HasColumnName("wo_name");
        });

        modelBuilder.Entity<TbProductionFullView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("tb_production_full_view");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.McId)
                .HasMaxLength(20)
                .HasColumnName("MC ID");
            entity.Property(e => e.McType)
                .HasMaxLength(10)
                .HasColumnName("MC Type");
            entity.Property(e => e.Pid)
                .HasMaxLength(30)
                .HasColumnName("pid");
            entity.Property(e => e.SmtAssyPN)
                .HasMaxLength(40)
                .HasColumnName("SMT Assy P/N");
            entity.Property(e => e.TestResult)
                .HasMaxLength(10)
                .HasColumnName("Test Result");
            entity.Property(e => e.TransactionDate)
                .HasColumnType("datetime")
                .HasColumnName("Transaction Date");
            entity.Property(e => e.UserResult)
                .HasMaxLength(10)
                .HasColumnName("User Result");
            entity.Property(e => e.WOName)
                .HasMaxLength(30)
                .HasColumnName("W/O Name");
        });

        modelBuilder.Entity<TbProductionMachine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_production_machines");

            entity.HasIndex(e => e.McId, "idx_mc_id").IsUnique();

            entity.HasIndex(e => e.McType, "idx_mc_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.McId)
                .HasMaxLength(20)
                .HasColumnName("mc_id");
            entity.Property(e => e.McType)
                .HasMaxLength(10)
                .HasColumnName("mc_type");
        });

        modelBuilder.Entity<TbProductionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_production_plan");

            entity.HasIndex(e => new { e.Buyer, e.ModelName, e.BoardName }, "IX_ProductionPlans_Buyer_ModelName_Board");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BarePcbPn)
                .HasMaxLength(11)
                .HasColumnName("BARE_PCB_PN");
            entity.Property(e => e.BoardName)
                .HasMaxLength(50)
                .HasColumnName("BOARD_NAME");
            entity.Property(e => e.Buyer)
                .HasMaxLength(50)
                .HasColumnName("BUYER");
            entity.Property(e => e.GmesWo)
                .HasMaxLength(13)
                .HasColumnName("GMES_WO");
            entity.Property(e => e.Lane)
                .HasMaxLength(2)
                .HasColumnName("LANE");
            entity.Property(e => e.Line)
                .HasMaxLength(2)
                .HasColumnName("LINE");
            entity.Property(e => e.ModelName)
                .HasMaxLength(50)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(50)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PcbaAssyPn)
                .HasMaxLength(11)
                .HasColumnName("PCBA_ASSY_PN");
            entity.Property(e => e.ProdDate).HasColumnName("PROD_DATE");
            entity.Property(e => e.ProdTime)
                .HasColumnType("datetime")
                .HasColumnName("PROD_TIME");
            entity.Property(e => e.SmtAssyPn)
                .HasMaxLength(11)
                .HasColumnName("SMT_ASSY_PN");
            entity.Property(e => e.WoQty).HasColumnName("WO_Qty");
        });

        modelBuilder.Entity<TbProductionResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_production_results");

            entity.HasIndex(e => new { e.McId, e.TransactionDate }, "idx_mc_transdate");

            entity.HasIndex(e => new { e.Pid, e.TransactionDate }, "idx_pid_transdate");

            entity.HasIndex(e => e.TestResult, "idx_test_result");

            entity.HasIndex(e => e.UserResult, "idx_user_result");

            entity.HasIndex(e => new { e.WoId, e.TransactionDate }, "idx_wo_transdate");

            entity.HasIndex(e => new { e.Pid, e.Seq, e.TransactionDate }, "uk_pid_seq_trans").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArraySeq).HasColumnName("array_seq");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.McId).HasColumnName("mc_id");
            entity.Property(e => e.Pid)
                .HasMaxLength(30)
                .HasColumnName("pid");
            entity.Property(e => e.Seq).HasColumnName("seq");
            entity.Property(e => e.TestResult)
                .HasMaxLength(10)
                .HasColumnName("test_result");
            entity.Property(e => e.TransactionDate)
                .HasColumnType("datetime")
                .HasColumnName("transaction_date");
            entity.Property(e => e.UserResult)
                .HasMaxLength(10)
                .HasColumnName("user_result");
            entity.Property(e => e.WoId).HasColumnName("wo_id");

            entity.HasOne(d => d.Mc).WithMany(p => p.TbProductionResults)
                .HasForeignKey(d => d.McId)
                .HasConstraintName("fk_prod_machine");

            entity.HasOne(d => d.Wo).WithMany(p => p.TbProductionResults)
                .HasForeignKey(d => d.WoId)
                .HasConstraintName("fk_prod_work_order");
        });

        modelBuilder.Entity<TbProductionWorkOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_production_work_orders");

            entity.HasIndex(e => e.SmtAssyPn, "idx_smt_assy_pn");

            entity.HasIndex(e => e.WoName, "idx_wo_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.SmtAssyPn)
                .HasMaxLength(40)
                .HasColumnName("smt_assy_pn");
            entity.Property(e => e.WoName)
                .HasMaxLength(30)
                .HasColumnName("wo_name");
        });

        modelBuilder.Entity<TbRescan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_rescan");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pba)
                .HasMaxLength(45)
                .HasColumnName("PBA");
            entity.Property(e => e.Pid)
                .HasMaxLength(45)
                .HasColumnName("PID");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.RescanAt)
                .HasColumnType("datetime")
                .HasColumnName("RESCAN_AT");
            entity.Property(e => e.ScanAt)
                .HasColumnType("datetime")
                .HasColumnName("SCAN_AT");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");
        });

        modelBuilder.Entity<TbRomColor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_rom_color");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(45)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(45)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TbRomDatum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_rom_data");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.Board)
                .HasMaxLength(45)
                .HasColumnName("BOARD");
            entity.Property(e => e.ChecksumTypeDedi)
                .HasMaxLength(255)
                .HasColumnName("CHECKSUM_TYPE_DEDI");
            entity.Property(e => e.ColorCode1)
                .HasMaxLength(45)
                .HasColumnName("COLOR_CODE1");
            entity.Property(e => e.ColorCode2)
                .HasMaxLength(45)
                .HasColumnName("COLOR_CODE2");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.EcoNo)
                .HasMaxLength(255)
                .HasColumnName("ECO_NO");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("FILE_NAME");
            entity.Property(e => e.FirstWo)
                .HasMaxLength(255)
                .HasColumnName("FIRST_WO");
            entity.Property(e => e.HseChecksumDedi)
                .HasMaxLength(255)
                .HasColumnName("HSE_CHECKSUM_DEDI");
            entity.Property(e => e.HseChecksumDitek)
                .HasMaxLength(255)
                .HasColumnName("HSE_CHECKSUM_DITEK");
            entity.Property(e => e.IcMakerPn)
                .HasMaxLength(255)
                .HasColumnName("IC_MAKER_PN");
            entity.Property(e => e.IcName)
                .HasMaxLength(11)
                .HasColumnName("IC_NAME");
            entity.Property(e => e.IcPn)
                .HasMaxLength(255)
                .HasColumnName("IC_PN");
            entity.Property(e => e.LastUpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("LAST_UPDATED_DATE");
            entity.Property(e => e.LgChecksum)
                .HasMaxLength(255)
                .HasColumnName("LG_CHECKSUM");
            entity.Property(e => e.MarkingColor1)
                .HasMaxLength(255)
                .HasColumnName("MARKING_COLOR1");
            entity.Property(e => e.MarkingColor2)
                .HasMaxLength(255)
                .HasColumnName("MARKING_COLOR2");
            entity.Property(e => e.MarkingLaser)
                .HasMaxLength(255)
                .HasColumnName("MARKING_LASER");
            entity.Property(e => e.Mc)
                .HasMaxLength(45)
                .HasColumnName("MC");
            entity.Property(e => e.Model)
                .HasMaxLength(45)
                .HasColumnName("MODEL");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PDataDedi)
                .HasMaxLength(255)
                .HasColumnName("P_DATA_DEDI");
            entity.Property(e => e.PackageInfoDedi)
                .HasMaxLength(255)
                .HasColumnName("PACKAGE_INFO_DEDI");
            entity.Property(e => e.PcbPn)
                .HasMaxLength(45)
                .HasColumnName("PCB_PN");
            entity.Property(e => e.PcbaAssyPn)
                .HasMaxLength(11)
                .HasColumnName("PCBA_ASSY_PN");
            entity.Property(e => e.ProgramType)
                .HasMaxLength(255)
                .HasColumnName("PROGRAM_TYPE");
            entity.Property(e => e.ReleaseDate)
                .HasColumnType("datetime")
                .HasColumnName("RELEASE_DATE");
            entity.Property(e => e.ReleaseNoteFile)
                .HasMaxLength(255)
                .HasColumnName("RELEASE_NOTE_FILE");
            entity.Property(e => e.Remarks)
                .HasMaxLength(255)
                .HasColumnName("REMARKS");
            entity.Property(e => e.SmtAssyPn)
                .HasMaxLength(11)
                .HasColumnName("SMT_ASSY_PN");
            entity.Property(e => e.SocketName)
                .HasMaxLength(255)
                .HasColumnName("SOCKET_NAME");
            entity.Property(e => e.Stage)
                .HasMaxLength(100)
                .HasColumnName("STAGE");
            entity.Property(e => e.SwProgramName)
                .HasMaxLength(255)
                .HasColumnName("SW_PROGRAM_NAME");
            entity.Property(e => e.SwVersion)
                .HasMaxLength(45)
                .HasColumnName("SW_VERSION");
        });

        modelBuilder.Entity<TbRomHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_rom_history");

            entity.HasIndex(e => e.ActionBy, "fk_romHis_actionBy_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionAt)
                .HasColumnType("datetime")
                .HasColumnName("action_at");
            entity.Property(e => e.ActionBy).HasColumnName("action_by");
            entity.Property(e => e.ActionContent)
                .HasColumnType("text")
                .HasColumnName("action_content");
            entity.Property(e => e.ActionType)
                .HasMaxLength(45)
                .HasColumnName("action_type");
            entity.Property(e => e.PrintAt)
                .HasColumnType("datetime")
                .HasColumnName("print_at");
            entity.Property(e => e.RomId).HasColumnName("rom_id");

            entity.HasOne(d => d.ActionByNavigation).WithMany(p => p.TbRomHistories)
                .HasForeignKey(d => d.ActionBy)
                .HasConstraintName("fk_romHis_actionBy");
        });

        modelBuilder.Entity<TbScanOut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_scan_out", tb => tb.HasComment("table to record scan out pid"));

            entity.HasIndex(e => e.Pid, "PID_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ClientId)
                .HasMaxLength(45)
                .HasColumnName("CLIENT_ID");
            entity.Property(e => e.FirstInspector)
                .HasMaxLength(45)
                .HasColumnName("FIRST_INSPECTOR");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PartNo)
                .HasMaxLength(45)
                .HasColumnName("PART_NO");
            entity.Property(e => e.Pid)
                .HasMaxLength(45)
                .HasColumnName("PID");
            entity.Property(e => e.PrintAt)
                .HasColumnType("datetime")
                .HasColumnName("PRINT_AT");
            entity.Property(e => e.PrintDate).HasColumnName("PRINT_DATE");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.ScanAt)
                .HasColumnType("datetime")
                .HasColumnName("SCAN_AT");
            entity.Property(e => e.ScanDate).HasColumnName("SCAN_DATE");
            entity.Property(e => e.SecondInspector)
                .HasMaxLength(45)
                .HasColumnName("SECOND_INSPECTOR");
            entity.Property(e => e.TagId).HasColumnName("TAG_ID");
            entity.Property(e => e.WorkOrder)
                .HasMaxLength(45)
                .HasColumnName("WORK_ORDER");
            entity.Property(e => e._4m)
                .HasMaxLength(45)
                .HasColumnName("4M");
        });

        modelBuilder.Entity<TbScanoutLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_scanout_line");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ip)
                .HasMaxLength(45)
                .HasColumnName("IP");
            entity.Property(e => e.LineNo)
                .HasMaxLength(45)
                .HasColumnName("LINE_NO");
            entity.Property(e => e.Model).HasMaxLength(45);
        });

        modelBuilder.Entity<TbSpecBlockJig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_spec_block_jig");

            entity.HasIndex(e => e.BlockJigId, "BLOCK_JIG_ID_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.BlockJigId)
                .HasMaxLength(45)
                .HasColumnName("BLOCK_JIG_ID");
            entity.Property(e => e.BlockJigName)
                .HasMaxLength(255)
                .HasColumnName("BLOCK_JIG_NAME");
            entity.Property(e => e.Board)
                .HasMaxLength(45)
                .HasColumnName("BOARD");
            entity.Property(e => e.Buyer)
                .HasMaxLength(45)
                .HasColumnName("BUYER");
            entity.Property(e => e.DateReceive)
                .HasColumnType("datetime")
                .HasColumnName("DATE_RECEIVE");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.Pn)
                .HasMaxLength(255)
                .HasColumnName("PN");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.RackNo)
                .HasMaxLength(45)
                .HasColumnName("RACK_NO");
            entity.Property(e => e.Remark)
                .HasMaxLength(255)
                .HasColumnName("REMARK");
            entity.Property(e => e.Side)
                .HasMaxLength(45)
                .HasColumnName("SIDE");
            entity.Property(e => e.Status)
                .HasMaxLength(45)
                .HasColumnName("STATUS");
            entity.Property(e => e.Ver)
                .HasMaxLength(45)
                .HasColumnName("VER");
        });

        modelBuilder.Entity<TbSpecSolder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_spec_solder");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.Board)
                .HasMaxLength(10)
                .HasColumnName("BOARD");
            entity.Property(e => e.Buyer)
                .HasMaxLength(20)
                .HasColumnName("BUYER");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.ModelSuffix)
                .HasMaxLength(45)
                .HasColumnName("MODEL_SUFFIX");
            entity.Property(e => e.PcbPn)
                .HasMaxLength(45)
                .HasColumnName("PCB_PN");
            entity.Property(e => e.PcbaPn)
                .HasMaxLength(45)
                .HasColumnName("PCBA_PN");
            entity.Property(e => e.SmtPn)
                .HasMaxLength(45)
                .HasColumnName("SMT_PN");
            entity.Property(e => e.SolderTypeBot)
                .HasMaxLength(100)
                .HasColumnName("SOLDER_TYPE_BOT");
            entity.Property(e => e.SolderTypeTop)
                .HasMaxLength(100)
                .HasColumnName("SOLDER_TYPE_TOP");
        });

        modelBuilder.Entity<TbSpecSqueegee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_spec_squeegee");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.Board)
                .HasMaxLength(45)
                .HasColumnName("BOARD");
            entity.Property(e => e.Buyer)
                .HasMaxLength(45)
                .HasColumnName("BUYER");
            entity.Property(e => e.DateReceive)
                .HasColumnType("datetime")
                .HasColumnName("DATE_RECEIVE");
            entity.Property(e => e.ModelName)
                .HasMaxLength(45)
                .HasColumnName("MODEL_NAME");
            entity.Property(e => e.PcbPn)
                .HasMaxLength(45)
                .HasColumnName("PCB_PN");
            entity.Property(e => e.RackId)
                .HasMaxLength(45)
                .HasColumnName("RACK_ID");
            entity.Property(e => e.Remark)
                .HasMaxLength(45)
                .HasColumnName("REMARK");
            entity.Property(e => e.Size)
                .HasMaxLength(45)
                .HasColumnName("SIZE");
            entity.Property(e => e.SqueegeeId)
                .HasMaxLength(45)
                .HasColumnName("SQUEEGEE_ID");
        });

        modelBuilder.Entity<TbSpecStencil>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_spec_stencil");

            entity.HasIndex(e => e.StencilId, "STENCIL_ID_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.Buyer)
                .HasMaxLength(100)
                .HasColumnName("BUYER");
            entity.Property(e => e.DateReceive)
                .HasColumnType("datetime")
                .HasColumnName("DATE_RECEIVE");
            entity.Property(e => e.Division)
                .HasMaxLength(100)
                .HasColumnName("DIVISION");
            entity.Property(e => e.ManagerNo)
                .HasMaxLength(100)
                .HasColumnName("MANAGER_NO");
            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .HasColumnName("MODEL");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.RackId)
                .HasMaxLength(100)
                .HasColumnName("RACK_ID");
            entity.Property(e => e.RackNo)
                .HasMaxLength(100)
                .HasColumnName("RACK_NO");
            entity.Property(e => e.Remark)
                .HasMaxLength(255)
                .HasColumnName("REMARK");
            entity.Property(e => e.Side)
                .HasMaxLength(100)
                .HasColumnName("SIDE");
            entity.Property(e => e.StencilId)
                .HasMaxLength(100)
                .HasColumnName("STENCIL_ID");
            entity.Property(e => e.StencilPn)
                .HasMaxLength(255)
                .HasColumnName("STENCIL_PN");
            entity.Property(e => e.StencilThickness)
                .HasMaxLength(100)
                .HasColumnName("STENCIL_THICKNESS");
            entity.Property(e => e.StencilType)
                .HasMaxLength(100)
                .HasColumnName("STENCIL_TYPE");
            entity.Property(e => e.Ver)
                .HasMaxLength(100)
                .HasColumnName("VER");
        });

        modelBuilder.Entity<TbStationMonitoring>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_station_monitoring");

            entity.HasIndex(e => e.IpAddress, "IP_ADDRESS_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("IP_ADDRESS");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("NOTE");
            entity.Property(e => e.StationName)
                .HasMaxLength(45)
                .HasColumnName("STATION_NAME");
            entity.Property(e => e.Status)
                .HasMaxLength(45)
                .HasComment("RUNNING\nOFF")
                .HasColumnName("STATUS");
            entity.Property(e => e.UpdateAt)
                .HasColumnType("datetime")
                .HasColumnName("UPDATE_AT");
        });

        modelBuilder.Entity<TbUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tb_user");

            entity.HasIndex(e => e.Email, "EMAIL_UNIQUE").IsUnique();

            entity.HasIndex(e => e.UserName, "USER_NAME_UNIQUE").IsUnique();

            entity.HasIndex(e => e.RoleId, "fk_user_role_idx");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.CreateAt)
                .HasColumnType("datetime")
                .HasColumnName("CREATE_AT");
            entity.Property(e => e.Email).HasColumnName("EMAIL");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("FULL_NAME");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("LAST_LOGIN");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.Position)
                .HasMaxLength(255)
                .HasColumnName("POSITION");
            entity.Property(e => e.RoleId).HasColumnName("ROLE_ID");
            entity.Property(e => e.UpdateAt)
                .HasColumnType("datetime")
                .HasColumnName("UPDATE_AT");
            entity.Property(e => e.UserName)
                .HasMaxLength(45)
                .HasColumnName("USER_NAME");

            entity.HasOne(d => d.Role).WithMany(p => p.TbUsers)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_user_role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
