# Loading Screen Lore

## Kondisi sebelum

- Folder `UI-Main Menu` tidak ditemukan di bawah `Assets`. Di folder yang benar-benar ada, `Assets/_Project/UI/MainMenu/`, ditemukan `LoadingIMG.jpeg` dan `Gambar backup.jpeg`. Foto `LoadingIMG.jpeg` dipilih sebagai gambar lore EDEN; nama file dan lokasinya dipertahankan.
- `LoadingIMG.jpeg` berukuran 2752 x 1536 dan belum memiliki file `.meta` saat inspeksi, sehingga pengaturan import sebelumnya belum ditetapkan. Tool akan mencatat nilai importer sebelum perubahan lalu mengaturnya ke Texture Type `Default`, Max Size `2048`, Compression `Compressed`, sRGB aktif, dan mipmap nonaktif untuk penggunaan UI.
- Sistem yang sudah ada dan dipertahankan: `SceneFader` untuk fallback pemindahan scene serta pemanggil pemuatan scene di `MainMenuController`. Scene Main Menu dan Build Settings tidak diubah. Inspeksi awal Build Settings menemukan `Main-Menu`, `UNS_Demo`, dan `SampleScene`; status saat ini tidak disunting oleh implementasi ini.
- Tidak ditemukan SceneLoader atau layar loading lore yang sudah ada untuk dipakai ulang. Integrasi menu hanya mengarahkan pemanggilan load game yang ada ke API baru `SceneLoader.Load`.
- Font TMP Cormorant Garamond SemiBold tersedia di `Assets/_Project/Fonts/TMP/CormorantGaramond-SemiBold SDF.asset`, jadi dipakai sebagai font loading.
- Saat pemeriksaan dimulai, worktree sudah memiliki perubahan lain termasuk `.gitignore` dan beberapa file `ProjectSettings`; perubahan tersebut tidak disentuh oleh implementasi loading screen ini.

## Fitur yang ditambahkan

- `LoadingLoreData` menyimpan entri bilingual dan semua toggle/durasi fitur.
- `SceneLoader` melakukan validasi nama scene terhadap Build Settings, memuat dengan `LoadSceneAsync`, menahan aktivasi hingga progress siap, minimum duration terpenuhi, dan input diterima (atau fitur input dimatikan). Overlay tetap menutup layar sampai operasi aktivasi selesai, scene tujuan terverifikasi sebagai scene aktif, dan satu frame scene baru sempat dirender; fade-out baru dimulai setelahnya. Pemanggilan ganda saat memuat diabaikan.
- UI loading overlay menggunakan dua lapisan gambar untuk cross-fade, teks lore, indikator titik, progress bar halus berbasis waktu unscaled, zoom opsional, prompt input, dan fade layar. Layout kini lebih minimal: teks kecil berwarna krem-emas di kiri bawah, gradien bawah yang lebih pendek, serta indikator dan progress bar tipis tanpa panel/kotak.
- Foto dipegang sebagai referensi langsung pada `LoadingLoreData` karena jumlah entri awal kecil. Hanya gambar aktif dan berikutnya yang dipasang pada dua `RawImage`; gambar sebelumnya dilepas dari UI setelah transisi.
- API `SceneLoader.SetLanguage(LoadingLoreLanguage)` tersedia untuk integrasi pengaturan bahasa kelak. Tidak ada perubahan pada panel Pengaturan.
- Tool editor idempoten `Tools > Prince > Setup Loading Screen` membuat atau memperbarui data serta prefab Resources tanpa memodifikasi scene atau Build Settings. Prefab yang sudah ada dipertahankan dan hanya referensi data/komponen yang diselaraskan.

## Parameter LoadingLoreData

