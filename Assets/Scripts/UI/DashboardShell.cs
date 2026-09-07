using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Persistent chrome across every view -- README section 2 (header + left
    // sidebar). Owns the ContentArea other view builders will fill in.
    public class DashboardShell : MonoBehaviour
    {
        private DashboardState _state;
        private TextMeshProUGUI _tickText;
        private TextMeshProUGUI _statusLabel;
        private Image _statusDot;
        public RectTransform ContentArea { get; private set; }

        private readonly Dictionary<DashView, (Image bg, TextMeshProUGUI label, ButtonHoverColor hover)> _navButtons = new();
        private TextMeshProUGUI _placeholderText;
        private RectTransform _placeholderRoot;
        private HomeView _homeView;

        public void Init(DashboardState state, string brandName, DashboardBootstrap bootstrap)
        {
            _state = state;
            var root = (RectTransform)transform;

            // forceExpand:true on both axes was stretching `header` past its
            // explicit fixed 52 height too, not just filling width as intended --
            // width should stretch (header/body span the full canvas width) but
            // height must NOT (header stays exactly 52; body's own flexibleHeight=1
            // is what correctly claims the rest). Set the two axes independently.
            var outer = UIBuilder.VCol(root, "Outer", gap: 0, controlWidth: true, controlHeight: true);
            var outerGroup = outer.GetComponent<VerticalLayoutGroup>();
            outerGroup.childForceExpandWidth = true;
            outerGroup.childForceExpandHeight = false;
            outer.anchorMin = Vector2.zero;
            outer.anchorMax = Vector2.one;
            outer.offsetMin = Vector2.zero;
            outer.offsetMax = Vector2.zero;

            BuildHeader(outer, brandName);

            var body = UIBuilder.HRow(outer, "Body", gap: 0, controlWidth: true, controlHeight: true, forceExpand: true);
            UIBuilder.Flex(body, 0, 1);

            BuildSidebar(body);

            ContentArea = UIBuilder.HRow(body, "ContentArea", gap: UITheme.GapMajor,
                padding: new RectOffset(24, 24, 20, 20), controlWidth: true, controlHeight: true, forceExpand: true);
            UIBuilder.Flex(ContentArea, 1, 1);

            _placeholderRoot = UIBuilder.NewRect(ContentArea, "Placeholder");
            UIBuilder.Flex(_placeholderRoot, 1, 1);
            _placeholderText = UIBuilder.Text(_placeholderRoot, "PlaceholderText", ViewLabel(_state.View),
                UITheme.TypeDisplay38, UIBuilder.Font(UITheme.FontPathDisplay), UITheme.TextMuted1, TextAlignmentOptions.TopLeft);
            _placeholderText.rectTransform.anchorMin = Vector2.zero;
            _placeholderText.rectTransform.anchorMax = Vector2.one;
            _placeholderText.rectTransform.offsetMin = Vector2.zero;
            _placeholderText.rectTransform.offsetMax = Vector2.zero;

            var homeGo = new GameObject("HomeView", typeof(RectTransform));
            homeGo.transform.SetParent(ContentArea, false);
            UIBuilder.Flex((RectTransform)homeGo.transform, 1, 1);
            _homeView = homeGo.AddComponent<HomeView>();
            _homeView.Init(_state, bootstrap);

            _state.Changed += RefreshAll;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.Changed -= RefreshAll;
        }

        private void BuildHeader(Transform parent, string brandName)
        {
            var header = UIBuilder.Rect(parent, "Header", UITheme.PanelBg);
            UIBuilder.Flex(header, 0, 0, -1, 52, -1, 52);
            var border = UIBuilder.Rect(header, "BottomBorder", UITheme.PanelBorder);
            ((RectTransform)border).anchorMin = new Vector2(0, 0);
            ((RectTransform)border).anchorMax = new Vector2(1, 0);
            ((RectTransform)border).pivot = new Vector2(0.5f, 0f);
            ((RectTransform)border).sizeDelta = new Vector2(0, 1);

            var row = UIBuilder.HRow(header, "HeaderRow", gap: 24, padding: new RectOffset(24, 24, 0, 0), controlWidth: true, controlHeight: true, forceExpand: true);
            row.anchorMin = Vector2.zero;
            row.anchorMax = Vector2.one;
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;

            // Left: logo tile + wordmark.
            var left = UIBuilder.HRow(row, "HeaderLeft", gap: 10, controlWidth: true, controlHeight: true, align: TextAnchor.MiddleLeft);
            // Explicit fixed width, same as the sidebar-button bug: `left` is
            // itself a HorizontalLayoutGroup, and leaving its width unset (-1)
            // hits the exact same nested-LayoutGroup mis-sizing bug regardless of
            // whether the nesting is same-axis or cross-axis. 420 comfortably
            // fits "OPERATIONS SYSTEM | Granja TEC" (260 was just too narrow).
            UIBuilder.Flex(left, 0, 1, 420, -1, 420, -1);
            var logoTile = UIBuilder.Panel(left, "LogoTile", UITheme.ChipBg, 7, exactWidth: 28, exactHeight: 28);
            UIBuilder.Flex(logoTile, 0, 0, 28, 28, 28, 28);
            BuildMiniLogoGrid(logoTile);

            // Three separate TMP elements (not one rich-text block) so each can use
            // its own font asset/weight without relying on TMP's <font> tag lookup.
            var wordmarkRow = UIBuilder.HRow(left, "Wordmark", gap: 6, controlWidth: true, controlHeight: true, align: TextAnchor.MiddleLeft);
            UIBuilder.Flex(wordmarkRow, 1, 1);

            var opsText = UIBuilder.Text(wordmarkRow, "OpsSystem", "OPERATIONS SYSTEM", UITheme.TypeWordmark15,
                UIBuilder.Font(UITheme.FontPathDisplay), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)opsText.transform, 0, 1);

            var divider = UIBuilder.Text(wordmarkRow, "Divider", "|", UITheme.TypeWordmark15,
                UIBuilder.Font(UITheme.FontPathBodyRegular), new Color(UITheme.TextMuted1.r, UITheme.TextMuted1.g, UITheme.TextMuted1.b, 0.5f), TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)divider.transform, 0, 1);

            var brandText = UIBuilder.Text(wordmarkRow, "BrandName", brandName, UITheme.TypeWordmark15,
                UIBuilder.Font(UITheme.FontPathBodyExtraLight), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)brandText.transform, 1, 1);

            // Right: 3 monospace pills.
            var right = UIBuilder.HRow(row, "HeaderRight", gap: 14, controlWidth: false, controlHeight: true, align: TextAnchor.MiddleRight);
            UIBuilder.Flex(right, 1, 1);

            BuildPill(right, "TurnoPill", 140, out _, out var turnoText);
            turnoText.text = "Turno: Diurno";
            turnoText.font = UIBuilder.Font(UITheme.FontPathMonoRegular);

            var statusPill = BuildPill(right, "StatusPill", 120, out var statusDotHost, out _statusLabel);
            _statusLabel.font = UIBuilder.Font(UITheme.FontPathMonoRegular);
            _statusDot = statusDotHost.gameObject.AddComponent<Image>();
            _statusDot.sprite = UIBuilder.RoundedSpriteExact(7, 7, 4);
            _statusDot.type = Image.Type.Simple;
            UIBuilder.Flex(statusDotHost, 0, 0, 7, 7, 7, 7);

            BuildPill(right, "TickPill", 160, out _, out _tickText);
            _tickText.font = UIBuilder.Font(UITheme.FontPathMonoRegular);
        }

        private RectTransform BuildPill(Transform parent, string name, int width, out RectTransform leadingSlot, out TextMeshProUGUI text)
        {
            // ContentSizeFitter + an anchor-stretched child turned out to size this
            // wildly wrong at runtime (a pill rendered ~900px wide, pushing the
            // other header pills off-screen) -- an explicit width sidesteps
            // whatever that interaction was. Each caller passes a width generous
            // enough for its own text.
            var pill = UIBuilder.Panel(parent, name, UITheme.ChipBg, 4f, exactWidth: width, exactHeight: 24);
            UIBuilder.Flex(pill, 0, 0, width, 24, width, 24);

            var inner = UIBuilder.HRow(pill, name + "_Row", gap: 6, padding: new RectOffset(10, 10, 0, 0), controlWidth: true, controlHeight: true, forceExpand: true, align: TextAnchor.MiddleLeft);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = Vector2.zero;
            inner.offsetMax = Vector2.zero;

            leadingSlot = UIBuilder.NewRect(inner, name + "_Lead");
            UIBuilder.Flex(leadingSlot, 0, 0, 0, 0, 0, 0);

            text = UIBuilder.Text(inner, name + "_Text", "", UITheme.TypeBody12, UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.HeaderMonoText, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)text.transform, 1, 1);
            return pill;
        }

        private void BuildMiniLogoGrid(Transform tile)
        {
            var grid = UIBuilder.VCol(tile, "Grid", gap: 2, padding: new RectOffset(3, 3, 3, 3), controlWidth: true, controlHeight: true, forceExpand: true);
            grid.anchorMin = Vector2.zero; grid.anchorMax = Vector2.one; grid.offsetMin = Vector2.zero; grid.offsetMax = Vector2.zero;
            var top = UIBuilder.HRow(grid, "Top", gap: 2, controlWidth: true, controlHeight: true, forceExpand: true); UIBuilder.Flex(top, 1, 1);
            var a = UIBuilder.Rect(top, "A", UITheme.GreenPrimary); UIBuilder.Flex(a, 1, 1);
            var b = UIBuilder.Rect(top, "B", UITheme.GoldAccent); UIBuilder.Flex(b, 1, 1);
            var bot = UIBuilder.HRow(grid, "Bottom", gap: 2, controlWidth: true, controlHeight: true, forceExpand: true); UIBuilder.Flex(bot, 1, 1);
            var c = UIBuilder.Rect(bot, "C", UITheme.GreenDark); UIBuilder.Flex(c, 1, 1);
            var d = UIBuilder.Rect(bot, "D", UITheme.BrownAccent); UIBuilder.Flex(d, 1, 1);
        }

        private void BuildSidebar(Transform parent)
        {
            var sidebar = UIBuilder.VCol(parent, "Sidebar", gap: 8, padding: new RectOffset(0, 0, 12, 12), controlWidth: true, controlHeight: true, align: TextAnchor.UpperCenter);
            UIBuilder.Flex(sidebar, 0, 0, 72, -1, 72, -1);

            // Each button's icon badge gets its own accent color (matching the
            // reference design) with a dark glyph on top -- only Home keeps the
            // flat ChipBg tile, since its "icon" IS the 2x2 color grid itself.
            AddNavButton(sidebar, DashView.Home, "Home", UITheme.ChipBg, host => BuildMiniLogoGrid(host), isHomeButton: true);
            AddNavButton(sidebar, DashView.Camaras, "Cámaras", UITheme.NearWhiteMarker, host => IconFactory.Camera(host, UITheme.BgBase, UITheme.TextMuted2));
            AddNavButton(sidebar, DashView.Combustible, "Combustible", UITheme.GoldAccent, host => IconFactory.FuelDrop(host, UITheme.BgBase));
            AddNavButton(sidebar, DashView.Cultivo, "Cultivo", UITheme.BrownAccent, host => IconFactory.CultivoDot(host, UITheme.BgBase));
            AddNavButton(sidebar, DashView.Tractores, "Tractores", UITheme.GreenPrimary, host => IconFactory.Tractor(host, UITheme.BgBase, UITheme.GreenPrimary));
            AddNavButton(sidebar, DashView.Cosechadoras, "Cosechadoras", UITheme.GreenDark, host => IconFactory.Harvester(host, UITheme.BgBase, UITheme.GreenDark));
        }

        private void AddNavButton(Transform parent, DashView view, string label, Color iconBg, System.Action<Transform> drawIcon, bool isHomeButton = false)
        {
            var btnRoot = UIBuilder.VCol(parent, $"Nav_{view}", gap: 6, padding: new RectOffset(4, 4, 12, 12), align: TextAnchor.UpperCenter, controlWidth: true, controlHeight: true);
            // Width must be explicit (not -1/"inherit"), or Unity's nested-LayoutGroup
            // cross-axis sizing can report btnRoot's own VerticalLayoutGroup preferred
            // width upward instead of clamping to the sidebar's actual 72px column --
            // that's what was rendering this button ~5x too wide (294px measured).
            UIBuilder.Flex(btnRoot, 0, 0, 64, 60, 64, 60);
            var bg = btnRoot.gameObject.AddComponent<Image>();
            bg.sprite = UIBuilder.RoundedSpriteExact(64, 60, Mathf.RoundToInt(UITheme.RadiusNested));
            bg.type = Image.Type.Simple;
            bg.color = Color.clear;
            var button = btnRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            var hover = btnRoot.gameObject.AddComponent<ButtonHoverColor>();
            hover.Image = bg;
            hover.NormalColor = Color.clear;
            hover.HoverColor = UITheme.ChipBg;

            var iconHost = UIBuilder.Panel(btnRoot, "IconHost", iconBg, 5f, exactWidth: 28, exactHeight: 28); // squarer than the default chip radius -- reads as a rounded square, not a circle, at 28x28
            UIBuilder.Flex(iconHost, 0, 0, 28, 28, 28, 28);
            drawIcon(iconHost);

            var labelText = UIBuilder.Text(btnRoot, "Label", label, UITheme.TypeCaption10, UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.TextMuted2, TextAlignmentOptions.Center);
            UIBuilder.Flex((RectTransform)labelText.transform, 1, 0, -1, 14, -1, 14);

            button.onClick.AddListener(() =>
            {
                _state.View = view;
                _state.DetailVehicleId = null;
                _state.NotifyChanged();
            });

            _navButtons[view] = (bg, labelText, hover);
            if (isHomeButton) _navButtons[DashView.Home] = (bg, labelText, hover);
        }

        public void RefreshRunningControls()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            _tickText.text = $"Tick {_state.Tick:D4} / {_state.Config.Pasos}";
            _statusLabel.text = _state.Running ? "En vivo" : "Pausado";
            _statusDot.color = _state.Running ? UITheme.GreenPrimary : UITheme.MidGray;

            foreach (var kv in _navButtons)
            {
                bool active = kv.Key == _state.View;
                kv.Value.hover.SetBaseColor(active ? UITheme.ChipBg : Color.clear);
                kv.Value.label.color = active ? UITheme.TextPrimary : UITheme.TextMuted2;
            }

            bool isHome = _state.View == DashView.Home;
            _placeholderRoot.gameObject.SetActive(!isHome);
            _homeView.gameObject.SetActive(isHome);
            if (!isHome && _placeholderText != null)
                _placeholderText.text = ViewLabel(_state.View) + "\n(vista en construcción -- próxima entrega)";
        }

        private static string ViewLabel(DashView v) => v switch
        {
            DashView.Home => "Home",
            DashView.Camaras => "Cámaras",
            DashView.Combustible => "Combustible",
            DashView.Cultivo => "Cultivo",
            DashView.Tractores => "Tractores",
            DashView.Cosechadoras => "Cosechadoras",
            _ => v.ToString(),
        };
    }
}
