using UnityEngine;

namespace Prince
{
    [CreateAssetMenu(fileName = "ClosingData", menuName = "Prince/Closing Data")]
    public sealed class ClosingData : ScriptableObject
    {
        [Header("Language")]
        [SerializeField] private LoadingLoreLanguage activeLanguage = LoadingLoreLanguage.Indonesian;

        [Header("Indonesian")]
        [SerializeField] private string titleIndonesian = "Terima kasih";
        [SerializeField, TextArea(2, 4)] private string messageIndonesian =
            "Terima kasih sudah menemani perjalanan ini dalam versi beta. Dukungan dan masukanmu berarti besar bagi kami.";
        [SerializeField] private string loreTitleIndonesian = "Bab 1: Hutan yang Tenang";
        [SerializeField, TextArea(2, 4)] private string loreIndonesian =
            "Sang pangeran meninggalkan rumahnya dengan sepucuk surat, sebuah janji, dan jalan panjang di depan. Di dalam hutan, seorang pemburu tua meminta bantuan kecil, dan bantuan kecil itu menjadi petualangan kecil. Ketika api unggun menyala, kastil di bukit yang jauh mulai berpendar. Perjalanan masih berlanjut.";
        [SerializeField] private string exploreButtonIndonesian = "Lanjut Menjelajah";
        [SerializeField] private string feedbackButtonIndonesian = "Saran & Kritik";
        [SerializeField] private string contactButtonIndonesian = "Hubungi Developer";
        [SerializeField] private string mainMenuButtonIndonesian = "Menu Utama";
        [SerializeField] private string githubButtonIndonesian = "GitHub";
        [SerializeField] private string instagramButtonIndonesian = "Instagram";
        [SerializeField] private string emailButtonIndonesian = "Email";
        [SerializeField] private string copyEmailButtonIndonesian = "Salin Email";
        [SerializeField] private string copiedIndonesian = "Tersalin";

        [Header("Feedback")]
        [SerializeField] private Sprite feedbackQr;

        [Header("English")]
        [SerializeField] private string titleEnglish = "Thank You";
        [SerializeField, TextArea(2, 4)] private string messageEnglish =
            "Thank you for sharing this journey in its beta. Your support and feedback mean a great deal to us.";
        [SerializeField] private string loreTitleEnglish = "Chapter 1: The Quiet Forest";
        [SerializeField, TextArea(2, 4)] private string loreEnglish =
            "The Prince left home with a letter, a promise, and a long road ahead. In the forest, an old hunter asked for a small favor, and that favor became a small adventure. As the campfire glowed, the distant castle on the hill began to shine. The journey continues.";
        [SerializeField] private string exploreButtonEnglish = "Keep Exploring";
        [SerializeField] private string feedbackButtonEnglish = "Feedback";
        [SerializeField] private string contactButtonEnglish = "Contact the Developer";
        [SerializeField] private string mainMenuButtonEnglish = "Main Menu";
        [SerializeField] private string githubButtonEnglish = "GitHub";
        [SerializeField] private string instagramButtonEnglish = "Instagram";
        [SerializeField] private string emailButtonEnglish = "Email";
        [SerializeField] private string copyEmailButtonEnglish = "Copy Email";
        [SerializeField] private string copiedEnglish = "Copied";

        [Header("Optional links (leave empty to hide)")]
        [SerializeField] private string feedbackUrl = string.Empty;
        [SerializeField] private string githubUrl = string.Empty;
        [SerializeField] private string instagramUrl = string.Empty;
        [SerializeField] private string developerEmail = string.Empty;

        [Header("Scene transition")]
        [SerializeField, Min(0f)] private float bagTakenDelay = 2.5f;
        [SerializeField, Min(0f)] private float transitionFadeDuration = 0.6f;
        [SerializeField, Range(0.35f, 0.5f)] private float backgroundDim = 0.42f;
        [SerializeField] private string forestSceneName = "UNS_Demo";
        [SerializeField] private string mainMenuSceneName = "Main-Menu";

        public LoadingLoreLanguage ActiveLanguage => activeLanguage;
        public string FeedbackUrl => feedbackUrl;
        public string GitHubUrl => githubUrl;
        public string InstagramUrl => instagramUrl;
        public string DeveloperEmail => developerEmail;
        public Sprite FeedbackQr => feedbackQr;
        public float BagTakenDelay => bagTakenDelay;
        public float TransitionFadeDuration => transitionFadeDuration;
        public float BackgroundDim => backgroundDim;
        public string ForestSceneName => forestSceneName;
        public string MainMenuSceneName => mainMenuSceneName;

        public string Title => Localized(titleIndonesian, titleEnglish);
        public string Message => Localized(messageIndonesian, messageEnglish);
        public string LoreTitle => Localized(loreTitleIndonesian, loreTitleEnglish);
        public string Lore => Localized(loreIndonesian, loreEnglish);
        public string ExploreButton => Localized(exploreButtonIndonesian, exploreButtonEnglish);
        public string FeedbackButton => Localized(feedbackButtonIndonesian, feedbackButtonEnglish);
        public string ContactButton => Localized(contactButtonIndonesian, contactButtonEnglish);
        public string MainMenuButton => Localized(mainMenuButtonIndonesian, mainMenuButtonEnglish);
        public string GitHubButton => Localized(githubButtonIndonesian, githubButtonEnglish);
        public string InstagramButton => Localized(instagramButtonIndonesian, instagramButtonEnglish);
        public string EmailButton => Localized(emailButtonIndonesian, emailButtonEnglish);
        public string CopyEmailButton => Localized(copyEmailButtonIndonesian, copyEmailButtonEnglish);
        public string CopiedMessage => Localized(copiedIndonesian, copiedEnglish);

        public void SetLanguage(LoadingLoreLanguage language)
        {
            activeLanguage = language;
        }

        private string Localized(string indonesian, string english) =>
            activeLanguage == LoadingLoreLanguage.English ? english : indonesian;

#if UNITY_EDITOR
        public void ConfigureFeedbackQr(Sprite sprite)
        {
            feedbackQr = sprite;
        }

        public void ConfigureDefaults()
        {
            feedbackUrl = string.Empty;
            githubUrl = string.Empty;
            instagramUrl = string.Empty;
            developerEmail = string.Empty;
            bagTakenDelay = 2.5f;
            transitionFadeDuration = 0.6f;
            backgroundDim = 0.42f;
            forestSceneName = "UNS_Demo";
            mainMenuSceneName = "Main-Menu";
        }
#endif
    }
}
