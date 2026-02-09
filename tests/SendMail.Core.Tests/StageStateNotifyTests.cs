using System.ComponentModel;
using SendMail.Core.Models;

namespace SendMail.Core.Tests;

public class StageStateNotifyTests
{
    [Fact]
    public void StageState_ImplementsNotifyPropertyChanged_AndRaisesForExcel()
    {
        var state = new StageState();
        var npc = Assert.IsAssignableFrom<INotifyPropertyChanged>(state);

        var raised = new List<string?>();
        npc.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        state.Excel = StageStatus.Success;

        Assert.Contains(nameof(StageState.Excel), raised);
        Assert.Contains(nameof(StageState.CanSend), raised);
    }
}

