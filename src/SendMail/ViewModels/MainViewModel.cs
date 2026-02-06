using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SendMail.Core.Config;
using SendMail.Core.Models;

namespace SendMail.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Stage = new StageState();

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
        LogInfo("LoadExcel: not implemented yet.");
        return Task.CompletedTask;
    }

    private Task ValidateExcelAsync()
    {
        LogInfo("ValidateExcel: not implemented yet.");
        return Task.CompletedTask;
    }

    private Task TestSmtpAsync()
    {
        LogInfo("TestSmtp(connect/auth): not implemented yet.");
        return Task.CompletedTask;
    }

    private Task ValidateTemplateAsync()
    {
        LogInfo("ValidateTemplate(tokens/attachment): not implemented yet.");
        return Task.CompletedTask;
    }

    private Task SendTestMailAsync()
    {
        LogInfo("SendTestMail: not implemented yet.");
        return Task.CompletedTask;
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
}
