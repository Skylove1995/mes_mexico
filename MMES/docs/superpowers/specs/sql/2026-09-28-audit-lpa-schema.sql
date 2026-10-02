-- =============================================================================
-- Audit system schema extension (LPA + extensible to System Audit / Daily Audit)
-- See docs/superpowers/specs/2026-09-28-audit-lpa-database-design.md
--
-- Extends the existing, currently-unused generic audit schema:
--   tb_audit_type, tb_audit_checksheet, tb_audit_result, tb_audit_status,
--   tb_audit_evidence_his
-- and replaces the unused tb_audit_action_his with a proper CAPA table.
--
-- No app code references these tables yet, but the environment this was tested
-- against already had leftover test/scaffold rows in tb_audit_result with SCORE
-- values outside the new 0/2/4/6/8/10 rubric, which fails the CHECK constraint
-- added in step 5 below. Step 0 clears that stale data. Confirm your own
-- tb_audit_result / tb_audit_evidence_his / tb_audit_action_his rows are
-- disposable test data (not real audit history) before running it.
--
-- Note: in MySQL, ALTER/CREATE/DROP/TRUNCATE all cause an implicit commit, so
-- START TRANSACTION / COMMIT below only brackets the plain INSERT statements --
-- this script is not atomic as a whole. Back up the database first.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. Clear stale test data from tb_audit_result and its child tables so the new
--    SCORE CHECK constraint (step 5) doesn't fail on rows outside 0/2/4/6/8/10.
--    SKIP this step if tb_audit_result already holds real audit history you
--    need to keep -- migrate those SCORE values to the new rubric instead.
-- -----------------------------------------------------------------------------
USE mex_mes;

SET SQL_SAFE_UPDATES = 0;
SET FOREIGN_KEY_CHECKS = 0;

SET @sql := (SELECT IF(COUNT(*) > 0, 'DELETE FROM tb_audit_evidence_his', 'SELECT 1') FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_evidence_his');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (SELECT IF(COUNT(*) > 0, 'DELETE FROM tb_audit_action_his', 'SELECT 1') FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_action_his');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (SELECT IF(COUNT(*) > 0, 'DELETE FROM tb_audit_result', 'SELECT 1') FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_result');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
SET SQL_SAFE_UPDATES = 1;

START TRANSACTION;

-- Guard every ADD COLUMN / ADD KEY / ADD CONSTRAINT below with an information_schema
-- check + PREPARE/EXECUTE, instead of "IF NOT EXISTS" (only in MySQL 8.0.29+ -- not
-- available on every server, as seen when this script was first applied). This makes
-- the whole script safe to re-run from the top after a partial failure, on any
-- MySQL/MariaDB version.
SET @schema := DATABASE();

-- -----------------------------------------------------------------------------
-- 1. tb_audit_checksheet: item ordering, es-MX translation, time limit, score type
-- -----------------------------------------------------------------------------
SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_checksheet ADD COLUMN SEQ_NO INT NULL AFTER CATEGORY',
  'SELECT 1')
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_checksheet' AND COLUMN_NAME = 'SEQ_NO');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_checksheet ADD COLUMN AUDIT_ITEM_TRANSLATE TEXT NULL AFTER AUDIT_ITEM',
  'SELECT 1')
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_checksheet' AND COLUMN_NAME = 'AUDIT_ITEM_TRANSLATE');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_checksheet ADD COLUMN TIME_LIMIT VARCHAR(100) NULL AFTER CRITICAL',
  'SELECT 1')
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_checksheet' AND COLUMN_NAME = 'TIME_LIMIT');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- MODIFY is naturally idempotent (re-applying the same type never errors).
ALTER TABLE tb_audit_checksheet MODIFY COLUMN MAX_SCORE DECIMAL(6,2) NULL;

-- -----------------------------------------------------------------------------
-- 2. tb_audit_status: shared status vocabulary, make it unique so seeds are safe
-- -----------------------------------------------------------------------------
SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_status ADD UNIQUE KEY STATUS_UNIQUE (STATUS)',
  'SELECT 1')
  FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_status' AND INDEX_NAME = 'STATUS_UNIQUE');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ON DUPLICATE KEY UPDATE already makes this safe to re-run.
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
CREATE TABLE IF NOT EXISTS tb_audit_session (
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
-- Requires MySQL 8.0.16+ for CHECK to be enforced. Each clause is applied as its
-- own guarded ALTER (rather than one multi-clause statement) so a partial re-run
-- is always safe regardless of where a previous attempt stopped.
SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_result ADD COLUMN SESSION_ID INT NULL AFTER AUDIT_ITEM_ID',
  'SELECT 1')
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_result' AND COLUMN_NAME = 'SESSION_ID');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

