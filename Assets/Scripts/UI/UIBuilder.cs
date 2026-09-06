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

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        // A rounded panel, optionally with a 1px (or given width) border drawn as
        // a second, slightly-larger rounded rect behind the fill -- the standard
        // trick for a crisp inset border with 9-sliced rounded corners.
        public static RectTransform Panel(Transform parent, string name, Color bg, float radius, Color? borderColor = null, float borderWidth = 1f)
        {
            RectTransform host;
            if (borderColor.HasValue)
            {
                host = NewRect(parent, name);
                var borderImg = host.gameObject.AddComponent<Image>();
                borderImg.sprite = RoundedSprite(Mathf.RoundToInt(radius));
                borderImg.type = Image.Type.Sliced;
                borderImg.color = borderColor.Value;

                var inner = NewRect(host, name + "_Fill");
                inner.anchorMin = Vector2.zero;
                inner.anchorMax = Vector2.one;
                inner.offsetMin = new Vector2(borderWidth, borderWidth);
                inner.offsetMax = new Vector2(-borderWidth, -borderWidth);
                var innerImg = inner.gameObject.AddComponent<Image>();
                innerImg.sprite = RoundedSprite(Mathf.RoundToInt(Mathf.Max(radius - borderWidth, 1)));
                innerImg.type = Image.Type.Sliced;
                innerImg.color = bg;
            }
            else
            {
                host = NewRect(parent, name);
                var img = host.gameObject.AddComponent<Image>();
                img.sprite = RoundedSprite(Mathf.RoundToInt(radius));
                img.type = Image.Type.Sliced;
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
            img.sprite = RoundedSprite(Mathf.CeilToInt(diameter / 2f));
            img.type = Image.Type.Sliced;
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

        public static ProgressBarHandle ProgressBar(Transform parent, string name, float height, float radius, Color trackColor, Color fillColor)
        {
            var track = Panel(parent, name, trackColor, radius);
            Flex(track, 1, 0, -1, height, -1, height);

            var fill = NewRect(track, name + "_Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = RoundedSprite(Mathf.RoundToInt(radius));
            fillImg.type = Image.Type.Sliced;
            fillImg.color = fillColor;

            return new ProgressBarHandle { Fill = fill, FillImage = fillImg };
        }

        public static Button Button(Transform parent, string name, Color normalBg, Color hoverBg, float radius = 0)
        {
            RectTransform rt = radius > 0 ? Panel(parent, name, normalBg, radius) : Rect(parent, name, normalBg);
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
    }
}
