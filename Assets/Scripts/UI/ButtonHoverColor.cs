using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Mirrors the design's `style-hover` background swap on buttons (e.g. sidebar
    // items going transparent -> chip-bg on hover). Kept separate from Selectable's
    // built-in color tint because several buttons need a *fixed* base color set
    // from code (active/inactive state) that hover must layer on top of, not replace.
    //
    // Also darkens on press (IPointerDown/Up): several buttons (Resume/Pause) pass
    // the same color for Normal and Hover since their real "hover" is the
    // enabled/disabled state swap driven from code, which left them with zero
    // click feedback -- the press-darken applies regardless of what Normal/Hover
    // happen to be, so every button still visibly responds to a click.
    public class ButtonHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image Image;
        public Color NormalColor;
        public Color HoverColor;

        public void SetBaseColor(Color color)
        {
            NormalColor = color;
            if (!_hovering && !_pressed && Image != null) Image.color = color;
        }

        private bool _hovering;
        private bool _pressed;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            if (!_pressed) Apply(HoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (!_pressed) Apply(NormalColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            var baseColor = _hovering ? HoverColor : NormalColor;
            Apply(new Color(baseColor.r * 0.75f, baseColor.g * 0.75f, baseColor.b * 0.75f, baseColor.a));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            Apply(_hovering ? HoverColor : NormalColor);
        }

        private void Apply(Color color)
        {
            if (Image != null) Image.color = color;
        }
    }
}
