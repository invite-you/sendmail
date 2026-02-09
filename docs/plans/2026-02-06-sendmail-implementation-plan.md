# SendMail Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the WPF Excel mail sender per `AGENTS.md` and the design doc, with staged validation, strict monthly continuity, and separate template validation vs test-mail sending.

**Architecture:** Lean MVVM (`MainViewModel`) + small services. Keep pure logic in `SendMail.Core` so it can be unit-tested without WPF/Excel.

**Tech Stack:** .NET 8 WPF, CommunityToolkit.Mvvm, MailKit, Excel Interop (COM), WebView2.

---

## Pre-flight

- Review spec: `AGENTS.md`
- Review docs: `docs/EXCEL_FORMAT.md`, `docs/MAIL_TEMPLATE.md`, `docs/ROUND_RULES.md`, `docs/RUNBOOK.md`, `docs/QA_CHECKLIST.md`
- Review design: `docs/plans/2026-02-06-sendmail-ui-logic-design.md`

Environment notes:
- Excel Interop requires Windows + Excel installed and an STA thread.
- WebView2 requires WebView2 runtime on Windows.

---

### Task 1: Solution and WPF Project Skeleton

**Files:**
- Create: `SendMail.sln`
- Create: `src/SendMail/SendMail.csproj` (net8.0-windows, UseWPF)
- Create: `src/SendMail/App.xaml`, `src/SendMail/App.xaml.cs`
- Create: `src/SendMail/MainWindow.xaml`, `src/SendMail/MainWindow.xaml.cs`

**Steps:**
1. Create the solution and WPF project.
2. Add NuGet packages to `src/SendMail/SendMail.csproj`:
   - CommunityToolkit.Mvvm
   - MailKit
   - Microsoft.Web.WebView2
   - Microsoft.Office.Interop.Excel
3. Build.

Run: `dotnet build -c Release`
Expected: success.

Commit:
```bash
git add SendMail.sln src/SendMail

git commit -m "chore: add WPF project skeleton"
```

---

### Task 2: Core Library + Tests (Models and Gating)

**Files:**
- Create: `src/SendMail.Core/SendMail.Core.csproj`
- Create: `src/SendMail.Core/Models/StageState.cs`
- Create: `src/SendMail.Core/Models/StageStatus.cs`
- Create: `src/SendMail.Core/Models/LogEntry.cs`
- Create: `src/SendMail.Core/Models/LogLevel.cs`
- Create: `src/SendMail.Core/Models/ExcelRow.cs`
- Create: `src/SendMail.Core/Models/EmailGroup.cs`
- Create: `tests/SendMail.Core.Tests/SendMail.Core.Tests.csproj`

**Steps:**
1. Write tests asserting default stage states are Pending and gating properties behave.
2. Implement minimal models.
3. Run tests.

Run: `dotnet test`
Expected: PASS.

Commit:
```bash
git add src/SendMail.Core tests/SendMail.Core.Tests

git commit -m "feat: add core models and stage state"
```

---

### Task 3: MainViewModel and UI Wiring (Staged Buttons)

**Files:**
- Create: `src/SendMail/ViewModels/MainViewModel.cs`
- Modify: `src/SendMail/MainWindow.xaml`
- Modify: `src/SendMail/MainWindow.xaml.cs`

**Spec requirements covered:**
- Stage gating:
  - Template validation enabled after SMTP success
  - Test mail enabled after template validation success
  - Send enabled after all success
- Sending locks input (later we will disable the input fields, not just buttons).

**Steps:**
1. Implement `MainViewModel` with `AsyncRelayCommand` for each stage action.
2. Ensure `CanExecute` reflects `AGENTS.md` gating.
3. Bind minimal UI to commands and show logs grid.

Build: `dotnet build -c Release`

Commit:
```bash
git add src/SendMail/ViewModels/MainViewModel.cs src/SendMail/MainWindow.xaml src/SendMail/MainWindow.xaml.cs

git commit -m "feat: add main view model and staged commands"
```

---

### Task 4: Config Loading (Session-Only Editing)

**Goal:** Load values from `config/appsettings.json` (user-edited) and display in UI. UI edits do not persist unless we explicitly add a save action.

