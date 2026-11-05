using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.Sheets;

namespace ICE.Utilities
{
    internal unsafe class ConsumableInfo
    {
        internal static (uint Id, string Name)[] Food;
        internal static (uint Id, string Name)[] Pots;
        internal static (uint Id, string Name)[] Manuals;
        internal static (uint Id, string Name)[] SquadronManuals;

        internal static void Init()
        {
            Food = Svc.Data.GetExcelSheet<Item>().Where(IsGatherFood).Select(x => (x.RowId, x.Name.ToString())).ToArray();
            // Pots = Svc.Data.GetExcelSheet<Item>().Where(IsCraftersPot).Select(x => (x.RowId, x.Name.ToString())).ToArray();
            Manuals = Svc.Data.GetExcelSheet<Item>().Where(IsManual).Select(x => (x.RowId, x.Name.ToString())).ToArray();
            SquadronManuals = Svc.Data.GetExcelSheet<Item>().Where(IsSquadronManual).Select(x => (x.RowId, x.Name.ToString())).ToArray();
        }

        internal static ItemFood? GetItemConsumableProperties(Item item, bool hq)
        {
            if (!item.ItemAction.IsValid)
                return null;
            var action = item.ItemAction.Value;
            var actionParams = hq ? action.DataHQ : action.Data; // [0] = status, [1] = extra == ItemFood row, [2] = duration
            if (actionParams[0] is not 48 and not 49)
                return null; // not 'well fed' or 'medicated'
            return Svc.Data.GetExcelSheet<ItemFood>()?.GetRow(actionParams[1]);
        }

        internal static bool IsGatherFood(Item item)
        {
            if (item.ItemUICategory.RowId != 46)
                return false; // not a 'meal'
            var consumable = GetItemConsumableProperties(item, false);
            return consumable != null && consumable.Value.Params.Any(p => p.BaseParam.RowId is 10 or 72 or 73); // gp/gathering/perception
        }

        internal static bool IsManual(Item item)
        {
            if (item.ItemUICategory.RowId != 63)
                return false; // not 'other'
            if (!item.ItemAction.IsValid)
                return false;
            var action = item.ItemAction.Value;
            return action.Action.RowId == 816 && action.Data[0] is 300 or 301 or 1751 or 5329;
        }

        internal static bool IsSquadronManual(Item item)
        {
            if (item.ItemUICategory.RowId != 63)
                return false; // not 'other'
            if (!item.ItemAction.IsValid)
                return false;
            var action = item.ItemAction.Value;
            return action.Action.RowId == 816 && action.Data[0] is 2291 or 2292 or 2293 or 2294;
        }
    }
}
