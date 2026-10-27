using System;
using System.Collections.Generic;
using System.IO;
using Prince;
using UnityEditor;
using UnityEngine;

namespace Prince.Editor
{
    internal static class PrinceMainMenuTextContrastAudit
    {
        private const string FallbackImagePath = "Assets/_Project/UI/MainMenu/Gambar backup.jpeg";
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private readonly struct Region
        {
            public readonly string Name;
            public readonly RectInt Bounds;
            public readonly Color TextColor;
            public readonly float MinimumContrast;

            public Region(string name, RectInt bounds, Color textColor, float minimumContrast)
            {
                Name = name;
                Bounds = bounds;
                TextColor = textColor;
                MinimumContrast = minimumContrast;
            }
        }

        [MenuItem("Tools/Prince/Audit Main Menu Text Contrast")]
        private static void AuditMenuItem()
        {
            MenuTextStyle style = AssetDatabase.LoadAssetAtPath<MenuTextStyle>(
                "Assets/_Project/Resources/MenuTextStyle.asset");
            Audit(style);
        }

        internal static void Audit(MenuTextStyle style)
        {
            if (style == null)
            {
                Debug.LogError("Main menu text contrast audit requires a MenuTextStyle asset.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("Could not resolve the Unity project root for the contrast audit.");
                return;
            }

            string fullImagePath = Path.Combine(projectRoot, FallbackImagePath);
            if (!File.Exists(fullImagePath))
            {
                Debug.LogWarning(
                    $"Static contrast audit skipped; no fallback image exists at '{FallbackImagePath}'.");
                return;
            }

            Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!ImageConversion.LoadImage(image, File.ReadAllBytes(fullImagePath), false))
                {
                    Debug.LogError($"Could not decode the main menu fallback image '{FallbackImagePath}'.");
                    return;
                }

                foreach (Region region in CreateRegions(style))
                {
                    float backgroundLuminance = AverageLuminance(image, region.Bounds, style);
                    float textLuminance = RelativeLuminance(region.TextColor);
                    float ratio = ContrastRatio(backgroundLuminance, textLuminance);
                    string result = region.MinimumContrast <= 0f
                        ? "MEASURED"
                        : ratio >= region.MinimumContrast ? "PASS" : "FAIL";
                    string line =
                        $"{result}: {region.Name} mean-area contrast {ratio:F2}:1 " +
                        $"{(region.MinimumContrast > 0f ? $"(required {region.MinimumContrast:F1}:1; " : "(")}" +
                        $"mean luminance {backgroundLuminance:F3}).";
                    if (region.MinimumContrast > 0f && ratio < region.MinimumContrast)
                        Debug.LogWarning(line);
                    else
                        Debug.Log(line);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static IEnumerable<Region> CreateRegions(MenuTextStyle style)
        {
            Color title = style.AuditTitleColor;
            Color subtitle = style.AuditSubtitleColor;
            Color button = style.AuditButtonColor;
            Color footer = style.AuditFooterColor;

            yield return new Region("Title", new RectInt(76, 125, 844, 145), title, 3f);
            yield return new Region("Subtitle", new RectInt(76, 235, 484, 55), subtitle, 4.5f);
            yield return new Region("Continue", new RectInt(76, 350, 430, 60), button, 4.5f);
            yield return new Region("New Game", new RectInt(76, 428, 430, 60), button, 4.5f);
            yield return new Region("Settings", new RectInt(76, 506, 430, 60), button, 4.5f);
            yield return new Region("Quit", new RectInt(76, 584, 430, 60), button, 4.5f);
            yield return new Region("Footer ExceedOG", new RectInt(76, 950, 420, 30), footer, 0f);
            yield return new Region("Footer copyright", new RectInt(76, 982, 540, 22), footer, 0f);
            yield return new Region("Footer BETA TEST", new RectInt(1364, 950, 280, 26), footer, 0f);
            yield return new Region("Footer version", new RectInt(1364, 982, 280, 22), footer, 0f);
        }

        private static float AverageLuminance(
            Texture2D image,
            RectInt referenceBounds,
            MenuTextStyle style)
        {
            int xMin = Mathf.Clamp(
                Mathf.RoundToInt(referenceBounds.xMin * image.width / (float)ReferenceWidth), 0, image.width - 1);
            int xMax = Mathf.Clamp(
                Mathf.RoundToInt(referenceBounds.xMax * image.width / (float)ReferenceWidth), xMin + 1, image.width);
            int yMin = Mathf.Clamp(
                Mathf.RoundToInt(referenceBounds.yMin * image.height / (float)ReferenceHeight), 0, image.height - 1);
            int yMax = Mathf.Clamp(
                Mathf.RoundToInt(referenceBounds.yMax * image.height / (float)ReferenceHeight), yMin + 1, image.height);
            const int sampleStep = 4;
            double total = 0d;
            int count = 0;
            for (int y = yMin; y < yMax; y += sampleStep)
            for (int x = xMin; x < xMax; x += sampleStep)
            {
                Color pixel = image.GetPixel(x, image.height - 1 - y);
                if (style.CurrentStyle == MenuTextStyle.TextStyleMode.LightOnDark)
                {
                    float screenX = x * ReferenceWidth / (float)image.width;
                    float t = Mathf.Clamp01(screenX / (ReferenceWidth * style.AuditGradientWidth));
                    float alpha = style.AuditGradientAlpha * (1f - t) * (1f - t);
                    Color gradient = style.AuditGradientColor;
                    pixel = new Color(
                        BlendLinear(pixel.r, gradient.r, alpha),
                        BlendLinear(pixel.g, gradient.g, alpha),
                        BlendLinear(pixel.b, gradient.b, alpha),
                        1f);
                }
                total += RelativeLuminance(pixel);
                count++;
            }

            return count > 0 ? (float)(total / count) : 0f;
        }

        private static float RelativeLuminance(Color color)
        {
            float red = Linearize(color.r);
            float green = Linearize(color.g);
            float blue = Linearize(color.b);
            return 0.2126f * red + 0.7152f * green + 0.0722f * blue;
        }

        private static float Linearize(float component) =>
            component <= 0.04045f
                ? component / 12.92f
                : Mathf.Pow((component + 0.055f) / 1.055f, 2.4f);

        private static float BlendLinear(float source, float overlay, float alpha)
        {
            float sourceLinear = Linearize(source);
            float overlayLinear = Linearize(overlay);
            float result = Mathf.Lerp(sourceLinear, overlayLinear, alpha);
            return result <= 0.0031308f
                ? result * 12.92f
                : 1.055f * Mathf.Pow(result, 1f / 2.4f) - 0.055f;
        }

        private static float ContrastRatio(float first, float second)
        {
            float lighter = Mathf.Max(first, second);
            float darker = Mathf.Min(first, second);
            return (lighter + 0.05f) / (darker + 0.05f);
        }
    }
}
