using BepInEx;
using RoR2.UI;
using System.Diagnostics;
using System.IO;

namespace RespawnButton
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(ProperSave.ProperSavePlugin.GUID)]
    public class RespawnButtonPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RespawnButton";
        public const string PluginVersion = "1.0.2";

        internal static RespawnButtonPlugin Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            LanguageFolderHandler.Register(Path.GetDirectoryName(Info.Location));

            On.RoR2.UI.GameEndReportPanelController.Awake += GameEndReportPanelController_Awake;
            On.RoR2.UI.GameEndReportPanelController.SetDisplayData += GameEndReportPanelController_SetDisplayData;

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        void OnDestroy()
        {
            On.RoR2.UI.GameEndReportPanelController.Awake -= GameEndReportPanelController_Awake;
            On.RoR2.UI.GameEndReportPanelController.SetDisplayData -= GameEndReportPanelController_SetDisplayData;

            Instance = SingletonHelper.Unassign(Instance, this);
        }

        static void GameEndReportPanelController_Awake(On.RoR2.UI.GameEndReportPanelController.orig_Awake orig, GameEndReportPanelController self)
        {
            orig(self);
            self.gameObject.AddComponent<RespawnButtonController>();
        }

        static void GameEndReportPanelController_SetDisplayData(On.RoR2.UI.GameEndReportPanelController.orig_SetDisplayData orig, GameEndReportPanelController self, GameEndReportPanelController.DisplayData newDisplayData)
        {
            orig(self, newDisplayData);

            if (self.TryGetComponent(out RespawnButtonController respawnButtonController))
            {
                respawnButtonController.OnSetDisplayData(newDisplayData);
            }
        }
    }
}
