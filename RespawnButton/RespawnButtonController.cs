using ProperSave;
using RespawnButton.Patches;
using RoR2;
using RoR2.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RespawnButton
{
    public class RespawnButtonController : MonoBehaviour
    {
        static bool shouldShowOnReportScreen(RunReport runReport)
        {
            // Eclipse and Prismatics for some reason just immediately disconnect on run end, no matter what.
            // This is fine for singleplayer since we can just override the behavior, but there's nothing we can do for clients in multiplayer
            if (!NetworkServer.dontListen)
            {
                switch (Run.instance)
                {
                    case EclipseRun:
                    case WeeklyRun:
                        return false;
                }
            }

            if (runReport is null)
                return false;

            if (!runReport.gameEnding || runReport.gameEnding.isWin)
                return false;

            return true;
        }

        static bool shouldBeInteractable()
        {
            return Loading.CurrentSave is not null;
        }

        Transform _respawnButtonInstance;

        public bool IsVisible
        {
            get
            {
                return _respawnButtonInstance && _respawnButtonInstance.gameObject.activeSelf;
            }
            set
            {
                if (IsVisible == value)
                    return;

                if (!_respawnButtonInstance)
                {
                    if (value)
                    {
                        MPButton continueButton = _gameEndPanelController.continueButton;
                        if (continueButton)
                        {
                            GameObject respawnButton = Instantiate(continueButton.gameObject, continueButton.transform.parent);
                            respawnButton.transform.SetAsLastSibling();
                            respawnButton.name = "RespawnButton";

                            HGButton button = respawnButton.GetComponent<HGButton>();
                            button.onClick.RemoveAllListeners();

                            button.onClick.AddListener(onRespawnClicked);

                            button.interactable = shouldBeInteractable();

                            LanguageTextMeshController labelText = respawnButton.GetComponentInChildren<LanguageTextMeshController>();
                            if (labelText)
                            {
                                labelText.token = "GAME_REPORT_SCREEN_RESPAWN_BUTTON_LABEL";
                            }

                            Transform glyphTransform = respawnButton.transform.Find("GenericGlyph");
                            if (glyphTransform)
                            {
                                glyphTransform.gameObject.SetActive(false);
                            }

                            _respawnButtonInstance = respawnButton.transform;
                        }
                    }
                }
                else
                {
                    _respawnButtonInstance.gameObject.SetActive(value);
                }
            }
        }

        GameEndReportPanelController _gameEndPanelController;

        void Awake()
        {
            _gameEndPanelController = GetComponent<GameEndReportPanelController>();
        }

        public void OnSetDisplayData(GameEndReportPanelController.DisplayData newDisplayData)
        {
            IsVisible = NetworkServer.active && shouldShowOnReportScreen(newDisplayData.runReport);
        }

        void onRespawnClicked()
        {
            Run run = Run.instance;
            if (!run)
                return;

            SceneDef currentRunScene = SceneCatalog.mostRecentSceneDef;

            ProperSave_SaveFileDeletionHook.SuppressSaveFileDeletion = true;

            var oldPostRunDestinationOverride = OverridePostRunDestinationPatch.PostRunDestinationOverride;

            // Some run types just straight up disconnect for some reason instead of returning to character select
            // Override this behavior so that the load can work properly
            OverridePostRunDestinationPatch.PostRunDestinationOverride = _ =>
            {
                if (NetworkServer.active)
                {
                    NetworkManager.singleton.ServerChangeScene("lobby");
                }
            };

            try
            {
                NetworkSession.instance.EndRun();
            }
            finally
            {
                // Frame wait required because OnDestroy is not called immediately
                IEnumerator waitThenDisableSaveSuppression()
                {
                    yield return new WaitForEndOfFrame();

                    ProperSave_SaveFileDeletionHook.SuppressSaveFileDeletion = false;
                    OverridePostRunDestinationPatch.PostRunDestinationOverride = oldPostRunDestinationOverride;

#if DEBUG
                    Log.Debug("Disabled save file suppression");
#endif
                }

                RoR2Application.instance.StartCoroutine(waitThenDisableSaveSuppression());
            }

            IEnumerator waitForLobbyLoadThenLoadSave()
            {
                SceneDef characterSelectScene = SceneCatalog.FindSceneDef("lobby");

                while (SceneCatalog.mostRecentSceneDef != characterSelectScene)
                {
                    if (SceneCatalog.mostRecentSceneDef != currentRunScene)
                    {
                        Log.Info($"Unexpected exit scene, run exited to {SceneCatalog.mostRecentSceneDef.cachedName}, expected {characterSelectScene.cachedName}. Aborting restart.");
                        yield break;
                    }

                    yield return 0;
                }

                yield return new WaitUntil(NetworkUser.AllParticipatingNetworkUsersReady);

                yield return new WaitForEndOfFrame();

                yield return Loading.LoadLobby();
            }

            RoR2Application.instance.StartCoroutine(waitForLobbyLoadThenLoadSave());
        }
    }
}