ALTER TABLE tb_audit_result
  MODIFY COLUMN SCORE DECIMAL(6,2) NULL,
  MODIFY COLUMN JUDGE VARCHAR(5) NULL;

SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_result ADD CONSTRAINT fk_auditResult_session FOREIGN KEY (SESSION_ID) REFERENCES tb_audit_session (ID)',
  'SELECT 1')
  FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_result' AND CONSTRAINT_NAME = 'fk_auditResult_session');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_result ADD KEY fk_auditResult_session_idx (SESSION_ID)',
  'SELECT 1')
  FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_result' AND INDEX_NAME = 'fk_auditResult_session_idx');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_result ADD CONSTRAINT chk_auditResult_score CHECK (SCORE IS NULL OR SCORE IN (0, 2, 4, 6, 8, 10))',
  'SELECT 1')
  FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_result' AND CONSTRAINT_NAME = 'chk_auditResult_score');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 6. tb_audit_action: repurpose the unused tb_audit_action_his into a CAPA table
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS tb_audit_action_his;

CREATE TABLE IF NOT EXISTS tb_audit_action (
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
CREATE TABLE IF NOT EXISTS tb_audit_action_evidence (
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
-- targets) per the revised scoring rule: each item is judged 0/2/4/6/8/10 (no
-- N/A -- every item must be answered), and the round's total is the average
-- of all 29 item scores.
--
-- ASSUMPTION: tb_dept.Dept = 'WH' identifies the Warehouse department.
-- Verify this matches the real data before running; adjust the literal below
-- if the department is coded differently in this environment.
-- =============================================================================

SET @lpa_type_id := (SELECT ID FROM tb_audit_type WHERE AUDIT_TYPE = 'LPA' LIMIT 1);
SET @wh_dept_id := (SELECT ID FROM tb_dept WHERE Dept = 'WH' LIMIT 1);

-- Unique key makes the seed below safe to re-run via ON DUPLICATE KEY UPDATE
-- instead of inserting duplicate rows on a second pass.
SET @sql := (SELECT IF(COUNT(*) = 0,
  'ALTER TABLE tb_audit_checksheet ADD UNIQUE KEY checksheet_type_dept_seq_unique (AUDIT_TYPE_ID, FOR_DEPT, SEQ_NO)',
  'SELECT 1')
  FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_audit_checksheet' AND INDEX_NAME = 'checksheet_type_dept_seq_unique');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ON DUPLICATE KEY UPDATE only refreshes CATEGORY/AUDIT_ITEM/MAX_SCORE/ACTIVE --
-- it deliberately leaves AUDIT_ITEM_TRANSLATE and TIME_LIMIT untouched so a
-- re-run never overwrites translations the business team has already filled in.
INSERT INTO tb_audit_checksheet
  (AUDIT_TYPE_ID, CATEGORY, SEQ_NO, AUDIT_ITEM, AUDIT_ITEM_TRANSLATE, MAX_SCORE, ACTIVE, FOR_DEPT)
VALUES
  (@lpa_type_id, '5S/3R', 1, 'Is there a 5S checklist map for the department?', '¿Existe un mapa de la lista de verificación 5S para el departamento?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 2, 'Are the items clearly laid out and placed in the correct positions?', '¿Los artículos están claramente distribuidos y colocados en las posiciones correctas?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 3, 'Are unnecessary items removed from the workspace?', '¿Se han eliminado los artículos innecesarios del área de trabajo?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 4, 'Are colored labels used to clearly distinguish between different types of goods?', '¿Se utilizan etiquetas de colores para distinguir claramente entre los diferentes tipos de mercancías?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 5, 'Are the cargo containers thoroughly cleaned?', '¿Los contenedores de carga están completamente limpios?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, '5S/3R', 6, 'Are materials stored in their designated locations without mixing different items?', '¿Los materiales se almacenan en las ubicaciones designadas sin mezclar diferentes artículos?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'ESD', 7, 'Are personnel following the applicable ESD protection requirements when handling ESD-sensitive materials or products?', '¿El personal cumple con los requisitos de protección ESD aplicables al manipular materiales o productos sensibles a ESD?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 8, 'Are ESD protective devices and grounding systems properly installed, connected, and functioning at the workstation? (Racks, Equipments, Carts, Workstation, etc)', '¿Los dispositivos de protección ESD y sistemas de puesta a tierra están correctamente instalados, conectados y funcionando en la estación de trabajo? (Racks, Equipos, Carritos, Estación, etc.)', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 9, 'Are ESD-sensitive materials and products properly identified, handled, and stored within designated ESD protected areas?', '¿Los materiales y productos sensibles a ESD están identificados, manipulados y almacenados adecuadamente en áreas protegidas de ESD?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 10, 'Are non-ESD-safe materials, tools, or equipment kept away from ESD-sensitive areas? Not-approved Material: -Wood, Cardboard -Unauthorized paper -Non-approved tools or equipment -Food and beverages -Materials that generate particles of fibers', '¿Se mantienen alejados de las áreas sensibles a ESD los materiales, herramientas o equipos no seguros para ESD? (Madera, cartón, papel no autorizado, comida/bebidas, etc.)', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 11, 'Are workstations, equipment, and conductive surfaces properly grounded and maintained as required for ESD protection?', '¿Las estaciones de trabajo, equipos y superficies conductoras están correctamente conectadas a tierra y mantenidas según lo requerido para la protección ESD?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 12, 'Are temperature and humidity within the specified ESD control limits and are the readings properly monitored and recorded?', '¿La temperatura y la humedad están dentro de los límites de control ESD especificados y las lecturas se monitorean y registran adecuadamente?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'ESD', 13, 'Are the air conditioning and humidification systems properly maintained and functioning to maintain the required environmental conditions?', '¿Los sistemas de aire acondicionado y humidificación reciben el mantenimiento adecuado y funcionan para mantener las condiciones ambientales requeridas?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Process', 14, 'Is the vacuum-sealed packaging checked?', '¿Se verifica el empaque sellado al vacío?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Process', 15, 'Is the FIFO process being followed correctly?', '¿Se sigue correctamente el proceso PEPS (FIFO)?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Process', 16, 'Is the material roll stored in the correct layout location?', '¿El rollo de material se almacena en la ubicación de distribución correcta?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Process', 17, 'Are there comprehensive procedures in place for label printing and line-side material supply?', '¿Existen procedimientos integrales para la impresión de etiquetas y el suministro de material al borde de la línea?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Work standards', 18, 'Are work instructions displayed at the work stations?', '¿Las instrucciones de trabajo están visibles en las estaciones de trabajo?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 19, 'Do the contents of the work instructions, TIS, and SOS align with actual operations?', '¿El contenido de las instrucciones de trabajo, TIS y SOS coincide con las operaciones reales?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 20, 'Are the process checksheets checked and signed daily?', '¿Las listas de verificación de proceso se revisan y firman diariamente?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 21, 'Do the temperature and humidity meet the specified requirements?', '¿La temperatura y la humedad cumplen con los requisitos especificados?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 22, 'Do the workers follow the label printing procedure?', '¿Los operadores siguen el procedimiento de impresión de etiquetas?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Work standards', 23, 'Does the solder paste storage unit meet the required standards?', '¿La unidad de almacenamiento de soldadura en pasta cumple con los estándares requeridos?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Control Plan', 24, 'Does the Control Plan match the actual production process and controls being performed at the workstation?', '¿El Plan de Control coincide con el proceso de producción real y los controles ejecutados en la estación de trabajo?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Control Plan', 25, 'Are the inspection items, specifications, control methods, and frequencies consistent between the Control Plan and the applicable checklist / work instructions?', '¿Los elementos de inspección, especificaciones, métodos de control y frecuencias son consistentes entre el Plan de Control y la lista de verificación/instrucciones de trabajo?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Control Plan', 26, 'Are the defined reaction plans followed and documented when a process parameter or product characteristic is out of specification?', '¿Se siguen y documentan los planes de reacción definidos cuando un parámetro de proceso o característica de producto está fuera de especificación?', 10.0, 'Y', @wh_dept_id),

  (@lpa_type_id, 'Other', 27, 'Are customer complaints reviewed and communicated to the responsible personnel, with appropriate corrective actions and due dates defined?', '¿Se revisan y comunican las quejas de los clientes al personal responsable, definiendo las acciones correctivas y fechas límite correspondientes?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Other', 28, 'Do the parameters on the solder paste storage unit meet the standards?', '¿Los parámetros de la unidad de almacenamiento de soldadura en pasta cumplen con los estándares?', 10.0, 'Y', @wh_dept_id),
  (@lpa_type_id, 'Other', 29, 'Does the dry chamber''s temperature meet the standard?', '¿La temperatura del gabinete seco cumple con el estándar?', 10.0, 'Y', @wh_dept_id)
ON DUPLICATE KEY UPDATE
  CATEGORY = VALUES(CATEGORY),
  AUDIT_ITEM = VALUES(AUDIT_ITEM),
  AUDIT_ITEM_TRANSLATE = VALUES(AUDIT_ITEM_TRANSLATE),
  MAX_SCORE = VALUES(MAX_SCORE),
  ACTIVE = VALUES(ACTIVE);

COMMIT;
