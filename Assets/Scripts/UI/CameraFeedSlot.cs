using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmDashboard
{
    // A reserved spot in the UI for a real Unity camera feed (a RenderTexture
    // from a Camera pointed at the 3D farm scene), instead of a flat 2D
    // abstraction. Whoever wires up the actual cameras (per the team's scope
    // split, that's not the UI work) finds a slot by name and assigns a
    // texture to it:
    //
    //   var slot = CameraFeedSlot.Find("general");
    //   if (slot != null) slot.SetFeed(myCamera.targetTexture);
    //
    // Renders as a flat fallback color until a texture is assigned, so the
    // card never looks broken/empty in the meantime.
    public class CameraFeedSlot : MonoBehaviour
    {
        private static readonly Dictionary<string, CameraFeedSlot> Registry = new();

        public string SlotName { get; private set; }
        public RawImage Image { get; private set; }
        public AspectRatioFitter Fitter { get; private set; }

        public static CameraFeedSlot Create(Transform parent, string slotName, Color fallbackColor)
        {
            var host = UIBuilder.NewRect(parent, $"CameraFeed_{slotName}");
            var img = host.gameObject.AddComponent<RawImage>();
            img.color = fallbackColor;

            var slot = host.gameObject.AddComponent<CameraFeedSlot>();
            slot.SlotName = slotName;
            slot.Image = img;
            Registry[slotName] = slot;
            return slot;
        }

        public void SetFeed(RenderTexture texture)
        {
            Image.texture = texture;
            Image.color = Color.white; // let the texture's own colors show through
        }

        // Ajusta la imagen al aspecto REAL de lo que enfoca la camara y la
        // mete dentro del hueco de la tarjeta (FitInParent, con barras al
        // lado o arriba/abajo si sobra espacio). Sin esto el RawImage estira
        // la RenderTexture hasta llenar el hueco y la escena se ve deformada.
        public void SetAspect(float aspect)
        {
            if (aspect <= 0f || float.IsNaN(aspect) || float.IsInfinity(aspect)) return;
            if (Fitter == null)
            {
                Fitter = gameObject.AddComponent<AspectRatioFitter>();
                Fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
            Fitter.aspectRatio = aspect;
        }

        public static CameraFeedSlot Find(string slotName) =>
            Registry.TryGetValue(slotName, out var slot) ? slot : null;

        private void OnDestroy()
        {
            if (Registry.TryGetValue(SlotName, out var current) && current == this)
                Registry.Remove(SlotName);
        }
    }
}
