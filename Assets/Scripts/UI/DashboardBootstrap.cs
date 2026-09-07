using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FarmDashboard
{
    // The ONE manual step: add this component to an empty GameObject in the
    // scene. Everything else -- Canvas, EventSystem, every screen -- is built
    // from code in Awake/Start. No prefabs, no Inspector wiring.
    public class DashboardBootstrap : MonoBehaviour
    {
        [Header("Config (matches README defaults)")]
        public string brandName = "Granja TEC";
        [Range(300, 1500)] public float tickSpeedMs = 700f;
        public bool autoPlayOnLoad = false;

        private DashboardState _state;
        private RectTransform _canvasRoot;
        private DashboardShell _shell;
        private SimulationDataAdapter _liveData;
        private float _tickTimer;

        private void Awake()
        {
            _state = new DashboardState();
            _state.Config.Pasos = 240;

            BuildCanvas();
            BuildEventSystem();

            // Full-canvas background so any gap during transitions is still on-brand.
            var bg = UIBuilder.Rect(_canvasRoot, "Background", UITheme.BgBase);
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;

            var shellGo = new GameObject("DashboardShell", typeof(RectTransform));
            shellGo.transform.SetParent(_canvasRoot, false);
            var shellRect = (RectTransform)shellGo.transform;
            shellRect.anchorMin = Vector2.zero;
            shellRect.anchorMax = Vector2.one;
            shellRect.offsetMin = Vector2.zero;
            shellRect.offsetMax = Vector2.zero;
            _shell = shellGo.AddComponent<DashboardShell>();
            _shell.Init(_state, brandName, this);
            shellGo.SetActive(false);

            BuildIntro();

            _state.RebuildVehicles();

            // If the scene has the real simulation bridge (Puente/GestorSimulacion),
            // this takes over the state's Vehicles/Tick/metrics from the server and
            // Update() below stops running the demo tick. See SimulationDataAdapter.
            _liveData = gameObject.AddComponent<SimulationDataAdapter>();
            _liveData.Init(_state);

            if (autoPlayOnLoad) StartRun();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("DashboardCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900); // matches the design's reference canvas
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasRoot = (RectTransform)canvasGo.transform;
        }

        private void BuildEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            esGo.transform.SetParent(transform, false);
            // Project uses the new Input System exclusively (Active Input Handling = 1),
            // so the classic StandaloneInputModule would not receive any events.
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildIntro()
        {
            var introGo = new GameObject("IntroView", typeof(RectTransform));
            introGo.transform.SetParent(_canvasRoot, false);
            var intro = introGo.AddComponent<IntroView>();
            intro.Init(brandName, OnIntroComplete);
        }

        private void OnIntroComplete()
        {
            _shell.gameObject.SetActive(true);
        }

        // Clicking the small logo tile in the header (see DashboardShell) replays
        // the splash screen -- a common "logo goes back to the start" pattern.
        // IntroView destroys itself once it hands off to the dashboard, so this
        // just builds a fresh one rather than trying to reset/reshow the old one.
        public void ShowIntro()
        {
            _shell.gameObject.SetActive(false);
            BuildIntro();
        }

        private void Update()
        {
            if (_state.UsingLiveData) return; // real data drives Vehicles/Tick instead
            if (!_state.Running) return;
            _tickTimer += Time.deltaTime * 1000f;
            if (_tickTimer >= tickSpeedMs)
            {
                _tickTimer = 0f;
                _state.TickDemo();
                if (!_state.Running) _shell.RefreshRunningControls();
            }
        }

        // Exposed for the Home view (next phase) to wire Resume/Pause/Restart to
        // the real simulation instead of the demo tick, when UsingLiveData is true.
        public SimulationDataAdapter LiveData => _liveData;

        public void StartRun()
        {
            if (_state.UsingLiveData) _liveData.Resume();
            _state.Running = true;
            _tickTimer = 0f;
            _state.NotifyChanged();
        }

        public void PauseRun()
        {
            if (_state.UsingLiveData) _liveData.Pause();
            _state.Running = false;
            _state.NotifyChanged();
        }

        public void RestartRun()
        {
            if (_state.UsingLiveData)
            {
                _liveData.Restart(_state.Config);
            }
            else
            {
                _tickTimer = 0f;
                _state.Running = false;
                _state.RebuildVehicles();
            }
            _state.ActiveCamera = "general";
            _state.NotifyChanged();
        }
    }
}
