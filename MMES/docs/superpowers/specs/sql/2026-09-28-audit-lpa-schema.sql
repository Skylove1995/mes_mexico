-- =============================================================================
-- Audit system schema extension (LPA + extensible to System Audit / Daily Audit)
-- See docs/superpowers/specs/2026-09-28-audit-lpa-database-design.md
--
-- Extends the existing, currently-unused generic audit schema:
--   tb_audit_type, tb_audit_checksheet, tb_audit_result, tb_audit_status,
--   tb_audit_evidence_his
-- and replaces the unused tb_audit_action_his with a proper CAPA table.
--
-- Assumes tb_audit_* tables are empty (no app code references them yet).
-- Run inside a transaction / on a backup first.
-- =============================================================================

START TRANSACTION;

-- -----------------------------------------------------------------------------
-- 1. tb_audit_checksheet: item ordering, es-MX translation, time limit, score type
-- -----------------------------------------------------------------------------
ALTER TABLE tb_audit_checksheet
  ADD COLUMN SEQ_NO INT NULL AFTER CATEGORY,
  ADD COLUMN AUDIT_ITEM_TRANSLATE TEXT NULL AFTER AUDIT_ITEM,
  ADD COLUMN TIME_LIMIT VARCHAR(100) NULL AFTER CRITICAL,
  MODIFY COLUMN MAX_SCORE DECIMAL(6,2) NULL;

-- -----------------------------------------------------------------------------
-- 2. tb_audit_status: shared status vocabulary, make it unique so seeds are safe
-- -----------------------------------------------------------------------------
ALTER TABLE tb_audit_status
  ADD UNIQUE KEY STATUS_UNIQUE (STATUS);

INSERT INTO tb_audit_status (STATUS) VALUES
  ('Draft'), ('Submitted'), ('Checked'), ('Approved'), ('Rejected'),
  ('Open'), ('In Progress'), ('Rectified'), ('Closed'), ('Overdue')
ON DUPLICATE KEY UPDATE STATUS = VALUES(STATUS);

-- -----------------------------------------------------------------------------
-- 3. tb_audit_type: seed LPA + the two audit types the program will grow into
-- -----------------------------------------------------------------------------
INSERT INTO tb_audit_type (AUDIT_TYPE) VALUES
  ('LPA'), ('System Audit'), ('Daily Audit')
ON DUPLICATE KEY UPDATE AUDIT_TYPE = VALUES(AUDIT_TYPE);

