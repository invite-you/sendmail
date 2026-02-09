using SendMail.Core.Models;

namespace SendMail.Core.Tests;

public class StageStateTests
{
    [Fact]
    public void CanValidateTemplate_RequiresExcelAndSmtpSuccess()
    {
        var state = new StageState
        {
            Excel = StageStatus.Pending,
            Smtp = StageStatus.Success
        };

        Assert.False(state.CanValidateTemplate);

        state.Excel = StageStatus.Success;
        Assert.True(state.CanValidateTemplate);
    }

    [Fact]
    public void CanSendTestMail_RequiresExcelSmtpAndTemplateSuccess()
    {
        var state = new StageState
        {
            Excel = StageStatus.Pending,
            Smtp = StageStatus.Success,
            Template = StageStatus.Success
        };

        Assert.False(state.CanSendTestMail);

        state.Excel = StageStatus.Success;
        Assert.True(state.CanSendTestMail);
    }
}

