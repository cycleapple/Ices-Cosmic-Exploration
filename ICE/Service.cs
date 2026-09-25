using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using ICE.IPC;

namespace ICE;

public static class Service
{
    public static IceCosmicExplorationIPC IPC { get; private set; } = null!;
}
