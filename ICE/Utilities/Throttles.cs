namespace ICE.Utilities;

internal class Throttles
{
    internal static bool GenericThrottle => FrameThrottler.Throttle("ICEGenericThrottle", 10);
    internal static bool OneSecondThrottle => EzThrottler.Throttle("TurnInThrottle", 1000);
}
