# Audit System Database Design (LPA + extensible to System Audit / Daily Audit)

## Source

`D:\22. Excel\LPA WH.csv` — Monthly LPA checksheet for the WH (Warehouse) department: 29
checklist items grouped into 6 process categories (5S/3R, ESD, Process, Work standards,
Control Plan, Other), each scored O/X/NC/N/A against a per-item score target, with a
3-role sign-off (Auditor / Check by / Approval by), photo evidence of findings, and a
corrective-action report with PIC (person in charge) and due date.

## Decision: extend the existing dormant audit schema

The DB already has a generic, currently-unused audit schema — `tb_audit_type`,
`tb_audit_checksheet`, `tb_audit_result`, `tb_audit_status`, `tb_audit_evidence_his`,
`tb_audit_action_his` — scaffolded into `MMesDbContext` with no controller/service
referencing it yet. It's shaped for exactly this: a checksheet's items are scoped by
`AUDIT_TYPE_ID` and `FOR_DEPT`, so LPA (and later **System Audit**, **Daily Audit**, or
any other audit type) is just another row in `tb_audit_type` with its own checksheet —
no parallel schema needed. This design extends that schema rather than duplicating it.

Because it's unused, `tb_audit_action_his` is repurposed (redesigned) into
`tb_audit_action`, a proper CAPA table — this is a breaking rename, safe only because
no application code or data depends on the old shape.

## Entity overview

```
tb_audit_type (LPA / System Audit / Daily Audit / ...)
   └─ tb_audit_checksheet (checklist items, per audit type + dept)
        └─ tb_audit_result (one row per item per audit round)
             ├─ tb_audit_evidence_his (finding photos — unchanged)
             └─ tb_audit_action (CAPA entries for this finding)
                    └─ tb_audit_action_evidence (proof-of-fix photos)

tb_audit_session (one row per audit round: dept, month, shift, 3-role sign-off, score)
   └─ tb_audit_result.SESSION_ID → groups all item results into one round
```

`tb_audit_status` is a shared status vocabulary reused by session/result/action
lifecycles (Draft, Submitted, Checked, Approved, Rejected, Open, In Progress, Rectified,
Closed, Overdue) rather than one lookup table per entity.

## Table changes

### `tb_audit_checksheet` (altered)
Added columns:
- `SEQ_NO INT NULL` — preserves the checklist's item order/number (CSV "Item" column,
  1..29); needed to render the checklist in the right order under each category, which
  wasn't captured by any existing column.
