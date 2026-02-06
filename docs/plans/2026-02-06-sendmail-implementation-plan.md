# SendMail Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the WPF app structure and validation flow defined in the UI/logic design, including staged validation, logging, and template/test-mail separation.

**Architecture:** Lean MVVM with a single `MainViewModel` orchestrating four services (`ExcelService`, `SmtpService`, `TemplateService`, `SendService`). Stage states drive UI gating and enablement.

**Tech Stack:** .NET 8 WPF, CommunityToolkit.Mvvm, MailKit, Excel Interop (COM), WebView2.

---

## Pre-flight

- Confirm design doc: `docs/plans/2026-02-06-sendmail-ui-logic-design.md`.
- Confirm policy docs updated: `AGENTS.md`, `docs/EXCEL_FORMAT.md`, `docs/MAIL_TEMPLATE.md`, `docs/RUNBOOK.md`, `docs/QA_CHECKLIST.md`.

---

### Task 1: Create solution skeleton and WPF project

**Files:**
- Create: `SendMail.sln`
- Create: `src/SendMail/SendMail.csproj`
- Create: `src/SendMail/App.xaml`
- Create: `src/SendMail/App.xaml.cs`
- Create: `src/SendMail/MainWindow.xaml`
- Create: `src/SendMail/MainWindow.xaml.cs`

**Step 1: Write the failing test**

Not applicable (project scaffolding).

**Step 2: Create minimal project**

- Create solution and WPF project targeting .NET 8.
- Add references: CommunityToolkit.Mvvm, MailKit, Microsoft.Web.WebView2, Microsoft.Office.Interop.Excel.

**Step 3: Build**

Run: `dotnet build -c Release`
Expected: Build succeeds.

**Step 4: Commit**

```bash
git add SendMail.sln src/SendMail

git commit -m "chore: add WPF project skeleton"
```

---

### Task 2: Define core models and stage state

**Files:**
- Create: `src/SendMail/Models/StageState.cs`
- Create: `src/SendMail/Models/LogEntry.cs`
- Create: `src/SendMail/Models/ExcelRow.cs`
- Create: `src/SendMail/Models/EmailGroup.cs`

**Step 1: Write failing test**

Create a small test harness project (`src/SendMail.Tests`) for model behavior.

```csharp
// pseudo
Assert.Equal(StageStatus.Pending, new StageState().Excel);
```

**Step 2: Run test to fail**

Run: `dotnet test`
Expected: FAIL (types not defined).

**Step 3: Implement minimal models**

- `StageState`: Excel/SMTP/Template/TestMail status enum.
- `LogEntry`: Time/Level/Message.
- `ExcelRow`: Dictionary-like row data + Email.
- `EmailGroup`: Email + list of rows.

**Step 4: Run tests**

Run: `dotnet test`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/SendMail/Models src/SendMail.Tests

git commit -m "feat: add core models and stage state"
```

---

### Task 3: MainViewModel with staged commands

**Files:**
- Create: `src/SendMail/ViewModels/MainViewModel.cs`
- Modify: `src/SendMail/App.xaml`
- Modify: `src/SendMail/MainWindow.xaml`

**Step 1: Write failing test**

Test initial state: buttons disabled when stages are Pending.

**Step 2: Run tests to fail**

Run: `dotnet test`
Expected: FAIL (MainViewModel missing).

**Step 3: Implement minimal ViewModel**

- Observable properties for stage status, log list, selected email.
- Commands: LoadExcel, ValidateExcel, TestSmtp, ValidateTemplate, SendTestMail, Preview, Send, Pause, Stop.
- `CanExecute` gating:
  - ValidateTemplate requires SMTP success.
  - SendTestMail requires Template success.
  - Send requires all stage success.

**Step 4: Run tests**

Run: `dotnet test`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/SendMail/ViewModels src/SendMail/MainWindow.xaml src/SendMail/App.xaml

git commit -m "feat: add main view model and staged commands"
```

---

### Task 4: ExcelService file scan + validation

