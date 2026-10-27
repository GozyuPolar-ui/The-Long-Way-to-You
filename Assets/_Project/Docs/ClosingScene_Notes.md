# Closing Scene

## Kondisi sebelum

- Scene utama yang ada adalah `Assets/_Project/Scene/Main-Menu.unity` dan scene hutan `Assets/_Project/Scene/UNS_Demo.unity`. Main Menu menggunakan `MainMenuCanvas`, `MainMenuCamera` dengan `AudioListener`, `MainMenuEventSystem`, `MenuVideoBackground`, `MenuMusicPlayer`, serta `MainMenuController`. Canvas scaler memakai referensi 1920 x 1080; tombol dan tipografinya dibuat runtime memakai `MenuUIFactory`, Cinzel Decorative Bold, dan Cormorant Garamond SemiBold.
- Video Main Menu adalah `Assets/_Project/UI/MainMenu/gemini_generated_video_728ee3eb.mp4` dengan RenderTexture yang sudah dipakai scene. BGM yang dipakai ulang adalah `Assets/_Project/Audio/MAIN MENU/royaltyfreemusicstudio-medieval-fantasy-music-462199.mp3`. Audio video Main Menu sudah dimatikan.
- Quest tas menggunakan `QuestPickup` dan `QuestManager`. Pickup menandai objective `find_bram_backpack`; penyelesaian quest hanya dipancarkan setelah Bram menjalankan `complete_quest`.
- `QuestManager` sudah persisten dan menyimpan progres quest untuk sesi berjalan. `SceneLoader` dan layar loading lore sudah ada dan dipakai untuk perpindahan scene.
- Folder bernama `UI-Main Menu` tidak ditemukan. Aset UI yang dipakai berada di folder `Assets/_Project/UI/MainMenu`.
- Build Settings sebelum setup berisi `Main-Menu`, `UNS_Demo`, dan `SampleScene`. Tool menambahkan scene Closing secara aditif dan mempertahankan entri yang sudah ada.

## Perubahan dan aset yang dipakai ulang

Scene baru `Assets/_Project/Scene/Closing.unity` dibuat dengan menyalin scene Main Menu melalui `AssetDatabase.CopyAsset`. Salinan mempertahankan kamera/listener, EventSystem, Canvas, VideoPlayer, RenderTexture, video, BGM, MenuMusicPlayer, serta referensi font yang sama. Pada salinan saja, `MainMenuController` dan `SceneFaderCanvas` dihapus; video tetap tanpa audio. UI penutup baru dibangun di Canvas memakai gaya tombol `MenuUIFactory`.

File baru:

- Runtime: `Assets/_Project/Scripts/Runtime/UI/ClosingData.cs`, `ClosingSceneController.cs`, `QuestCompleteTrigger.cs`, `PostQuestExploreMode.cs`, `Assets/_Project/Scripts/Runtime/UI/Closing/QuestProgress.cs`, beserta `.meta`.
- Setup: `Assets/_Project/Scripts/Editor/PrinceClosingSceneSetup.cs` beserta `.meta`.
- Data dibuat tool: `Assets/_Project/Data/Closing/ClosingData.asset`.
- Scene dibuat tool: `Assets/_Project/Scene/Closing.unity`.
- Catatan ini: `Assets/_Project/Docs/ClosingScene_Notes.md`.

File lama yang diubah saat tool dijalankan:

- `Assets/_Project/Scene/UNS_Demo.unity`: menambahkan `QuestCompleteTrigger` dan `PostQuestExploreMode` pada `BackpackQuest_Runtime`. Backup dibuat sebelum perubahan di `Assets/_Project/Docs/ClosingBackup~/UNS_Demo.unity.bak`.
- `ProjectSettings/EditorBuildSettings.asset`: menambahkan `Assets/_Project/Scene/Closing.unity` tanpa mengganti entri lama. Backup dibuat sebelum perubahan di `Assets/_Project/Docs/ClosingBackup~/EditorBuildSettings.asset.bak`.

`Main-Menu.unity`, pickup/quest, beruang, combat, Starter Assets, `SceneLoader`, dan loading screen tidak diubah. Backup dibuat sekali saja; setup berulang tidak menimpa backup.

## Alur dan konfigurasi

