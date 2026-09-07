using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // The design's one signature interactive-state detail: every text input
    // gets a gold border on focus, no visible outline. Swaps a border Image's
    // color based on TMP_InputField's own onSelect/onDeselect events.
    public class InputFocusBorder : MonoBehaviour
    {
        public Image Border;
        public Color NormalColor;
        public Color FocusColor;

        public void Attach(TMP_InputField field)
        {
            field.onSelect.AddListener(_ => { if (Border != null) Border.color = FocusColor; });
            field.onDeselect.AddListener(_ => { if (Border != null) Border.color = NormalColor; });
        }
    }
}
