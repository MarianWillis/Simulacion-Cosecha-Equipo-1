using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Mirrors the design's `style-hover` background swap on buttons (e.g. sidebar
    // items going transparent -> chip-bg on hover). Kept separate from Selectable's
    // built-in color tint because several buttons need a *fixed* base color set
    // from code (active/inactive state) that hover must layer on top of, not replace.
    public class ButtonHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Image;
        public Color NormalColor;
        public Color HoverColor;

        public void SetBaseColor(Color color)
        {
            NormalColor = color;
            if (!_hovering && Image != null) Image.color = color;
        }

        private bool _hovering;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            if (Image != null) Image.color = HoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (Image != null) Image.color = NormalColor;
        }
    }
}
