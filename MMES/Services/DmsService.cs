using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMES.Data;
using MMES.Models.Dms;

namespace MMES.Services
{
    public class DmsService : IDmsService
    {
        private readonly MMesDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<DmsService> _logger;
        private readonly string _dmsUploadPath;
        private static bool _tablesCreated = false;
        private static readonly object _lock = new object();

        public DmsService(
            MMesDbContext dbContext,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<DmsService> logger)
        {
            _dbContext = dbContext;
            _env = env;
            _config = config;
            _logger = logger;

            string baseUploadDir = _config["FileStorage:StoragePath"] ?? "App_Data/Uploads";
            string fullBaseDir = Path.IsPathRooted(baseUploadDir) ? baseUploadDir : Path.Combine(_env.ContentRootPath, baseUploadDir);
            _dmsUploadPath = Path.Combine(fullBaseDir, "DmsDocuments");

            if (!Directory.Exists(_dmsUploadPath))
            {
                Directory.CreateDirectory(_dmsUploadPath);
            }
        }

        public async Task EnsureDatabaseTablesAsync()
        {
            if (_tablesCreated) return;

            try
            {
                DbConnection conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using DbCommand cmd = conn.CreateCommand();

                // 1. Create dms_document_types
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_document_types` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `type_code` VARCHAR(50) NOT NULL UNIQUE,
                        `type_name` VARCHAR(100) NOT NULL,
                        `icon_class` VARCHAR(50) DEFAULT 'bi-file-earmark-text',
                        `display_order` INT DEFAULT 0,
                        `is_active` TINYINT(1) DEFAULT 1
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 2. Create dms_departments
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_departments` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `dept_code` VARCHAR(50) NOT NULL UNIQUE,
                        `dept_name` VARCHAR(100) NOT NULL,
                        `badge_color` VARCHAR(30) DEFAULT '#0B72B9',
                        `is_active` TINYINT(1) DEFAULT 1
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 3. Create dms_processes
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_processes` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `process_code` VARCHAR(50) NOT NULL UNIQUE,
                        `process_name` VARCHAR(100) NOT NULL,
                        `is_active` TINYINT(1) DEFAULT 1
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 4. Create dms_machines
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_machines` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `machine_code` VARCHAR(50) NOT NULL UNIQUE,
                        `machine_name` VARCHAR(100) NOT NULL,
                        `process_id` INT NULL,
                        `is_active` TINYINT(1) DEFAULT 1
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 5. Create dms_models
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_models` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `model_code` VARCHAR(50) NOT NULL UNIQUE,
                        `model_name` VARCHAR(100) NOT NULL,
                        `customer_name` VARCHAR(100) NULL,
                        `is_active` TINYINT(1) DEFAULT 1
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 6. Create dms_documents
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_documents` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `doc_number` VARCHAR(100) NOT NULL UNIQUE,
                        `title` VARCHAR(255) NOT NULL,
                        `type_id` INT NOT NULL,
                        `dept_id` INT NOT NULL,
                        `current_version` VARCHAR(20) DEFAULT 'v1.0',
                        `status` VARCHAR(20) DEFAULT 'Draft',
                        `created_by` VARCHAR(100) NOT NULL,
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `approved_by` VARCHAR(100) NULL,
                        `approved_at` DATETIME NULL,
                        `description` TEXT NULL,
                        INDEX `idx_doc_type` (`type_id`),
                        INDEX `idx_doc_dept` (`dept_id`),
                        INDEX `idx_doc_status` (`status`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 7. Create dms_document_versions
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_document_versions` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `doc_id` INT NOT NULL,
                        `version_number` VARCHAR(20) NOT NULL,
                        `file_path` VARCHAR(500) NOT NULL,
                        `file_name` VARCHAR(255) NOT NULL,
                        `file_size` BIGINT DEFAULT 0,
                        `file_extension` VARCHAR(10) DEFAULT '',
                        `file_hash` VARCHAR(64) NULL,
                        `change_summary` TEXT NULL,
                        `uploaded_by` VARCHAR(100) NOT NULL,
                        `uploaded_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `is_active` TINYINT(1) DEFAULT 1,
                        INDEX `idx_ver_doc` (`doc_id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // 8. Mapping Tables
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_doc_processes` (
                        `doc_id` INT NOT NULL,
                        `process_id` INT NOT NULL,
                        PRIMARY KEY (`doc_id`, `process_id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_doc_machines` (
                        `doc_id` INT NOT NULL,
                        `machine_id` INT NOT NULL,
                        PRIMARY KEY (`doc_id`, `machine_id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `dms_doc_models` (
                        `doc_id` INT NOT NULL,
                        `model_id` INT NOT NULL,
                        PRIMARY KEY (`doc_id`, `model_id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();

                // Seed initial data if tables are empty
                await SeedMasterDataAsync(conn);

                _tablesCreated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing DMS database initialization");
                throw;
            }
        }

        private async Task SeedMasterDataAsync(DbConnection conn)
        {
            using DbCommand cmd = conn.CreateCommand();

            // Seed Document Types (Image 1)
            cmd.CommandText = "SELECT COUNT(*) FROM dms_document_types;";
            var typeCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (typeCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO dms_document_types (type_code, type_name, icon_class, display_order) VALUES
                    ('PROCESS', 'Process', 'bi-gear', 1),
                    ('CHECKSHEET', 'Checksheet', 'bi-card-checklist', 2),
                    ('STANDARD', 'Standard', 'bi-file-earmark-text', 3),
                    ('COMMON', 'Common', 'bi-folder', 4),
                    ('CSR', 'CSR', 'bi-file-earmark-check', 5),
                    ('IFP', 'IFP', 'bi-globe', 6),
                    ('WORK_INSTRUCTION', 'Work Instruction', 'bi-file-earmark-code', 7),
                    ('PFD', 'PFD', 'bi-file-earmark', 8),
                    ('PFMEA', 'PFMEA', 'bi-diagram-3', 9),
                    ('MAINTENANCE', 'Maintenance', 'bi-wrench', 10),
                    ('CONTROL_PLAN', 'Control Plan', 'bi-sliders', 11);";
                await cmd.ExecuteNonQueryAsync();
            }

            // Seed Departments (Image 2)
            cmd.CommandText = "SELECT COUNT(*) FROM dms_departments;";
            var deptCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (deptCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO dms_departments (dept_code, dept_name, badge_color) VALUES
                    ('CS', 'Customer Service', '#0284C7'),
                    ('ESD', 'ESD Control', '#2563EB'),
                    ('ESH', 'Environmental Safety', '#16A34A'),
                    ('HR', 'Human Resources', '#D97706'),
                    ('HSEVN', 'HSE Vietnam', '#DC2626'),
                    ('IQC', 'Incoming Quality', '#06B6D4'),
                    ('IT', 'Information Technology', '#4F46E5'),
                    ('LQC SMT', 'Line QC SMT', '#EA580C'),
                    ('OQC', 'Outgoing Quality', '#EC4899'),
                    ('PCBA', 'Printed Circuit Board', '#84CC16'),
                    ('PM PCBA', 'Prod Mgr PCBA', '#0284C7'),
                    ('PM SMT', 'Prod Mgr SMT', '#059669'),
                    ('QA', 'Quality Assurance', '#E11D48'),
                    ('RD', 'Research & Dev', '#10B981'),
                    ('REPAIR', 'Repair Dept', '#F43F5E'),
                    ('FG', 'Finished Goods', '#8B5CF6'),
                    ('WH', 'Warehouse', '#6366F1');";
                await cmd.ExecuteNonQueryAsync();
            }

            // Seed Processes
            cmd.CommandText = "SELECT COUNT(*) FROM dms_processes;";
            var procCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (procCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO dms_processes (process_code, process_name) VALUES
                    ('ALL_PROCESS', 'ALL PROCESS'),
                    ('SCREEN_PRINTER', 'Screen Printer'),
                    ('MOUNT', 'Mount'),
                    ('AOI', 'AOI'),
                    ('MOI', 'MOI'),
                    ('SPI', 'SPI'),
                    ('AUTO_LABEL', 'Auto label'),
                    ('BUFFER', 'Buffer'),
                    ('COATING', 'Coating');";
                await cmd.ExecuteNonQueryAsync();
            }

            // Seed Machines
            cmd.CommandText = "SELECT COUNT(*) FROM dms_machines;";
            var machCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (machCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO dms_machines (machine_code, machine_name) VALUES
                    ('ALL_MACHINES', 'ALL MACHINES'),
                    ('AOI', 'AOI Machine'),
                    ('AUTO_LABEL', 'AUTO LABEL'),
                    ('AUTO_VISION', 'AUTO VISION'),
                    ('BONDING_AXXON', 'BONDING AXXON'),
                    ('SPI_PARMI', 'SPI PARMI');";
                await cmd.ExecuteNonQueryAsync();
            }

            // Seed Models
            cmd.CommandText = "SELECT COUNT(*) FROM dms_models;";
            var modelCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (modelCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO dms_models (model_code, model_name) VALUES
                    ('ALL_MODELS', 'ALL MODELS'),
                    ('AMT', 'AMT Model'),
                    ('AR_HUD', 'AR HUD Model'),
                    ('AUDI', 'AUDI Model'),
                    ('AUDI_CID', 'AUDI CID Model');";
                await cmd.ExecuteNonQueryAsync();
            }

            // Always ensure ALL_PROCESS, ALL_MACHINES, ALL_MODELS exist if table already had rows
            cmd.CommandText = @"
                INSERT IGNORE INTO dms_processes (process_code, process_name) VALUES ('ALL_PROCESS', 'ALL PROCESS');
                INSERT IGNORE INTO dms_machines (machine_code, machine_name) VALUES ('ALL_MACHINES', 'ALL MACHINES');
                INSERT IGNORE INTO dms_models (model_code, model_name) VALUES ('ALL_MODELS', 'ALL MODELS');";
            await cmd.ExecuteNonQueryAsync();

            // Seed Initial Documents from Image 2
            cmd.CommandText = "SELECT COUNT(*) FROM dms_documents;";
            var docCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (docCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO dms_documents (doc_number, title, type_id, dept_id, current_version, status, created_by) VALUES
                    ('HSEVN-IVI-LQC-WI-024', 'HSEVN-IVI-LQC-WI-024_HƯỚNG DẪN TẠO CHƯƠNG TRÌNH MÁY MOI.AOI_VER 01', 7, 8, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-001', 'HSEVN-IVI-QA-WI-001_HƯỚNG DẪN THAO TÁC VERIFY_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-002', 'HSEVN-IVI-QA-WI-002_HƯỚNG DẪN ĐO LCR_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-003', 'HSEVN-IVI-QA-WI-003_HƯỚNG DẪN KIỂM TRA GOLDEN SAMPLE_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-004', 'HSEVN-IVI-QA-WI-004_HƯỚNG DẪN XỬ LÝ KHI XẢY RA LỖI ĐẦU VÀO_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-005', 'HSEVN-IVI-QA-WI-005_HƯỚNG DẪN XỬ LÝ KHI XẢY RA LỖI TRÊN LINE_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-006', 'HSEVN-IVI-QA-WI-006_HƯỚNG DẪN KIỂM TRA LẤY MẪU THEO AQL_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-007', 'HSEVN-IVI-QA-WI-007_HƯỚNG DẪN KIỂM TRA PCB_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-008', 'HSEVN-IVI-QA-WI-008_HƯỚNG DẪN KIỂM TRA TÀI LIỆU RETAPE_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin'),
                    ('HSEVN-IVI-QA-WI-009', 'HSEVN-IVI-QA-WI-009_HƯỚNG DẪN KIỂM TRA BGA_VER 01', 7, 13, 'v1.0', 'Draft', 'System Admin');";
                await cmd.ExecuteNonQueryAsync();

                // Create default versions for initial docs
                cmd.CommandText = @"
                    INSERT INTO dms_document_versions (doc_id, version_number, file_path, file_name, file_size, file_extension, uploaded_by)
                    SELECT id, 'v1.0', 'App_Data/Uploads/DmsDocuments/sample.pdf', CONCAT(doc_number, '.pdf'), 102400, '.pdf', 'System Admin'
                    FROM dms_documents;";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<DmsCategoryCountDto>> GetCategoriesWithCountsAsync()
        {
            await EnsureDatabaseTablesAsync();
            var list = new List<DmsCategoryCountDto>();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT t.id, t.type_code, t.type_name, t.icon_class, COUNT(d.id) AS doc_count
                FROM dms_document_types t
                LEFT JOIN dms_documents d ON d.type_id = t.id
                WHERE t.is_active = 1
                GROUP BY t.id, t.type_code, t.type_name, t.icon_class, t.display_order
                ORDER BY t.display_order ASC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DmsCategoryCountDto
                {
                    TypeId = Convert.ToInt32(reader.GetValue(0)),
                    TypeCode = Convert.ToString(reader.GetValue(1)) ?? "",
                    TypeName = Convert.ToString(reader.GetValue(2)) ?? "",
                    IconClass = reader.IsDBNull(3) ? "bi-file-earmark-text" : Convert.ToString(reader.GetValue(3))!,
                    Count = Convert.ToInt32(reader.GetValue(4))
                });
            }

            return list;
        }

        public async Task<List<DmsDepartment>> GetDepartmentsAsync()
        {
            await EnsureDatabaseTablesAsync();
            var list = new List<DmsDepartment>();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, dept_code, dept_name, badge_color, is_active FROM dms_departments WHERE is_active = 1 ORDER BY dept_code ASC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DmsDepartment
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    DeptCode = Convert.ToString(reader.GetValue(1)) ?? "",
                    DeptName = Convert.ToString(reader.GetValue(2)) ?? "",
                    BadgeColor = Convert.ToString(reader.GetValue(3)) ?? "#0B72B9",
                    IsActive = Convert.ToBoolean(reader.GetValue(4))
                });
            }

            return list;
        }

        public async Task<List<DmsProcess>> GetProcessesAsync()
        {
            await EnsureDatabaseTablesAsync();
            var list = new List<DmsProcess>();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, process_code, process_name, is_active FROM dms_processes WHERE is_active = 1 ORDER BY CASE WHEN process_code LIKE 'ALL%' THEN 0 ELSE 1 END, process_name ASC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DmsProcess
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    ProcessCode = Convert.ToString(reader.GetValue(1)) ?? "",
                    ProcessName = Convert.ToString(reader.GetValue(2)) ?? "",
                    IsActive = Convert.ToBoolean(reader.GetValue(3))
                });
            }

            return list;
        }

        public async Task<List<DmsMachine>> GetMachinesAsync()
        {
            await EnsureDatabaseTablesAsync();
            var list = new List<DmsMachine>();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, machine_code, machine_name, process_id, is_active FROM dms_machines WHERE is_active = 1 ORDER BY CASE WHEN machine_code LIKE 'ALL%' THEN 0 ELSE 1 END, machine_name ASC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DmsMachine
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    MachineCode = Convert.ToString(reader.GetValue(1)) ?? "",
                    MachineName = Convert.ToString(reader.GetValue(2)) ?? "",
                    ProcessId = reader.IsDBNull(3) ? null : Convert.ToInt32(reader.GetValue(3)),
                    IsActive = Convert.ToBoolean(reader.GetValue(4))
                });
            }

