using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SendMail.Core.Models;

public sealed class StageState : INotifyPropertyChanged
{
    private StageStatus excel = StageStatus.Pending;
    private StageStatus smtp = StageStatus.Pending;
    private StageStatus template = StageStatus.Pending;
    private StageStatus testMail = StageStatus.Pending;

    public StageStatus Excel
    {
        get => excel;
        set
        {
            if (excel == value)
            {
                return;
            }

            excel = value;
            OnPropertyChanged();
            OnGatingChanged();
        }
    }

    public StageStatus Smtp
    {
        get => smtp;
        set
        {
            if (smtp == value)
            {
                return;
            }

            smtp = value;
            OnPropertyChanged();
            OnGatingChanged();
        }
    }

    public StageStatus Template
    {
        get => template;
        set
        {
            if (template == value)
            {
                return;
            }

            template = value;
            OnPropertyChanged();
            OnGatingChanged();
        }
    }

    public StageStatus TestMail
    {
        get => testMail;
        set
        {
            if (testMail == value)
            {
                return;
            }

            testMail = value;
            OnPropertyChanged();
            OnGatingChanged();
        }
    }

    public bool CanValidateTemplate => Excel == StageStatus.Success && Smtp == StageStatus.Success;
    public bool CanSendTestMail => Excel == StageStatus.Success && Smtp == StageStatus.Success && Template == StageStatus.Success;
    public bool CanSend => Excel == StageStatus.Success
        && Smtp == StageStatus.Success
        && Template == StageStatus.Success
        && TestMail == StageStatus.Success;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void OnGatingChanged()
    {
        OnPropertyChanged(nameof(CanValidateTemplate));
        OnPropertyChanged(nameof(CanSendTestMail));
        OnPropertyChanged(nameof(CanSend));
    }
}
