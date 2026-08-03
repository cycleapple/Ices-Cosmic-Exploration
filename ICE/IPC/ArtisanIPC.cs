using ECommons.EzIpcManager;

namespace ICE.IPC
{
    public class ArtisanIPC
    {
        public const string Name = "Artisan";
        public ArtisanIPC() => EzIPC.Init(this, Name, SafeWrapper.AnyException);

        [EzIPC] public Func<bool> IsBusy;
        [EzIPC] public Func<bool> GetEnduranceStatus;
        [EzIPC] public Action<bool> SetEnduranceStatus;
        [EzIPC] public Func<bool> IsListRunning;
        [EzIPC] public Func<bool> IsListPaused;
        [EzIPC] public Action<bool> SetListPause;
        [EzIPC] public Func<bool> GetStopRequest;
        [EzIPC] public Action<bool> SetStopRequest;
        [EzIPC] public Action<ushort, int> CraftItem;
        [EzIPC] public Func<ushort, int> GetRaphaelStatus;
        [EzIPC] public Func<ushort, string> GetRaphaelFailure;
        [EzIPC] public Action<ushort, uint, uint, uint, uint> AssignRecipie;

        public void AssignArtisanRecipe(ushort recipeId, uint reqFood, uint reqPotion = 0, uint reqManual = 0, uint reqSquadronManual = 0)
        {
            P.Artisan.AssignRecipie(recipeId, reqFood, reqPotion, reqManual, reqSquadronManual);
        }
    }
}
