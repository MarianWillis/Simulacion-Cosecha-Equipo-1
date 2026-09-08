using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Mitad derecha de la vista Cultivo: una camara por zona con el
    // porcentaje cosechado de esa zona. OperationsSectionView ocupa la
    // mitad izquierda del ContentArea y deja esta libre.
    //
    // Las zonas son las MISMAS que usa el panel izquierdo: los 4
    // cuadrantes del campo (ver SimulationDataAdapter.CropZone), asi que
    // las tarjetas van en una rejilla 2x2 colocada igual que el campo:
    //
    //     Zona 1 | Zona 2
    //     -------+-------
    //     Zona 3 | Zona 4
    //
    // Los slots se llaman "zona-1".."zona-4"; CamarasZonasCultivo les
    // asigna su RenderTexture sola, sin arrastrar nada a mano.
    public class CultivoCamarasView : MonoBehaviour
    {
        public const int NumZonas = 4;

        private DashboardState _state;
        private readonly TextMeshProUGUI[] _porcentajes = new TextMeshProUGUI[NumZonas];

        public void Init(DashboardState state)
        {
            _state = state;

            var root = (RectTransform)transform;

            // ContentArea es un HorizontalLayoutGroup que reparte el ancho
            // entre las vistas activas. Si esta vista participa del reparto,
            // OperationsSectionView se queda con media pantalla y ancla su
            // panel a la mitad de esa mitad (un cuarto), dejando un hueco
            // enorme en medio. Se sale del layout y se encima al ContentArea
            // completo: asi la vista de al lado sigue creyendo que tiene todo
            // el ancho (y su panel queda en la mitad izquierda de verdad).
            // ignoreLayout en TODOS los LayoutElement del objeto: el grupo
            // incluye al hijo si cualquiera de ellos lo tiene en false.
            var ignorar = root.GetComponents<LayoutElement>();
            if (ignorar.Length == 0) root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            else foreach (var le in ignorar) le.ignoreLayout = true;

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            // Al salirse del layout, el padding del ContentArea deja de
            // aplicarse a esta vista: se replica a mano para que el panel de
            // camaras quede alineado con el de la izquierda (que si lo recibe
            // del grupo) y no se pegue al borde de la pantalla.
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

            var panel = UIBuilder.Rect(root, "CamarasPanel", UITheme.PanelBg);
            panel.anchorMin = new Vector2(UITheme.SplitPanelInfo, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.offsetMin = new Vector2(UITheme.SplitSeparacion, 0f);
            panel.offsetMax = Vector2.zero;

            var titulo = UIBuilder.Text(panel, "Titulo", "Cámaras por zona", UITheme.TypeSectionTitle14,
                UIBuilder.Font(UITheme.FontPathBodyBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            titulo.rectTransform.anchorMin = new Vector2(0f, 1f);
            titulo.rectTransform.anchorMax = new Vector2(1f, 1f);
            titulo.rectTransform.offsetMin = new Vector2(20f, -46f);
            titulo.rectTransform.offsetMax = new Vector2(-20f, -20f);
            titulo.raycastTarget = false;

            // Contenedor de las tarjetas, debajo del titulo. Anclas directas,
            // sin LayoutGroups anidados (en este proyecto ya dieron
            // demasiados problemas de tamano).
            var contenido = UIBuilder.NewRect(panel, "Tarjetas");
            contenido.anchorMin = Vector2.zero;
            contenido.anchorMax = Vector2.one;
            contenido.offsetMin = new Vector2(20f, 20f);
            contenido.offsetMax = new Vector2(-20f, -56f);

            for (int i = 0; i < NumZonas; i++)
                ConstruirTarjeta(contenido, i);

            _state.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.Changed -= Refresh;
        }

        private void ConstruirTarjeta(Transform parent, int indice)
        {
            const float separacion = 6f;

            var tarjeta = UIBuilder.Rect(parent, $"Zona{indice + 1}", UITheme.CardBgNested);
            var rt = (RectTransform)tarjeta;
            // Rejilla 2x2 puesta como el campo: la zona 1 arriba-izquierda,
            // la 4 abajo-derecha (mismo orden que SimulationDataAdapter).
            int fila = indice / 2;
            int columna = indice % 2;
            rt.anchorMin = new Vector2(columna / 2f, 1f - (fila + 1) / 2f);
            rt.anchorMax = new Vector2((columna + 1) / 2f, 1f - fila / 2f);
            rt.offsetMin = new Vector2(separacion, separacion);
            rt.offsetMax = new Vector2(-separacion, -separacion);

            // Mismo color que la tarjeta: la imagen se mete con
            // AspectRatioFitter (CameraFeedSlot.SetAspect) respetando el
            // aspecto del cuadrante, y lo que sobra a los lados tiene que
            // leerse como margen de la tarjeta, no como barras negras.
            var feedHost = UIBuilder.Rect(rt, "Feed", UITheme.CardBgNested);
            feedHost.anchorMin = Vector2.zero;
            feedHost.anchorMax = Vector2.one;
            feedHost.offsetMin = new Vector2(8f, 26f);
            feedHost.offsetMax = new Vector2(-8f, -8f);

            var slot = CameraFeedSlot.Create(feedHost, NombreSlot(indice), UITheme.GridFieldBg);
            var slotRt = slot.Image.rectTransform;
            slotRt.anchorMin = Vector2.zero;
            slotRt.anchorMax = Vector2.one;
            slotRt.offsetMin = Vector2.zero;
            slotRt.offsetMax = Vector2.zero;

            var etiqueta = UIBuilder.Text(rt, "Etiqueta", $"Zona {indice + 1}", UITheme.TypeCaption10,
                UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.TextMuted2, TextAlignmentOptions.MidlineLeft);
            etiqueta.rectTransform.anchorMin = new Vector2(0f, 0f);
            etiqueta.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            etiqueta.rectTransform.offsetMin = new Vector2(10f, 4f);
            etiqueta.rectTransform.offsetMax = new Vector2(0f, 24f);
            etiqueta.raycastTarget = false;

            // Solo el porcentaje: el detalle de celdas ya lo muestra el panel
            // de la izquierda, y en poco ancho los dos textos se encimaban.
            var pct = UIBuilder.Text(rt, "Porcentaje", "0%", UITheme.TypeCaption10,
                UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.TextMuted2, TextAlignmentOptions.MidlineRight);
            pct.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            pct.rectTransform.anchorMax = new Vector2(1f, 0f);
            pct.rectTransform.offsetMin = new Vector2(0f, 4f);
            pct.rectTransform.offsetMax = new Vector2(-10f, 24f);
            pct.raycastTarget = false;
            _porcentajes[indice] = pct;
        }

        public static string NombreSlot(int indice) => "zona-" + (indice + 1);

        private void Refresh()
        {
            for (int i = 0; i < NumZonas; i++)
            {
                if (_porcentajes[i] == null) continue;

                int total = _state.CropZoneTotals[i];
                int hechas = _state.CropZoneHarvested[i];
                float pct = total > 0 ? 100f * hechas / total : 0f;

                _porcentajes[i].text = $"{pct:0.#}%";
                _porcentajes[i].color = pct >= 99.99f ? UITheme.GreenPrimary
                    : pct > 0f ? UITheme.GoldBright
                    : UITheme.TextMuted2;
            }
        }
    }
}