            return list;
        }

        public async Task<List<DmsModelItem>> GetModelsAsync()
        {
            await EnsureDatabaseTablesAsync();
            var list = new List<DmsModelItem>();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, model_code, model_name, customer_name, is_active FROM dms_models WHERE is_active = 1 ORDER BY CASE WHEN model_code LIKE 'ALL%' THEN 0 ELSE 1 END, model_name ASC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DmsModelItem
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    ModelCode = Convert.ToString(reader.GetValue(1)) ?? "",
                    ModelName = Convert.ToString(reader.GetValue(2)) ?? "",
                    CustomerName = reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3)),
                    IsActive = Convert.ToBoolean(reader.GetValue(4))
                });
            }

            return list;
        }

        public async Task<DmsPagedResult<DmsDocumentDto>> GetDocumentsPagedAsync(DmsFilterRequest filter)
        {
            await EnsureDatabaseTablesAsync();
            var result = new DmsPagedResult<DmsDocumentDto>
            {
                Page = filter.Page > 0 ? filter.Page : 1,
                PageSize = filter.PageSize > 0 ? filter.PageSize : 20
            };

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();

            var whereClauses = new List<string> { "1=1" };

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                whereClauses.Add("(d.doc_number LIKE @search OR d.title LIKE @search OR d.created_by LIKE @search)");
                AddParam(cmd, "@search", $"%{filter.Search.Trim()}%");
            }

            if (filter.TypeId.HasValue && filter.TypeId.Value > 0)
            {
                whereClauses.Add("d.type_id = @typeId");
                AddParam(cmd, "@typeId", filter.TypeId.Value);
            }

            if (filter.DeptId.HasValue && filter.DeptId.Value > 0)
            {
                whereClauses.Add("d.dept_id = @deptId");
                AddParam(cmd, "@deptId", filter.DeptId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "ALL")
            {
                whereClauses.Add("d.status = @status");
                AddParam(cmd, "@status", filter.Status);
            }

            if (filter.ProcessIds != null && filter.ProcessIds.Count > 0)
            {
                whereClauses.Add($"d.id IN (SELECT doc_id FROM dms_doc_processes WHERE process_id IN ({string.Join(",", filter.ProcessIds)}))");
            }

            if (filter.MachineIds != null && filter.MachineIds.Count > 0)
            {
                whereClauses.Add($"d.id IN (SELECT doc_id FROM dms_doc_machines WHERE machine_id IN ({string.Join(",", filter.MachineIds)}))");
            }

            if (filter.ModelIds != null && filter.ModelIds.Count > 0)
            {
                whereClauses.Add($"d.id IN (SELECT doc_id FROM dms_doc_models WHERE model_id IN ({string.Join(",", filter.ModelIds)}))");
            }

            string whereSql = string.Join(" AND ", whereClauses);

            // Count Query
            cmd.CommandText = $"SELECT COUNT(*) FROM dms_documents d WHERE {whereSql};";
            result.TotalItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Data Query
            int offset = (result.Page - 1) * result.PageSize;
            cmd.CommandText = $@"
                SELECT d.id, d.doc_number, d.title, d.type_id, t.type_name, d.dept_id, dp.dept_code, dp.badge_color,
                       d.current_version, d.status, d.created_by, d.created_at, d.approved_by, d.approved_at, d.description,
                       v.file_name, v.file_extension, v.file_size, v.uploaded_at
                FROM dms_documents d
                LEFT JOIN dms_document_types t ON d.type_id = t.id
                LEFT JOIN dms_departments dp ON d.dept_id = dp.id
                LEFT JOIN dms_document_versions v ON v.doc_id = d.id AND v.is_active = 1
                WHERE {whereSql}
                ORDER BY d.created_at DESC
                LIMIT {result.PageSize} OFFSET {offset};";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var createdAt = Convert.ToDateTime(reader.GetValue(11));
                var updatedAt = reader.IsDBNull(18) ? createdAt : Convert.ToDateTime(reader.GetValue(18));

                result.Items.Add(new DmsDocumentDto
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    DocNumber = Convert.ToString(reader.GetValue(1)) ?? "",
                    Title = Convert.ToString(reader.GetValue(2)) ?? "",
                    TypeId = Convert.ToInt32(reader.GetValue(3)),
                    TypeName = reader.IsDBNull(4) ? "" : Convert.ToString(reader.GetValue(4))!,
                    DeptId = Convert.ToInt32(reader.GetValue(5)),
                    DeptCode = reader.IsDBNull(6) ? "" : Convert.ToString(reader.GetValue(6))!,
                    DeptBadgeColor = reader.IsDBNull(7) ? "#0B72B9" : Convert.ToString(reader.GetValue(7))!,
                    CurrentVersion = reader.IsDBNull(8) ? "v1.0" : Convert.ToString(reader.GetValue(8))!,
                    Status = reader.IsDBNull(9) ? "Draft" : Convert.ToString(reader.GetValue(9))!,
                    CreatedBy = Convert.ToString(reader.GetValue(10)) ?? "",
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                    ApprovedBy = reader.IsDBNull(12) ? null : Convert.ToString(reader.GetValue(12)),
                    ApprovedAt = reader.IsDBNull(13) ? null : Convert.ToDateTime(reader.GetValue(13)),
                    Description = reader.IsDBNull(14) ? null : Convert.ToString(reader.GetValue(14)),
                    FileName = reader.IsDBNull(15) ? "" : Convert.ToString(reader.GetValue(15))!,
                    FileExtension = reader.IsDBNull(16) ? "" : Convert.ToString(reader.GetValue(16))!,
                    FileSize = reader.IsDBNull(17) ? 0 : Convert.ToInt64(reader.GetValue(17))
                });
            }

            return result;
        }

        public async Task<List<DmsDocumentVersion>> GetDocumentVersionsAsync(int docId)
        {
            await EnsureDatabaseTablesAsync();
            var list = new List<DmsDocumentVersion>();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, doc_id, version_number, file_name, file_size, file_extension, change_summary, uploaded_by, uploaded_at, is_active
                FROM dms_document_versions
                WHERE doc_id = @docId
                ORDER BY uploaded_at DESC;";
            AddParam(cmd, "@docId", docId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DmsDocumentVersion
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    DocId = Convert.ToInt32(reader.GetValue(1)),
                    VersionNumber = Convert.ToString(reader.GetValue(2)) ?? "v1.0",
                    FileName = Convert.ToString(reader.GetValue(3)) ?? "",
                    FileSize = Convert.ToInt64(reader.GetValue(4)),
                    FileExtension = Convert.ToString(reader.GetValue(5)) ?? "",
                    ChangeSummary = reader.IsDBNull(6) ? null : Convert.ToString(reader.GetValue(6)),
                    UploadedBy = Convert.ToString(reader.GetValue(7)) ?? "",
                    UploadedAt = Convert.ToDateTime(reader.GetValue(8)),
                    IsActive = Convert.ToBoolean(reader.GetValue(9))
                });
            }

            return list;
        }

        public async Task<DmsDocumentDto?> GetDocumentByIdAsync(int id)
        {
            var paged = await GetDocumentsPagedAsync(new DmsFilterRequest { Search = null, Page = 1, PageSize = 1000 });
            return paged.Items.FirstOrDefault(i => i.Id == id);
        }

        public async Task<DmsDocumentDto> CreateDocumentAsync(CreateDocumentRequest request)
        {
            await EnsureDatabaseTablesAsync();

            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("Please attach a valid document file.");
            }

            string ext = Path.GetExtension(request.File.FileName);
            string storedName = $"{Guid.NewGuid():N}{ext}";
            string physicalPath = Path.Combine(_dmsUploadPath, storedName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                using DbCommand cmd = conn.CreateCommand();
                cmd.Transaction = transaction;

                // 1. Insert dms_documents
                cmd.CommandText = @"
                    INSERT INTO dms_documents (doc_number, title, type_id, dept_id, current_version, status, created_by, created_at, description)
                    VALUES (@docNum, @title, @typeId, @deptId, 'v1.0', 'Draft', @createdBy, NOW(), @desc);
                    SELECT LAST_INSERT_ID();";

                AddParam(cmd, "@docNum", request.DocNumber);
                AddParam(cmd, "@title", request.Title);
                AddParam(cmd, "@typeId", request.TypeId);
                AddParam(cmd, "@deptId", request.DeptId);
                AddParam(cmd, "@createdBy", string.IsNullOrWhiteSpace(request.CreatedBy) ? "Admin" : request.CreatedBy);
                AddParam(cmd, "@desc", (object?)request.Description ?? DBNull.Value);

                int docId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // 2. Insert dms_document_versions
                cmd.CommandText = @"
                    INSERT INTO dms_document_versions (doc_id, version_number, file_path, file_name, file_size, file_extension, uploaded_by, uploaded_at, is_active)
                    VALUES (@docId, 'v1.0', @path, @name, @size, @ext, @by, NOW(), 1);";

                cmd.Parameters.Clear();
                AddParam(cmd, "@docId", docId);
                AddParam(cmd, "@path", physicalPath);
                AddParam(cmd, "@name", request.File.FileName);
                AddParam(cmd, "@size", request.File.Length);
                AddParam(cmd, "@ext", ext);
                AddParam(cmd, "@by", request.CreatedBy);
                await cmd.ExecuteNonQueryAsync();

                // 3. Mappings
                if (request.ProcessIds != null)
                {
                    foreach (var procId in request.ProcessIds)
                    {
                        cmd.CommandText = "INSERT IGNORE INTO dms_doc_processes (doc_id, process_id) VALUES (@docId, @pId);";
                        cmd.Parameters.Clear();
                        AddParam(cmd, "@docId", docId);
                        AddParam(cmd, "@pId", procId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (request.MachineIds != null)
                {
                    foreach (var mId in request.MachineIds)
                    {
                        cmd.CommandText = "INSERT IGNORE INTO dms_doc_machines (doc_id, machine_id) VALUES (@docId, @mId);";
                        cmd.Parameters.Clear();
                        AddParam(cmd, "@docId", docId);
                        AddParam(cmd, "@mId", mId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (request.ModelIds != null)
                {
                    foreach (var modId in request.ModelIds)
                    {
                        cmd.CommandText = "INSERT IGNORE INTO dms_doc_models (doc_id, model_id) VALUES (@docId, @modId);";
                        cmd.Parameters.Clear();
                        AddParam(cmd, "@docId", docId);
                        AddParam(cmd, "@modId", modId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();

                var dto = await GetDocumentByIdAsync(docId);
                return dto ?? new DmsDocumentDto { Id = docId, Title = request.Title, DocNumber = request.DocNumber };
            }
            catch
            {
                await transaction.RollbackAsync();
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
                throw;
            }
        }

        public async Task<DmsDocumentDto> UpdateVersionAsync(UpdateVersionRequest request)
        {
            await EnsureDatabaseTablesAsync();

            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("Please select a valid file for the new version.");
            }

            string ext = Path.GetExtension(request.File.FileName);
            string storedName = $"{Guid.NewGuid():N}{ext}";
            string physicalPath = Path.Combine(_dmsUploadPath, storedName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                using DbCommand cmd = conn.CreateCommand();
                cmd.Transaction = transaction;

                // Deactivate previous active version
                cmd.CommandText = "UPDATE dms_document_versions SET is_active = 0 WHERE doc_id = @docId;";
                AddParam(cmd, "@docId", request.DocId);
                await cmd.ExecuteNonQueryAsync();

                // Insert new version
                cmd.CommandText = @"
                    INSERT INTO dms_document_versions (doc_id, version_number, file_path, file_name, file_size, file_extension, change_summary, uploaded_by, uploaded_at, is_active)
                    VALUES (@docId, @ver, @path, @name, @size, @ext, @summary, @by, NOW(), 1);";

                cmd.Parameters.Clear();
                AddParam(cmd, "@docId", request.DocId);
                AddParam(cmd, "@ver", request.NewVersionNumber);
                AddParam(cmd, "@path", physicalPath);
                AddParam(cmd, "@name", request.File.FileName);
                AddParam(cmd, "@size", request.File.Length);
                AddParam(cmd, "@ext", ext);
                AddParam(cmd, "@summary", (object?)request.ChangeSummary ?? DBNull.Value);
                AddParam(cmd, "@by", request.UploadedBy);
                await cmd.ExecuteNonQueryAsync();

                // Update document current version & set status to Pending
                cmd.CommandText = "UPDATE dms_documents SET current_version = @ver, status = 'Pending' WHERE id = @docId;";
                cmd.Parameters.Clear();
                AddParam(cmd, "@ver", request.NewVersionNumber);
                AddParam(cmd, "@docId", request.DocId);
                await cmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                var dto = await GetDocumentByIdAsync(request.DocId);
                return dto ?? new DmsDocumentDto { Id = request.DocId };
            }
            catch
            {
                await transaction.RollbackAsync();
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
                throw;
            }
        }

        public async Task<bool> ApproveDocumentAsync(int docId, string approvedBy)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE dms_documents 
                SET status = 'Approved', approved_by = @by, approved_at = NOW() 
                WHERE id = @docId;";

            AddParam(cmd, "@by", string.IsNullOrWhiteSpace(approvedBy) ? "Admin" : approvedBy);
            AddParam(cmd, "@docId", docId);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> RejectDocumentAsync(int docId, string rejectedBy)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_documents SET status = 'Rejected' WHERE id = @docId;";
            AddParam(cmd, "@docId", docId);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?> GetFileDownloadAsync(int docId, int? versionId = null)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();

            if (versionId.HasValue && versionId.Value > 0)
            {
                cmd.CommandText = "SELECT file_path, file_name, file_extension FROM dms_document_versions WHERE id = @vId;";
                AddParam(cmd, "@vId", versionId.Value);
            }
            else
            {
                cmd.CommandText = "SELECT file_path, file_name, file_extension FROM dms_document_versions WHERE doc_id = @docId AND is_active = 1 LIMIT 1;";
                AddParam(cmd, "@docId", docId);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            string filePath = Convert.ToString(reader.GetValue(0)) ?? "";
            string fileName = Convert.ToString(reader.GetValue(1)) ?? "";
            string ext = Convert.ToString(reader.GetValue(2)) ?? "";

            if (!File.Exists(filePath)) return null;

            string contentType = ext.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            return (memoryStream, contentType, fileName);
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?> GetFilePreviewAsync(int docId, int? versionId = null)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();

            if (versionId.HasValue && versionId.Value > 0)
            {
                cmd.CommandText = "SELECT file_path, file_name, file_extension FROM dms_document_versions WHERE id = @vId;";
                AddParam(cmd, "@vId", versionId.Value);
            }
            else
            {
                cmd.CommandText = "SELECT file_path, file_name, file_extension FROM dms_document_versions WHERE doc_id = @docId AND is_active = 1 LIMIT 1;";
                AddParam(cmd, "@docId", docId);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            string filePath = Convert.ToString(reader.GetValue(0)) ?? "";
            string fileName = Convert.ToString(reader.GetValue(1)) ?? "";
            string ext = (Convert.ToString(reader.GetValue(2)) ?? "").ToLower();

            if (!File.Exists(filePath)) return null;

            // 1. Direct preview for PDF & Images
            if (ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                string contentType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    _ => "image/jpeg"
                };

                var ms = new MemoryStream();
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await fs.CopyToAsync(ms);
                }
                ms.Position = 0;
                return (ms, contentType, fileName);
            }

            // 2. Office files (.docx, .doc, .xlsx, .xls, .pptx, .ppt): Convert to PDF via LibreOffice CLI & Cache
            string cacheDir = Path.Combine(_dmsUploadPath, "pdf_cache");
            Directory.CreateDirectory(cacheDir);

            string fileHash = $"{Path.GetFileNameWithoutExtension(filePath)}_{File.GetLastWriteTimeUtc(filePath):yyyyMMddHHmmss}";
            string cachedPdfPath = Path.Combine(cacheDir, $"{fileHash}.pdf");

            if (!File.Exists(cachedPdfPath))
            {
                string sofficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";
                if (File.Exists(sofficePath))
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = sofficePath,
                            Arguments = $"--headless --convert-to pdf --outdir \"{cacheDir}\" \"{filePath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using var process = Process.Start(startInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();

                            // LibreOffice saves file as {cacheDir}/{FileNameWithoutExtension}.pdf
                            string convertedFile = Path.Combine(cacheDir, $"{Path.GetFileNameWithoutExtension(filePath)}.pdf");
                            if (File.Exists(convertedFile))
                            {
                                if (File.Exists(cachedPdfPath)) File.Delete(cachedPdfPath);
                                File.Move(convertedFile, cachedPdfPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting document to PDF via LibreOffice.");
                    }
                }
            }

            // If converted PDF exists, return it
            if (File.Exists(cachedPdfPath))
            {
                var ms = new MemoryStream();
                using (var fs = new FileStream(cachedPdfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await fs.CopyToAsync(ms);
                }
                ms.Position = 0;
                string previewName = $"{Path.GetFileNameWithoutExtension(fileName)}.pdf";
                return (ms, "application/pdf", previewName);
            }

            // Fallback: Return original download stream if conversion not available
            return await GetFileDownloadAsync(docId, versionId);
        }

        // Admin Master Data CRUD
        public async Task<DmsDepartment> AddDepartmentAsync(DmsDepartment dept)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO dms_departments (dept_code, dept_name, badge_color, is_active)
                VALUES (@code, @name, @color, 1);
                SELECT LAST_INSERT_ID();";

            AddParam(cmd, "@code", dept.DeptCode);
            AddParam(cmd, "@name", dept.DeptName);
            AddParam(cmd, "@color", string.IsNullOrWhiteSpace(dept.BadgeColor) ? "#0B72B9" : dept.BadgeColor);

            dept.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return dept;
        }

        public async Task<DmsProcess> AddProcessAsync(DmsProcess proc)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO dms_processes (process_code, process_name, is_active)
                VALUES (@code, @name, 1);
                SELECT LAST_INSERT_ID();";

            AddParam(cmd, "@code", proc.ProcessCode);
            AddParam(cmd, "@name", proc.ProcessName);

            proc.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return proc;
        }

        public async Task<DmsMachine> AddMachineAsync(DmsMachine mach)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO dms_machines (machine_code, machine_name, process_id, is_active)
                VALUES (@code, @name, @pId, 1);
                SELECT LAST_INSERT_ID();";

            AddParam(cmd, "@code", mach.MachineCode);
            AddParam(cmd, "@name", mach.MachineName);
            AddParam(cmd, "@pId", (object?)mach.ProcessId ?? DBNull.Value);

            mach.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return mach;
        }

        public async Task<DmsModelItem> AddModelAsync(DmsModelItem model)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO dms_models (model_code, model_name, customer_name, is_active)
                VALUES (@code, @name, @cust, 1);
                SELECT LAST_INSERT_ID();";

            AddParam(cmd, "@code", model.ModelCode);
            AddParam(cmd, "@name", model.ModelName);
            AddParam(cmd, "@cust", (object?)model.CustomerName ?? DBNull.Value);

            model.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return model;
        }

        public async Task<DmsDocumentType> AddTypeAsync(DmsDocumentType type)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO dms_document_types (type_code, type_name, icon_class, display_order, is_active)
                VALUES (@code, @name, @icon, @order, 1);
                SELECT LAST_INSERT_ID();";

            AddParam(cmd, "@code", type.TypeCode);
            AddParam(cmd, "@name", type.TypeName);
            AddParam(cmd, "@icon", string.IsNullOrWhiteSpace(type.IconClass) ? "bi-file-earmark-text" : type.IconClass);
            AddParam(cmd, "@order", type.DisplayOrder);

            type.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return type;
        }

        public async Task<bool> UpdateDepartmentAsync(DmsDepartment dept)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_departments SET dept_code = @code, dept_name = @name, badge_color = @color, is_active = @active WHERE id = @id;";
            AddParam(cmd, "@code", dept.DeptCode);
            AddParam(cmd, "@name", dept.DeptName);
            AddParam(cmd, "@color", string.IsNullOrWhiteSpace(dept.BadgeColor) ? "#0B72B9" : dept.BadgeColor);
            AddParam(cmd, "@active", dept.IsActive ? 1 : 0);
            AddParam(cmd, "@id", dept.Id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_departments SET is_active = 0 WHERE id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateProcessAsync(DmsProcess proc)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_processes SET process_code = @code, process_name = @name, is_active = @active WHERE id = @id;";
            AddParam(cmd, "@code", proc.ProcessCode);
            AddParam(cmd, "@name", proc.ProcessName);
            AddParam(cmd, "@active", proc.IsActive ? 1 : 0);
            AddParam(cmd, "@id", proc.Id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteProcessAsync(int id)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_processes SET is_active = 0 WHERE id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateMachineAsync(DmsMachine mach)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_machines SET machine_code = @code, machine_name = @name, process_id = @pId, is_active = @active WHERE id = @id;";
            AddParam(cmd, "@code", mach.MachineCode);
            AddParam(cmd, "@name", mach.MachineName);
            AddParam(cmd, "@pId", (object?)mach.ProcessId ?? DBNull.Value);
            AddParam(cmd, "@active", mach.IsActive ? 1 : 0);
            AddParam(cmd, "@id", mach.Id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteMachineAsync(int id)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_machines SET is_active = 0 WHERE id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateModelAsync(DmsModelItem model)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_models SET model_code = @code, model_name = @name, customer_name = @cust, is_active = @active WHERE id = @id;";
            AddParam(cmd, "@code", model.ModelCode);
            AddParam(cmd, "@name", model.ModelName);
            AddParam(cmd, "@cust", (object?)model.CustomerName ?? DBNull.Value);
            AddParam(cmd, "@active", model.IsActive ? 1 : 0);
            AddParam(cmd, "@id", model.Id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteModelAsync(int id)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_models SET is_active = 0 WHERE id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateTypeAsync(DmsDocumentType type)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_document_types SET type_code = @code, type_name = @name, icon_class = @icon, display_order = @order, is_active = @active WHERE id = @id;";
            AddParam(cmd, "@code", type.TypeCode);
            AddParam(cmd, "@name", type.TypeName);
            AddParam(cmd, "@icon", string.IsNullOrWhiteSpace(type.IconClass) ? "bi-file-earmark-text" : type.IconClass);
            AddParam(cmd, "@order", type.DisplayOrder);
            AddParam(cmd, "@active", type.IsActive ? 1 : 0);
            AddParam(cmd, "@id", type.Id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteTypeAsync(int id)
        {
            await EnsureDatabaseTablesAsync();
            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dms_document_types SET is_active = 0 WHERE id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> CheckDocumentNumberExistsAsync(string docNumber)
        {
            if (string.IsNullOrWhiteSpace(docNumber)) return false;

            await EnsureDatabaseTablesAsync();

            DbConnection conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM dms_documents WHERE LOWER(doc_number) = LOWER(@docNum);";
            AddParam(cmd, "@docNum", docNumber.Trim());

            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