-- -----------------------------------------------------------------------------
-- 4. tb_audit_session: one row per audit round (dept/month/shift + 3-role sign-off)
-- -----------------------------------------------------------------------------
CREATE TABLE tb_audit_session (
  ID INT NOT NULL AUTO_INCREMENT,
  AUDIT_TYPE_ID INT NULL,
  DEPT_ID INT NULL,
  AUDIT_MONTH DATE NULL,
  SHIFT VARCHAR(45) NULL,
  AUDITOR_ID INT NULL,
  CHECKED_BY_ID INT NULL,
  CHECKED_AT DATETIME NULL,
  APPROVED_BY_ID INT NULL,
  APPROVED_AT DATETIME NULL,
  STATUS_ID INT NULL,
  TOTAL_SCORE DECIMAL(8,2) NULL,
  TOTAL_SCORE_TARGET DECIMAL(8,2) NULL,
  SCORE_PERCENT DECIMAL(5,2) NULL,
  GRADE VARCHAR(10) NULL,
  NOTE TEXT NULL,
  CREATED_AT DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
  UPDATED_AT DATETIME NULL,
  PRIMARY KEY (ID),
  KEY fk_auditSession_auditType_idx (AUDIT_TYPE_ID),
  KEY fk_auditSession_dept_idx (DEPT_ID),
  KEY fk_auditSession_auditor_idx (AUDITOR_ID),
  KEY fk_auditSession_checkedBy_idx (CHECKED_BY_ID),
  KEY fk_auditSession_approvedBy_idx (APPROVED_BY_ID),
  KEY fk_auditSession_status_idx (STATUS_ID),
  CONSTRAINT fk_auditSession_auditType FOREIGN KEY (AUDIT_TYPE_ID) REFERENCES tb_audit_type (ID),
  CONSTRAINT fk_auditSession_dept FOREIGN KEY (DEPT_ID) REFERENCES tb_dept (ID),
  CONSTRAINT fk_auditSession_auditor FOREIGN KEY (AUDITOR_ID) REFERENCES tb_user (ID),
  CONSTRAINT fk_auditSession_checkedBy FOREIGN KEY (CHECKED_BY_ID) REFERENCES tb_user (ID),
  CONSTRAINT fk_auditSession_approvedBy FOREIGN KEY (APPROVED_BY_ID) REFERENCES tb_user (ID),
  CONSTRAINT fk_auditSession_status FOREIGN KEY (STATUS_ID) REFERENCES tb_audit_status (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- -----------------------------------------------------------------------------
-- 5. tb_audit_result: link each item result to its audit round; fix column sizes
-- -----------------------------------------------------------------------------
-- SCORE follows a fixed discrete rubric: 0 (fail), 2/4/6/8 (partial, auditor's call),
-- 10 (pass), or NULL (not yet answered -- valid ONLY while the session is Draft; N/A
-- is not a supported outcome, and the app must block submit until every item in the
-- session has a non-null score). See design doc's "Scoring rule" section.
-- Requires MySQL 8.0.16+ for CHECK to be enforced.
ALTER TABLE tb_audit_result
  ADD COLUMN SESSION_ID INT NOT NULL AFTER AUDIT_ITEM_ID,
  MODIFY COLUMN SCORE DECIMAL(6,2) NULL,
  MODIFY COLUMN JUDGE VARCHAR(5) NULL,
  ADD CONSTRAINT fk_auditResult_session FOREIGN KEY (SESSION_ID) REFERENCES tb_audit_session (ID),
  ADD KEY fk_auditResult_session_idx (SESSION_ID),
  ADD CONSTRAINT chk_auditResult_score CHECK (SCORE IS NULL OR SCORE IN (0, 2, 4, 6, 8, 10));

-- -----------------------------------------------------------------------------
-- 6. tb_audit_action: repurpose the unused tb_audit_action_his into a CAPA table
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS tb_audit_action_his;

CREATE TABLE tb_audit_action (
  ID INT NOT NULL AUTO_INCREMENT,
  AUDIT_RESULT_ID INT NOT NULL,
  ACTION_TYPE VARCHAR(45) NULL,
  DESCRIPTION TEXT NULL,
  PIC_ID INT NULL,
  DUE_DATE DATE NULL,
  STATUS_ID INT NULL,
  COMPLETED_AT DATETIME NULL,
  CREATED_BY_ID INT NULL,
  CREATED_AT DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
  UPDATED_AT DATETIME NULL,
  PRIMARY KEY (ID),
  KEY fk_auditAction_result_idx (AUDIT_RESULT_ID),
  KEY fk_auditAction_pic_idx (PIC_ID),
  KEY fk_auditAction_status_idx (STATUS_ID),
  KEY fk_auditAction_createdBy_idx (CREATED_BY_ID),
  CONSTRAINT fk_auditAction_result FOREIGN KEY (AUDIT_RESULT_ID) REFERENCES tb_audit_result (ID),
  CONSTRAINT fk_auditAction_pic FOREIGN KEY (PIC_ID) REFERENCES tb_user (ID),
  CONSTRAINT fk_auditAction_status FOREIGN KEY (STATUS_ID) REFERENCES tb_audit_status (ID),
  CONSTRAINT fk_auditAction_createdBy FOREIGN KEY (CREATED_BY_ID) REFERENCES tb_user (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- -----------------------------------------------------------------------------
-- 7. tb_audit_action_evidence: proof-of-fix photos per action
-- -----------------------------------------------------------------------------
CREATE TABLE tb_audit_action_evidence (
  ID INT NOT NULL AUTO_INCREMENT,
  ACTION_ID INT NOT NULL,
  URL TEXT NOT NULL,
  MIME_TYPE VARCHAR(255) NULL,
  CREATED_AT DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (ID),
  KEY fk_auditActionEvidence_action_idx (ACTION_ID),
  CONSTRAINT fk_auditActionEvidence_action FOREIGN KEY (ACTION_ID) REFERENCES tb_audit_action (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================================================
-- Seed: WH LPA checksheet (29 items from LPA WH.csv)
--
-- MAX_SCORE is uniformly 10.0 for every item (not the CSV's original 3.0/4.0
-- targets) per the revised scoring rule: each item is judged 0/2/4/6/8/10, and
-- the round's total is the average of all applicable (non-N/A) item scores.
--
-- ASSUMPTION: tb_dept.Dept = 'WH' identifies the Warehouse department.
-- Verify this matches the real data before running; adjust the literal below
-- if the department is coded differently in this environment.
-- =============================================================================

SET @lpa_type_id := (SELECT ID FROM tb_audit_type WHERE AUDIT_TYPE = 'LPA' LIMIT 1);
SET @wh_dept_id := (SELECT ID FROM tb_dept WHERE Dept = 'WH' LIMIT 1);

INSERT INTO tb_audit_checksheet
  (AUDIT_TYPE_ID, CATEGORY, SEQ_NO, AUDIT_ITEM, MAX_SCORE, ACTIVE, FOR_DEPT)
VALUES
  (@lpa_type_id, '5S/3R', 1, 'Is there a 5S checklist map for the department?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 2, 'Are the items clearly laid out and placed in the correct positions?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 3, 'Are unnecessary items removed from the workspace?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 4, 'Are colored labels used to clearly distinguish between different types of goods?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 5, 'Are the cargo containers thoroughly cleaned?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 6, 'Are materials stored in their designated locations without mixing different items?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'ESD', 7, 'Are personnel following the applicable ESD protection requirements when handling ESD-sensitive materials or products?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 8, 'Are ESD protective devices and grounding systems properly installed, connected, and functioning at the workstation? (Racks, Equipments, Carts, Workstation, etc)', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 9, 'Are ESD-sensitive materials and products properly identified, handled, and stored within designated ESD protected areas?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 10, 'Are non-ESD-safe materials, tools, or equipment kept away from ESD-sensitive areas? Not-approved Material: -Wood, Cardboard -Unauthorized paper -Non-approved tools or equipment -Food and beverages -Materials that generate particles of fibers', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 11, 'Are workstations, equipment, and conductive surfaces properly grounded and maintained as required for ESD protection?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 12, 'Are temperature and humidity within the specified ESD control limits and are the readings properly monitored and recorded?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 13, 'Are the air conditioning and humidification systems properly maintained and functioning to maintain the required environmental conditions?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Process', 14, 'Is the vacuum-sealed packaging checked?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Process', 15, 'Is the FIFO process being followed correctly?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Process', 16, 'Is the material roll stored in the correct layout location?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Process', 17, 'Are there comprehensive procedures in place for label printing and line-side material supply?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Work standards', 18, 'Are work instructions displayed at the work stations?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 19, 'Do the contents of the work instructions, TIS, and SOS align with actual operations?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 20, 'Are the process checksheets checked and signed daily?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 21, 'Do the temperature and humidity meet the specified requirements?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 22, 'Do the workers follow the label printing procedure?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 23, 'Does the solder paste storage unit meet the required standards?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Control Plan', 24, 'Does the Control Plan match the actual production process and controls being performed at the workstation?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Control Plan', 25, 'Are the inspection items, specifications, control methods, and frequencies consistent between the Control Plan and the applicable checklist / work instructions?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Control Plan', 26, 'Are the defined reaction plans followed and documented when a process parameter or product characteristic is out of specification?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Other', 27, 'Are customer complaints reviewed and communicated to the responsible personnel, with appropriate corrective actions and due dates defined?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Other', 28, 'Do the parameters on the solder paste storage unit meet the standards?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Other', 29, 'Does the dry chamber''s temperature meet the standard?', 10.0, 'Y', @wh_dept_id);

COMMIT;
