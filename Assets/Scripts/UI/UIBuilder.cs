using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Small code-first "component library" for building the dashboard's uGUI
    // hierarchy from C#, so nothing needs to be hand-wired in the Inspector.
    // Mirrors the CSS primitives the design spec is written in: rounded panels,
    // flex rows/columns (via LayoutGroup + LayoutElement), text, buttons, bars.
    public static class UIBuilder
    {
        private static readonly Dictionary<int, Sprite> RoundedSpriteCache = new();
        private static readonly Dictionary<(int w, int h, int r), Sprite> ExactRoundedSpriteCache = new();
        private static readonly Dictionary<string, TMP_FontAsset> FontCache = new();

        public static TMP_FontAsset Font(string resourcesPath)
        {
            if (FontCache.TryGetValue(resourcesPath, out var cached)) return cached;
            var font = Resources.Load<TMP_FontAsset>(resourcesPath);
            if (font == null)
            {
                Debug.LogError($"UIBuilder: font not found at Resources/{resourcesPath}. " +
                                "Did the .ttf finish importing / did TmpFontAssetGenerator run?");
            }
            FontCache[resourcesPath] = font;
            return font;
        }

        // A white texture with rounded corners baked to `radius` px, sliced so the
        // corner radius stays exact regardless of how the RectTransform stretches.
        public static Sprite RoundedSprite(int radius)
        {
            radius = Mathf.Max(radius, 1);
            if (RoundedSpriteCache.TryGetValue(radius, out var cached)) return cached;

            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"RoundedRectMask_{radius}",
            };

            var pixels = new Color32[size * size];
            float r = radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    bool xOutside = px < r || px > size - r;
                    bool yOutside = py < r || py > size - r;
                    float alpha;
                    if (xOutside && yOutside)
                    {
                        // Only the four true corner zones need the circular falloff --
                        // straight edges (only one axis outside the inset rect) must
                        // stay fully opaque all the way to the image boundary, or the
                        // whole shape reads as a soft blob instead of a rounded square.
                        float cx = px < r ? r : size - r;
                        float cy = py < r ? r : size - r;
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                        alpha = Mathf.Clamp01(r - dist + 0.5f);
                    }
                    else
                    {
                        alpha = 1f;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var border = new Vector4(radius, radius, radius, radius);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, border);
            sprite.name = $"RoundedRect_{radius}";
            RoundedSpriteCache[radius] = sprite;
            return sprite;
        }

        private static Sprite _softGlowSprite;

        // A true radial gradient (opaque center fading smoothly to fully
        // transparent at the edge) for soft glows/shadows -- NOT sliced, since
        // slicing only makes sense for hard-edged corner masks like RoundedSprite.
        // One shared 256x256 texture is reused for every glow regardless of the
        // RectTransform size it's stretched onto (Image.Type.Simple).
        public static Sprite SoftGlowSprite()
        {
            if (_softGlowSprite != null) return _softGlowSprite;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "SoftGlow",
            };
            var pixels = new Color32[size * size];
            var center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                    // smoothstep falloff, fully opaque core fading out past ~40% radius
                    float t = Mathf.Clamp01(dist);
                    float alpha = 1f - (t * t * (3f - 2f * t));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            _softGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _softGlowSprite.name = "SoftGlowSprite";
            return _softGlowSprite;
        }

        // Same rounded-corner math as RoundedSprite, but baked at the EXACT final
        // pixel size and rendered as Image.Type.Simple (no 9-slice stretching).
        // Use this whenever the caller already knows the panel's fixed on-screen
        // size (which is true almost everywhere in this codebase, since a Panel()
        // call is always immediately followed by an explicit-size Flex() call) --
        // Image.Type.Sliced on a runtime-generated Sprite was not respecting the
        // border here, so small/tightly-cropped masks (e.g. a 120px box with a
        // 22px radius) rendered as if the whole tiny source texture had been
        // stretched over the box, which -- since so much of that texture is
        // corner-falloff -- looked like a circle instead of a rounded square.
        public static Sprite RoundedSpriteExact(int width, int height, int radius)
        {
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);
            radius = Mathf.Clamp(radius, 0, Mathf.Min(width, height) / 2);
            var key = (width, height, radius);
            if (ExactRoundedSpriteCache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"RoundedExact_{width}x{height}_{radius}",
            };
            var pixels = new Color32[width * height];
            float r = radius;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    bool xOutside = px < r || px > width - r;
                    bool yOutside = py < r || py > height - r;
                    float alpha;
                    if (xOutside && yOutside)
                    {
                        float cx = px < r ? r : width - r;
                        float cy = py < r ? r : height - r;
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                        alpha = Mathf.Clamp01(r - dist + 0.5f);
                    }
                    else
                    {
                        alpha = 1f;
                    }
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = $"RoundedExactSprite_{width}x{height}_{radius}";
            ExactRoundedSpriteCache[key] = sprite;
            return sprite;
        }

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        // A rounded panel, optionally with a 1px (or given width) border drawn as
        // a second, slightly-larger rounded rect behind the fill.
        //
        // Pass exactWidth/exactHeight whenever the final on-screen size is already
        // known (true almost everywhere -- a Panel() call is nearly always
        // immediately followed by an explicit-size Flex() call): this bakes the
        // rounded-corner mask at that exact pixel size (Image.Type.Simple), which
        // is what actually renders correct crisp corners. Leave them null only for
        // panels whose size isn't known yet (e.g. a pill that self-sizes via
        // ContentSizeFitter) -- those fall back to the 9-sliced RoundedSprite,
        // which is the technique that's supposed to handle a variable size, though
        // corner fidelity there hasn't been verified as carefully.
        public static RectTransform Panel(Transform parent, string name, Color bg, float radius,
            int? exactWidth = null, int? exactHeight = null, Color? borderColor = null, float borderWidth = 1f)
        {
            RectTransform host;
            if (borderColor.HasValue)
            {
                host = NewRect(parent, name);
                var borderImg = host.gameObject.AddComponent<Image>();
                if (exactWidth.HasValue && exactHeight.HasValue)
                {
                    borderImg.sprite = RoundedSpriteExact(exactWidth.Value, exactHeight.Value, Mathf.RoundToInt(radius));
                    borderImg.type = Image.Type.Simple;
                }
                else
                {
                    borderImg.sprite = RoundedSprite(Mathf.RoundToInt(radius));
                    borderImg.type = Image.Type.Sliced;
                }
                borderImg.color = borderColor.Value;

                var inner = NewRect(host, name + "_Fill");
                inner.anchorMin = Vector2.zero;
                inner.anchorMax = Vector2.one;
                inner.offsetMin = new Vector2(borderWidth, borderWidth);
                inner.offsetMax = new Vector2(-borderWidth, -borderWidth);
                var innerImg = inner.gameObject.AddComponent<Image>();
                int innerRadius = Mathf.RoundToInt(Mathf.Max(radius - borderWidth, 1));
                if (exactWidth.HasValue && exactHeight.HasValue)
                {
                    innerImg.sprite = RoundedSpriteExact(Mathf.RoundToInt(exactWidth.Value - borderWidth * 2), Mathf.RoundToInt(exactHeight.Value - borderWidth * 2), innerRadius);
                    innerImg.type = Image.Type.Simple;
                }
                else
                {
                    innerImg.sprite = RoundedSprite(innerRadius);
                    innerImg.type = Image.Type.Sliced;
                }
                innerImg.color = bg;
                // The fill sits ON TOP of (renders after) the border/host in the
                // hierarchy, so it was the one catching every raycast -- clicks
                // never reached components added to `host` itself (e.g. a
                // TMP_InputField or Button using host as targetGraphic), which is
                // exactly why the Configuración number fields could be seen but
                // not clicked into. Not a raycast target itself; host stays the
                // one thing pointer events land on.
                innerImg.raycastTarget = false;
            }
            else
            {
                host = NewRect(parent, name);
                var img = host.gameObject.AddComponent<Image>();
                if (exactWidth.HasValue && exactHeight.HasValue)
                {
                    img.sprite = RoundedSpriteExact(exactWidth.Value, exactHeight.Value, Mathf.RoundToInt(radius));
                    img.type = Image.Type.Simple;
                }
                else
                {
                    img.sprite = RoundedSprite(Mathf.RoundToInt(radius));
                    img.type = Image.Type.Sliced;
                }
                img.color = bg;
            }
            return host;
        }

        // Flat color rect, no rounding (dividers, accent bars, plain fills).
        public static RectTransform Rect(Transform parent, string name, Color color)
        {
            var rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string content, float size, TMP_FontAsset font, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Normal)
        {
            var rt = NewRect(parent, name);
            var txt = rt.gameObject.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = size;
            txt.font = font;
            txt.color = color;
            txt.alignment = align;
            txt.fontStyle = style;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Overflow;
            return txt;
        }

        public static RectTransform HRow(Transform parent, string name, float gap = 0, RectOffset padding = null,
            TextAnchor align = TextAnchor.MiddleLeft, bool controlWidth = true, bool controlHeight = true, bool forceExpand = false)
        {
            var rt = NewRect(parent, name);
            var lg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            lg.spacing = gap;
            lg.padding = padding ?? new RectOffset();
            lg.childAlignment = align;
            lg.childControlWidth = controlWidth;
            lg.childControlHeight = controlHeight;
            lg.childForceExpandWidth = forceExpand;
            lg.childForceExpandHeight = forceExpand;
            return rt;
        }

        public static RectTransform VCol(Transform parent, string name, float gap = 0, RectOffset padding = null,
            TextAnchor align = TextAnchor.UpperLeft, bool controlWidth = true, bool controlHeight = true, bool forceExpand = false)
        {
            var rt = NewRect(parent, name);
            var lg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            lg.spacing = gap;
            lg.padding = padding ?? new RectOffset();
            lg.childAlignment = align;
            lg.childControlWidth = controlWidth;
            lg.childControlHeight = controlHeight;
            lg.childForceExpandWidth = forceExpand;
            lg.childForceExpandHeight = forceExpand;
            return rt;
        }

        public static LayoutElement Flex(RectTransform target, float flexWidth = 0, float flexHeight = 0,
            float minWidth = -1, float minHeight = -1, float preferredWidth = -1, float preferredHeight = -1)
        {
            var le = target.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWidth;
            le.flexibleHeight = flexHeight;
            le.minWidth = minWidth;
            le.minHeight = minHeight;
            le.preferredWidth = preferredWidth;
            le.preferredHeight = preferredHeight;
            return le;
        }

        public static RectTransform StatusDot(Transform parent, string name, Color color, float diameter = 7f)
        {
            var rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            int d = Mathf.RoundToInt(diameter);
            // Image.Type.Sliced on a tiny runtime sprite stretches instead of
            // slicing (see RoundedSpriteExact usages elsewhere) -- baked this at
            // its exact final size with Simple instead, otherwise it renders as
            // a squashed oval whenever the row's layout group isn't perfectly square.
            img.sprite = RoundedSpriteExact(d, d, d / 2);
            img.type = Image.Type.Simple;
            img.color = color;
            Flex(rt, 0, 0, diameter, diameter, diameter, diameter);
            return rt;
        }

        public struct ProgressBarHandle
        {
            public RectTransform Fill;
            public Image FillImage;

            public void SetPercent(float pct, Color color)
            {
                pct = Mathf.Clamp01(pct / 100f);
                Fill.anchorMax = new Vector2(pct, 1f);
                FillImage.color = color;
            }
        }

        public static ProgressBarHandle ProgressBar(Transform parent, string name, float height, float radius, Color trackColor, Color fillColor, int exactWidth = 260)
        {
            var track = Panel(parent, name, trackColor, radius, exactWidth: exactWidth, exactHeight: Mathf.RoundToInt(height));
            Flex(track, 1, 0, -1, height, -1, height);

            // Flat rect, not rounded: the fill's right edge moves every frame as
            // the percentage changes, so there's no fixed size to bake a mask at.
            // The track's own rounded shape reads as the bar's silhouette.
            var fill = Rect(track, name + "_Fill", fillColor);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();

            return new ProgressBarHandle { Fill = fill, FillImage = fillImg };
        }

        public static Button Button(Transform parent, string name, Color normalBg, Color hoverBg, float radius = 0, int exactWidth = 0, int exactHeight = 0)
        {
            RectTransform rt = radius > 0
                ? (exactWidth > 0 && exactHeight > 0
                    ? Panel(parent, name, normalBg, radius, exactWidth: exactWidth, exactHeight: exactHeight)
                    : Panel(parent, name, normalBg, radius))
                : Rect(parent, name, normalBg);
            var img = rt.GetComponent<Image>();
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white; // we drive hover via SetButtonColors below instead of tint
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.transition = Selectable.Transition.None;
            var hover = rt.gameObject.AddComponent<ButtonHoverColor>();
            hover.Image = img;
            hover.NormalColor = normalBg;
            hover.HoverColor = hoverBg;
            return btn;
        }

        // Apariencia y medidas de un campo numerico. Los valores por defecto
        // son el campo OSCURO original (label 10px gris sobre chip #212016);
        // la tarjeta "Configuracion" pasa Config(), que mantiene la paleta pero
        // sube el contraste de la etiqueta y la tipografia -- ver
        // UITheme.ConfigPanelBg y HomeView.BuildConfigCard.
        //
        // Es una clase y no un pun~ado de parametros opcionales porque son
        // once ajustes que solo tienen sentido juntos: media docena de
        // argumentos sueltos en la firma se prestaba a mezclar la mitad de un
        // estilo con la mitad del otro.
        public class FieldStyle
        {
            public Color LabelColor = UITheme.TextMuted2;
            public float LabelSize = UITheme.TypeCaption10;
            public float LabelHeight = 18f;
            public bool LabelWrap = false;
            public TextAlignmentOptions LabelAlign = TextAlignmentOptions.MidlineLeft;
            public string LabelFont = UITheme.FontPathBodyRegular;

            public Color FieldBg = UITheme.ChipBg;
            public Color FieldBorder = UITheme.DividerTrackBg;
            public Color TextColor = UITheme.TextPrimary;
            public float TextSize = UITheme.TypeBody12;
            public string TextFont = UITheme.FontPathBodyRegular;
            public float FieldHeight = 32f;
            // Ancho al que se hornea el sprite de esquinas redondeadas del
            // recuadro (ver Panel): no es el ancho real, solo tiene que
            // parecerse para que las curvas no salgan estiradas.
            public int BakeWidth = 260;
            public Color CaretColor = UITheme.GoldBright;
            public Color FocusColor = UITheme.GoldBright;

            // -1 = "sin opinion", que es lo que hacia el campo original
            // (el ancho lo decidia el texto de la etiqueta). La rejilla de dos
            // columnas de Configuracion lo pone en 0 para que las dos mitades
            // salgan EXACTAMENTE iguales: sin preferencia propia, todo el
            // ancho sobrante se reparte por peso flexible, que es 1 y 1.
            public float PreferredWidth = -1f;

            public float HostHeight => LabelHeight + LabelToFieldGap + FieldHeight;
            public const float LabelToFieldGap = 4f;

            // Campo grande de la tarjeta "Configuracion": misma paleta oscura
            // que el campo por defecto, pero con la etiqueta en gris claro y
            // la tipografia subida (10/12 -> 15/20). Ver UITheme.
            public static FieldStyle Config() => new FieldStyle
            {
                LabelColor = UITheme.ConfigTextLabel,
                LabelSize = UITheme.TypeConfigLabel15,
                LabelHeight = 36f,   // tope de DOS renglones a 15px
                LabelWrap = true,    // "Capacidad Cosechadora" a media tarjeta
                LabelAlign = TextAlignmentOptions.BottomLeft, // pegada a su campo, caiga en 1 o 2 renglones
                LabelFont = UITheme.FontPathBodySemiBold,
                FieldBg = UITheme.ConfigFieldBg,
                FieldBorder = UITheme.ConfigFieldBorder,
                TextColor = UITheme.ConfigTextPrimary,
                TextSize = UITheme.TypeConfigValue20,
                TextFont = UITheme.FontPathBodySemiBold,
                FieldHeight = 40f,
                BakeWidth = 110,   // media tarjeta, no la tarjeta entera
                CaretColor = UITheme.ConfigFocusRing,
                FocusColor = UITheme.ConfigFocusRing,
                PreferredWidth = 0f,
            };

            // Igual, pero para un parametro SOLO en su renglon. Al ancho
            // completo de la tarjeta la etiqueta entra sobrada en un renglon,
            // asi que no hace falta reservar el segundo -- y esos 14px menos
            // por fila son los que permiten apilar cuatro parametros sin que
            // la tarjeta crezca (ver HomeView.ConfigAltoTarjeta).
            public static FieldStyle ConfigAncha()
            {
                var estilo = Config();
                estilo.LabelHeight = 22f;
                estilo.BakeWidth = 230;
                return estilo;
            }
        }

        // Labeled numeric field matching the "Configuración" card style: label
        // above, input below with a 1px border that turns gold on focus (the
        // app's one interactive-state signature, per spec). `style` decides
        // dark-and-small vs light-and-large -- see FieldStyle.
        public static TMP_InputField NumberField(Transform parent, string name, string label, int initialValue, System.Action<int> onChanged, FieldStyle style = null)
        {
            var field = BuildLabeledField(parent, name, label, initialValue.ToString(), TMP_InputField.ContentType.IntegerNumber, style);
            field.onEndEdit.AddListener(v =>
            {
                if (int.TryParse(v, out var iv)) onChanged?.Invoke(iv);
            });
            return field;
        }

        public static TMP_InputField NumberFieldFloat(Transform parent, string name, string label, float initialValue, System.Action<float> onChanged, FieldStyle style = null)
        {
            var field = BuildLabeledField(parent, name, label, initialValue.ToString(System.Globalization.CultureInfo.InvariantCulture), TMP_InputField.ContentType.DecimalNumber, style);
            field.onEndEdit.AddListener(v =>
            {
                if (float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fv))
                    onChanged?.Invoke(fv);
            });
            return field;
        }

        private static TMP_InputField BuildLabeledField(Transform parent, string name, string label, string initialText, TMP_InputField.ContentType contentType, FieldStyle style = null)
        {
            style ??= new FieldStyle();

            var host = VCol(parent, name, gap: FieldStyle.LabelToFieldGap, controlWidth: true, controlHeight: true, forceExpand: true);
            Flex(host, 1, 0, -1, style.HostHeight, style.PreferredWidth, style.HostHeight);

            var labelText = Text(host, name + "_Label", label, style.LabelSize, Font(style.LabelFont), style.LabelColor, style.LabelAlign);
            // Text() deja todo en NoWrap; a media tarjeta una etiqueta larga
            // se saldria del recuadro en vez de partirse en dos renglones.
            if (style.LabelWrap)
            {
                labelText.textWrappingMode = TextWrappingModes.Normal;
                // Y con Overflow (el default de Text()) un tercer renglon no
                // se corta: se dibuja FUERA del recuadro, encima del campo de
                // la fila de arriba -- que es exactamente lo que hacia
                // "Probabilidad de Descompostura". Ellipsis lo corta con "..."
                // dentro de su hueco: la etiqueta se ve incompleta, que es
                // feo pero visible, en vez de romper la fila vecina en
                // silencio. Si aparece un "...", la respuesta es acortar la
                // etiqueta, no subir LabelHeight.
                labelText.overflowMode = TextOverflowModes.Ellipsis;
            }
            Flex((RectTransform)labelText.transform, 1, 0, -1, style.LabelHeight, -1, style.LabelHeight);

            int altoExacto = Mathf.RoundToInt(style.FieldHeight);
            var fieldHost = Panel(host, name + "_Field", style.FieldBg, 6f, exactWidth: style.BakeWidth, exactHeight: altoExacto, borderColor: style.FieldBorder, borderWidth: 1f);
            Flex(fieldHost, 1, 0, -1, style.FieldHeight, -1, style.FieldHeight);

            // TMP_InputField generates its caret as a child under textViewport at
            // runtime -- pointing textViewport at the SAME rect as the text
            // component itself (self-referential) was likely why the caret never
            // showed. Standard TMP hierarchy: a "TextArea" container (this is the
            // viewport) holding "Text" as its child.
            var textArea = NewRect(fieldHost, "TextArea");
            textArea.anchorMin = Vector2.zero;
            textArea.anchorMax = Vector2.one;
            textArea.offsetMin = new Vector2(9, 4);
            textArea.offsetMax = new Vector2(-9, -4);
            textArea.gameObject.AddComponent<RectMask2D>();

            var textGo = NewRect(textArea, "Text");
            textGo.anchorMin = Vector2.zero;
            textGo.anchorMax = Vector2.one;
            textGo.offsetMin = Vector2.zero;
            textGo.offsetMax = Vector2.zero;
            var tmpText = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            tmpText.font = Font(style.TextFont);
            tmpText.fontSize = style.TextSize;
            tmpText.color = style.TextColor;
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;

            var fieldImg = fieldHost.GetComponent<Image>();
            var field = fieldHost.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = fieldImg;
            field.textComponent = tmpText;
            field.textViewport = textArea;
            field.contentType = contentType;
            field.text = initialText;
            field.transition = Selectable.Transition.None;
            // TMP_InputField already blinks a caret by default, but it inherits
            // the text color unless told otherwise -- make it gold and a touch
            // wider so it's clearly visible against the dark field background.
            field.customCaretColor = true;
            field.caretColor = style.CaretColor;
            field.caretWidth = 2;
            field.caretBlinkRate = 0.85f;

            var focus = fieldHost.gameObject.AddComponent<InputFocusBorder>();
            focus.Border = fieldImg;
            focus.NormalColor = style.FieldBorder;
            focus.FocusColor = style.FocusColor;
            focus.Attach(field);

            return field;
        }
    }
}
