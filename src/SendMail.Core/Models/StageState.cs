namespace SendMail.Core.Models;

public sealed class StageState
{
    public StageStatus Excel { get; set; } = StageStatus.Pending;
    public StageStatus Smtp { get; set; } = StageStatus.Pending;
    public StageStatus Template { get; set; } = StageStatus.Pending;
    public StageStatus TestMail { get; set; } = StageStatus.Pending;

    public bool CanValidateTemplate => Excel == StageStatus.Success && Smtp == StageStatus.Success;
    public bool CanSendTestMail => Excel == StageStatus.Success && Smtp == StageStatus.Success && Template == StageStatus.Success;
    public bool CanSend => Excel == StageStatus.Success
        && Smtp == StageStatus.Success
        && Template == StageStatus.Success
        && TestMail == StageStatus.Success;
}