**Files:**
- Create: `src/SendMail.Core/Config/AppConfig.cs`
- Create: `src/SendMail.Core/Config/ConfigLoader.cs`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`
- Modify: `src/SendMail/MainWindow.xaml`

**Steps:**
1. Create config POCOs matching `config/appsettings.sample.json`.
2. Implement JSON load with good error messages into log.
3. Bind config values to UI fields.

Tests:
- Unit-test JSON load/parsing in `tests/SendMail.Core.Tests`.

Commit:
```bash
git add src/SendMail.Core/Config tests/SendMail.Core.Tests src/SendMail/ViewModels/MainViewModel.cs src/SendMail/MainWindow.xaml

git commit -m "feat: load config for smtp/template/excel"
```

---

### Task 5: Excel Scan and Validation

**Files:**
- Create: `src/SendMail.Core/Validation/ExcelBatchScanner.cs` (filename scan)
- Create: `src/SendMail/Services/ExcelInteropReader.cs` (COM)
- Create: `src/SendMail/Services/ExcelService.cs` (orchestrates scan + interop + validation)
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

**Rules:**
- Regex: `^(?<date>\d{8}).*\.xlsx$`
- One file per YYYYMM -> EX001
- Continuous YYYYMM -> EX002
- Open all files via Interop; failure -> EX003
- Exactly one sheet -> EX010 (include filename)
- Email column required; trim; RFC 5322; blanks skipped; invalid fails and logs invalid text
- Duplicate emails allowed; merge rows

**Key engineering constraint:**
- Interop must run on STA. Do not run Interop work on `Task.Run` threadpool (MTA). Use a dedicated STA thread or marshal to UI thread.

Tests:
- Unit-test filename/month continuity rules in `SendMail.Core.Tests`.
- Unit-test email parsing/validation as pure functions.

Commit:
```bash
git add src/SendMail.Core/Validation src/SendMail/Services/ExcelInteropReader.cs src/SendMail/Services/ExcelService.cs src/SendMail/ViewModels/MainViewModel.cs tests/SendMail.Core.Tests

git commit -m "feat: add excel scan and validation"
```

---

### Task 6: Round Calculation

**Files:**
- Create: `src/SendMail.Core/Round/RoundCalculator.cs`
- Tests: `tests/SendMail.Core.Tests/RoundCalculatorTests.cs`

Implement `docs/ROUND_RULES.md` exactly (key: email; continuous months increment; break resets).

---

### Task 7: Template Validation (Tokens/Attachment Only)

**Rules:**
- Subject must contain `{round}`
- Tokens in body must exist and match columns (case-insensitive)
- Attachment <= 10MB

**Files:**
- Create: `src/SendMail.Core/Template/TokenScanner.cs`
- Create: `src/SendMail.Core/Template/TemplateValidator.cs`
- Tests: `tests/SendMail.Core.Tests/TemplateValidatorTests.cs`

Note:
- Spec says “Jinja2”. Choose a .NET library that supports the Jinja2 syntax we need (loops/ifs, dict access). If the library isn’t truly Jinja2-compatible, that is a spec violation.

---

### Task 8: SMTP Connect/Auth Verification

**Files:**
- Create: `src/SendMail/Services/SmtpService.cs`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

Only connect + authenticate.

---

### Task 9: Test Mail Sending (Separate Button)

**Files:**
- Modify: `src/SendMail/Services/SmtpService.cs` (send)
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

Rules:
- Uses first target data
- TO replaced with test recipient
- Same template/attachments/from/reply
- Does not consume batch

---

### Task 10: SendService Sequential Send + Output Files

**Files:**
- Create: `src/SendMail/Services/SendService.cs`
- Create: `src/SendMail/Services/OutputService.cs`

Rules:
- Sequential only
- No retries
- “Success” means SMTP accepted (no exception)
- Write `results-<Batch>.csv` and `failures-<Batch>.csv` per `AGENTS.md`

---

### Task 11: WebView2 Preview (Manual Refresh)

**Files:**
- Modify: `src/SendMail/MainWindow.xaml`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

Rules:
- Only refresh on button click
- Uses selected email from grid

---

### Task 12: Final End-to-End Verification (Windows)

Manual checks:
- Excel validation errors: EX001/EX002/EX003/EX010
- Template validation gating (SMTP success required)
- Test mail button gating (Template success required)
- Send button gating (all success)
- Inputs disabled during send (Stop/Pause only)
- Output CSV and logs created
