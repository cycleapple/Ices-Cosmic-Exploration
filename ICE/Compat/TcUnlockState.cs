using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using Lumina.Excel.Sheets;

namespace ICE.Compat;

// The API13 SDK has no IUnlockState service. Use its mapped native query instead.
public sealed unsafe class TcUnlockState
{
    private static long Status(Item item)
    {
        if (item.RowId == 0 || UIState.Instance() == null) return 4;
        var row = ExdModule.GetItemRowById(item.RowId);
        return row == null ? 4 : UIState.Instance()->IsItemActionUnlocked(row);
    }

    public bool IsItemUnlockable(Item item) => Status(item) is 1 or 2;
    public bool IsItemUnlocked(Item item) => Status(item) == 1;
}
