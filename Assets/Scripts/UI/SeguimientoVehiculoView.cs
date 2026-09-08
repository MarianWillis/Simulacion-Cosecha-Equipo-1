using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Mitad derecha de Combustible / Tractores / Cosechadoras: la camara que
    // persigue al vehiculo marcado en la lista de la izquierda, con sus datos
    // debajo. Antes esas tres pestanas dejaban media pantalla vacia.
    //
    // Quien esta marcado lo decide DashboardState.VehiculoSeguido() (la lista
    // de OperationsSectionView escribe la seleccion al hacer click), asi que
    // la tarjeta resaltada y esta camara nunca pueden discrepar.
    //
    // El slot se llama "seguimiento"; CamaraSeguimientoVehiculo le asigna su
    // RenderTexture sola, sin arrastrar nada en el Inspector.
    public class SeguimientoVehiculoView : MonoBehaviour
    {
        public const string NombreSlot = "seguimiento";
        public const float AspectoFeed = 16f / 9f;

        private DashboardState _state;
        private TextMeshProUGUI _titulo, _sub, _estado, _pos, _carga, _extra, _gasTexto;
        private UIBuilder.ProgressBarHandle _gasBarra;

        public void Init(DashboardState state)
        {
            _state = state;

            var root = (RectTransform)transform;

            // Igual que CultivoCamarasView: se sale del reparto del
            // HorizontalLayoutGroup y se encima al ContentArea completo, para
            // que OperationsSectionView siga creyendo que tiene todo el ancho
            // y su panel quede en la mitad izquierda de verdad. Ojo: si
            // alguien le agrega un LayoutElement con ignoreLayout=false, el
            // grupo lo vuelve a meter al reparto (basta uno en false).
            var ignorar = root.GetComponents<LayoutElement>();
            if (ignorar.Length == 0) root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            else foreach (var le in ignorar) le.ignoreLayout = true;

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            var grupo = root.parent != null ? root.parent.GetComponent<HorizontalLayoutGroup>() : null;
            if (grupo != null)
            {
                root.offsetMin = new Vector2(grupo.padding.left, grupo.padding.bottom);
                root.offsetMax = new Vector2(-grupo.padding.right, -grupo.padding.top);
            }
            else
            {
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }

            var panel = UIBuilder.Rect(root, "SeguimientoPanel", UITheme.PanelBg);
            panel.anchorMin = new Vector2(UITheme.SplitPanelInfo, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.offsetMin = new Vector2(UITheme.SplitSeparacion, 0f);
            panel.offsetMax = Vector2.zero;

            _titulo = UIBuilder.Text(panel, "Titulo", "Seguimiento", UITheme.TypeSectionTitle14,
                UIBuilder.Font(UITheme.FontPathBodyBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            _titulo.rectTransform.anchorMin = new Vector2(0f, 1f);
            _titulo.rectTransform.anchorMax = new Vector2(1f, 1f);
            _titulo.rectTransform.offsetMin = new Vector2(20f, -44f);
            _titulo.rectTransform.offsetMax = new Vector2(-20f, -20f);
            _titulo.raycastTarget = false;

            _sub = UIBuilder.Text(panel, "Sub", "Toca una tarjeta de la izquierda para seguirla.",
                UITheme.TypeCaption10, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted1,
                TextAlignmentOptions.MidlineLeft);
            _sub.rectTransform.anchorMin = new Vector2(0f, 1f);
            _sub.rectTransform.anchorMax = new Vector2(1f, 1f);
            _sub.rectTransform.offsetMin = new Vector2(20f, -64f);
            _sub.rectTransform.offsetMax = new Vector2(-20f, -46f);
            _sub.raycastTarget = false;

            // Video arriba (16:9 fijo, centrado con AspectRatioFitter) y la
            // ficha de datos abajo. Alto del video por anclas, no por aspecto
            // del panel: asi la ficha siempre tiene su espacio.
            var hueco = UIBuilder.Rect(panel, "Feed", UITheme.CardBgNested);
            hueco.anchorMin = new Vector2(0f, 0.42f);
            hueco.anchorMax = new Vector2(1f, 1f);
            hueco.offsetMin = new Vector2(20f, 10f);
            hueco.offsetMax = new Vector2(-20f, -74f);

            var slot = CameraFeedSlot.Create(hueco, NombreSlot, UITheme.GridFieldBg);
            var slotRt = slot.Image.rectTransform;
            slotRt.anchorMin = Vector2.zero;
            slotRt.anchorMax = Vector2.one;
            slotRt.offsetMin = Vector2.zero;
            slotRt.offsetMax = Vector2.zero;
            slot.SetAspect(AspectoFeed);

            var ficha = UIBuilder.Rect(panel, "Ficha", UITheme.CardBgNested);
            ficha.anchorMin = new Vector2(0f, 0f);
            ficha.anchorMax = new Vector2(1f, 0.42f);
            ficha.offsetMin = new Vector2(20f, 20f);
            ficha.offsetMax = new Vector2(-20f, -10f);

            var col = UIBuilder.VCol(ficha, "Col", gap: 9,
                padding: new RectOffset(16, 16, 14, 14), controlWidth: true, controlHeight: true);
            col.anchorMin = Vector2.zero;
            col.anchorMax = Vector2.one;
            col.offsetMin = Vector2.zero;
            col.offsetMax = Vector2.zero;
            col.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

            _estado = Linea(col, "Estado");
            _gasTexto = Linea(col, "Combustible");
            _gasBarra = UIBuilder.ProgressBar(col, "GasBarra", 7, 3,
                UITheme.DividerTrackBg, UITheme.GreenPrimary, exactWidth: 600);
            _pos = Linea(col, "Posición");
            _carga = Linea(col, "Carga actual");
            _extra = Linea(col, "Total");

            _state.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.Changed -= Refresh;
        }

        private static TextMeshProUGUI Linea(Transform parent, string etiqueta)
        {
            var fila = UIBuilder.HRow(parent, etiqueta.Replace(" ", "") + "Linea",
                controlWidth: true, controlHeight: true);
            UIBuilder.Flex(fila, 1, 0, -1, 16, -1, 16);

            var titulo = UIBuilder.Text(fila, "Etiqueta", etiqueta, UITheme.TypeBody12,
                UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted1,
                TextAlignmentOptions.MidlineLeft);
            UIBuilder.Flex(titulo.rectTransform, 1, 1);

            var valor = UIBuilder.Text(fila, "Valor", "—", UITheme.TypeBody12,
                UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.TextPrimary,
                TextAlignmentOptions.MidlineRight);
            UIBuilder.Flex(valor.rectTransform, 1, 1);
            return valor;
        }

        private void Refresh()
        {
            var v = _state.VehiculoSeguido();
            if (v == null)
            {
                _titulo.text = "Seguimiento";
                _sub.text = "Esperando datos de la simulación…";
                _estado.text = _pos.text = _carga.text = _extra.text = "—";
                _gasTexto.text = "—";
                _gasBarra.SetPercent(0f, UITheme.DividerTrackBg);
                return;
            }

            _titulo.text = v.Label;
            _sub.text = "Toca otra tarjeta de la izquierda para cambiar de cámara.";
            _estado.text = EstadoTexto(v.Status);
            _estado.color = v.Status == VehicleStatus.Descargando ? UITheme.StatusDescargando
                : v.Status == VehicleStatus.Cargando ? UITheme.StatusCargando
                : v.Status == VehicleStatus.EnCamino ? UITheme.StatusEnCamino
                : UITheme.StatusActivo;
            _gasTexto.text = $"{v.Fuel:0.#}%  ·  {v.FuelRaw:0.#}/{v.FuelMax:0.#} u.";
            _gasBarra.SetPercent(v.Fuel, v.Fuel <= 20f ? UITheme.StatusRed : UITheme.GreenPrimary);
            _pos.text = $"Fila {v.Fila}, columna {v.Col}";
            _carga.text = $"{v.Carga}/{v.Capacity} u.";
            _extra.text = v.Type == VehicleType.Cosechador
                ? v.CosechadoTotal + " celdas cosechadas"
                : v.Capacity + " u. de capacidad";
        }

        private static string EstadoTexto(VehicleStatus s) => s switch
        {
            VehicleStatus.EnCamino => "En camino",
            VehicleStatus.Cargando => "Cargando",
            VehicleStatus.Descargando => "Descargando",
            _ => "Activo",
        };
    }
}
