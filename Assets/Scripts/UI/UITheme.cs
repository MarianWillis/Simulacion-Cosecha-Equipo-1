using UnityEngine;

namespace FarmDashboard
{
    // Design tokens ported 1:1 from the "Granja TEC" dashboard handoff README.
    // Single source of truth for every color/size/radius used by the UI code.
    public static class UITheme
    {
        public static readonly Color BgBase = Hex("#0c0b05");
        public static readonly Color PanelBg = Hex("#15140c");
        public static readonly Color PanelBorder = Hex("#29271b");
        public static readonly Color CardBgNested = Hex("#1c1b12");
        public static readonly Color ChipBg = Hex("#212016");
        public static readonly Color DividerTrackBg = Hex("#2b291e");

        public static readonly Color TextPrimary = Hex("#efefea");
        public static readonly Color TextMuted1 = Hex("#81807a");
        public static readonly Color TextMuted2 = Hex("#73726b");
        public static readonly Color MidGray = Hex("#64635d");
        public static readonly Color HeaderMonoText = Hex("#999892");
        public static readonly Color LightGrayText = Hex("#cfcec7");
        public static readonly Color ButtonOutlineText = Hex("#afaea7");
        public static readonly Color NearWhiteMarker = Hex("#dfdeda");

        public static readonly Color GreenPrimary = Hex("#50a248");
        public static readonly Color GreenDark = Hex("#30752a");
        public static readonly Color GoldAccent = Hex("#be9a2a");
        public static readonly Color GoldBright = Hex("#e3b916");
        public static readonly Color BrownAccent = Hex("#6f4c34");
        public static readonly Color OnGreenText = Hex("#040704");
        public static readonly Color OnGoldText = Hex("#151107");

        public static readonly Color GridFieldBg = Hex("#0e5200");
        public static readonly Color GridCellFill = Hex("#155e00");
        public static readonly Color GridCellBorder = Hex("#448f22", 0.6f);
        public static readonly Color CameraThumbBg = Hex("#0e4b00");

        public static readonly Color StatusRed = Hex("#e54c4a");
        public static readonly Color StatusActivo = Hex("#e3b916");
        public static readonly Color StatusEnCamino = Hex("#c99d4e");
        public static readonly Color StatusCargando = Hex("#e27641");
        public static readonly Color StatusDescargando = Hex("#5b8abb");

        public static readonly Color OutlineButtonBorder = Hex("#4a4838");

        // Marker colors (intentionally: tractor = gold, cosechador = green,
        // opposite of the sidebar icon dot colors -- called out in the README).
        public static readonly Color TractorMarker = GoldAccent;
        public static readonly Color CosechadorMarker = GreenPrimary;

        // Fonts (free equivalents -- see Assets/UI/Fonts/LICENSES.txt).
        // Resources.Load keys -- generated into Assets/UI/Fonts/Resources/TMP/ by
        // TmpFontAssetGenerator.cs the moment the raw .ttf files are imported.
        public const string FontPathDisplay = "TMP/ArchivoBlack-Regular SDF";
        public const string FontPathBodyExtraLight = "TMP/Inter-ExtraLight SDF";
        public const string FontPathBodyRegular = "TMP/Inter-Regular SDF";
        public const string FontPathBodySemiBold = "TMP/Inter-SemiBold SDF";
        public const string FontPathBodyBold = "TMP/Inter-Bold SDF";
        public const string FontPathBodyExtraBold = "TMP/Inter-ExtraBold SDF";
        public const string FontPathMonoRegular = "TMP/RobotoMono-Regular SDF";
        public const string FontPathMonoMedium = "TMP/RobotoMono-Medium SDF";
        public const string FontPathMonoBold = "TMP/RobotoMono-Bold SDF";

        // Radii (px, matched against the 1440x900 reference canvas -- see UIBuilder).
        public const float RadiusCard = 12f;
        public const float RadiusNested = 10f;
        public const float RadiusRow = 9f;
        public const float RadiusChip = 7f;
        public const float RadiusButton = 4f;

        // Spacing (px).
        public const float GapMajor = 20f;

        // Reparto horizontal de las vistas de operacion (Cultivo, etc):
        // panel de informacion a la izquierda, camaras por zona a la
        // derecha. Definido en un solo lugar porque los dos paneles viven
        // en archivos distintos (OperationsSectionView y CultivoCamarasView)
        // y si los numeros no coinciden se encinan o dejan hueco.
        public const float SplitPanelInfo = 0.5f;
        public const float SplitSeparacion = 10f;
        public const float GapWithinCard = 12f;
        public const float PaddingCompact = 16f;
        public const float PaddingSpacious = 22f;

        // Type scale (px, matches the design's 1440x900 canvas 1:1).
        public const float TypeDisplay38 = 38f;
        public const float TypeIntroBrand34 = 34f;
        public const float TypeStat30 = 30f;
        public const float TypeDetailStat22 = 22f;
        public const float TypeCardTitle20 = 20f;
        public const float TypeSubtitle16 = 16f;
        public const float TypeWordmark15 = 15f;
        public const float TypeSectionTitle14 = 14f;
        public const float TypeRowLabel13 = 13f;
        public const float TypeBody12 = 12f;
        public const float TypeStatusButton11 = 11f;
        public const float TypeCaption10 = 10f;

        private static Color Hex(string hex, float alphaOverride = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var c))
            {
                Debug.LogError($"UITheme: invalid hex color '{hex}'");
                return Color.magenta;
            }
            c.a = alphaOverride;
            return c;
        }
    }
}
