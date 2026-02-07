using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MimeKit;
using SendMail.Core.Config;
using SendMail.Core.Models;
using SendMail.Core.Round;
using SendMail.Core.Smtp;
using SendMail.Core.Template;
using SendMail.Core.Validation;
using SendMail.Services;

namespace SendMail.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ExcelInteropReader excelReader = new();
    private readonly SmtpService smtpService = new();

    private IReadOnlyList<string> latestValidRecipients = Array.Empty<string>();
    private IReadOnlyList<EmailGroup> latestEmailGroups = Array.Empty<EmailGroup>();
    private IReadOnlyDictionary<string, int> latestRoundsByEmail = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public MainViewModel()
    {
        Stage = new StageState();
        Stage.PropertyChanged += OnStagePropertyChanged;

        ReloadConfigCommand = new AsyncRelayCommand(ReloadConfigAsync, CanReloadConfig);
        LoadExcelCommand = new AsyncRelayCommand(LoadExcelAsync, CanLoadExcel);
        ValidateExcelCommand = new AsyncRelayCommand(ValidateExcelAsync, CanValidateExcel);
        TestSmtpCommand = new AsyncRelayCommand(TestSmtpAsync, CanTestSmtp);
        ValidateTemplateCommand = new AsyncRelayCommand(ValidateTemplateAsync, CanValidateTemplate);
        SendTestMailCommand = new AsyncRelayCommand(SendTestMailAsync, CanSendTestMail);
        RefreshPreviewCommand = new AsyncRelayCommand(RefreshPreviewAsync, CanRefreshPreview);

        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        PauseCommand = new RelayCommand(Pause, CanControlSending);
        StopCommand = new RelayCommand(Stop, CanControlSending);

        // Config defaults: user edits the file for persistence; UI edits are session-only.
        ConfigPath = Path.Combine("config", "appsettings.json");
        ReloadConfigFromDisk();
    }

    [ObservableProperty]
    private StageState stage;

    [ObservableProperty]
    private bool isSending;

    [ObservableProperty]
    private string configPath = string.Empty;

    [ObservableProperty]
    private string loadedConfigPath = string.Empty;

    // App
    [ObservableProperty] private string outputDir = "output";
    [ObservableProperty] private string logDir = "logs";
    [ObservableProperty] private long maxAttachmentBytes = 10 * 1024 * 1024;
    [ObservableProperty] private string excelRegex = "^(?<date>\\d{8}).*\\.xlsx$";

    // Excel
    [ObservableProperty] private string excelFolderPath = string.Empty;
    [ObservableProperty] private string excelPassword = string.Empty;
    [ObservableProperty] private string excelEmailColumn = "이메일";

    // SMTP
    [ObservableProperty] private string smtpHost = string.Empty;
    [ObservableProperty] private int smtpPort = 587;
    [ObservableProperty] private string smtpSecurity = "StartTls";
    [ObservableProperty] private string smtpSender = string.Empty;
    [ObservableProperty] private string smtpPassword = string.Empty;
    [ObservableProperty] private int smtpTimeoutSeconds = 30;

    // Mail
    [ObservableProperty] private string mailSubject = "[{round}회차] 고위험자 안내";
    [ObservableProperty] private string mailBodyPath = "config/body.html";
    [ObservableProperty] private string mailAttachmentsText = string.Empty;
    [ObservableProperty] private string testRecipient = "test@example.com";

    // Excel load results (scan + latest email extraction)
    public ObservableCollection<string> ExcelTargetFiles { get; } = new();

    [ObservableProperty] private string excelMonthRangeText = string.Empty;
    [ObservableProperty] private string excelLatestFileName = string.Empty;
    [ObservableProperty] private int excelTargetFileCount;
    [ObservableProperty] private string excelRecipientCountText = "-";

    [ObservableProperty]
    private string? selectedEmail;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public IAsyncRelayCommand ReloadConfigCommand { get; }
    public IAsyncRelayCommand LoadExcelCommand { get; }
    public IAsyncRelayCommand ValidateExcelCommand { get; }
    public IAsyncRelayCommand TestSmtpCommand { get; }
    public IAsyncRelayCommand ValidateTemplateCommand { get; }
    public IAsyncRelayCommand SendTestMailCommand { get; }
    public IAsyncRelayCommand RefreshPreviewCommand { get; }

    public IAsyncRelayCommand SendCommand { get; }
    public IRelayCommand PauseCommand { get; }
    public IRelayCommand StopCommand { get; }

    private bool CanReloadConfig() => !IsSending;
    private bool CanLoadExcel() => !IsSending;
    private bool CanValidateExcel() => !IsSending;

    private bool CanTestSmtp() => !IsSending && Stage.Excel == StageStatus.Success;

    private bool CanValidateTemplate() => !IsSending
        && Stage.Excel == StageStatus.Success
        && Stage.Smtp == StageStatus.Success;

    private bool CanSendTestMail() => !IsSending
        && Stage.Excel == StageStatus.Success
        && Stage.Smtp == StageStatus.Success
        && Stage.Template == StageStatus.Success;

    private bool CanRefreshPreview() => !IsSending && Stage.Template == StageStatus.Success;

    private bool CanSend() => !IsSending && Stage.CanSend;

    private bool CanControlSending() => IsSending;

    private Task ReloadConfigAsync()
    {
        ReloadConfigFromDisk();
        return Task.CompletedTask;
    }

    private void ReloadConfigFromDisk()
    {
        // Try appsettings.json first, then fall back to sample to keep UI usable.
        var primary = TryLoadConfig(ConfigPath);
        if (primary.Config is not null)
        {
            ApplyConfig(primary.Config);
            LoadedConfigPath = primary.LoadedPath;
            LogInfo($"Config loaded: {primary.LoadedPath}");
            return;
        }

        var samplePath = Path.Combine("config", "appsettings.sample.json");
        var sample = TryLoadConfig(samplePath);
        if (sample.Config is not null)
        {
            ApplyConfig(sample.Config);
            LoadedConfigPath = sample.LoadedPath;
            LogWarning($"Config load failed, using sample: {primary.Error}");
            LogInfo($"Config loaded: {sample.LoadedPath}");
            return;
        }

        // Final fallback: defaults.
        ApplyConfig(new AppConfig());
        LoadedConfigPath = "(defaults)";
        LogError($"Config load failed. primary={primary.Error} sample={sample.Error}");
    }

    private static (AppConfig? Config, string LoadedPath, string? Error) TryLoadConfig(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (null, string.Empty, "Config path is empty.");
        }

        // We resolve relative paths both from CWD and from the executable directory,
        // because Windows launches often use the bin/ output folder as CWD.
        var candidates = Path.IsPathRooted(path)
            ? new[] { path }
            : new[]
            {
                Path.GetFullPath(path, Environment.CurrentDirectory),
                Path.GetFullPath(path, AppContext.BaseDirectory)
            };

        foreach (var candidate in candidates)
        {
            var (config, error) = ConfigLoader.LoadFromFile(candidate);
            if (config is not null)
            {
                return (config, candidate, null);
            }

            // If it's not found, keep trying the next candidate; otherwise stop early.
            if (!string.IsNullOrEmpty(error) && !error.StartsWith("Config file not found:", StringComparison.Ordinal))
            {
                return (null, candidate, error);
            }
        }

        return (null, candidates[^1], $"Config file not found: {candidates[^1]}");
    }

    private void ApplyConfig(AppConfig config)
    {
        OutputDir = config.App.OutputDir;
        LogDir = config.App.LogDir;
        MaxAttachmentBytes = config.App.MaxAttachmentBytes;
        ExcelRegex = config.App.ExcelRegex;

        ExcelPassword = config.Excel.Password;
        ExcelEmailColumn = config.Excel.EmailColumn;

        SmtpHost = config.Smtp.Host;
        SmtpPort = config.Smtp.Port;
        SmtpSecurity = config.Smtp.Security;
        SmtpSender = config.Smtp.Sender;
        SmtpPassword = config.Smtp.Password;
        SmtpTimeoutSeconds = config.Smtp.Timeout;

        MailSubject = config.Mail.Subject;
        MailBodyPath = config.Mail.BodyPath;
        MailAttachmentsText = string.Join(Environment.NewLine, config.Mail.Attachments);
        TestRecipient = config.Mail.DefaultTestRecipient;
    }

    private Task LoadExcelAsync()
    {
        return LoadExcelInternalAsync();
    }

    private Task ValidateExcelAsync()
    {
        return ValidateExcelInternalAsync();
    }

    private Task TestSmtpAsync()
    {
        return TestSmtpInternalAsync();
    }

    private Task ValidateTemplateAsync()
    {
        return ValidateTemplateInternalAsync();
    }

    private Task SendTestMailAsync()
    {
        return SendTestMailInternalAsync();
    }

    private Task RefreshPreviewAsync()
    {
        LogInfo("RefreshPreview: not implemented yet.");
        return Task.CompletedTask;
    }

    private Task SendAsync()
    {
        LogInfo("Send: not implemented yet.");
        return Task.CompletedTask;
    }

    private void Pause()
    {
        LogInfo("Pause: not implemented yet.");
    }

    private void Stop()
    {
        LogInfo("Stop: not implemented yet.");
    }

    private void LogInfo(string message) => Logs.Add(new LogEntry(DateTime.Now, LogLevel.Info, message));
    private void LogWarning(string message) => Logs.Add(new LogEntry(DateTime.Now, LogLevel.Warning, message));
    private void LogError(string message) => Logs.Add(new LogEntry(DateTime.Now, LogLevel.Error, message));

    partial void OnIsSendingChanged(bool value)
    {
        // Ensure command enablement updates when sending state flips.
        NotifyCommandCanExecuteChanged();
    }

    private void NotifyCommandCanExecuteChanged()
    {
        ReloadConfigCommand.NotifyCanExecuteChanged();
        LoadExcelCommand.NotifyCanExecuteChanged();
        ValidateExcelCommand.NotifyCanExecuteChanged();
        TestSmtpCommand.NotifyCanExecuteChanged();
        ValidateTemplateCommand.NotifyCanExecuteChanged();
        SendTestMailCommand.NotifyCanExecuteChanged();
        RefreshPreviewCommand.NotifyCanExecuteChanged();

        SendCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private void OnStagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyCommandCanExecuteChanged();
    }

    private async Task LoadExcelInternalAsync()
    {
        ResetAfterExcelInputsChanged();

        ExcelTargetFiles.Clear();
        ExcelMonthRangeText = string.Empty;
        ExcelLatestFileName = string.Empty;
        ExcelTargetFileCount = 0;
        ExcelRecipientCountText = "-";

        if (string.IsNullOrWhiteSpace(ExcelFolderPath) || !Directory.Exists(ExcelFolderPath))
        {
            LogError("Excel folder path is empty or does not exist.");
            return;
        }

        var candidates = ExcelBatchScanner.EnumerateCandidateFiles(ExcelFolderPath);
        var (result, error) = ExcelBatchScanner.ValidateMonthlyContinuity(candidates);

        if (error is not null)
        {
            LogError($"[{error.Code}] {error.Message}");
            return;
        }

        var scan = result!;
        ExcelMonthRangeText = $"{scan.MinMonth}-{scan.MaxMonth}";
        ExcelTargetFileCount = scan.FileCount;

        foreach (var file in scan.Files)
        {
            ExcelTargetFiles.Add(file.FileName);
        }

        var latest = scan.Files[^1];
        ExcelLatestFileName = latest.FileName;

        try
        {
            // Only the latest file determines the send-recipient set.
            var emailColumnValues = await StaThreadRunner.RunAsync(() =>
                excelReader.ReadColumnValues(latest.FullPath, ExcelPassword, ExcelEmailColumn));
            var recipients = RecipientCalculator.FromEmailColumnValues(emailColumnValues);
            latestValidRecipients = recipients;
            ExcelRecipientCountText = recipients.Count.ToString();

            LogInfo($"Excel loaded. range={ExcelMonthRangeText} files={ExcelTargetFileCount} latest={ExcelLatestFileName} recipients={ExcelRecipientCountText}");
        }
        catch (Exception ex)
        {
            LogError($"Failed to extract recipients from latest Excel: {ex.Message}");
        }
    }

    private async Task ValidateExcelInternalAsync()
    {
        // Keep UI summary in sync even if user skips "Load Excel".
        await LoadExcelInternalAsync();

        Stage.Excel = StageStatus.Pending;

        if (string.IsNullOrWhiteSpace(ExcelFolderPath) || !Directory.Exists(ExcelFolderPath))
        {
            LogError("Excel folder path is empty or does not exist.");
            Stage.Excel = StageStatus.Fail;
            return;
        }

        var candidates = ExcelBatchScanner.EnumerateCandidateFiles(ExcelFolderPath);
        var (result, error) = ExcelBatchScanner.ValidateMonthlyContinuity(candidates);

        if (error is not null)
        {
            LogError($"[{error.Code}] {error.Message}");
            Stage.Excel = StageStatus.Fail;
            return;
        }

        var scan = result!;
        var latestFile = scan.Files[^1];
        IReadOnlyList<ExcelInteropReader.CellString> latestCells = Array.Empty<ExcelInteropReader.CellString>();
        var monthEmailSets = new List<HashSet<string>>();

        var invalidTotal = 0;
        const int invalidLogLimit = 50;

        foreach (var file in scan.Files)
        {
            IReadOnlyList<ExcelInteropReader.CellString> cells;

            try
            {
                cells = await StaThreadRunner.RunAsync(() =>
                    excelReader.ReadColumnCellStrings(file.FullPath, ExcelPassword, ExcelEmailColumn));
            }
            catch (ExcelInteropException ex)
            {
                LogError($"[{ex.Code}] {ex.Message}");
                Stage.Excel = StageStatus.Fail;
                return;
            }
            catch (Exception ex)
            {
                LogError($"Excel open/read failed: {file.FileName}: {ex.Message}");
                Stage.Excel = StageStatus.Fail;
                return;
            }

            if (ReferenceEquals(file, latestFile) || string.Equals(file.FullPath, latestFile.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                latestCells = cells;
            }

            var monthSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in cells)
            {
                monthSet.Add(cell.Value);
                if (!EmailValidator.IsValidRfc5322AddrSpec(cell.Value))
                {
                    invalidTotal++;
                    if (invalidTotal <= invalidLogLimit)
                    {
                        LogError($"Invalid email (RFC 5322): file={file.FileName} row={cell.RowNumber} value='{cell.Value}'");
                    }
                }
            }

            monthEmailSets.Add(monthSet);
        }

        if (invalidTotal > 0)
        {
            if (invalidTotal > invalidLogLimit)
            {
                LogError($"Invalid email(s): {invalidTotal - invalidLogLimit} more not shown.");
            }

            Stage.Excel = StageStatus.Fail;
            return;
        }

        // Refresh recipient count from the latest file using the same RFC validator.
        var latestValues = latestCells.Select(c => c.Value);
        latestValidRecipients = RecipientCalculator.FromEmailColumnValues(latestValues);
        ExcelRecipientCountText = latestValidRecipients.Count.ToString();

        try
        {
            latestRoundsByEmail = RoundCalculator.CalculateLatestRounds(monthEmailSets);
        }
        catch (Exception ex)
        {
            LogError($"Failed to calculate rounds: {ex.Message}");
            Stage.Excel = StageStatus.Fail;
            return;
        }

        IReadOnlyList<ExcelRow> latestRows;
        try
        {
            latestRows = await StaThreadRunner.RunAsync(() =>
                excelReader.ReadRows(latestFile.FullPath, ExcelPassword, ExcelEmailColumn));
        }
        catch (ExcelInteropException ex)
        {
            LogError($"[{ex.Code}] {ex.Message}");
            Stage.Excel = StageStatus.Fail;
            return;
        }
        catch (Exception ex)
        {
            LogError($"Failed to read latest Excel rows: {latestFile.FileName}: {ex.Message}");
            Stage.Excel = StageStatus.Fail;
            return;
        }

        // Build merged groups in the same order as the recipient list (row order preserved).
        var groupMap = new Dictionary<string, List<ExcelRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in latestRows)
        {
            if (!groupMap.TryGetValue(row.Email, out var list))
            {
                list = new List<ExcelRow>();
                groupMap[row.Email] = list;
            }

            list.Add(row);
        }

        var groups = new List<EmailGroup>();
        foreach (var email in latestValidRecipients)
        {
            if (groupMap.TryGetValue(email, out var rows))
            {
                groups.Add(new EmailGroup(email, rows));
            }
        }

        latestEmailGroups = groups;

        Stage.Excel = StageStatus.Success;
        LogInfo($"Excel validated. recipients={ExcelRecipientCountText} groups={latestEmailGroups.Count}");
    }

    private void ResetAfterExcelInputsChanged()
    {
        // Any change to Excel inputs invalidates downstream stages.
        Stage.Excel = StageStatus.Pending;
        Stage.Smtp = StageStatus.Pending;
        Stage.Template = StageStatus.Pending;
        Stage.TestMail = StageStatus.Pending;

        latestValidRecipients = Array.Empty<string>();
        latestEmailGroups = Array.Empty<EmailGroup>();
        latestRoundsByEmail = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private void ResetAfterSmtpInputsChanged()
    {
        Stage.Smtp = StageStatus.Pending;
        Stage.Template = StageStatus.Pending;
        Stage.TestMail = StageStatus.Pending;
    }

    private void ResetAfterTemplateInputsChanged()
    {
        Stage.Template = StageStatus.Pending;
        Stage.TestMail = StageStatus.Pending;
    }

    private void ResetAfterTestRecipientChanged()
    {
        Stage.TestMail = StageStatus.Pending;
    }

    partial void OnExcelFolderPathChanged(string value) => ResetAfterExcelInputsChanged();
    partial void OnExcelPasswordChanged(string value) => ResetAfterExcelInputsChanged();
    partial void OnExcelEmailColumnChanged(string value) => ResetAfterExcelInputsChanged();

    partial void OnSmtpHostChanged(string value) => ResetAfterSmtpInputsChanged();
    partial void OnSmtpPortChanged(int value) => ResetAfterSmtpInputsChanged();
    partial void OnSmtpSecurityChanged(string value) => ResetAfterSmtpInputsChanged();
    partial void OnSmtpSenderChanged(string value) => ResetAfterSmtpInputsChanged();
    partial void OnSmtpPasswordChanged(string value) => ResetAfterSmtpInputsChanged();
    partial void OnSmtpTimeoutSecondsChanged(int value) => ResetAfterSmtpInputsChanged();

    partial void OnMailSubjectChanged(string value) => ResetAfterTemplateInputsChanged();
    partial void OnMailBodyPathChanged(string value) => ResetAfterTemplateInputsChanged();
    partial void OnMailAttachmentsTextChanged(string value) => ResetAfterTemplateInputsChanged();
    partial void OnMaxAttachmentBytesChanged(long value) => ResetAfterTemplateInputsChanged();

    partial void OnTestRecipientChanged(string value) => ResetAfterTestRecipientChanged();

    private async Task TestSmtpInternalAsync()
    {
        Stage.Smtp = StageStatus.Pending;
        Stage.Template = StageStatus.Pending;
        Stage.TestMail = StageStatus.Pending;

        try
        {
            var securityMode = SmtpSecurityParser.Parse(SmtpSecurity);

            await smtpService.VerifyConnectAndAuthAsync(
                host: SmtpHost,
                port: SmtpPort,
                security: securityMode,
                username: SmtpSender,
                password: SmtpPassword,
                timeoutSeconds: SmtpTimeoutSeconds);

            Stage.Smtp = StageStatus.Success;
            LogInfo("SMTP verified (connect+auth).");
        }
        catch (Exception ex)
        {
            Stage.Smtp = StageStatus.Fail;
            LogError($"SMTP verify failed: {ex.Message}");
        }
    }

    private async Task SendTestMailInternalAsync()
    {
        Stage.TestMail = StageStatus.Pending;

        if (Stage.Excel != StageStatus.Success || Stage.Smtp != StageStatus.Success || Stage.Template != StageStatus.Success)
        {
            LogError("Test mail requires Excel+SMTP+Template success.");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        if (latestEmailGroups.Count == 0)
        {
            LogError("No recipients found in latest Excel (cannot send test mail).");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        if (!EmailValidator.IsValidRfc5322AddrSpec(TestRecipient))
        {
            LogError($"Invalid test recipient (RFC 5322): '{TestRecipient}'");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        if (!EmailValidator.IsValidRfc5322AddrSpec(SmtpSender))
        {
            LogError($"Invalid sender (RFC 5322): '{SmtpSender}'");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        var bodyPath = ResolveExistingFilePath(MailBodyPath);
        if (bodyPath is null)
        {
            LogError($"Body file not found: {MailBodyPath}");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        string bodyTemplate;
        try
        {
            bodyTemplate = await File.ReadAllTextAsync(bodyPath);
        }
        catch (Exception ex)
        {
            LogError($"Failed to read body template: {bodyPath}: {ex.Message}");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        var attachmentPaths = ParseAttachmentPaths(MailAttachmentsText);
        var resolvedAttachments = new List<string>();
        foreach (var attachmentPath in attachmentPaths)
        {
            var resolved = ResolveExistingFilePath(attachmentPath);
            if (resolved is null)
            {
                LogError($"Attachment not found: {attachmentPath}");
                Stage.TestMail = StageStatus.Fail;
                return;
            }

            var fi = new FileInfo(resolved);
            if (fi.Length > MaxAttachmentBytes)
            {
                LogError($"Attachment too large: {resolved} ({fi.Length} bytes)");
                Stage.TestMail = StageStatus.Fail;
                return;
            }

            resolvedAttachments.Add(resolved);
        }

        // Use the first real target data; only the TO is replaced.
        var group = latestEmailGroups[0];
        var round = latestRoundsByEmail.TryGetValue(group.Email, out var r) ? r : 1;

        RenderedTemplate rendered;
        try
        {
            rendered = MailTemplateRenderer.Render(
                subjectTemplate: MailSubject,
                bodyTemplate: bodyTemplate,
                round: round,
                group: group);
        }
        catch (Exception ex)
        {
            LogError($"Template render failed: {ex.Message}");
            Stage.TestMail = StageStatus.Fail;
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(string.Empty, SmtpSender.Trim()));
        message.ReplyTo.Add(new MailboxAddress(string.Empty, SmtpSender.Trim()));
        message.To.Add(new MailboxAddress(string.Empty, TestRecipient.Trim()));
        message.Subject = rendered.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = rendered.HtmlBody };
        foreach (var resolved in resolvedAttachments)
        {
            bodyBuilder.Attachments.Add(resolved);
        }

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            var securityMode = SmtpSecurityParser.Parse(SmtpSecurity);

            await smtpService.SendAsync(
                host: SmtpHost,
                port: SmtpPort,
                security: securityMode,
                username: SmtpSender,
                password: SmtpPassword,
                timeoutSeconds: SmtpTimeoutSeconds,
                message: message);

            Stage.TestMail = StageStatus.Success;
            LogInfo($"Test mail sent (accepted). to={TestRecipient} using data={group.Email} round={round}");
        }
        catch (Exception ex)
        {
            Stage.TestMail = StageStatus.Fail;
            LogError($"Test mail send failed: {ex.Message}");
        }
    }

    private async Task ValidateTemplateInternalAsync()
    {
        Stage.Template = StageStatus.Pending;
        Stage.TestMail = StageStatus.Pending;

        if (Stage.Excel != StageStatus.Success || Stage.Smtp != StageStatus.Success)
        {
            LogError("Template validation requires Excel+SMTP success.");
            Stage.Template = StageStatus.Fail;
            return;
        }

        if (string.IsNullOrWhiteSpace(ExcelFolderPath) || !Directory.Exists(ExcelFolderPath))
        {
            LogError("Excel folder path is empty or does not exist.");
            Stage.Template = StageStatus.Fail;
            return;
        }

        if (string.IsNullOrWhiteSpace(MailBodyPath))
        {
            LogError("BodyPath is empty.");
            Stage.Template = StageStatus.Fail;
            return;
        }

        var resolvedBodyPath = ResolveExistingFilePath(MailBodyPath);
        if (resolvedBodyPath is null)
        {
            LogError($"Body file not found: {MailBodyPath}");
            Stage.Template = StageStatus.Fail;
            return;
        }

        string bodyTemplate;
        try
        {
            bodyTemplate = await File.ReadAllTextAsync(resolvedBodyPath);
        }
        catch (Exception ex)
        {
            LogError($"Failed to read body template: {resolvedBodyPath}: {ex.Message}");
            Stage.Template = StageStatus.Fail;
            return;
        }

        var attachmentPaths = ParseAttachmentPaths(MailAttachmentsText);
        var attachments = attachmentPaths
            .Select(p =>
            {
                var resolved = ResolveExistingFilePath(p);
                if (resolved is null)
                {
                    return new AttachmentInfo(p, LengthBytes: 0, Exists: false);
                }

                try
                {
                    var fi = new FileInfo(resolved);
                    return new AttachmentInfo(resolved, fi.Length, Exists: true);
                }
                catch
                {
                    return new AttachmentInfo(resolved, LengthBytes: 0, Exists: false);
                }
            })
            .ToArray();

        // For token validation we match against the latest file's header columns.
        var candidates = ExcelBatchScanner.EnumerateCandidateFiles(ExcelFolderPath);
        var (scan, scanError) = ExcelBatchScanner.ValidateMonthlyContinuity(candidates);
        if (scanError is not null)
        {
            LogError($"[{scanError.Code}] {scanError.Message}");
            Stage.Template = StageStatus.Fail;
            return;
        }

        var latest = scan!.Files[^1];
        IReadOnlyList<string> latestColumns;

        try
        {
            latestColumns = await StaThreadRunner.RunAsync(() =>
                excelReader.ReadHeaderRowValues(latest.FullPath, ExcelPassword));
        }
        catch (ExcelInteropException ex)
        {
            LogError($"[{ex.Code}] {ex.Message}");
            Stage.Template = StageStatus.Fail;
            return;
        }
        catch (Exception ex)
        {
            LogError($"Failed to read Excel headers: {latest.FileName}: {ex.Message}");
            Stage.Template = StageStatus.Fail;
            return;
        }

        var error = TemplateValidator.Validate(
            subject: MailSubject,
            bodyTemplate: bodyTemplate,
            availableColumns: latestColumns,
            attachments: attachments,
            maxAttachmentBytes: MaxAttachmentBytes);

        if (error is not null)
        {
            LogError($"[{error.Code}] {error.Message}");
            Stage.Template = StageStatus.Fail;
            return;
        }

        Stage.Template = StageStatus.Success;
        LogInfo("Template validated (tokens/attachment).");
    }

    private static IReadOnlyList<string> ParseAttachmentPaths(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();
    }

    private static string? ResolveExistingFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Path.IsPathRooted(path))
        {
            return File.Exists(path) ? path : null;
        }

        var candidates = new[]
        {
            Path.GetFullPath(path, Environment.CurrentDirectory),
            Path.GetFullPath(path, AppContext.BaseDirectory)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
