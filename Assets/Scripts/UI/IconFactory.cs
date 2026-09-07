using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Small vector-style icons built from UIBuilder primitives (rects/rounded
    // rects/rotation), mirroring the HTML prototype's div-based icons. These are
    // deliberately simple approximations of the CSS shapes described in the
    // README's sidebar section -- easy to swap for real sprites later if the
    // team wants hand-drawn icons instead.
    public static class IconFactory
    {
        private static RectTransform Dot(Transform parent, string name, Color color, float diameter, Vector2 anchoredPos)
        {
            var rt = UIBuilder.NewRect(parent, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.anchoredPosition = anchoredPos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIBuilder.RoundedSprite(Mathf.CeilToInt(diameter / 2f));
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        // Hollow ring: an outline-only circle, faked by stacking a filled circle
        // (fg) under a slightly smaller filled circle in the icon's own background
        // color, punching a visual hole in the middle.
        private static void Ring(Transform parent, string name, Color fg, Color bgToPunchThrough, float diameter, float thickness, Vector2 anchoredPos)
        {
            Dot(parent, name + "_Outer", fg, diameter, anchoredPos);
            Dot(parent, name + "_Inner", bgToPunchThrough, diameter - thickness * 2, anchoredPos);
        }

        private static RectTransform Box(Transform parent, string name, Color color, Vector2 size, Vector2 anchoredPos, float radius = 0f, float rotationZ = 0f)
        {
            var rt = UIBuilder.NewRect(parent, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            rt.localRotation = Quaternion.Euler(0, 0, rotationZ);
            var img = rt.gameObject.AddComponent<Image>();
            if (radius > 0f)
            {
                img.sprite = UIBuilder.RoundedSprite(Mathf.RoundToInt(radius));
                img.type = Image.Type.Sliced;
            }
            img.color = color;
            return rt;
        }

        public static void House(Transform parent, Color fg)
        {
            Box(parent, "Base", fg, new Vector2(14, 9), new Vector2(0, -3), 1.5f);
            Box(parent, "Roof", fg, new Vector2(9, 9), new Vector2(0, 2.5f), 1.5f, 45f);
        }

        public static void Camera(Transform parent, Color fg, Color accent)
        {
            Box(parent, "Body", fg, new Vector2(11, 7), new Vector2(-2, 0), 2f);
            Box(parent, "Mount", fg, new Vector2(4, 2), new Vector2(-6, 4), 1f);
            Dot(parent, "Lens", fg, 8, new Vector2(5.5f, -1.5f));
            Dot(parent, "LensGlint", accent, 3, new Vector2(7.5f, 0.5f));
        }

        public static void FuelDrop(Transform parent, Color fg)
        {
            // Teardrop approximated as a rounded square rotated 45 degrees.
            Box(parent, "Drop", fg, new Vector2(11, 11), Vector2.zero, 3f, 45f);
        }

        public static void CultivoDot(Transform parent, Color fg)
        {
            Box(parent, "Dot", fg, new Vector2(12, 12), Vector2.zero, 2f);
        }

        public static void Tractor(Transform parent, Color fg, Color bg)
        {
            Ring(parent, "WheelBig", fg, bg, 9, 2, new Vector2(-3.5f, -3.5f));
            Ring(parent, "WheelSmall", fg, bg, 5, 1.6f, new Vector2(5f, -5.5f));
            Box(parent, "Body", fg, new Vector2(8, 5), new Vector2(-2, 3), 1f);
        }

        public static void Harvester(Transform parent, Color fg, Color bg)
        {
            Box(parent, "Body", fg, new Vector2(14, 6), new Vector2(0, -1), 1f);
            Box(parent, "Header", fg, new Vector2(6, 5), new Vector2(-3, 4), 1f);
            Ring(parent, "WheelL", fg, bg, 5, 1.6f, new Vector2(-5.5f, -5f));
            Ring(parent, "WheelR", fg, bg, 5, 1.6f, new Vector2(4.5f, -5f));
        }
    }
}
