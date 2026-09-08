using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Builds only the left information panel from the supplied references.
    // Home, Cameras, and the unused right side of the content area are untouched.
    public class OperationsSectionView : MonoBehaviour
    {
        private sealed class VehicleRow
        {
            public string Id;
            public TextMeshProUGUI Status, FuelText, Position, Load, Extra;
            public UIBuilder.ProgressBarHandle FuelBar;
        }

        private sealed class ZoneRow
        {
            public TextMeshProUGUI Status, Percent;
            public UIBuilder.ProgressBarHandle Bar;
        }

        private DashboardState _state;
        private RectTransform _list;
        private TextMeshProUGUI _title, _subtitle, _cropPercent, _empty;
        private readonly List<VehicleRow> _vehicleRows = new();
        private readonly List<ZoneRow> _zoneRows = new();
        private DashView _builtView = (DashView)(-1);
        private string _vehicleSignature;

        public void Init(DashboardState state)
        {
            _state = state;

            // A dynamically stretched rounded sprite rendered as a large dark
            // circle here. This large background does not need rounded masking;
            // the individual information cards keep their rounded shapes.
            var panel = UIBuilder.Rect(transform, "LeftPanel", UITheme.PanelBg);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0.50f, 1f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = new Vector2(-10f, 0f);

            _title = Text(panel, "Title", "", UITheme.TypeDisplay38,
                UITheme.FontPathDisplay, UITheme.TextPrimary, 20, -76, -20, -18);

            var accent = UIBuilder.Rect(panel, "TitleAccent", UITheme.GreenPrimary);
            accent.anchorMin = new Vector2(0, 1);
            accent.anchorMax = new Vector2(0, 1);
            accent.pivot = new Vector2(0, 1);
            accent.anchoredPosition = new Vector2(20, -82);
            accent.sizeDelta = new Vector2(32, 4);

            _subtitle = Text(panel, "Subtitle", "", UITheme.TypeBody12,
                UITheme.FontPathBodyRegular, UITheme.TextMuted1, 20, -116, -20, -88);

            _cropPercent = Text(panel, "CropPercent", "", UITheme.TypeStat30,
                UITheme.FontPathBodyBold, UITheme.GoldBright, -170, -80, -20, -20,
                TextAlignmentOptions.TopRight);

            var scrollHost = UIBuilder.NewRect(panel, "ScrollHost");
            scrollHost.anchorMin = Vector2.zero;
            scrollHost.anchorMax = Vector2.one;
            scrollHost.offsetMin = new Vector2(20, 20);
            scrollHost.offsetMax = new Vector2(-20, -120);
            scrollHost.gameObject.AddComponent<RectMask2D>();
            var scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            _list = UIBuilder.VCol(scrollHost, "List", gap: 12,
                controlWidth: true, controlHeight: true);
            var group = _list.GetComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            _list.anchorMin = new Vector2(0, 1);
            _list.anchorMax = new Vector2(1, 1);
            _list.pivot = new Vector2(.5f, 1);
            _list.offsetMin = new Vector2(0, _list.offsetMin.y);
            _list.offsetMax = new Vector2(0, _list.offsetMax.y);
            var fitter = _list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _list;
            scroll.viewport = scrollHost;
        }

        public void Show(DashView view)
        {
            string signature = VehicleSignature(view);
            if (_builtView != view || signature != _vehicleSignature)
            {
                _builtView = view;
                _vehicleSignature = signature;
                Rebuild(view);
            }
            Refresh(view);
        }

        private void Rebuild(DashView view)
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                _list.GetChild(i).gameObject.SetActive(false);
                Destroy(_list.GetChild(i).gameObject);
            }
            _vehicleRows.Clear();
            _zoneRows.Clear();
            _empty = null;

            if (view == DashView.Cultivo)
            {
                for (int i = 0; i < 4; i++) BuildZoneRow(i);
                return;
            }

            foreach (var vehicle in _state.Vehicles)
            {
                if (view == DashView.Tractores && vehicle.Type != VehicleType.Tractor) continue;
                if (view == DashView.Cosechadoras && vehicle.Type != VehicleType.Cosechador) continue;
                BuildVehicleRow(vehicle, view == DashView.Combustible);
            }

            if (_vehicleRows.Count == 0)
            {
                _empty = UIBuilder.Text(_list, "Empty", "Esperando datos de la simulación…",
                    UITheme.TypeSubtitle16, UIBuilder.Font(UITheme.FontPathBodyRegular),
                    UITheme.TextMuted1, TextAlignmentOptions.Center);
                UIBuilder.Flex(_empty.rectTransform, 1, 0, -1, 80, -1, 80);
            }
        }

        private void BuildVehicleRow(VehicleData vehicle, bool compact)
        {
            int height = compact ? 98 : 150;
            var card = UIBuilder.Panel(_list, "Row_" + vehicle.Id, UITheme.CardBgNested,
                UITheme.RadiusRow, exactWidth: 620, exactHeight: height);
            UIBuilder.Flex(card, 1, 0, -1, height, -1, height);
            var col = UIBuilder.VCol(card, "Col", gap: 7,
                padding: new RectOffset(14, 14, 12, 12), controlWidth: true, controlHeight: true);
            Stretch(col);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

            var header = UIBuilder.HRow(col, "Header", controlWidth: true, controlHeight: true);
            UIBuilder.Flex(header, 1, 0, -1, 18, -1, 18);
            var label = UIBuilder.Text(header, "Label", vehicle.Label,
                UITheme.TypeRowLabel13, UIBuilder.Font(UITheme.FontPathBodySemiBold),
                UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex(label.rectTransform, 1, 1);
            var status = UIBuilder.Text(header, "Status", "Activo", UITheme.TypeBody12,
                UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.StatusActivo,
                TextAlignmentOptions.MidlineRight);
            UIBuilder.Flex(status.rectTransform, 0, 1, 100, -1);

            var fuelLine = ValueLine(col, "Combustible", out var fuelText);
            UIBuilder.Flex(fuelLine, 1, 0, -1, 16, -1, 16);
            var fuelBar = UIBuilder.ProgressBar(col, "FuelBar", 7, 3,
                UITheme.DividerTrackBg, UITheme.GreenPrimary, exactWidth: 620);

            var row = new VehicleRow
            {
                Id = vehicle.Id,
                Status = status,
                FuelText = fuelText,
                FuelBar = fuelBar
            };

            if (!compact)
            {
                ValueLine(col, "Posición", out row.Position);
                ValueLine(col, "Carga actual", out row.Load);
                ValueLine(col, vehicle.Type == VehicleType.Cosechador
                    ? "Trigo cosechado" : "Capacidad de carga", out row.Extra);
            }
            _vehicleRows.Add(row);
        }

        private void BuildZoneRow(int index)
        {
            var card = UIBuilder.Panel(_list, "Zone_" + index, UITheme.CardBgNested,
                UITheme.RadiusRow, exactWidth: 620, exactHeight: 88);
            UIBuilder.Flex(card, 1, 0, -1, 88, -1, 88);
            var col = UIBuilder.VCol(card, "Col", gap: 7,
                padding: new RectOffset(14, 14, 12, 12), controlWidth: true, controlHeight: true);
            Stretch(col);
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            var header = UIBuilder.HRow(col, "Header", controlWidth: true, controlHeight: true);
            UIBuilder.Flex(header, 1, 0, -1, 18, -1, 18);
            var label = UIBuilder.Text(header, "Label", "Zona " + (index + 1),
                UITheme.TypeRowLabel13, UIBuilder.Font(UITheme.FontPathBodySemiBold),
                UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex(label.rectTransform, 1, 1);
            var status = UIBuilder.Text(header, "Status", "Pendiente", UITheme.TypeBody12,
                UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted1,
                TextAlignmentOptions.MidlineRight);
            UIBuilder.Flex(status.rectTransform, 0, 1, 100, -1);
            var bar = UIBuilder.ProgressBar(col, "Progress", 7, 3,
                UITheme.DividerTrackBg, UITheme.GreenPrimary, exactWidth: 620);
            var percent = UIBuilder.Text(col, "Percent", "0% cosechado", UITheme.TypeCaption10,
                UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted2,
                TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex(percent.rectTransform, 1, 0, -1, 14, -1, 14);
            _zoneRows.Add(new ZoneRow { Status = status, Bar = bar, Percent = percent });
        }

        private void Refresh(DashView view)
        {
            _title.text = ViewTitle(view);
            _subtitle.text = view switch
            {
                DashView.Combustible => "Nivel actual de combustible por tractor y cosechadora.",
                DashView.Cultivo => "Progreso por cuatro zonas del campo, calculado con las celdas cosechadas.",
                DashView.Tractores => "Vista comparativa del estado actual de cada tractor.",
                DashView.Cosechadoras => "Vista comparativa del estado actual de cada cosechadora.",
                _ => ""
            };
            _cropPercent.text = view == DashView.Cultivo
                ? _state.CosechadoPctServer.ToString("0.#") + "%"
                : "";

            if (view == DashView.Cultivo)
            {
                for (int i = 0; i < _zoneRows.Count; i++)
                {
                    int total = _state.CropZoneTotals[i];
                    int done = _state.CropZoneHarvested[i];
                    float pct = total > 0 ? 100f * done / total : 0;
                    var row = _zoneRows[i];
                    row.Bar.SetPercent(pct, UITheme.GreenPrimary);
                    row.Percent.text = $"{pct:0.#}% cosechado  ·  {done}/{total} celdas";
                    row.Status.text = pct >= 99.99f ? "Completado" : pct > 0 ? "En progreso" : "Pendiente";
                    row.Status.color = pct >= 99.99f ? UITheme.GreenPrimary : pct > 0 ? UITheme.GoldBright : UITheme.TextMuted1;
                }
                return;
            }

            foreach (var row in _vehicleRows)
            {
                var vehicle = _state.FindVehicle(row.Id);
                if (vehicle == null) continue;
                float fuelPct = vehicle.Fuel;
                row.FuelText.text = $"{fuelPct:0.#}%  ·  {vehicle.FuelRaw:0.#}/{vehicle.FuelMax:0.#} u.";
                row.FuelBar.SetPercent(fuelPct, fuelPct <= 20 ? UITheme.StatusRed : UITheme.GreenPrimary);
                row.Status.text = FriendlyStatus(vehicle.EstadoRaw, vehicle.Status);
                row.Status.color = StatusColor(vehicle.Status, vehicle.EstadoRaw);
                if (row.Position != null) row.Position.text = $"Fila {vehicle.Fila}, columna {vehicle.Col}";
                if (row.Load != null) row.Load.text = $"{vehicle.Carga}/{vehicle.Capacity} u.";
                if (row.Extra != null) row.Extra.text = vehicle.Type == VehicleType.Cosechador
                    ? vehicle.CosechadoTotal + " celdas"
                    : vehicle.Capacity + " u.";
            }
        }

        private string VehicleSignature(DashView view)
        {
            // Generation forces a clean rebuild after every Python init/reset,
            // even if the new run happens to reuse the same internal agent IDs.
            var signature = view + "|generation:" + _state.SimulationGeneration;
            foreach (var v in _state.Vehicles) signature += "|" + v.Id + ":" + v.Type;
            return signature;
        }

        private static RectTransform ValueLine(Transform parent, string label, out TextMeshProUGUI value)
        {
            var line = UIBuilder.HRow(parent, label.Replace(" ", "") + "Line",
                controlWidth: true, controlHeight: true);
            UIBuilder.Flex(line, 1, 0, -1, 16, -1, 16);
            var caption = UIBuilder.Text(line, "Label", label, UITheme.TypeBody12,
                UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted2,
                TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex(caption.rectTransform, 1, 1);
            value = UIBuilder.Text(line, "Value", "", UITheme.TypeBody12,
                UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.LightGrayText,
                TextAlignmentOptions.MidlineRight);
            UIBuilder.Flex(value.rectTransform, 0, 1, 190, -1);
            return line;
        }

        private static string FriendlyStatus(string raw, VehicleStatus fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback.ToString();
            string value = raw.Replace('_', ' ');
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static Color StatusColor(VehicleStatus status, string raw)
        {
            if (!string.IsNullOrEmpty(raw) &&
                (raw.Contains("descompuesto") || raw.Contains("sin_gasolina"))) return UITheme.StatusRed;
            return status switch
            {
                VehicleStatus.Cargando => UITheme.StatusCargando,
                VehicleStatus.Descargando => UITheme.StatusDescargando,
                VehicleStatus.EnCamino => UITheme.StatusEnCamino,
                _ => UITheme.StatusActivo
            };
        }

        private static string ViewTitle(DashView view) => view switch
        {
            DashView.Combustible => "Combustible",
            DashView.Cultivo => "Cultivo",
            DashView.Tractores => "Tractores",
            DashView.Cosechadoras => "Cosechadoras",
            _ => ""
        };

        private static TextMeshProUGUI Text(Transform parent, string name, string content,
            float size, string font, Color color, float left, float bottom,
            float right, float top, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var text = UIBuilder.Text(parent, name, content, size, UIBuilder.Font(font), color, alignment);
            text.rectTransform.anchorMin = new Vector2(0, 1);
            text.rectTransform.anchorMax = new Vector2(1, 1);
            text.rectTransform.offsetMin = new Vector2(left, bottom);
            text.rectTransform.offsetMax = new Vector2(right, top);
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
