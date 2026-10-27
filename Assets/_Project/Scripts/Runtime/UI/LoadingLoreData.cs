using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prince
{
    public enum LoadingLoreLanguage
    {
        Indonesian,
        English
    }

    [Serializable]
    public sealed class LoreEntry
    {
        [SerializeField] private Texture2D image;
        [SerializeField] private string titleIndonesian = "EDEN";
        [SerializeField, TextArea(2, 4)] private string textIndonesian =
            "Hutan mimpi. Saking indahnya, siapa pun yang masuk tidak akan bisa keluar.";
        [SerializeField] private string titleEnglish = "EDEN";
        [SerializeField, TextArea(2, 4)] private string textEnglish =
            "A forest of dreams. So beautiful that no one who enters can ever leave.";
        [SerializeField, Min(0.1f)] private float displayDuration = 7f;

        public Texture2D Image => image;
        public float DisplayDuration => displayDuration;

        public string GetTitle(LoadingLoreLanguage language) =>
            language == LoadingLoreLanguage.English ? titleEnglish : titleIndonesian;

        public string GetText(LoadingLoreLanguage language) =>
            language == LoadingLoreLanguage.English ? textEnglish : textIndonesian;
    }

    [CreateAssetMenu(fileName = "LoadingLoreData", menuName = "Prince/Loading Lore Data")]
    public sealed class LoadingLoreData : ScriptableObject
    {
        [Header("Lore")]
        [SerializeField] private List<LoreEntry> entries = new List<LoreEntry>();
        [SerializeField] private LoadingLoreLanguage activeLanguage = LoadingLoreLanguage.Indonesian;

        [Header("Loading behavior")]
        [SerializeField] private bool enableLoadingScreen = true;
        [SerializeField] private bool useLoreSlides = true;
        [SerializeField] private bool randomizeSlides = true;
        [SerializeField] private bool useCrossFade = true;
        [SerializeField] private bool useSlowZoom = true;
        [SerializeField] private bool showProgressBar = true;
        [SerializeField] private bool waitForInput = true;
        [SerializeField] private bool useScreenFade = true;

        [Header("Timing and motion")]
        [SerializeField, Min(0f)] private float minimumDisplayDuration = 1.5f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.4f;
        [SerializeField, Min(0f)] private float crossFadeDuration = 0.6f;
        [SerializeField, Range(0f, 0.1f)] private float zoomAmount = 0.05f;
        [SerializeField, Min(0.01f)] private float progressUnitsPerSecond = 0.8f;

        public IReadOnlyList<LoreEntry> Entries => entries;
        public LoadingLoreLanguage ActiveLanguage => activeLanguage;
        public bool EnableLoadingScreen => enableLoadingScreen;
        public bool UseLoreSlides => useLoreSlides;
        public bool RandomizeSlides => randomizeSlides;
        public bool UseCrossFade => useCrossFade;
        public bool UseSlowZoom => useSlowZoom;
        public bool ShowProgressBar => showProgressBar;
        public bool WaitForInput => waitForInput;
        public bool UseScreenFade => useScreenFade;
        public float MinimumDisplayDuration => minimumDisplayDuration;
        public float FadeDuration => fadeDuration;
        public float CrossFadeDuration => crossFadeDuration;
        public float ZoomAmount => zoomAmount;
        public float ProgressUnitsPerSecond => progressUnitsPerSecond;

        public void SetLanguage(LoadingLoreLanguage language)
        {
            activeLanguage = language;
        }

    }
}
