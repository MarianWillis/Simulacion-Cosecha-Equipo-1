using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmDashboard
{
    // Branded splash screen -- README section 1. Auto-advances to the dashboard
    // after ~2.4s, or the user can click anywhere to skip immediately.
    public class IntroView : MonoBehaviour, IPointerClickHandler
    {
        private Action _onComplete;
        private CanvasGroup _rootGroup;
        private bool _advancing;

        // Call right after AddComponent<IntroView>() on a GameObject that is
        // already parented under the canvas -- this builds directly onto that
        // GameObject (it needs to own the full-screen Image so clicks anywhere
        // on the intro are caught by this same IPointerClickHandler).
        public void Init(string brandName, Action onComplete)
        {
            _onComplete = onComplete;

            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            gameObject.AddComponent<Image>().color = UITheme.BgBase;
            _rootGroup = gameObject.AddComponent<CanvasGroup>();

            // Soft radial glow behind the logo -- a real gradient (SoftGlowSprite),
            // not a 9-sliced rounded-rect mask, so it actually fades out instead
            // of showing a hard circular edge.
            var glow = UIBuilder.Rect(root, "Glow", new Color(UITheme.GreenPrimary.r, UITheme.GreenPrimary.g, UITheme.GreenPrimary.b, 0.35f));
            var glowImg = glow.GetComponent<Image>();
            glowImg.sprite = UIBuilder.SoftGlowSprite();
            glowImg.type = Image.Type.Simple;
            glow.sizeDelta = new Vector2(320, 320);
            glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.5f);
            glow.anchoredPosition = Vector2.zero;

            // Centered column: logo, brand name, subtitle. Built with a VerticalLayoutGroup
            // acting as flex column + gap:20px, sized to content and centered on screen.
            // forceExpand:false is deliberate here -- with it true, Unity's cross-axis
            // sizing stretches EVERY child to the column's full width regardless of
            // its own LayoutElement (that's what was squashing the 120x120 logo into
            // a short wide rectangle). false lets each child keep its own preferred
            // width -- the logo stays 120x120, and the text hosts still center fine
            // via childAlignment since their preferred width already matches their text.
            var column = UIBuilder.VCol(root, "Column", gap: 20, align: TextAnchor.MiddleCenter, controlWidth: true, controlHeight: true, forceExpand: false);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(560, 0);
            var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildLogo(column);
            BuildBrandText(column, brandName);
            BuildSubtitle(column);
            BuildHint(root);
        }

        private void BuildLogo(Transform parent)
        {
            var logoHost = UIBuilder.NewRect(parent, "LogoHost");
            UIBuilder.Flex(logoHost, 0, 0, 120, 120, 120, 120);
            var logoGroup = logoHost.gameObject.AddComponent<CanvasGroup>();

            // Faux drop-shadow: soft dark glow behind, offset down. Uses the same
            // radial-gradient sprite as the background glow (not a rounded-rect
            // mask) so it actually blurs out instead of showing a hard edge.
            var shadow = UIBuilder.NewRect(logoHost, "LogoShadow");
            var shadowImg = shadow.gameObject.AddComponent<Image>();
            shadowImg.sprite = UIBuilder.SoftGlowSprite();
            shadowImg.type = Image.Type.Simple;
            shadowImg.color = new Color(0, 0, 0, 0.45f);
            shadow.anchorMin = shadow.anchorMax = new Vector2(0.5f, 0.5f);
            shadow.sizeDelta = new Vector2(200, 200);
            shadow.anchoredPosition = new Vector2(0, -10);

            var tile = UIBuilder.Panel(logoHost, "LogoTile", UITheme.ChipBg, 22);
            tile.anchorMin = Vector2.zero;
            tile.anchorMax = Vector2.one;
            tile.offsetMin = Vector2.zero;
            tile.offsetMax = Vector2.zero;

            var grid = UIBuilder.VCol(tile, "Grid2x2", gap: 5, padding: new RectOffset(8, 8, 8, 8), controlWidth: true, controlHeight: true, forceExpand: true);
            grid.anchorMin = Vector2.zero;
            grid.anchorMax = Vector2.one;
            grid.offsetMin = Vector2.zero;
            grid.offsetMax = Vector2.zero;

            var topRow = UIBuilder.HRow(grid, "TopRow", gap: 5, controlWidth: true, controlHeight: true, forceExpand: true);
            UIBuilder.Flex(topRow, 1, 1);
            var tl = UIBuilder.Panel(topRow, "TL", UITheme.GreenPrimary, 7); UIBuilder.Flex(tl, 1, 1);
            var tr = UIBuilder.Panel(topRow, "TR", UITheme.GoldAccent, 7); UIBuilder.Flex(tr, 1, 1);

            var bottomRow = UIBuilder.HRow(grid, "BottomRow", gap: 5, controlWidth: true, controlHeight: true, forceExpand: true);
            UIBuilder.Flex(bottomRow, 1, 1);
            var bl = UIBuilder.Panel(bottomRow, "BL", UITheme.GreenDark, 7); UIBuilder.Flex(bl, 1, 1);
            var br = UIBuilder.Panel(bottomRow, "BR", UITheme.BrownAccent, 7); UIBuilder.Flex(br, 1, 1);

            StartCoroutine(UITween.ScaleFadeIn((RectTransform)logoHost, logoGroup, 0.25f, 0.9f));
        }

        private void BuildBrandText(Transform parent, string brandName)
        {
            var host = UIBuilder.NewRect(parent, "BrandNameHost");
            UIBuilder.Flex(host, 1, 0, 0, UITheme.TypeIntroBrand34 + 10, -1, UITheme.TypeIntroBrand34 + 10);
            var cg = host.gameObject.AddComponent<CanvasGroup>();
            var txt = UIBuilder.Text(host, "BrandName", brandName, UITheme.TypeIntroBrand34, UIBuilder.Font(UITheme.FontPathBodyBold), UITheme.TextPrimary, TextAlignmentOptions.Center);
            txt.characterSpacing = -1f;
            ((RectTransform)txt.transform).anchorMin = Vector2.zero;
            ((RectTransform)txt.transform).anchorMax = Vector2.one;
            ((RectTransform)txt.transform).offsetMin = Vector2.zero;
            ((RectTransform)txt.transform).offsetMax = Vector2.zero;
            StartCoroutine(UITween.FadeIn(cg, 0.7f, 0.4f));
        }

        private void BuildSubtitle(Transform parent)
        {
            var host = UIBuilder.NewRect(parent, "SubtitleHost");
            UIBuilder.Flex(host, 1, 0, 0, 18, -1, 18);
            var cg = host.gameObject.AddComponent<CanvasGroup>();
            var txt = UIBuilder.Text(host, "Subtitle", "OPERATIONS CENTER", UITheme.TypeBody12 + 1, UIBuilder.Font(UITheme.FontPathBodySemiBold), UITheme.TextMuted1, TextAlignmentOptions.Center);
            txt.characterSpacing = 8f;
            ((RectTransform)txt.transform).anchorMin = Vector2.zero;
            ((RectTransform)txt.transform).anchorMax = Vector2.one;
            ((RectTransform)txt.transform).offsetMin = Vector2.zero;
            ((RectTransform)txt.transform).offsetMax = Vector2.zero;
            StartCoroutine(UITween.FadeIn(cg, 0.7f, 0.55f));
        }

        private void BuildHint(Transform parent)
        {
            var host = UIBuilder.NewRect(parent, "HintHost");
            host.anchorMin = new Vector2(0.5f, 0f);
            host.anchorMax = new Vector2(0.5f, 0f);
            host.pivot = new Vector2(0.5f, 0f);
            host.sizeDelta = new Vector2(300, 18);
            host.anchoredPosition = new Vector2(0, 32);
            var cg = host.gameObject.AddComponent<CanvasGroup>();
            var txt = UIBuilder.Text(host, "Hint", "Click para continuar", UITheme.TypeBody12, UIBuilder.Font(UITheme.FontPathBodyRegular), UITheme.MidGray, TextAlignmentOptions.Center);
            ((RectTransform)txt.transform).anchorMin = Vector2.zero;
            ((RectTransform)txt.transform).anchorMax = Vector2.one;
            ((RectTransform)txt.transform).offsetMin = Vector2.zero;
            ((RectTransform)txt.transform).offsetMax = Vector2.zero;
            StartCoroutine(UITween.FadeUp((RectTransform)host, cg, 10f, 0.7f, 0.7f));
        }

        private void Start()
        {
            StartCoroutine(AutoAdvance());
        }

        private IEnumerator AutoAdvance()
        {
            yield return new WaitForSeconds(2.0f);
            if (!_advancing) StartCoroutine(AdvanceToDashboard());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_advancing) return;
            StartCoroutine(AdvanceToDashboard());
        }

        private IEnumerator AdvanceToDashboard()
        {
            _advancing = true;
            yield return StartCoroutine(UITween.FadeTo(_rootGroup, 0f, 0.38f));
            _onComplete?.Invoke();
            Destroy(_rootGroup.gameObject);
        }
    }
}