| Parameter | Default | Fungsi |
|---|---:|---|
| Active Language | Indonesian | Memilih teks Indonesia atau Inggris |
| Enable Loading Screen | On | Memakai layar baru; jika off, kembali ke transisi lama |
| Use Lore Slides | On | Menampilkan foto dan teks lore |
| Randomize Slides | On | Mengacak urutan tanpa pengulangan berurutan |
| Use Cross Fade | On | Cross-fade antar gambar |
| Use Slow Zoom | On | Zoom pelan pada foto |
| Show Progress Bar | On | Menampilkan progress bar |
| Wait For Input | On | Menunggu input setelah loading selesai |
| Use Screen Fade | On | Fade masuk dan keluar |
| Minimum Display Duration | 1.5 s | Durasi minimum tampilan loading |
| Fade Duration | 0.4 s | Durasi fade layar |
| Cross Fade Duration | 0.6 s | Durasi pergantian slide |
| Zoom Amount | 0.05 | Skala akhir 1.05x |
| Progress Units Per Second | 0.8 | Kecepatan progress visual |
| Lore entry display duration | 7 s | Durasi awal untuk tiap entri |

Entri awal:

- Indonesia: **EDEN**: “Hutan mimpi. Saking indahnya, siapa pun yang masuk tidak akan bisa keluar.”
- English: **EDEN**: “A forest of dreams. So beautiful that no one who enters can ever leave.”

## File dibuat atau diubah

Dibuat:

- `Assets/_Project/Scripts/Runtime/UI/LoadingLoreData.cs` dan `.meta`
- `Assets/_Project/Scripts/Runtime/UI/LoreSlideView.cs` dan `.meta`
- `Assets/_Project/Scripts/Runtime/UI/LoadingScreenController.cs` dan `.meta`
- `Assets/_Project/Scripts/Runtime/UI/SceneLoader.cs` dan `.meta`
- `Assets/_Project/Scripts/Editor/PrinceLoadingScreenSetup.cs` dan `.meta`
- `Assets/_Project/Docs/LoadingScreen_Notes.md`
- Backup asli `MainMenuController.cs` di `Assets/_Project/Docs/LoadingBackup~/MainMenuController.cs.bak`

Diubah:

- `Assets/_Project/Scripts/Runtime/UI/MainMenuController.cs`: satu pemanggilan `SceneFader.LoadScene` diarahkan ke `SceneLoader.Load`.

Prefab `Assets/_Project/Resources/LoadingScreen/SceneLoader.prefab` dan data `LoadingLoreData.asset` dibuat saat tool editor dijalankan, bukan ditulis manual. Build Settings tidak ditambah; karena itu semua scene tujuan tetap harus sudah terdaftar di Build Settings.

## Batasan verifikasi

- Tidak menjalankan Unity, tool setup, scene, atau Play Mode sesuai batasan tugas.
- Belum dapat menguji tampilan pada berbagai resolusi, hasil import gambar di Unity, keterbacaan di atas foto, kehalusan fade/zoom pada sekitar 19 FPS, input aktual, atau kecepatan progress asli saat scene dimuat.
- Tool mencatat pengaturan import sebelum/sesudah saat dijalankan. Karena file gambar belum memiliki `.meta` ketika inspeksi, nilai importer sebelumnya tidak dapat dilaporkan sebagai nilai konfigurasi yang sudah tersimpan.

## Langkah manual

1. Buka proyek di Unity dan tunggu import aset selesai.
2. Jalankan `Tools > Prince > Setup Loading Screen`; tool membuat atau memperbarui prefab termasuk layout minimalis terbaru. Periksa Console untuk hasil import gambar dan peringatan.
3. Pastikan setiap scene tujuan sudah ada di Build Settings; tool tidak mengubah daftar tersebut.
4. Uji tombol Continue/New Game dari menu. Loading hanya fade-out setelah scene tujuan aktif; pastikan scene tujuan tercantum di Build Settings.
5. Untuk lore baru, buka `Assets/_Project/Resources/LoadingScreen/LoadingLoreData.asset`, tambah elemen pada **Entries**, isi gambar, judul dan teks Indonesia/Inggris, serta durasi tampil.
6. Untuk mematikan fitur, ubah toggle terkait pada asset tersebut. Untuk mematikan seluruh sistem, nonaktifkan **Enable Loading Screen**.
7. Jika perlu membatalkan integrasi menu, pulihkan isi `Assets/_Project/Docs/LoadingBackup~/MainMenuController.cs.bak` ke `Assets/_Project/Scripts/Runtime/UI/MainMenuController.cs`.
