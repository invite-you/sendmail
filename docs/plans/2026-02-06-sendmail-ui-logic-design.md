# WPF Excel Mail Sender - UI/Logic Design

Date: 2026-02-06

## Scope

Design the single-window WPF UI and validation flow for the Excel-driven mail sender. This document reflects the latest policy decisions:
- Validation is staged (no “전체 검증” button).
- Monthly continuity is enforced (YYYYMM only; day is ignored).
- One file per month (EX001 if duplicated).
- Duplicate emails are allowed and merged for sending.
- SMTP verification is connect+auth only; actual test mail is sent in the separate test-mail step.

## Goals

- Keep architecture minimal (lean MVVM).
- Prevent input failure during sending (UI lock required).
- Ensure only fully validated batches can be sent.
- Make errors immediately actionable (email format error text appears in logs).

## Architecture

- **MainView + MainViewModel** as the app shell.
- Minimal services:
  - `ExcelService`: file scan/open/validate, extract rows and emails.
  - `SmtpService`: connect+auth only.
  - `TemplateService`: token/attachment validation and render prep.
  - `SendService`: sequential send + CSV output.

`MainViewModel` owns:
- Stage states: Excel / SMTP / Template / TestMail (Pending/Success/Fail).
- Selected email for preview.
- Log collection.
- Commands: LoadExcel, ValidateExcel, TestSmtp, ValidateTemplate, SendTestMail, Preview, Send, Pause, Stop.

## UI Layout (Single Window)

Top to bottom:
1. **Stage status bar**: Excel / SMTP / Template / TestMail / Send (icon + status).
2. **Input forms**: four group boxes (Excel / SMTP / Template / Send).
3. **Preview area**: WebView2 (manual “Preview Refresh”).
4. **Log grid**: Time / Level / Message (errors and validation details).

### Excel Group
- Folder path + browse.
- Password input (used only when file is password-protected).
- Buttons: `Load Excel`, `Validate Excel`.
- Email grid (select one for preview).
- Summary bar above grid: `선택 월: YYYYMM–YYYYMM, 파일 n개`.

### SMTP Group
- Host/Port/User/Pass/SSL settings (loaded from config; in-form edits are session-only).
- Button: `SMTP 연결 확인`.

### Template Group
- HTML file path + browse.
- Subject input (must include `{round}`).
- Attachment path + browse.
- Button: `템플릿 검증` (tokens/attachment only).
- Button: `테스트 메일 발송` (enabled after template validation).
- Button: `프리뷰 갱신` (manual only).

### Send Group
- `발송`, `중지`, `일시정지`.
- `발송` enabled only when all stages succeed.

## Stage Gating Rules

- **Validate Excel** must succeed before `SMTP 연결 확인` is relevant.
- **SMTP 연결 확인** must succeed before `템플릿 검증` is enabled.
- **템플릿 검증** must succeed before `테스트 메일 발송` is enabled.
- **테스트 메일 발송** must succeed before `발송` is enabled.

During sending, all input controls are disabled; only Stop/Pause are enabled.

## Validation Rules

### Excel
- File name pattern: `^(?<date>\d{8}).*\.xlsx$`.
- One file per YYYYMM; duplicates -> **EX001**.
- YYYYMM must be continuous; gaps -> **EX002**. Day (DD) ignored.
- Open all files with Excel Interop; failure -> **EX003**.
- Must have exactly one sheet; otherwise **EX010** (include filename).

### Email
- Column required.
- Trim spaces then validate RFC 5322 format.
- Blank -> skip.
- Invalid format -> fail; **invalid text is logged**.
- Duplicate emails are allowed; rows are merged for sending and preview.

### Template
- `{round}` must parse and be in subject.
- `{column}` tokens must exist (case-insensitive match to Excel columns).
- Attachment <= 10MB.

### Test Mail
- Uses first target data; TO replaced with test recipient.
- Same template/attachments/from/reply.
- Does not consume batch.

## Preview

- WebView2 renders HTML only when `프리뷰 갱신` is clicked.
- Uses the selected email from the grid.

## Logging

- All validation results are logged (Time/Level/Message).
- Email format errors log the invalid text (and filename/row if available).

## Outputs

- `logs/app-YYYYMMDD.log`
- `output/results-<Batch>.csv`
- `output/failures-<Batch>.csv`

## Open Questions

None.