- `AUDIT_ITEM_TRANSLATE TEXT NULL` — es-MX translation of `AUDIT_ITEM` (CSV "Item check
  (Translate to Mexico)").
- `TIME_LIMIT VARCHAR(100) NULL` — free-text SLA per item (CSV "Time limit").

Changed:
- `MAX_SCORE` widened `INT` → `DECIMAL(6,2)`. Kept as a per-item weight column so other
  audit types can use variable targets, but for the WH LPA checksheet every item is
  seeded at a uniform `10.0` — see **Scoring rule** below, which replaces the CSV's
  original "0/50/100% of a 3.0-or-4.0 target" model.

### Scoring rule (revised)

Per-item scoring is a fixed discrete rubric, not a percentage of a variable target, and
**N/A is not a valid outcome** — every checklist item must be answered:

- Fail → **0**
- Pass → **10**
- Partial compliance → auditor picks **2, 4, 6, or 8**

A session's `TOTAL_SCORE_TARGET` is the sum of `MAX_SCORE` across all of that audit
round's items and `TOTAL_SCORE` is the sum of their `SCORE`; `SCORE_PERCENT =
TOTAL_SCORE / TOTAL_SCORE_TARGET * 100`. Since every LPA item's target is uniformly
`10`, this is mathematically the same as a simple average of the item scores — if all
29 items pass (score 10), the average is 10/10 = 100%; a single fail (score 0) pulls
the average down accordingly. `tb_audit_result.SCORE` gets a `CHECK` constraint
restricting it to `{0, 2, 4, 6, 8, 10}` or `NULL`, so the database still enforces the
discrete rubric.

**`NULL` means "not answered yet", not N/A.** A result row can sit at `SCORE = NULL`
only while its session is still `Draft` (the auditor hasn't gotten to that item yet).
Nothing in the DB stops a `Draft` session from having empty scores — completeness is an
application-level submit-time check, not a DB constraint, consistent with how grading
thresholds are also app-computed (see below). The submit flow must:
1. Query all checksheet items for the session's audit type/dept against
   `tb_audit_result` (`LEFT JOIN` on `SEQ_NO`/`AUDIT_ITEM_ID`, filtering
   `SCORE IS NULL`) to find any unanswered item.
2. Block the `STATUS` transition to `Submitted` and surface exactly which items (by
   category + `SEQ_NO`) are still blank, so the auditor can jump straight to them
   instead of re-scanning all 29 questions.

This validation/UX logic belongs to the controller/service layer built on top of this
schema — out of scope for this DB design pass, but called out here since it depends on
`SCORE` staying nullable during `Draft` and non-null being enforced only at submit time.

### `tb_audit_session` (new)
The paper form's header — one row per audit round:

| Column | Type | Notes |
|---|---|---|
| ID | INT PK | |
| AUDIT_TYPE_ID | FK → tb_audit_type | LPA / System Audit / Daily Audit / ... |
| DEPT_ID | FK → tb_dept | audited department |
| AUDIT_MONTH | DATE | |
| SHIFT | VARCHAR(45) | |
| AUDITOR_ID | FK → tb_user | "Auditor" |
| CHECKED_BY_ID | FK → tb_user, nullable | "Check by" |
| CHECKED_AT | DATETIME, nullable | |
| APPROVED_BY_ID | FK → tb_user, nullable | "Approval by" |
| APPROVED_AT | DATETIME, nullable | |
| STATUS_ID | FK → tb_audit_status | |
| TOTAL_SCORE | DECIMAL(8,2) | sum of achieved scores |
| TOTAL_SCORE_TARGET | DECIMAL(8,2) | sum of applicable max scores |
| SCORE_PERCENT | DECIMAL(5,2) | |
| GRADE | VARCHAR(10) | Green/Yellow/Red, app-computed |
| NOTE | TEXT, nullable | |
| CREATED_AT / UPDATED_AT | DATETIME | |

Grading thresholds (Green ≥95%, Yellow 90–94%, Red <90%, per the CSV's evaluation guide)
are business logic computed at save time into `SCORE_PERCENT`/`GRADE`, not DB-enforced.

### `tb_audit_result` (altered)
- `SESSION_ID INT NOT NULL` + FK → `tb_audit_session` — every item result now belongs to
  one audit round. (Set `NOT NULL` since the table has no existing data.)
- `SCORE` widened `INT` → `DECIMAL(6,2)`, plus a `CHECK` constraint restricting values to
  `{0, 2, 4, 6, 8, 10}` or `NULL` (`NULL` = not yet answered, valid only during `Draft`
  — see **Scoring rule** above; N/A is not a supported outcome).
- `JUDGE` widened `VARCHAR(2)` → `VARCHAR(5)` — the existing column can't even hold
  `"N/A"` (3 chars); this was a latent bug in the dormant schema.

Everything else (`NOTE`, `ISSUE_OWNER_ID`, `STATUS_ID`, `LAST_COMMENT`, `MODEL_ID`,
`LINE_ID`) is unchanged and still usable for audit types where those apply.

### `tb_audit_action` (new, replaces `tb_audit_action_his`)
CAPA table: one-to-many per finding, so re-checks/follow-ups are possible, not just a
single fix per issue.

| Column | Type | Notes |
|---|---|---|
| ID | INT PK | |
| AUDIT_RESULT_ID | FK → tb_audit_result, not null | the finding this action addresses |
| ACTION_TYPE | VARCHAR(45), nullable | free classification, e.g. Corrective/Preventive |
| DESCRIPTION | TEXT | |
| PIC_ID | FK → tb_user, nullable | person in charge |
| DUE_DATE | DATE, nullable | |
| STATUS_ID | FK → tb_audit_status | |
| COMPLETED_AT | DATETIME, nullable | |
| CREATED_BY_ID | FK → tb_user, nullable | |
| CREATED_AT / UPDATED_AT | DATETIME | |

### `tb_audit_action_evidence` (new)
Proof-of-fix photos per action, mirroring the existing `tb_audit_evidence_his` shape:
`ID, ACTION_ID (FK), URL, MIME_TYPE, CREATED_AT`.

`tb_audit_evidence_his` itself is unchanged — it still holds the auditor's finding
photos, attached directly to `tb_audit_result`.

## Extensibility to System Audit / Daily Audit

Nothing in this design is LPA-specific beyond the seed data. Adding **System Audit** or
**Daily Audit** later means: insert a new `tb_audit_type` row, insert that audit's
checklist items into `tb_audit_checksheet` under the new `AUDIT_TYPE_ID`, and audit
rounds are recorded the same way through `tb_audit_session` → `tb_audit_result` →
`tb_audit_action`. No schema change needed.

## Seed data included in the DDL script

- `tb_audit_type`: `LPA`, `System Audit`, `Daily Audit`.
- `tb_audit_status`: shared vocabulary (Draft, Submitted, Checked, Approved, Rejected,
  Open, In Progress, Rectified, Closed, Overdue), with a new unique constraint so it
  isn't seeded twice by accident.
- `tb_audit_checksheet`: all 29 WH LPA items from `LPA WH.csv`, with category and
  sequence number populated; `MAX_SCORE` seeded uniformly at `10.0` per the revised
  scoring rule (not the CSV's original 3.0/4.0 targets); `AUDIT_ITEM_TRANSLATE` and
  `TIME_LIMIT` left NULL for the business team to fill in.

**Assumption to verify before running:** the checksheet seed resolves `FOR_DEPT` via
`(SELECT ID FROM tb_dept WHERE Dept = 'WH')`. Confirm `'WH'` matches the actual value
stored in `tb_dept.Dept` in the target database before executing — it wasn't queryable
from this session.

The script guards every `ALTER`/`CREATE`/seed step with an `information_schema` check
(rather than MySQL 8.0.29's `IF NOT EXISTS`, which isn't available on every server) so
it's safe to re-run end to end after a partial failure. `tb_audit_checksheet` also gets
a `UNIQUE KEY (AUDIT_TYPE_ID, FOR_DEPT, SEQ_NO)` so the 29-item seed uses
`ON DUPLICATE KEY UPDATE` instead of inserting duplicates on a second run — it only
refreshes `CATEGORY`/`AUDIT_ITEM`/`MAX_SCORE`/`ACTIVE`, never touching
`AUDIT_ITEM_TRANSLATE`/`TIME_LIMIT` so a re-run can't wipe out translations the
business team has already filled in.

**Discovered while applying the script:** `tb_audit_result` in the target database
already had leftover test/scaffold rows with `SCORE` values outside the new
`{0,2,4,6,8,10}` rubric, which the `CHECK` constraint in step 5 rejects (MySQL error
3819). The script now includes a step 0 that truncates `tb_audit_result` and its child
tables (`tb_audit_evidence_his`, `tb_audit_action_his`) first — confirmed disposable
since no app code reads this data. If a target environment ever has real audit history
in these tables, that step must be replaced with a `SCORE` migration instead of a
truncate.

## Out of scope (not done in this pass)

- EF Core model classes / `MMesDbContext` config updates for the new/changed tables —
  natural follow-up once this schema is applied, kept separate since the ask was the DB
  design itself.
- Controllers/services/views to actually run LPA audits.
- Enforcing grading thresholds or score roll-ups in the database (triggers/generated
  columns) — deliberately left as application logic for flexibility.
