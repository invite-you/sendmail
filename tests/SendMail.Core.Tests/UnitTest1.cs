using SendMail.Core.Models;

namespace SendMail.Core.Tests;

public class UnitTest1
{
    [Fact]
    public void StageState_DefaultsToPending()
    {
        var state = new StageState();

        Assert.Equal(StageStatus.Pending, state.Excel);
        Assert.Equal(StageStatus.Pending, state.Smtp);
        Assert.Equal(StageStatus.Pending, state.Template);
        Assert.Equal(StageStatus.Pending, state.TestMail);
    }

    [Fact]
    public void StageState_Gating_IsDerivedFromStatuses()
    {
        var state = new StageState
        {
            Excel = StageStatus.Success,
            Smtp = StageStatus.Success,
            Template = StageStatus.Success,
            TestMail = StageStatus.Success
        };

        Assert.True(state.CanValidateTemplate);
        Assert.True(state.CanSendTestMail);
        Assert.True(state.CanSend);
    }
}
