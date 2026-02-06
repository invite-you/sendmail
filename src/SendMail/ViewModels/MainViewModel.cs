using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SendMail.Core.Models;

namespace SendMail.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Stage = new StageState();

        LoadExcelCommand = new AsyncRelayCommand(LoadExcelAsync, CanLoadExcel);
        ValidateExcelCommand = new AsyncRelayCommand(ValidateExcelAsync, CanValidateExcel);
        TestSmtpCommand = new AsyncRelayCommand(TestSmtpAsync, CanTestSmtp);
        ValidateTemplateCommand = new AsyncRelayCommand(ValidateTemplateAsync, CanValidateTemplate);
        SendTestMailCommand = new AsyncRelayCommand(SendTestMailAsync, CanSendTestMail);
        RefreshPreviewCommand = new AsyncRelayCommand(RefreshPreviewAsync, CanRefreshPreview);

        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        PauseCommand = new RelayCommand(Pause, CanControlSending);
        StopCommand = new RelayCommand(Stop, CanControlSending);
    }

    [ObservableProperty]
    private StageState stage;

    [ObservableProperty]
    private bool isSending;

    [ObservableProperty]
    private string? selectedEmail;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public IAsyncRelayCommand LoadExcelCommand { get; }
    public IAsyncRelayCommand ValidateExcelCommand { get; }
    public IAsyncRelayCommand TestSmtpCommand { get; }
    public IAsyncRelayCommand ValidateTemplateCommand { get; }
    public IAsyncRelayCommand SendTestMailCommand { get; }
    public IAsyncRelayCommand RefreshPreviewCommand { get; }

    public IAsyncRelayCommand SendCommand { get; }
    public IRelayCommand PauseCommand { get; }
    public IRelayCommand StopCommand { get; }

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

    partial void OnIsSendingChanged(bool value)
    {
        // Ensure command enablement updates when sending state flips.
        NotifyCommandCanExecuteChanged();
    }

    private void NotifyCommandCanExecuteChanged()
    {
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
