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
        private OperationsSectionView _operationsView;
        private CultivoCamarasView _cultivoCamarasView;
        private CamarasView _camarasView;
        private SeguimientoVehiculoView _seguimientoView;

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

            BuildHeader(outer, brandName, bootstrap);

            // Plain anchored container instead of a HorizontalLayoutGroup: after
            // repeatedly hitting nested-LayoutGroup sizing bugs all session (the
            // sidebar itself was the latest victim, drifting ~250px right for no
            // traceable reason), the sidebar/content split is simple enough to
            // just pin directly with anchors -- no layout recalculation involved,
            // so nothing downstream can ever knock it loose again.
            var body = UIBuilder.NewRect(outer, "Body");
            UIBuilder.Flex(body, 0, 1);

            const float sidebarWidth = 72f;
            var sidebarHost = UIBuilder.NewRect(body, "SidebarHost");
            sidebarHost.anchorMin = new Vector2(0f, 0f);
            sidebarHost.anchorMax = new Vector2(0f, 1f);
            sidebarHost.pivot = new Vector2(0f, 0.5f);
            sidebarHost.sizeDelta = new Vector2(sidebarWidth, 0f);
            sidebarHost.anchoredPosition = new Vector2(6f, 0f); // small nudge off the screen edge -- icons were getting clipped flush against x=0
            BuildSidebar(sidebarHost);

            ContentArea = UIBuilder.HRow(body, "ContentArea", gap: UITheme.GapMajor,
                padding: new RectOffset(24, 24, 20, 20), controlWidth: true, controlHeight: true, forceExpand: true);
            ContentArea.anchorMin = new Vector2(0f, 0f);
            ContentArea.anchorMax = new Vector2(1f, 1f);
            ContentArea.pivot = new Vector2(0.5f, 0.5f);
            ContentArea.offsetMin = new Vector2(sidebarWidth, 0f);
            ContentArea.offsetMax = Vector2.zero;

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

            var operationsGo = new GameObject("OperationsSectionView", typeof(RectTransform));
            operationsGo.transform.SetParent(ContentArea, false);
            UIBuilder.Flex((RectTransform)operationsGo.transform, 1, 1);
            _operationsView = operationsGo.AddComponent<OperationsSectionView>();
            _operationsView.Init(_state);

            // Mitad derecha de Cultivo: las 4 camaras por zona.
            // OperationsSectionView solo ocupa la mitad izquierda.
            var cultivoCamarasGo = new GameObject("CultivoCamarasView", typeof(RectTransform));
            cultivoCamarasGo.transform.SetParent(ContentArea, false);
            // SIN UIBuilder.Flex a proposito: esta vista se sale del reparto
            // del HorizontalLayoutGroup (se encima al ContentArea completo,
            // ver CultivoCamarasView.Init). Un LayoutElement con
            // ignoreLayout=false aca ganaria: el grupo incluye al hijo si
            // CUALQUIER LayoutElement suyo tiene ignoreLayout=false, asi que
            // agregar Flex volveria a meterla al reparto y las dos vistas se
            // quedarian con un cuarto de pantalla cada una (el hueco enorme
            // en medio).
            _cultivoCamarasView = cultivoCamarasGo.AddComponent<CultivoCamarasView>();
            _cultivoCamarasView.Init(_state);

            // Mitad derecha de Combustible/Tractores/Cosechadoras: camara que
            // sigue al vehiculo marcado en la lista de la izquierda.
            var seguimientoGo = new GameObject("SeguimientoVehiculoView", typeof(RectTransform));
            seguimientoGo.transform.SetParent(ContentArea, false);
            _seguimientoView = seguimientoGo.AddComponent<SeguimientoVehiculoView>();
            _seguimientoView.Init(_state);

            // Pestana Camaras: cuatro angulos cercanos, a pantalla completa.
            var camarasGo = new GameObject("CamarasView", typeof(RectTransform));
            camarasGo.transform.SetParent(ContentArea, false);
            UIBuilder.Flex((RectTransform)camarasGo.transform, 1, 1);
            _camarasView = camarasGo.AddComponent<CamarasView>();
            _camarasView.Init(_state);

            _state.Changed += RefreshAll;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.Changed -= RefreshAll;
        }

        private void BuildHeader(Transform parent, string brandName, DashboardBootstrap bootstrap)
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
            // Clicking the corner logo replays the splash screen.
            var logoButton = logoTile.gameObject.AddComponent<Button>();
            logoButton.targetGraphic = logoTile.GetComponent<Image>();
            logoButton.transition = Selectable.Transition.None;
            logoButton.onClick.AddListener(() => bootstrap.ShowIntro());
            var logoHover = logoTile.gameObject.AddComponent<ButtonHoverColor>();
            logoHover.Image = logoTile.GetComponent<Image>();
            logoHover.NormalColor = UITheme.ChipBg;
            logoHover.HoverColor = UITheme.DividerTrackBg;

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
            // controlWidth must be true now that pills use explicit fixed
            // LayoutElement widths (not ContentSizeFitter): false means "never
            // apply the LayoutElement width to the RectTransform at all", which
            // silently left every pill stuck at its default 100 width no matter
            // what min/preferredWidth said -- confirmed via the Inspector (sprite
            // baked correctly at 140x24, but the RectTransform itself read 100),
            // which is exactly why the text (sized for 140) was overflowing a box
            // that was actually only 100 wide.
            var right = UIBuilder.HRow(row, "HeaderRight", gap: 14, controlWidth: true, controlHeight: true, align: TextAnchor.MiddleCenter);
            UIBuilder.Flex(right, 1, 1);

            BuildPill(right, "TurnoPill", 140, out _, out var turnoText);
            turnoText.text = "Turno: Diurno";
            turnoText.font = UIBuilder.Font(UITheme.FontPathMonoRegular);

            var statusPill = BuildPill(right, "StatusPill", 118, out var statusDotHost, out _statusLabel, hasLeadingSlot: true);
            _statusLabel.font = UIBuilder.Font(UITheme.FontPathMonoRegular);
            _statusDot = statusDotHost.gameObject.AddComponent<Image>();
            // 8x8 with radius 4 = an exact half-radius circle (the StatusDot
            // helper's proven-good ratio), baked at its final pixel size --
            // Type.Sliced on a tiny runtime sprite stretches instead of slicing.
            _statusDot.sprite = UIBuilder.RoundedSpriteExact(8, 8, 4);
            _statusDot.type = Image.Type.Simple;
            // BuildPill already added a LayoutElement to this GameObject (min/
            // preferred 0,0) -- calling Flex() again would ADD A SECOND one
            // instead of replacing it, leaving two components fighting over the
            // size and stretching the dot into a capsule. Reuse the existing one.
            var dotLayout = statusDotHost.GetComponent<LayoutElement>();
            dotLayout.minWidth = dotLayout.preferredWidth = 8;
            dotLayout.minHeight = dotLayout.preferredHeight = 8;

            BuildPill(right, "TickPill", 172, out _, out _tickText);
            _tickText.font = UIBuilder.Font(UITheme.FontPathMonoRegular);
        }

        private RectTransform BuildPill(Transform parent, string name, int width, out RectTransform leadingSlot, out TextMeshProUGUI text, bool hasLeadingSlot = false)
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
            // forceExpand:true stretches BOTH axes for every child regardless of
            // its own flexible weight (same bug as the Flota title) -- height was
            // already turned off below, but width was still forceExpanding the
            // dot (flexibleWidth 0) out into a capsule alongside the label text
            // (flexibleWidth 1), which absorbs leftover width fine on its own
            // without forceExpand's help.
            var innerGroup = inner.GetComponent<HorizontalLayoutGroup>();
            innerGroup.childForceExpandWidth = false;
            innerGroup.childForceExpandHeight = false;

            // Only pills that actually show something before the text (the status
            // dot) get a leading slot -- it used to always exist at 0 width, but
            // `inner`'s gap:6 still applied around it either way, quietly eating
            // 6px that should've gone to the text and pushing it past the pill's
            // own edge for the plain text-only pills (Turno/Tick).
            if (hasLeadingSlot)
            {
                leadingSlot = UIBuilder.NewRect(inner, name + "_Lead");
                UIBuilder.Flex(leadingSlot, 0, 0, 0, 0, 0, 0);
            }
            else
            {
                leadingSlot = null;
            }

            text = UIBuilder.Text(inner, name + "_Text", "", UITheme.TypeBody12, UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.HeaderMonoText, TextAlignmentOptions.Center);
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
            // Purely decorative -- don't let them catch the click meant for the
            // logo tile's own Button (see the input-field "_Fill" raycast fix).
            foreach (var tileRect in new[] { a, b, c, d })
                tileRect.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildSidebar(Transform parent)
        {
            // `parent` (SidebarHost) is already pinned to exactly 72px via direct
            // anchors in Init() -- just stretch-fill it, no Flex/LayoutElement
            // needed for sidebar's own sizing.
            var sidebar = UIBuilder.VCol(parent, "Sidebar", gap: 8, padding: new RectOffset(0, 0, 12, 12), controlWidth: true, controlHeight: true, align: TextAnchor.UpperCenter);
            sidebar.anchorMin = Vector2.zero;
            sidebar.anchorMax = Vector2.one;
            sidebar.offsetMin = Vector2.zero;
            sidebar.offsetMax = Vector2.zero;

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
            var statusColor = _state.Running ? UITheme.GreenPrimary : UITheme.MidGray;
            _statusDot.color = statusColor;
            _statusLabel.color = statusColor;

            foreach (var kv in _navButtons)
            {
                bool active = kv.Key == _state.View;
                kv.Value.hover.SetBaseColor(active ? UITheme.ChipBg : Color.clear);
                kv.Value.label.color = active ? UITheme.TextPrimary : UITheme.TextMuted2;
            }

            bool isHome = _state.View == DashView.Home;
            bool isCamaras = _state.View == DashView.Camaras;
            bool isOperations = _state.View == DashView.Combustible || _state.View == DashView.Cultivo ||
                                _state.View == DashView.Tractores || _state.View == DashView.Cosechadoras;
            _placeholderRoot.gameObject.SetActive(!isHome && !isCamaras && !isOperations);
            _homeView.gameObject.SetActive(isHome);
            _camarasView.gameObject.SetActive(isCamaras);
            _operationsView.gameObject.SetActive(isOperations);
            if (isOperations) _operationsView.Show(_state.View);
            _cultivoCamarasView.gameObject.SetActive(_state.View == DashView.Cultivo);
            // Cultivo ya usa la mitad derecha para sus camaras por zona; las
            // otras tres vistas de operacion la usan para el seguimiento.
            _seguimientoView.gameObject.SetActive(isOperations && _state.View != DashView.Cultivo);
            if (!isHome && !isCamaras && !isOperations && _placeholderText != null)
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
