using ICE.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Utilities
{
    internal static class Mission_Settings
    {
        // States that get set in the main Ui
        internal static bool StopAfterCurrent = false;
        internal static uint previouslyAbandoned = 0;

        // Gather Specifics
        internal static Vector2 previousMap = Vector2.Zero;
        internal static int nodeCounter = 0;
        internal static HashSet<uint> ExhaustedGatheringNodes = [];
        internal static bool GatheringNodesDepleted = false;
        private static int _nodeTotal = 0;
        internal static int nodeTotal
        {
            get => _nodeTotal;
            set
            {
                _nodeTotal = value;
                if (value == 0)
                {
                    ExhaustedGatheringNodes.Clear();
                    GatheringNodesDepleted = false;
                }
            }
        }
        internal static uint item_collectableId = 0;
        internal static int CollectableStep = 0;
        internal static int NextCollectableStep = 0;
        internal static int SelectedRotation = 0;

        internal static Dictionary<string, uint> SkillUseAmount { get; set; } = new()
        {
            ["BoonIncrease2"] = 0,
            ["BoonIncrease1"] = 0,
            ["Tidings"] = 0,
            ["YieldII"] = 0,
            ["YieldI"] = 0,
            ["BountifulYieldII"] = 0,
            ["BonusIntegrityChance"] = 0,
            ["BonusIntegrity"] = 0,
            ["FieldMasteryIII"] = 0,
            ["FieldMasteryII"] = 0,
            ["FieldMasteryI"] = 0,
            ["FieldMasteryTemp"] = 0,
        };

        internal static bool Abandon = false;
        internal static bool AnimationLockAbandonState = false;
        internal static uint PossiblyStuck = 0;
        internal static uint StartJob = 0;

        internal static Vector3? NearestCollectionPoint = null;

        internal static TurninState TurninState = TurninState.None;

        internal static void ResetNodeCounter()
        {
            nodeCounter = 0;
            nodeTotal = 0;
        }
        internal static void ResetCollectableState()
        {
            CollectableStep = 0;
            NextCollectableStep = 0;
            item_collectableId = 0;
        }

        public static Dictionary<uint, int> missionAppearanceCounts = new Dictionary<uint, int>();
        public static int rerollThreshold = 3;
    }
}
