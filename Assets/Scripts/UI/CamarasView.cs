using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Pestana "Camaras": cuatro angulos EN PRIMER PLANO del campo, para ver
    // lo que la vista cenital de Home no deja ver (a que distancia van los
    // agentes, como entran al silo, el relieve del terreno).
    //
    //     Cosechadora (persecucion) | Tractor (persecucion)
    //     --------------------------+----------------------
    //     Silo (fija, angulo bajo)  | Dron (orbita lenta)
    //
    // Ocupa todo el ContentArea (no comparte pantalla con el panel de
    // informacion, a diferencia de Cultivo). Los slots se llaman
    // "angulo-1".."angulo-4" y CamarasAngulos les asigna su RenderTexture
    // sola, sin arrastrar nada en el Inspector.
    public class CamarasView : MonoBehaviour
    {
        public const int NumAngulos = 4;

        // Aspecto de las tarjetas de video. Fijo (no sale del tamano de la
        // tarjeta) para no recrear las RenderTextures cada vez que cambia el
        // tamano de la ventana: la imagen se centra con AspectRatioFitter.
        public const float AspectoFeed = 16f / 9f;

        // Que muestra cada angulo. El orden manda: es el mismo indice que
        // usa CamarasAngulos para colocar sus camaras.
        private static readonly string[] Titulos =
        {
            "Cosechadora", "Tractor", "Silo", "Dron",
        };
        private static readonly string[] Etiquetas =
        {
            "PERSECUCION", "PERSECUCION", "FIJA", "ORBITA",
        };

        private DashboardState _state;
        private readonly TextMeshProUGUI[] _titulos = new TextMeshProUGUI[NumAngulos];
        private readonly TextMeshProUGUI[] _pies = new TextMeshProUGUI[NumAngulos];

        public void Init(DashboardState state)
        {
            _state = state;

            var panel = UIBuilder.Rect(transform, "CamarasPanel", UITheme.PanelBg);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            // Mismo encabezado que OperationsSectionView (titulo + acento +
            // subtitulo) para que la pestana no se sienta de otra app.
            var titulo = UIBuilder.Text(panel, "Titulo", "Cámaras", UITheme.TypeDisplay38,
                UIBuilder.Font(UITheme.FontPathDisplay), UITheme.TextPrimary, TextAlignmentOptions.TopLeft);
            titulo.rectTransform.anchorMin = new Vector2(0f, 1f);
            titulo.rectTransform.anchorMax = new Vector2(1f, 1f);
            titulo.rectTransform.offsetMin = new Vector2(20f, -76f);
            titulo.rectTransform.offsetMax = new Vector2(-20f, -18f);
            titulo.raycastTarget = false;

            var acento = UIBuilder.Rect(panel, "TituloAcento", UITheme.GreenPrimary);
            acento.anchorMin = new Vector2(0f, 1f);
            acento.anchorMax = new Vector2(0f, 1f);
            acento.pivot = new Vector2(0f, 1f);
            acento.anchoredPosition = new Vector2(20f, -82f);
            acento.sizeDelta = new Vector2(32f, 4f);

            var subtitulo = UIBuilder.Text(panel, "Subtitulo",
                "Cuatro ángulos en primer plano: dos siguiendo a los agentes, el silo y una órbita sobre el campo.",
                UITheme.TypeBody12, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.TextMuted1,
                TextAlignmentOptions.MidlineLeft);
            subtitulo.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitulo.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitulo.rectTransform.offsetMin = new Vector2(20f, -116f);
            subtitulo.rectTransform.offsetMax = new Vector2(-20f, -88f);
            subtitulo.raycastTarget = false;

            var rejilla = UIBuilder.NewRect(panel, "Angulos");
            rejilla.anchorMin = Vector2.zero;
            rejilla.anchorMax = Vector2.one;
            rejilla.offsetMin = new Vector2(20f, 20f);
            rejilla.offsetMax = new Vector2(-20f, -124f);

            for (int i = 0; i < NumAngulos; i++) ConstruirTarjeta(rejilla, i);

            _state.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.Changed -= Refresh;
        }

        private void ConstruirTarjeta(Transform parent, int indice)
        {
            const float separacion = 8f;

            var tarjeta = UIBuilder.Rect(parent, $"Angulo{indice + 1}", UITheme.CardBgNested);
            int fila = indice / 2;
            int columna = indice % 2;
            tarjeta.anchorMin = new Vector2(columna / 2f, 1f - (fila + 1) / 2f);
            tarjeta.anchorMax = new Vector2((columna + 1) / 2f, 1f - fila / 2f);
            tarjeta.offsetMin = new Vector2(separacion, separacion);
            tarjeta.offsetMax = new Vector2(-separacion, -separacion);

            var titulo = UIBuilder.Text(tarjeta, "Titulo", Titulos[indice], UITheme.TypeRowLabel13,
                UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            titulo.rectTransform.anchorMin = new Vector2(0f, 1f);
            titulo.rectTransform.anchorMax = new Vector2(0.6f, 1f);
            titulo.rectTransform.offsetMin = new Vector2(12f, -30f);
            titulo.rectTransform.offsetMax = new Vector2(0f, -8f);
            titulo.raycastTarget = false;
            _titulos[indice] = titulo;

            var etiqueta = UIBuilder.Text(tarjeta, "Etiqueta", Etiquetas[indice], UITheme.TypeCaption10,
                UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.TextMuted2, TextAlignmentOptions.MidlineRight);
            etiqueta.rectTransform.anchorMin = new Vector2(0.4f, 1f);
            etiqueta.rectTransform.anchorMax = new Vector2(1f, 1f);
            etiqueta.rectTransform.offsetMin = new Vector2(0f, -30f);
            etiqueta.rectTransform.offsetMax = new Vector2(-12f, -8f);
            etiqueta.raycastTarget = false;

            // Hueco del video: entre el titulo y el pie. Mismo color que la
            // tarjeta para que las franjas del AspectRatioFitter se lean
            // como margen y no como barras negras.
            var hueco = UIBuilder.Rect(tarjeta, "Feed", UITheme.CardBgNested);
            hueco.anchorMin = Vector2.zero;
            hueco.anchorMax = Vector2.one;
            hueco.offsetMin = new Vector2(8f, 28f);
            hueco.offsetMax = new Vector2(-8f, -32f);

            var slot = CameraFeedSlot.Create(hueco, NombreSlot(indice), UITheme.GridFieldBg);
            var slotRt = slot.Image.rectTransform;
            slotRt.anchorMin = Vector2.zero;
            slotRt.anchorMax = Vector2.one;
            slotRt.offsetMin = Vector2.zero;
            slotRt.offsetMax = Vector2.zero;
            slot.SetAspect(AspectoFeed);

            var pie = UIBuilder.Text(tarjeta, "Pie", "", UITheme.TypeCaption10,
                UIBuilder.Font(UITheme.FontPathMonoRegular), UITheme.TextMuted2, TextAlignmentOptions.MidlineLeft);
            pie.rectTransform.anchorMin = new Vector2(0f, 0f);
            pie.rectTransform.anchorMax = new Vector2(1f, 0f);
            pie.rectTransform.offsetMin = new Vector2(12f, 4f);
            pie.rectTransform.offsetMax = new Vector2(-12f, 24f);
            pie.raycastTarget = false;
            _pies[indice] = pie;
        }

        public static string NombreSlot(int indice) => "angulo-" + (indice + 1);

        // Vehiculo que sigue cada camara de persecucion. CamarasAngulos elige
        // el PRIMER harvester y el PRIMER tractor del "init", y DashboardState
        // llena Vehicles en ese mismo orden (ver SimulationDataAdapter.OnInit),
        // asi que "el primero de este tipo" es el mismo de los dos lados.
        private VehicleData Seguido(VehicleType tipo)
        {
            foreach (var v in _state.Vehicles)
                if (v.Type == tipo) return v;
            return null;
        }

        private void Refresh()
        {
            ActualizarSeguido(0, VehicleType.Cosechador);
            ActualizarSeguido(1, VehicleType.Tractor);

            if (_pies[2] != null)
                _pies[2].text = $"Entregado {_state.GranoEntregado} · {_state.RecargasTotales} recargas";
            if (_pies[3] != null)
                _pies[3].text = $"Campo {_state.Config.Rows}x{_state.Config.Cols} · {_state.CosechadoPctServer:0.#}% cosechado";
        }

        private void ActualizarSeguido(int indice, VehicleType tipo)
        {
            var v = Seguido(tipo);
            if (_titulos[indice] != null)
                _titulos[indice].text = v != null ? v.Label : Titulos[indice];

            if (_pies[indice] == null) return;
            if (v == null)
            {
                _pies[indice].text = "Sin agentes";
                _pies[indice].color = UITheme.TextMuted2;
                return;
            }

            _pies[indice].text = $"{EstadoTexto(v.Status)} · carga {v.Carga}/{v.Capacity} · gas {v.Fuel:0}%";
            _pies[indice].color = v.Fuel <= 15f ? UITheme.StatusRed : UITheme.TextMuted2;
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