**Files:**
- Create: `src/SendMail/Services/ExcelService.cs`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`
- Modify: `src/SendMail/Models/ExcelRow.cs`

**Step 1: Write failing test**

Create tests for:
- EX001 month duplication
- EX002 month non-continuity
- Email format error logs text

**Step 2: Run tests to fail**

Run: `dotnet test`
Expected: FAIL.

**Step 3: Implement ExcelService**

- Scan folder for `.xlsx`.
- Parse filenames with `^(?<date>\d{8}).*\.xlsx$`.
- Enforce one file per YYYYMM.
- Enforce continuous YYYYMM.
- Open all files with Excel Interop.
- Single sheet required.
- Extract UsedRange into rows; email column required.
- RFC 5322 validation; invalid text logs to `LogEntry`.

**Step 4: Run tests**

Run: `dotnet test`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/SendMail/Services/ExcelService.cs src/SendMail/ViewModels/MainViewModel.cs src/SendMail/Models/ExcelRow.cs src/SendMail.Tests

git commit -m "feat: add excel scanning and validation"
```

---

### Task 5: SMTP connect/auth verification

**Files:**
- Create: `src/SendMail/Services/SmtpService.cs`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

**Step 1: Write failing test**

Test that `TestSmtp` command fails without config and succeeds with valid mock config.

**Step 2: Run tests to fail**

Run: `dotnet test`
Expected: FAIL.

**Step 3: Implement SmtpService**

- Connect + authenticate only.
- No sending in this stage.

**Step 4: Run tests**

Run: `dotnet test`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/SendMail/Services/SmtpService.cs src/SendMail/ViewModels/MainViewModel.cs src/SendMail.Tests

git commit -m "feat: add smtp connect/auth verification"
```

---

### Task 6: Template validation and test mail sending

**Files:**
- Create: `src/SendMail/Services/TemplateService.cs`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`
- Modify: `src/SendMail/Services/SmtpService.cs`

**Step 1: Write failing test**

- Validate `{round}` in subject.
- Token presence check.
- Attachment size check.

**Step 2: Run tests to fail**

Run: `dotnet test`
Expected: FAIL.

**Step 3: Implement TemplateService**

- Token parsing and matching (case-insensitive).
- Attachment size validation.

**Step 4: Implement Test Mail**

- Build full message using real first row.
- Replace TO with test recipient.
- Use template + attachments.

**Step 5: Run tests**

Run: `dotnet test`
Expected: PASS.

**Step 6: Commit**

```bash
git add src/SendMail/Services/TemplateService.cs src/SendMail/Services/SmtpService.cs src/SendMail/ViewModels/MainViewModel.cs src/SendMail.Tests

git commit -m "feat: add template validation and test mail sending"
```

---

### Task 7: Preview rendering (WebView2)

**Files:**
- Modify: `src/SendMail/MainWindow.xaml`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

**Step 1: Write failing test**

Not applicable (UI binding).

**Step 2: Implement Preview**

- WebView2 bound to HTML string.
- `Preview` command updates HTML string.

**Step 3: Manual verification**

Run app and verify manual preview refresh uses selected email.

**Step 4: Commit**

```bash
git add src/SendMail/MainWindow.xaml src/SendMail/ViewModels/MainViewModel.cs

git commit -m "feat: add preview rendering"
```

---

### Task 8: SendService sequential sending + outputs

**Files:**
- Create: `src/SendMail/Services/SendService.cs`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`
- Create: `src/SendMail/Services/OutputService.cs`

**Step 1: Write failing test**

- Ensure sequential send order.
- Ensure results/failures CSV format.

**Step 2: Run tests to fail**

Run: `dotnet test`
Expected: FAIL.

**Step 3: Implement SendService**

- Sequential sending only.
- No retries.
- Status written to results/failures CSV.
- Logs for each send.

**Step 4: Run tests**

Run: `dotnet test`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/SendMail/Services/SendService.cs src/SendMail/Services/OutputService.cs src/SendMail/ViewModels/MainViewModel.cs src/SendMail.Tests

git commit -m "feat: add sequential send and output csv"
```

---

### Task 9: UI enablement + final wiring

**Files:**
- Modify: `src/SendMail/MainWindow.xaml`
- Modify: `src/SendMail/ViewModels/MainViewModel.cs`

**Step 1: Manual verification**

- Validate button enablement ordering.
- Ensure inputs disabled during send.

**Step 2: Commit**

```bash
git add src/SendMail/MainWindow.xaml src/SendMail/ViewModels/MainViewModel.cs

git commit -m "feat: wire ui enablement rules"
```

---

## Execution Handoff

Plan complete and saved to `docs/plans/2026-02-06-sendmail-implementation-plan.md`.

Two execution options:

1. **Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration.
2. **Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints.

Which approach?
