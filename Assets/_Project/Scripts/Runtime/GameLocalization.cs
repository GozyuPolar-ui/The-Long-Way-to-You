using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    public static class GameLocalization
    {
        private const string LanguagePreferenceKey = "Prince.Language";
        private static readonly Dictionary<string, string> IndonesianToEnglish =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> EnglishToIndonesian =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public static event Action<LoadingLoreLanguage> LanguageChanged;

        public static LoadingLoreLanguage CurrentLanguage
        {
            get
            {
                int language = PlayerPrefs.GetInt(
                    LanguagePreferenceKey, (int)LoadingLoreLanguage.Indonesian);
                return language == (int)LoadingLoreLanguage.English
                    ? LoadingLoreLanguage.English
                    : LoadingLoreLanguage.Indonesian;
            }
        }

        static GameLocalization()
        {
            Add("Perjalanan yang tenang", "A quiet journey");
            Add("Lanjutkan", "Continue");
            Add("Permainan Baru", "New Game");
            Add("Pengaturan", "Settings");
            Add("Keluar", "Quit");
            Add("UJI BETA", "BETA TEST");
            Add("Volume Utama", "Master Volume");
            Add("Musik", "Music");
            Add("Efek", "Effects");
            Add("Kualitas Grafis", "Graphics Quality");
            Add("Layar Penuh", "Fullscreen");
            Add("Sensitivitas Kamera", "Camera Sensitivity");
            Add("Bahasa", "Language");
            Add("Kembali", "Back");
            Add("Mulai permainan baru? Progres saat ini akan diganti.",
                "Start a new game? Your current progress will be replaced.");
            Add("Keluar dari permainan?", "Quit the game?");
            Add("Ya", "Yes");
            Add("Batal", "Cancel");
            Add("Bawaan", "Default");
            Add("Rendah", "Low");
            Add("Sedang", "Medium");
            Add("Tinggi", "High");
            Add("Ultra", "Ultra");
            Add("© {0} ExceedOG. Hak cipta dilindungi.",
                "© {0} ExceedOG. All rights reserved.");

            Add("Tekan E untuk berbicara: {0}", "Press E to talk: {0}");
            Add("Tekan E untuk mengambil {0}", "Press E to pick up {0}");
            Add("Tekan E untuk melanjutkan", "Press E to continue");
            Add("Bicara dengan Bram untuk menerima misi", "Talk to Bram to accept the quest");
            Add("Beruang itu sedang menjaganya.", "The bear is guarding it.");
            Add("Tekan E untuk berinteraksi", "Press E to interact");
            Add("Tekan tombol apa saja untuk melanjutkan", "Press any button to continue");
            Add("Warga", "Villager");
            Add("Pangeran", "Prince");
            Add("Hutan", "Forest");
            Add("Wilayah", "Area");
            Add("Ransel Bram", "Bram's Backpack");
            Add("Pandai Besi 01", "Blacksmith 01");
            Add("Pandai Besi 02", "Blacksmith 02");
            Add("Pandai Besi 03", "Blacksmith 03");
            Add("Tetua 01", "Elder 01");
            Add("Tetua 02", "Elder 02");
            Add("Tetua 03", "Elder 03");
            Add("Petani 01", "Farmer 01");
            Add("Petani 02", "Farmer 02");
            Add("Petani 03", "Farmer 03");
            Add("Penjaga 01", "Guard 01");
            Add("Penjaga 02", "Guard 02");
            Add("Penjaga 03", "Guard 03");
            Add("Pedagang 01", "Merchant 01");
            Add("Pedagang 02", "Merchant 02");
            Add("Pedagang 03", "Merchant 03");
            Add("Warga 01", "Villager 01");
            Add("Warga 02", "Villager 02");
            Add("Warga 03", "Villager 03");
            Add("Sangat Rendah", "Very Low");
            Add("Sangat Tinggi", "Very High");

            Add("MISI AKTIF", "ACTIVE QUEST");
            Add("MISI SELESAI", "QUEST COMPLETE");
            Add("Belum ada misi", "No active quest");
            Add("Misi", "Quest");
            Add("CATATAN LAPANGAN", "FIELD NOTES");
            Add("MISI YANG AKTIF", "ACTIVE QUESTS");
            Add("LEGENDA", "LEGEND");
            Add("Titik Jalan", "Waypoint");
            Add("Musuh", "Enemy");
            Add("Kemah", "Campfire");
            Add("Pemain", "Player");
            Add("NPC", "NPC");
            Add("KEMBALI KE PANGERAN", "RETURN TO THE PRINCE");
            Add("UNGKAP SEMUA  [DEBUG]", "REVEAL ALL  [DEBUG]");
            Add("M / Esc / B: Tutup  |  Scroll / LB-RB: Zoom  |  Seret / Stick: Geser  |  Klik kanan: Tandai",
                "M / Esc / B: Close  |  Scroll / LB-RB: Zoom  |  Drag / Stick: Pan  |  Right-click: Mark");
            Add("Wilayah ini menanti untuk dijelajahi.",
                "This region is waiting to be explored.");
            Add("Jelajahi jalur, hutan, dan tempat-tempat penting di wilayah ini.",
                "Explore the trails, forests, and landmarks across this region.");

            Add("Ransel Sang Pemburu", "The Hunter's Backpack");
            Add("Ransel Bram sudah dikembalikan. Hadiah: Peta Hutan.",
                "Bram's backpack has been returned. Reward: Forest Map.");
            Add("Bram kehilangan ranselnya saat dikejar babi hutan. Cari di dekat sungai.",
                "Bram lost his backpack while being chased by a wild boar. Search near the river.");
            Add("Kembalilah ke Bram dan serahkan ranselnya.",
                "Return to Bram and hand over the backpack.");
            Add("Kalahkan beruang yang menjaga ransel.",
                "Defeat the bear guarding the backpack.");
            Add("Ambil ransel Bram di dekat sungai.",
                "Pick up Bram's backpack near the river.");
            Add("Kau harus menghadapi beruang itu lebih dulu. Hati-hati.",
                "You'll have to deal with that bear first. Be careful.");
            Add("Hei, kau! Jangan lewat situ sembarangan. Eh... pakaianmu bagus sekali. Kau bukan orang sini, ya?",
                "Hey, you! Don't wander through here carelessly. Oh... those are fine clothes. You're not from around here, are you?");
            Add("Aku sedang dalam perjalanan menuju kerajaan seberang. Ada yang bisa kubantu?",
                "I'm on my way to the kingdom beyond. Is there anything I can help you with?");
            Add("Perjalanan jauh, ya. Hmm... Sebenarnya ada. Tadi pagi aku dikejar babi hutan di dekat sungai. Aku lari tunggang-langgang dan ranselku jatuh di sana.",
                "A long journey, eh? Hmm... As a matter of fact, there is. A wild boar chased me near the river this morning. I ran for my life and dropped my backpack there.");
            Add("Di dalamnya ada peta, tali, dan sedikit bekalku. Tanpa itu aku tidak bisa berburu seminggu ini.",
                "It has a map, rope, and some of my supplies. Without it, I can't hunt for a week.");
            Add("Tenang saja. Di mana tepatnya?", "Don't worry. Where exactly?");
            Add("Aku akan mengambilnya.", "I'll get it.");
            Add("Seperti apa ransel itu?", "What does the backpack look like?");
            Add("Maaf, aku belum bisa sekarang.", "Sorry, I can't right now.");
            Add("Tidak apa-apa. Aku tidak ke mana-mana. Kembalilah kapan saja.",
                "That's all right. I'm not going anywhere. Come back whenever you can.");
            Add("Sudah ketemu? Dekat sungai, ranselnya cokelat dengan tali merah. Ikuti jalan setapak saja.",
                "Have you found it? It's near the river, brown with a red strap. Just follow the trail.");
            Add("Belum, aku masih mencari.", "Not yet, I'm still looking.");
            Add("Pelan-pelan saja. Hutan ini tidak ke mana-mana.",
                "Take your time. The forest isn't going anywhere.");
            Add("Bram, ini ranselmu.", "Bram, here's your backpack.");
            Add("Itu dia! Astaga, kau benar-benar menemukannya!", "There it is! Goodness, you really found it!");
            Add("Tali merahnya masih ada. Petaku... masih utuh. Bekalku... yah, tinggal separuh. Pasti dimakan tikus.",
                "The red strap is still there. My map... it's intact. My supplies... well, half are gone. Must've been mice.");
            Add("Terima kasih, Nak. Tidak ada yang mau repot membantu pemburu tua seperti aku.",
                "Thank you, lad. No one usually bothers helping an old hunter like me.");
            Add("Aku senang bisa membantu.", "I'm glad I could help.");
            Add("Tunggu. Ambil ini, Peta Hutan sebagai hadiah kecil dariku.",
                "Wait. Take this Forest Map as a small gift from me.");
            Add("Kalau kau lewat desa di timur, sebut namaku. Kau akan disambut dengan baik di sana.",
                "If you pass through the village to the east, give them my name. They'll welcome you there.");
            Add("Hati-hati di jalan. Semoga perjalananmu lancar.",
                "Take care on the road. I hope your journey goes well.");
            Add("Cokelat tua, kulitnya sudah belel, ada tali merah di sisinya. Susah dilewatkan.",
                "Dark brown, worn leather, with a red strap on the side. Hard to miss.");
            Add("Oh, satu lagi. Babi itu masih berkeliaran di sekitar sana. Jangan sok berani. Kalau pingsan, kau akan terbangun di api unggun. Aku sudah pernah.",
                "Oh, one more thing. That boar is still roaming around there. Don't be a hero. If you faint, you'll wake up by the campfire. It's happened to me.");
            Add("Kau baik sekali, Pangeran... eh, maksudku, orang asing. Ikuti jalan setapak ke arah sungai. Ranselnya jatuh dekat situ.",
                "That's kind of you, Prince... I mean, stranger. Follow the trail toward the river. The backpack fell nearby.");
            Add("Hati-hati dengan babi itu. Hindari dulu kalau bisa, hadapi kalau perlu.",
                "Watch out for that boar. Avoid it if you can; face it if you must.");

            Add("Tekan E untuk berbicara", "Press E to talk");
        }

        public static void SetLanguage(LoadingLoreLanguage language)
        {
            if (language != LoadingLoreLanguage.Indonesian &&
                language != LoadingLoreLanguage.English)
            {
                Debug.LogWarning($"Unsupported game language '{language}'.");
                return;
            }

            bool languageChanged = CurrentLanguage != language;
            if (!languageChanged && PlayerPrefs.HasKey(LanguagePreferenceKey))
                return;

            PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
            PlayerPrefs.Save();
            if (!languageChanged)
                return;

            LanguageChanged?.Invoke(language);
            RefreshVisibleText();
            SceneLoader.SetLanguage(language);
        }

        public static string Get(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            const string studioMarker = " ExceedOG.";
            int studioIndex = value.IndexOf(studioMarker, StringComparison.Ordinal);
            if (value.StartsWith("© ", StringComparison.Ordinal) &&
                studioIndex > 2 &&
                int.TryParse(value.Substring(2, studioIndex - 2), NumberStyles.None,
                    CultureInfo.InvariantCulture, out _))
            {
                string year = value.Substring(2, studioIndex - 2);
                string rights = CurrentLanguage == LoadingLoreLanguage.Indonesian
                    ? "Hak cipta dilindungi."
                    : "All rights reserved.";
                return $"© {year}{studioMarker} {rights}";
            }

            Dictionary<string, string> translations =
                CurrentLanguage == LoadingLoreLanguage.English
                    ? IndonesianToEnglish
                    : EnglishToIndonesian;
            return translations.TryGetValue(value, out string translated) ? translated : value;
        }

        public static string Get(string format, params object[] arguments)
        {
            string localizedFormat = Get(format);
            return string.Format(CultureInfo.CurrentCulture, localizedFormat, arguments);
        }

        public static void RefreshVisibleText()
        {
            TMP_Text[] tmpTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < tmpTexts.Length; i++)
                if (tmpTexts[i] != null)
                    tmpTexts[i].text = Get(tmpTexts[i].text);

            Text[] legacyTexts = UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < legacyTexts.Length; i++)
                if (legacyTexts[i] != null)
                    legacyTexts[i].text = Get(legacyTexts[i].text);
        }

        private static void Add(string indonesian, string english)
        {
            IndonesianToEnglish[indonesian] = english;
            EnglishToIndonesian[english] = indonesian;
        }
    }
}
