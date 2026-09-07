using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // README section 3: two-column Home layout -- "Vista general" camera card
    // on the left, Configuración + Flota cards on the right. Built once in
    // Init(); Refresh() (called on every DashboardState.Changed) updates marker
    // positions, fuel bars, and button states without rebuilding the grid/cards.
    public class HomeView : MonoBehaviour
    {
        private DashboardState _state;
        private DashboardBootstrap _bootstrap;

        private RectTransform _gridCanvas;
        private RectTransform _markersLayer;
        private readonly Dictionary<string, RectTransform> _markers = new();

        private Button _resumeButton;
        private Image _resumeBg;
        private TextMeshProUGUI _resumeLabel;
        private Button _pauseButton;
        private Image _pauseBg;
        private TextMeshProUGUI _pauseLabel;

        private RectTransform _fleetList;
        private readonly Dictionary<string, (Image dot, TextMeshProUGUI status, UIBuilder.ProgressBarHandle fuel, TextMeshProUGUI fuelPct, TextMeshProUGUI rounds)> _fleetRows = new();
        private int _lastVehicleCount = -1;
        private int _lastRows = -1;
        private int _lastCols = -1;

        public void Init(DashboardState state, DashboardBootstrap bootstrap)
        {
            _state = state;
            _bootstrap = bootstrap;
            var root = (RectTransform)transform;

            var layout = UIBuilder.HRow(root, "HomeLayout", gap: UITheme.GapMajor, controlWidth: true, controlHeight: true, forceExpand: true);
            layout.anchorMin = Vector2.zero;
            layout.anchorMax = Vector2.one;
            layout.offsetMin = Vector2.zero;
            layout.offsetMax = Vector2.zero;

            BuildCameraCard(layout);

            var rightCol = UIBuilder.VCol(layout, "RightColumn", gap: UITheme.GapMajor, controlWidth: true, controlHeight: true, forceExpand: true);
            UIBuilder.Flex(rightCol, 1, 1, 260, -1);

            BuildConfigCard(rightCol);
            BuildFleetCard(rightCol);

            _state.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.Changed -= Refresh;
        }

        private void BuildCameraCard(Transform parent)
        {
            // exactWidth/Height here are just a reference bake -- the card's real
            // size is flexible (flex:2) and varies with window size, but the
            // 9-sliced fallback (no exact size) doesn't render correctly at
            // runtime at all (see the header-pill and sidebar-icon fixes), so an
            // approximate exact size, even if not pixel-perfect, is required.
            var card = UIBuilder.Panel(parent, "VistaGeneralCard", UITheme.PanelBg, UITheme.RadiusCard, exactWidth: 700, exactHeight: 520, borderColor: UITheme.PanelBorder);
            UIBuilder.Flex(card, 2, 1, 280, -1);

            var col = UIBuilder.VCol(card, "Col", gap: 10, padding: new RectOffset(16, 16, 16, 16), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;

            var title = UIBuilder.Text(col, "Title", "Vista general", UITheme.TypeCardTitle20, UIBuilder.Font(UITheme.FontPathDisplay), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)title.transform, 1, 0, -1, 26, -1, 26);

            // Nested inside the title (not a layout-flow sibling in `col`): with
            // `col` using forceExpand to stretch-fill its full-width children
            // (grid canvas, inputs, buttons), a plain sibling here would get
            // force-stretched to the card's full width instead of staying a
            // small 36x4 accent bar. Anchoring it to title's own rect sidesteps
            // that entirely.
            var accent = UIBuilder.Rect(title.transform, "Accent", UITheme.GreenPrimary);
            accent.anchorMin = accent.anchorMax = new Vector2(0f, 0f);
            accent.pivot = new Vector2(0f, 1f);
            accent.sizeDelta = new Vector2(36, 4);
            accent.anchoredPosition = new Vector2(0, -4);

            _gridCanvas = UIBuilder.Panel(col, "GridCanvas", UITheme.GridFieldBg, UITheme.RadiusNested, exactWidth: 660, exactHeight: 420);
            UIBuilder.Flex(_gridCanvas, 1, 1, -1, 320);

            _markersLayer = UIBuilder.NewRect(_gridCanvas, "Markers");
            _markersLayer.anchorMin = Vector2.zero; _markersLayer.anchorMax = Vector2.one;
            _markersLayer.offsetMin = Vector2.zero; _markersLayer.offsetMax = Vector2.zero;
        }

        private void RebuildGridLines()
        {
            // Clear previous lines (cheap: grid only rebuilds when rows/cols change).
            foreach (Transform child in _gridCanvas)
            {
                if (child == _markersLayer) continue;
                Destroy(child.gameObject);
            }

            int rows = Mathf.Max(_state.Config.Rows, 1);
            int cols = Mathf.Max(_state.Config.Cols, 1);

            for (int r = 1; r < rows; r++)
            {
                var line = UIBuilder.Rect(_gridCanvas, $"RowLine{r}", UITheme.GridCellBorder);
                float y = 1f - (float)r / rows;
                line.anchorMin = new Vector2(0f, y);
                line.anchorMax = new Vector2(1f, y);
                line.sizeDelta = new Vector2(0f, 1f);
                line.transform.SetAsFirstSibling();
            }
            for (int c = 1; c < cols; c++)
            {
                var line = UIBuilder.Rect(_gridCanvas, $"ColLine{c}", UITheme.GridCellBorder);
                float x = (float)c / cols;
                line.anchorMin = new Vector2(x, 0f);
                line.anchorMax = new Vector2(x, 1f);
                line.sizeDelta = new Vector2(1f, 0f);
                line.transform.SetAsFirstSibling();
            }
            _markersLayer.SetAsLastSibling();
        }

        private void BuildConfigCard(Transform parent)
        {
            var card = UIBuilder.Panel(parent, "ConfigCard", UITheme.PanelBg, UITheme.RadiusCard, exactWidth: 300, exactHeight: 450, borderColor: UITheme.PanelBorder);
            var col = UIBuilder.VCol(card, "Col", gap: 10, padding: new RectOffset(16, 16, 16, 16), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;
            // card has no LayoutGroup of its own, so its reported height (when
            // unset) doesn't auto-detect from col's children -- it would fall
            // back to some small default, squishing everything inside. Explicit
            // height matching the summed content (title + 4 inputs + divider +
            // 3 buttons + gaps + padding) instead.
            UIBuilder.Flex(card, 0, 0, -1, 450, -1, 450);

            var title = UIBuilder.Text(col, "Title", "Configuración", UITheme.TypeRowLabel13, UIBuilder.Font(UITheme.FontPathBodyBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)title.transform, 1, 0, -1, 16, -1, 16);

            UIBuilder.NumberField(col, "Filas", "Filas", _state.Config.Rows, v => { _state.Config.Rows = Mathf.Clamp(v, 3, 8); RebuildGridLines(); });
            UIBuilder.NumberField(col, "Columnas", "Columnas", _state.Config.Cols, v => { _state.Config.Cols = Mathf.Clamp(v, 3, 8); RebuildGridLines(); });
            UIBuilder.NumberField(col, "Tractores", "Tractores", _state.Config.Tractores, v => _state.Config.Tractores = Mathf.Clamp(v, 0, 4));
            UIBuilder.NumberField(col, "Cosechadores", "Cosechadores", _state.Config.Cosechadores, v => _state.Config.Cosechadores = Mathf.Clamp(v, 0, 4));

            var divider = UIBuilder.Rect(col, "Divider", UITheme.DividerTrackBg);
            UIBuilder.Flex(divider, 1, 0, -1, 1, -1, 1);

            _resumeButton = UIBuilder.Button(col, "ResumeBtn", UITheme.GreenPrimary, UITheme.GreenPrimary, UITheme.RadiusButton, exactWidth: 268, exactHeight: 34);
            UIBuilder.Flex((RectTransform)_resumeButton.transform, 1, 0, -1, 34, -1, 34);
            _resumeBg = _resumeButton.GetComponent<Image>();
            _resumeLabel = UIBuilder.Text(_resumeButton.transform, "Label", "RESUME", UITheme.TypeStatusButton11, UIBuilder.Font(UITheme.FontPathBodyExtraBold), UITheme.OnGreenText, TextAlignmentOptions.Center);
            StretchFill((RectTransform)_resumeLabel.transform);
            _resumeButton.onClick.AddListener(() => _bootstrap.StartRun());

            _pauseButton = UIBuilder.Button(col, "PauseBtn", UITheme.DividerTrackBg, UITheme.DividerTrackBg, UITheme.RadiusButton, exactWidth: 268, exactHeight: 34);
            UIBuilder.Flex((RectTransform)_pauseButton.transform, 1, 0, -1, 34, -1, 34);
            _pauseBg = _pauseButton.GetComponent<Image>();
            _pauseLabel = UIBuilder.Text(_pauseButton.transform, "Label", "PAUSE", UITheme.TypeStatusButton11, UIBuilder.Font(UITheme.FontPathBodyExtraBold), UITheme.MidGray, TextAlignmentOptions.Center);
            StretchFill((RectTransform)_pauseLabel.transform);
            _pauseButton.onClick.AddListener(() => _bootstrap.PauseRun());

            var restartButton = UIBuilder.Button(col, "RestartBtn", Color.clear, UITheme.ChipBg, UITheme.RadiusButton, exactWidth: 268, exactHeight: 34);
            UIBuilder.Flex((RectTransform)restartButton.transform, 1, 0, -1, 34, -1, 34);
            var restartLabel = UIBuilder.Text(restartButton.transform, "Label", "RESTART", UITheme.TypeStatusButton11, UIBuilder.Font(UITheme.FontPathBodyExtraBold), UITheme.ButtonOutlineText, TextAlignmentOptions.Center);
            StretchFill((RectTransform)restartLabel.transform);
            restartButton.onClick.AddListener(() => _bootstrap.RestartRun());
        }

        private void BuildFleetCard(Transform parent)
        {
            var card = UIBuilder.Panel(parent, "FleetCard", UITheme.PanelBg, UITheme.RadiusCard, exactWidth: 300, exactHeight: 300, borderColor: UITheme.PanelBorder);
            UIBuilder.Flex(card, 0, 1, -1, 220);
            var col = UIBuilder.VCol(card, "Col", gap: 12, padding: new RectOffset(16, 16, 16, 16), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;

            var title = UIBuilder.Text(col, "Title", "Flota", UITheme.TypeSectionTitle14, UIBuilder.Font(UITheme.FontPathBodyBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)title.transform, 1, 0, -1, 18, -1, 18);

            var scrollHost = UIBuilder.NewRect(col, "ScrollHost");
            UIBuilder.Flex(scrollHost, 1, 1);
            scrollHost.gameObject.AddComponent<RectMask2D>();
            var scrollRect = scrollHost.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // controlHeight must be true here, not false: false doesn't mean
            // "respect each row's own fixed height" (that was the wrong
            // assumption) -- it means the group never applies a row's
            // LayoutElement height to its RectTransform at all, leaving whatever
            // tiny default size it had, which squished/overlapped each row's
            // internal header/fuel/rounds lines. With controlHeight:true and
            // forceExpandHeight left false (set below), each row gets clamped to
            // exactly its own min/preferred height (84) instead.
            _fleetList = UIBuilder.VCol(scrollHost, "FleetList", gap: 10, controlWidth: true, controlHeight: true);
            var fleetListGroup = _fleetList.GetComponent<VerticalLayoutGroup>();
            fleetListGroup.childForceExpandWidth = true;
            fleetListGroup.childForceExpandHeight = false;
            _fleetList.anchorMin = new Vector2(0, 1);
            _fleetList.anchorMax = new Vector2(1, 1);
            _fleetList.pivot = new Vector2(0.5f, 1f);
            // Changing anchors alone doesn't reset offsetMin/Max -- they were
            // still carrying the default 100x100 point-anchor's offsets, insetting
            // the list from the left edge and clipping row text against the mask.
            _fleetList.offsetMin = new Vector2(0f, _fleetList.offsetMin.y);
            _fleetList.offsetMax = new Vector2(0f, _fleetList.offsetMax.y);
            var fitter = _fleetList.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = _fleetList;
            scrollRect.viewport = scrollHost;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            if (_state.Config.Rows != _lastRows || _state.Config.Cols != _lastCols)
            {
                _lastRows = _state.Config.Rows;
                _lastCols = _state.Config.Cols;
                RebuildGridLines();
            }

            RefreshMarkers();
            RefreshButtons();
            RefreshFleet();
        }

        private void RefreshMarkers()
        {
            int rows = Mathf.Max(_state.Config.Rows, 1);
            int cols = Mathf.Max(_state.Config.Cols, 1);

            var seen = new HashSet<string>();
            foreach (var v in _state.Vehicles)
            {
                seen.Add(v.Id);
                if (!_markers.TryGetValue(v.Id, out var marker))
                {
                    marker = UIBuilder.NewRect(_markersLayer, $"Marker_{v.Id}");
                    var img = marker.gameObject.AddComponent<Image>();
                    img.sprite = UIBuilder.RoundedSpriteExact(14, 14, 7);
                    marker.sizeDelta = new Vector2(14, 14);
                    marker.pivot = new Vector2(0.5f, 0.5f);
                    _markers[v.Id] = marker;
                }
                float ax = (v.Col + 0.5f) / cols;
                float ay = 1f - (v.Fila + 0.5f) / rows;
                marker.anchorMin = marker.anchorMax = new Vector2(ax, ay);
                marker.GetComponent<Image>().color = v.Type == VehicleType.Tractor ? UITheme.TractorMarker : UITheme.CosechadorMarker;
            }

            var stale = new List<string>();
            foreach (var kv in _markers) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale)
            {
                Destroy(_markers[id].gameObject);
                _markers.Remove(id);
            }
        }

        private void RefreshButtons()
        {
            bool running = _state.Running;
            _resumeBg.color = running ? UITheme.DividerTrackBg : UITheme.GreenPrimary;
            _resumeLabel.color = running ? UITheme.MidGray : UITheme.OnGreenText;
            _resumeButton.interactable = !running;

            _pauseBg.color = running ? UITheme.GoldBright : UITheme.DividerTrackBg;
            _pauseLabel.color = running ? UITheme.OnGoldText : UITheme.MidGray;
            _pauseButton.interactable = running;
        }

        private void RefreshFleet()
        {
            var seen = new HashSet<string>();
            foreach (var v in _state.Vehicles)
            {
                seen.Add(v.Id);
                if (!_fleetRows.TryGetValue(v.Id, out var row))
                {
                    row = BuildFleetRow(v.Id, v.Label);
                    _fleetRows[v.Id] = row;
                }

                var (statusLabel, statusColor) = StatusDisplay(v.Status);
                row.status.text = statusLabel;
                row.status.color = statusColor;
                row.dot.color = statusColor;

                int fuelPct = Mathf.RoundToInt(Mathf.Clamp(v.Fuel, 0, 100));
                row.fuelPct.text = $"{fuelPct}%";
                row.fuel.SetPercent(fuelPct, fuelPct <= 20 ? UITheme.StatusRed : UITheme.GreenPrimary);
                row.rounds.text = v.Rounds.ToString();
            }

            var stale = new List<string>();
            foreach (var kv in _fleetRows) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) _fleetRows.Remove(id);

            if (_lastVehicleCount != _state.Vehicles.Count)
            {
                _lastVehicleCount = _state.Vehicles.Count;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_fleetList);
            }
        }

        private (Image dot, TextMeshProUGUI status, UIBuilder.ProgressBarHandle fuel, TextMeshProUGUI fuelPct, TextMeshProUGUI rounds) BuildFleetRow(string id, string label)
        {
            var row = UIBuilder.Panel(_fleetList, $"Row_{id}", UITheme.CardBgNested, UITheme.RadiusRow, exactWidth: 280, exactHeight: 84);
            UIBuilder.Flex(row, 1, 0, -1, 84, -1, 84);
            var col = UIBuilder.VCol(row, "Col", gap: 8, padding: new RectOffset(12, 12, 12, 12), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;

            var headerRow = UIBuilder.HRow(col, "Header", controlWidth: true, controlHeight: true);
            UIBuilder.Flex(headerRow, 1, 0, -1, 16, -1, 16);
            var labelText = UIBuilder.Text(headerRow, "Label", label, UITheme.TypeRowLabel13, UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)labelText.transform, 1, 1);
            var statusRow = UIBuilder.HRow(headerRow, "Status", gap: 6, controlWidth: false, controlHeight: true, align: TextAnchor.MiddleRight);
            UIBuilder.Flex(statusRow, 0, 1);
            var dotRt = UIBuilder.StatusDot(statusRow, "Dot", UITheme.StatusActivo, 6);
            var statusText = UIBuilder.Text(statusRow, "StatusText", "Activo", UITheme.TypeBody12 - 1, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.StatusActivo, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)statusText.transform, 0, 1, 60, -1);

            var fuelRow = UIBuilder.HRow(col, "FuelRow", controlWidth: true, controlHeight: true);
            UIBuilder.Flex(fuelRow, 1, 0, -1, 12, -1, 12);
            var fuelLabel = UIBuilder.Text(fuelRow, "FuelLabel", "Combustible", UITheme.TypeCaption10, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted2, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)fuelLabel.transform, 1, 1);
            var fuelPctText = UIBuilder.Text(fuelRow, "FuelPct", "100%", UITheme.TypeCaption10, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted2, TextAlignmentOptions.MidlineRight);
            UIBuilder.Flex((RectTransform)fuelPctText.transform, 0, 1, 40, -1);

            // ProgressBar() already adds its own LayoutElement (flex width 1, fixed
            // height) to the track -- no need to Flex() it again here.
            var fuelBar = UIBuilder.ProgressBar(col, "FuelBar", 6, 3, UITheme.DividerTrackBg, UITheme.GreenPrimary, exactWidth: 250);

            var roundsRow = UIBuilder.HRow(col, "RoundsRow", controlWidth: true, controlHeight: true);
            UIBuilder.Flex(roundsRow, 1, 0, -1, 16, -1, 16);
            var roundsLabel = UIBuilder.Text(roundsRow, "RoundsLabel", "Vueltas completadas", UITheme.TypeBody12, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted2, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)roundsLabel.transform, 1, 1);
            var roundsValue = UIBuilder.Text(roundsRow, "RoundsValue", "0", UITheme.TypeBody12, UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.TextPrimary, TextAlignmentOptions.MidlineRight);
            UIBuilder.Flex((RectTransform)roundsValue.transform, 0, 1, 30, -1);

            return (dotRt.GetComponent<Image>(), statusText, fuelBar, fuelPctText, roundsValue);
        }

        private static (string label, Color color) StatusDisplay(VehicleStatus status) => status switch
        {
            VehicleStatus.Activo => ("Activo", UITheme.StatusActivo),
            VehicleStatus.EnCamino => ("En camino", UITheme.StatusEnCamino),
            VehicleStatus.Cargando => ("Cargando", UITheme.StatusCargando),
            VehicleStatus.Descargando => ("Descargando", UITheme.StatusDescargando),
            _ => ("Activo", UITheme.StatusActivo),
        };
    }
}
