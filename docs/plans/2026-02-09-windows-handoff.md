# Windows Handoff (Codex/LLM)

## Repo State (2026-02-09)

- Branch: `plan-implementation`
- Head commit: check with `git log -1 --oneline`
- Task 10 commit: `5f5b582` (`feat: bulk send with stop/pause and csv/log output`)
- Status:
  - Task 10 (Bulk Send + CSV/Log output): implemented
  - Task 11 (WebView2 Preview): NOT implemented (placeholder)
  - Task 12 (Windows E2E verification): to do on Windows

Notes:
- WPF build is expected to fail on Linux (WindowsDesktop SDK not available). Build/run on Windows only.
- Excel Interop requires Microsoft Excel installed on Windows.

## What Was Implemented (Task 10)

- Bulk send (sequential, no retry, no parallel):
  - Core loop: `src/SendMail.Core/Sending/BulkSender.cs`
  - Result record: `src/SendMail.Core/Sending/SendAttempt.cs`
  - Status: `SENT` / `FAILED`
  - ErrorCode:
    - `SM001`: send exception
    - `US001`: stopped by user (cancel)
- Output files:
  - CSV writer: `src/SendMail.Core/Output/BatchOutputWriter.cs`
  - `output/results-<Batch>.csv`
  - `output/failures-<Batch>.csv`
  - Batch format: `YYYYMMDD-HHmmss`
  - Timestamp format: `YYYY-MM-DD HH:mm:ss`
- File log:
  - Appender: `src/SendMail.Core/Output/LogFileAppender.cs`
  - `logs/app-YYYYMMDD.log` (append)
- WPF wiring:
  - Send/Pause/Stop: `src/SendMail/ViewModels/MainViewModel.cs`
  - UI input lock while sending: `src/SendMail/MainWindow.xaml`

## Windows Setup (Copy/Paste)

### 1) Clone and checkout

```powershell
git clone https://github.com/invite-you/sendmail.git
cd sendmail
git checkout plan-implementation
git log -1 --oneline
```

Expected:
- `git log -1` shows a recent `docs:` or `feat:` commit on `plan-implementation`
- `git log --oneline --max-count 50` includes `5f5b582` (Task 10)

### 2) Config files

- Ensure `config/appsettings.json` exists (copy from sample, then edit as needed):

```powershell
copy config\\appsettings.sample.json config\\appsettings.json
```

- Ensure mail body HTML exists (default path is `config/body.html`):
  - Minimal placeholder is enough for test:

```powershell
@'
<html><body>
<h2>{round}회차</h2>
<p>{이메일}</p>
</body></html>
'@ | Out-File -Encoding utf8 config\\body.html
```

### 3) Build + run

```powershell
dotnet --version
dotnet test tests\\SendMail.Core.Tests\\SendMail.Core.Tests.csproj
dotnet build src\\SendMail\\SendMail.csproj -c Release
dotnet run --project src\\SendMail\\SendMail.csproj
```

## Windows E2E Verification (Task 12)

Follow `docs/RUNBOOK.md`:
- Excel load/validate
- SMTP verify (connect+auth)
- Template validate (token/attachment)
- Test mail send
- Bulk send

Verify:
- During bulk send:
  - Excel/SMTP/Template/Config inputs are disabled
  - Only Pause/Stop are usable
- After bulk send:
  - `logs/app-YYYYMMDD.log` exists and lines append
  - `output/results-<Batch>.csv` exists
  - `output/failures-<Batch>.csv` exists

## Next Work (Task 11: WebView2 Preview)

Goal:
- "Preview Refresh" should render HTML using the SAME renderer as send/test-mail.
- It should select one target (e.g. first EmailGroup or a selected email) and show subject + HTML.

Files:
- UI placeholder: `src/SendMail/MainWindow.xaml` (Preview section)
- Hook: `src/SendMail/ViewModels/MainViewModel.cs` (`RefreshPreviewInternalAsync` is currently placeholder)
- Renderer: `src/SendMail.Core/Template/MailTemplateRenderer.cs`

## Skills To Use (Codex Superpowers)

In the next session (Windows machine), follow this order:
1. `using-superpowers`
2. `verification-before-completion`
3. `brainstorming` (before Task 11 UI/preview work)
4. `test-driven-development` (for new core logic)
5. `systematic-debugging` (if WPF runtime/build behaves unexpectedly)

## If Remote Push Is Needed

If this branch has new commits locally, push from Windows:

```powershell
git push origin plan-implementation
```