1. Setelah `complete_quest hunter_backpack`, trigger menunggu dialog Bram tertutup dan `DialogueInputLock` terlepas, menunggu 1,8 detik waktu nyata, lalu memanggil `SceneFader.LoadScene("Closing")`. Flag penutup disimpan pada event quest selesai dan penjaga satu-kali mencegah transisi ganda.
2. Scene Closing membuka kursor, mengembalikan `Time.timeScale` ke 1, lalu menampilkan ucapan terima kasih, lore penutup EDEN, dan tombol. SceneLoader tetap menangani transisi/loading.
3. **Lanjut Menjelajah** memuat ulang `UNS_Demo` secara single-scene. `PostQuestExploreMode` menyembunyikan target tas jika flag selesai; opsi **Calm Bears After Quest** aktif secara default dan menonaktifkan komponen `BearAI` tanpa mengubah script beruang. Pemain dibuat ulang oleh scene hutan pada titik awalnya.
4. **Saran & Kritik** dan tombol kontak hanya tampil jika URL/email diisi. Kontak developer membuka GitHub, Instagram, atau mailto; tombol salin email memakai clipboard sistem dan konfirmasi yang memudar.
5. **Menu Utama** kembali ke scene yang path Build Settings-nya bernama `Main-Menu`.

`ClosingData` menyimpan teks Indonesia/Inggris, bahasa aktif, URL saran, GitHub, Instagram, email (semuanya kosong secara default), sprite QR saran/kritik, nama scene hutan/menu, delay tas lama, fade penutup, dan dim latar 0,42. Alur masuk Closing memakai jeda 1,8 detik sesudah dialog selesai dan durasi fade yang dikonfigurasi pada `SceneFader`. Isi lore Indonesia adalah **Bab 1: Hutan yang Tenang** beserta narasi yang diberikan. Tool mengimpor `Assets/_Project/UI/Feedback/qrcode.png` sebagai Sprite tunggal tanpa kompresi dengan filter Point, lalu menetapkannya ke ClosingData. Tombol **Saran & Kritik** menampilkan/menyembunyikan QR. Penyesuaian toggle beruang tersedia di komponen `PostQuestExploreMode` pada scene hutan.

`QuestProgress.Reset()` menghapus hanya flag PlayerPrefs penutup. Tool juga menyediakan **Tools > Prince > Reset Closing Quest Progress** untuk reset saat menguji.

## Batasan verifikasi

- Tidak membuka Unity, menjalankan tool, membuka Play Mode, atau mengambil screenshot.
- Belum dapat menguji kompilasi/import aktual Unity, video dan BGM saat diputar di scene salinan, tampilan/teks di atas video dan rasio layar, navigasi perangkat, transisi kursor/action map setelah pindah scene, perilaku beruang setelah flag quest, pemuatan ulang posisi pemain, URL/email di build, ataupun perpindahan `SceneLoader` secara runtime.
- Source video, musik, font, dan konfigurasi Main Menu telah diperiksa statis dari scene/aset. Scene baru serta backup baru benar-benar dibuat ketika tool dijalankan di Unity.

## Langkah manual

1. Buka proyek dan tunggu kompilasi selesai.
2. Jalankan **Tools > Prince > Setup Closing Scene** bila scene Closing atau entri Build Settings belum disiapkan.
3. Jalankan **Tools > Prince > Fix Backpack Quest Flow** untuk menerapkan objective beruang-pickup-turn-in dan memasang referensi/pemicu Closing yang benar.
4. Buka `Assets/_Project/Data/Closing/ClosingData.asset`; tombol **Saran & Kritik** menampilkan QR yang dipasang tool. Isi `Feedback Url`, `GitHub Url`, `Instagram Url`, dan `Developer Email` bila ingin memakai alamat web/kontak tambahan. Field kontak yang kosong menyembunyikan tombolnya.
5. Uji alur dari scene hutan: kalahkan beruang, ambil tas, serahkan ke Bram, dan lanjutkan dialog sampai selesai; periksa jeda, fade, layar loading, dan penutup. Tombol **Lanjut Menjelajah** memuat ulang hutan dengan tas tersembunyi dan beruang tenang.
6. Untuk menguji ulang, hentikan Play Mode, jalankan **Tools > Prince > Reset Closing Quest Progress**, lalu mulai sesi Play Mode baru dan ulangi quest dari awal. Reset ini hanya menghapus flag PlayerPrefs penutup; `QuestManager` juga menyimpan status quest di memori sampai sesi berjalan berakhir.
7. Untuk membatalkan perubahan pada scene hutan atau Build Settings, pulihkan masing-masing `.bak` dari `Assets/_Project/Docs/ClosingBackup~/` ke path aslinya. Hapus scene Closing/data baru secara manual bila ingin menghapus fitur sepenuhnya; Main Menu asli tidak pernah ditimpa.
