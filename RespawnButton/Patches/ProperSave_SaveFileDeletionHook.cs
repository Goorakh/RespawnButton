using MonoMod.RuntimeDetour;
using ProperSave;
using RoR2;
using UnityEngine.Networking;

namespace RespawnButton.Patches
{
    static class ProperSave_SaveFileDeletionHook
    {
        delegate void orig_Saving_RunOnServerGameOver(Run run, GameEndingDef ending);

        static orig_Saving_RunOnServerGameOver _origRunOnServerGameOver;

        [SystemInitializer]
        static void Init()
        {
            Hook onServerGameOverHook = new Hook(() => Saving.RunOnServerGameOver(default, default), Saving_RunOnServerGameOver);
            _origRunOnServerGameOver = onServerGameOverHook.GenerateTrampoline<orig_Saving_RunOnServerGameOver>();

            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        public static bool SuppressSaveFileDeletion;

        static bool _deleteSaveFileOnRunEnd;
        static Run _lastRunInstance;
        static GameEndingDef _lastGameEndingDef;

        static void Saving_RunOnServerGameOver(orig_Saving_RunOnServerGameOver orig, Run run, GameEndingDef ending)
        {
            if (ending.isWin)
            {
                orig(run, ending);
                return;
            }

#if DEBUG
            Log.Debug("buffering save file deletion");
#endif

            _deleteSaveFileOnRunEnd = true;
            _lastRunInstance = run;
            _lastGameEndingDef = ending;
        }

        static void deleteSaveFile()
        {
#if DEBUG
            Log.Debug("deleting save file");
#endif

            _origRunOnServerGameOver(_lastRunInstance, _lastGameEndingDef);
        }

        static void Run_onRunDestroyGlobal(Run _)
        {
            if (!_deleteSaveFileOnRunEnd)
                return;

            try
            {
                if (!NetworkServer.active)
                    return;

                if (SuppressSaveFileDeletion)
                    return;

                deleteSaveFile();
            }
            finally
            {
#if DEBUG
                Log.Debug("finally");
#endif

                _deleteSaveFileOnRunEnd = false;
                _lastRunInstance = null;
                _lastGameEndingDef = null;
            }
        }
    }
}
