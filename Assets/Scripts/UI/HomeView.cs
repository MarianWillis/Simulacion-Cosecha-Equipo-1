using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // README section 3: two-column Home layout -- "Vista general" camera card
    // (a CameraFeedSlot reserved for the real 3D Unity camera feed, not a 2D
    // abstraction) on the left, Configuración + Flota cards on the right.
    // Built once in Init(); Refresh() (on every DashboardState.Changed)
    // updates fuel bars and button states without rebuilding the cards.
    public class HomeView : MonoBehaviour
    {
        private DashboardState _state;
        private DashboardBootstrap _bootstrap;

        private RectTransform _gridCanvas;
        private Button _resumeButton;
        private ButtonHoverColor _resumeHover;
        private TextMeshProUGUI _resumeLabel;
        private Button _pauseButton;
        private ButtonHoverColor _pauseHover;
        private TextMeshProUGUI _pauseLabel;
        private TMP_InputField _rowsField;
        private TMP_InputField _colsField;
        private TMP_InputField _harvestersField;
        private TMP_InputField _tractorsField;
        private TMP_InputField _harvesterCapacityField;
        private TMP_InputField _tractorCapacityField;
        private int _lastSimulationGeneration = -1;

        private RectTransform _fleetList;
        private readonly Dictionary<string, (Image dot, TextMeshProUGUI status, UIBuilder.ProgressBarHandle fuel, TextMeshProUGUI fuelPct, TextMeshProUGUI rounds)> _fleetRows = new();
        private int _lastVehicleCount = -1;

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

            // Configuración (~690px con la rejilla 2x5, ver ConfigAltoTarjeta)
            // + Flota pueden pasarse de la altura disponible en una ventana
            // chica -- wrap the whole right column in a scroll view (same
            // pattern as the Flota list's own internal scroll) instead of
            // letting it overflow/get cut off at the bottom of the screen.
            var rightColHost = UIBuilder.NewRect(layout, "RightColumnHost");
            UIBuilder.Flex(rightColHost, 1, 1, 260, -1);
            rightColHost.gameObject.AddComponent<RectMask2D>();
            var rightScrollRect = rightColHost.gameObject.AddComponent<ScrollRect>();
            rightScrollRect.horizontal = false;
            rightScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var rightCol = UIBuilder.VCol(rightColHost, "RightColumn", gap: UITheme.GapMajor, controlWidth: true, controlHeight: true, forceExpand: true);
            rightCol.anchorMin = new Vector2(0f, 1f);
            rightCol.anchorMax = new Vector2(1f, 1f);
            rightCol.pivot = new Vector2(0.5f, 1f);
            // Reset offsets after re-anchoring -- leaving stale ones from the
            // default 100x100 rect is exactly what clipped the Flota row text
            // earlier this session.
            rightCol.offsetMin = new Vector2(0f, rightCol.offsetMin.y);
            rightCol.offsetMax = new Vector2(0f, rightCol.offsetMax.y);
            var rightColFitter = rightCol.gameObject.AddComponent<ContentSizeFitter>();
            rightColFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rightScrollRect.content = rightCol;
            rightScrollRect.viewport = rightColHost;

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

            // Reserved for the real Unity camera feed (RenderTexture) of the 3D
            // farm scene -- not a 2D abstraction. Whoever wires up the actual
            // camera rig assigns it via CameraFeedSlot.Find("general").SetFeed(...).
            _gridCanvas = UIBuilder.Panel(col, "GridCanvas", UITheme.GridFieldBg, UITheme.RadiusNested, exactWidth: 660, exactHeight: 420);
            UIBuilder.Flex(_gridCanvas, 1, 1, -1, 320);
            var feedSlot = CameraFeedSlot.Create(_gridCanvas, "general", UITheme.GridFieldBg);
            feedSlot.Image.rectTransform.anchorMin = Vector2.zero;
            feedSlot.Image.rectTransform.anchorMax = Vector2.one;
            feedSlot.Image.rectTransform.offsetMin = Vector2.zero;
            feedSlot.Image.rectTransform.offsetMax = Vector2.zero;
        }

        // Medidas de la tarjeta "Configuracion". Estan aca arriba y no
        // regadas en el cuerpo porque AltoTarjeta es la suma de las otras, y
        // si una cambia sin la otra la tarjeta corta el ultimo boton (el
        // panel no tiene LayoutGroup propio, asi que no deduce su alto de lo
        // que lleva dentro -- ver el comentario de abajo).
        private const float ConfigPadding = 16f;
        private const float ConfigGap = 12f;
        private const float ConfigAltoTitulo = 26f;
        private const float ConfigAltoBoton = 40f;
        // Los 6 parametros de nombre corto van de a dos por renglon; los 4 de
        // nombre largo, uno por renglon. La columna derecha del Home mide
        // ~110px por mitad, y ahi "Capacidad Cosechadora" no entra ni
        // partida en dos: lado a lado el nombre se cortaba y quedaba un
        // campo sin titulo legible.
        private const int ConfigFilasDobles = 3;
        private const int ConfigFilasSimples = 4;

        private static float ConfigAltoTarjeta(float altoDoble, float altoSimple)
        {
            // titulo + 3 filas dobles + 4 simples + separador + 3 botones
            // = 12 hijos => 11 huecos.
            float contenido = ConfigAltoTitulo
                              + ConfigFilasDobles * altoDoble
                              + ConfigFilasSimples * altoSimple
                              + 1f
                              + 3f * ConfigAltoBoton;
            int huecos = ConfigFilasDobles + ConfigFilasSimples + 4;
            return contenido + huecos * ConfigGap + 2f * ConfigPadding;
        }

        private void BuildConfigCard(Transform parent)
        {
            // Misma paleta oscura que el resto del dashboard, pero es la
            // unica tarjeta con la que se INTERACTUA (leer un parametro,
            // escribir un numero), asi que sube el contraste de la etiqueta y
            // el tamano de todo: 10/12px en gris apagado no se leen comodos.
            // Los tokens y sus contrastes medidos viven en UITheme (bloque
            // "Tarjeta Configuracion").
            var estilo = UIBuilder.FieldStyle.Config();
            var estiloAncho = UIBuilder.FieldStyle.ConfigAncha();
            float altoFila = estilo.HostHeight;
            int altoTarjeta = Mathf.RoundToInt(ConfigAltoTarjeta(altoFila, estiloAncho.HostHeight));

            var card = UIBuilder.Panel(parent, "ConfigCard", UITheme.ConfigPanelBg, UITheme.RadiusCard, exactWidth: 300, exactHeight: altoTarjeta, borderColor: UITheme.ConfigPanelBorder);
            var col = UIBuilder.VCol(card, "Col", gap: ConfigGap, padding: new RectOffset((int)ConfigPadding, (int)ConfigPadding, (int)ConfigPadding, (int)ConfigPadding), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;
            // card has no LayoutGroup of its own, so its reported height (when
            // unset) doesn't auto-detect from col's children -- it would fall
            // back to some small default, squishing everything inside. Explicit
            // height from ConfigAltoTarjeta instead.
            UIBuilder.Flex(card, 0, 0, -1, altoTarjeta, -1, altoTarjeta);

            var title = UIBuilder.Text(col, "Title", "Configuración", UITheme.TypeCardTitle20, UIBuilder.Font(UITheme.FontPathBodyBold), UITheme.ConfigTextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)title.transform, 1, 0, -1, ConfigAltoTitulo, -1, ConfigAltoTitulo);

            // Same 10 parameters the old (already-working) input-fields panel
            // sends via PanelControlSimulacion/ConexionSimulacion.EnviarReiniciar
            // -- see Puente/MensajesDTO.cs ParametrosReinicioDTO.
            // Antes limitado a 3-8: resabio de un demo chico que ya no aplica
            // -- confirmado que un grid 30x30 funciona bien. El techo real lo
            // pone granja.py (manda "error" si los parametros no sirven, ver
            // ConexionSimulacion.ProcesarMensaje), no un numero fijo aca.
            //
            // De a DOS por renglon los de nombre corto: ningun valor pasa de
            // 4 digitos, asi que una fila entera por parametro desperdiciaba
            // casi todo el ancho y estiraba la tarjeta a 820px (habia que
            // hacer scroll para llegar a los botones).
            var fila1 = FilaParametros(col, "FilaCampo", altoFila);
            _rowsField = UIBuilder.NumberField(fila1, "Filas", "Filas", _state.Config.Rows, v => _state.Config.Rows = Mathf.Clamp(v, 3, 60), estilo);
            _colsField = UIBuilder.NumberField(fila1, "Columnas", "Columnas", _state.Config.Cols, v => _state.Config.Cols = Mathf.Clamp(v, 3, 60), estilo);

            var fila2 = FilaParametros(col, "FilaFlota", altoFila);
            // Sin techo y con piso de 1: el 4 de antes no venia de ningun lado
            // -- EnviarReiniciar manda n_harvesters/n_tractores tal cual (ver
            // Puente/ConexionSimulacion.cs), asi que ese Clamp era el UNICO
            // limite que habia y no dejaba probar flotas mas grandes. Quien
            // decide si un numero grande sirve es granja.py, que responde
            // "error" (ver ConexionSimulacion.ProcesarMensaje). El 1 si se
            // queda: con cero cosechadoras no se cosecha nada y con cero
            // tractores no se descarga, o sea una corrida que no avanza.
            _harvestersField = UIBuilder.NumberField(fila2, "Cosechadores", "Cosechadores", _state.Config.Cosechadores, v => _state.Config.Cosechadores = Mathf.Max(v, 1), estilo);
            _tractorsField = UIBuilder.NumberField(fila2, "Tractores", "Tractores", _state.Config.Tractores, v => _state.Config.Tractores = Mathf.Max(v, 1), estilo);

            var fila3 = FilaParametros(col, "FilaCorrida", altoFila);
            UIBuilder.NumberField(fila3, "Pasos", "Pasos", _state.Config.Pasos, v => _state.Config.Pasos = Mathf.Max(v, 20), estilo);
            UIBuilder.NumberField(fila3, "Semilla", "Semilla", _state.Config.Semilla, v => _state.Config.Semilla = v, estilo);

            // Estos cuatro, APILADOS: sus nombres no entran en media tarjeta
            // (~110px), y un campo sin titulo legible no sirve de nada por
            // mas que se ahorren dos renglones. Van directo a `col`, sin fila
            // intermedia -- el VerticalLayoutGroup ya los estira al ancho
            // completo. Los dos porcentajes van de 0 a 100, asi que el "%"
            // dice lo mismo que "Porcentaje de" en un caracter.
            UIBuilder.NumberFieldFloat(col, "ProbDescompostura", "Descompostura %", _state.Config.ProbDescompostura, v => _state.Config.ProbDescompostura = Mathf.Clamp(v, 0f, 100f), estiloAncho);
            UIBuilder.NumberFieldFloat(col, "PctObstaculos", "Obstáculos %", _state.Config.PctObstaculos, v => _state.Config.PctObstaculos = Mathf.Clamp(v, 0f, 100f), estiloAncho);
            _harvesterCapacityField = UIBuilder.NumberField(col, "CapacidadCosechador", "Capacidad Cosechadora", _state.Config.CapacidadCosechador, v => _state.Config.CapacidadCosechador = Mathf.Max(v, 1), estiloAncho);
            _tractorCapacityField = UIBuilder.NumberField(col, "CapacidadTractor", "Capacidad Tractor", _state.Config.CapacidadTractor, v => _state.Config.CapacidadTractor = Mathf.Max(v, 1), estiloAncho);

            var divider = UIBuilder.Rect(col, "Divider", UITheme.ConfigDivider);
            UIBuilder.Flex(divider, 1, 0, -1, 1, -1, 1);

            // Hover color is a lighter tint rather than matching Normal exactly,
            // so there's visible feedback even before the press-darken kicks in.
            // RefreshButtons() below must go through
            // ButtonHoverColor.SetBaseColor(), not set .color directly -- otherwise
            // moving the mouse off the button after a state change would snap it
            // back to whatever Normal color the button was CREATED with, not the
            // current enabled/disabled color.
            //
            // Los tres llevan relleno propio: el RESTART "outline" de antes
            // (fondo transparente) se confundia con la tarjeta ahora que los
            // botones son mas altos y estan mas juntos.
            _resumeButton = BotonConfig(col, "ResumeBtn", UITheme.GreenPrimary, Hex("#5fb857"));
            _resumeHover = _resumeButton.GetComponent<ButtonHoverColor>();
            _resumeLabel = EtiquetaBoton(_resumeButton, "REANUDAR", UITheme.OnGreenText);
            _resumeButton.onClick.AddListener(() => _bootstrap.StartRun());

            _pauseButton = BotonConfig(col, "PauseBtn", UITheme.ConfigNeutralBtnBg, UITheme.ConfigNeutralBtnHover);
            _pauseHover = _pauseButton.GetComponent<ButtonHoverColor>();
            _pauseLabel = EtiquetaBoton(_pauseButton, "PAUSAR", UITheme.ConfigBtnText);
            _pauseButton.onClick.AddListener(() => _bootstrap.PauseRun());

            var restartButton = BotonConfig(col, "RestartBtn", UITheme.ConfigOutlineBtnBg, UITheme.ConfigOutlineBtnHover);
            EtiquetaBoton(restartButton, "REINICIAR", UITheme.ConfigBtnText);
            restartButton.onClick.AddListener(() => _bootstrap.RestartRun());
        }

        // Un renglon de la rejilla: dos parametros repartiendose el ancho a
        // partes iguales. El reparto exacto lo consigue FieldStyle.Clara() con
        // PreferredWidth = 0 -- sin eso la columna de la etiqueta mas larga se
        // quedaba con mas ancho que la otra.
        private static RectTransform FilaParametros(Transform parent, string name, float alto)
        {
            var row = UIBuilder.HRow(parent, name, gap: ConfigGap, controlWidth: true, controlHeight: true, forceExpand: true);
            UIBuilder.Flex(row, 1, 0, -1, alto, -1, alto);
            return row;
        }

        private static Button BotonConfig(Transform parent, string name, Color normal, Color hover)
        {
            var btn = UIBuilder.Button(parent, name, normal, hover, UITheme.RadiusButton, exactWidth: 268, exactHeight: (int)ConfigAltoBoton);
            UIBuilder.Flex((RectTransform)btn.transform, 1, 0, -1, ConfigAltoBoton, -1, ConfigAltoBoton);
            return btn;
        }

        private static TextMeshProUGUI EtiquetaBoton(Button btn, string texto, Color color)
        {
            var label = UIBuilder.Text(btn.transform, "Label", texto, UITheme.TypeConfigButton13, UIBuilder.Font(UITheme.FontPathBodyExtraBold), color, TextAlignmentOptions.Center);
            StretchFill((RectTransform)label.transform);
            return label;
        }

        private void BuildFleetCard(Transform parent)
        {
            var card = UIBuilder.Panel(parent, "FleetCard", UITheme.PanelBg, UITheme.RadiusCard, exactWidth: 300, exactHeight: 300, borderColor: UITheme.PanelBorder);
            // Fixed height, not flexible: now that the whole right column lives
            // inside a ContentSizeFitter'd scroll view (see Init()), "flexible,
            // fills remaining space" doesn't apply -- there's no fixed remaining
            // space to fill, the column just grows and the scrollbar handles
            // overflow. The card's own list still scrolls internally if the
            // fleet has more vehicles than fit in 300px.
            UIBuilder.Flex(card, 0, 0, -1, 300, -1, 300);
            var col = UIBuilder.VCol(card, "Col", gap: 12, padding: new RectOffset(16, 16, 16, 16), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;
            // forceExpand:true stretches ALL children on BOTH axes regardless of
            // their own flexible weight (see StatusDot-style bug elsewhere) -- that
            // was pushing the fixed-height "Flota" title to fill leftover vertical
            // space too, centering it and shoving the scroll list past the card's
            // fixed 300px, clipping it. Height should only force-expand the
            // scrollHost (which asks for it via Flex's flexible height).
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

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

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
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
            RefreshConfigurationFromSimulation();
            RefreshButtons();
            RefreshFleet();
        }

        private void RefreshConfigurationFromSimulation()
        {
            if (_lastSimulationGeneration == _state.SimulationGeneration) return;
            _lastSimulationGeneration = _state.SimulationGeneration;
            if (_state.SimulationGeneration == 0) return;

            _rowsField?.SetTextWithoutNotify(_state.Config.Rows.ToString());
            _colsField?.SetTextWithoutNotify(_state.Config.Cols.ToString());
            _harvestersField?.SetTextWithoutNotify(_state.Config.Cosechadores.ToString());
            _tractorsField?.SetTextWithoutNotify(_state.Config.Tractores.ToString());
            _harvesterCapacityField?.SetTextWithoutNotify(_state.Config.CapacidadCosechador.ToString());
            _tractorCapacityField?.SetTextWithoutNotify(_state.Config.CapacidadTractor.ToString());
        }

        private void RefreshButtons()
        {
            bool running = _state.Running;
            // Deshabilitado = relleno mas apagado que el normal y letra gris:
            // sin eso un boton inactivo pesa igual que el activo.
            _resumeHover.SetBaseColor(running ? UITheme.ConfigDisabledBtnBg : UITheme.GreenPrimary);
            _resumeLabel.color = running ? UITheme.ConfigDisabledBtnText : UITheme.OnGreenText;
            _resumeButton.interactable = !running;

            _pauseHover.SetBaseColor(running ? UITheme.GoldBright : UITheme.ConfigDisabledBtnBg);
            _pauseLabel.color = running ? UITheme.OnGoldText : UITheme.ConfigDisabledBtnText;
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
            // Content (header 16 + fuel label 12 + fuel bar 6 + rounds 16, with
            // 8px gaps between each) adds up to ~74px on its own -- 84 barely
            // left room for the padding, let alone breathing room at the bottom.
            var row = UIBuilder.Panel(_fleetList, $"Row_{id}", UITheme.CardBgNested, UITheme.RadiusRow, exactWidth: 280, exactHeight: 106);
            UIBuilder.Flex(row, 1, 0, -1, 106, -1, 106);
            var col = UIBuilder.VCol(row, "Col", gap: 8, padding: new RectOffset(12, 16, 12, 18), controlWidth: true, controlHeight: true, forceExpand: true);
            col.anchorMin = Vector2.zero; col.anchorMax = Vector2.one; col.offsetMin = Vector2.zero; col.offsetMax = Vector2.zero;

            var headerRow = UIBuilder.HRow(col, "Header", controlWidth: true, controlHeight: true);
            UIBuilder.Flex(headerRow, 1, 0, -1, 16, -1, 16);
            var labelText = UIBuilder.Text(headerRow, "Label", label, UITheme.TypeRowLabel13, UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex((RectTransform)labelText.transform, 1, 1);
            // controlWidth:false here doesn't mean "leave the dot at its own
            // 6px" -- it means the group never applies that width at all, so the
            // dot's RectTransform kept Unity's default ~100 width and rendered as
            // a bar smeared across the label text (same bug as the header pills).
            var statusRow = UIBuilder.HRow(headerRow, "Status", gap: 6, controlWidth: true, controlHeight: true, align: TextAnchor.MiddleRight);
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
