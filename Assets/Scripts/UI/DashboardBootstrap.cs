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
            _shell.Init(_state, brandName);
            shellGo.SetActive(false);

            var introGo = new GameObject("IntroView", typeof(RectTransform));
            introGo.transform.SetParent(_canvasRoot, false);
            var intro = introGo.AddComponent<IntroView>();
            intro.Init(brandName, OnIntroComplete);

            _state.RebuildVehicles();
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

        private void OnIntroComplete()
        {
            _shell.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_state.Running) return;
            _tickTimer += Time.deltaTime * 1000f;
            if (_tickTimer >= tickSpeedMs)
            {
                _tickTimer = 0f;
                _state.TickDemo();
                if (!_state.Running) _shell.RefreshRunningControls();
            }
        }

        public void StartRun()
        {
            if (_state.Running) return;
            _state.Running = true;
            _tickTimer = 0f;
            _shell.RefreshRunningControls();
        }
    }
}
