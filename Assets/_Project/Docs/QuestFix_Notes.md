# Perbaikan Alur Quest Ransel Sang Pemburu

## Temuan awal

- Closing sebelumnya dipicu oleh `QuestCompleteTrigger` yang mendengarkan `QuestManager.OnObjectiveChanged` untuk objective `Collect` `find_bram_backpack`. `QuestPickup.Interact()` mengirim `QuestTarget.targetId` (`bram_backpack`) ke `NotifyTargetCollected`, lalu menonaktifkan objek ransel. Akibatnya Closing dimulai saat pickup, bukan setelah turn-in.
- Dialog Bram memanggil `complete_quest hunter_backpack` sebelum baris dialog penutup. Event selesai karena itu terjadi saat dialog masih terbuka dan input masih terkunci.
- `QuestManager` sebelumnya belum menyediakan event khusus quest selesai. Notifikasi target memproses objective pertama yang belum selesai agar urutan quest tidak bisa dilewati; pola ini dipertahankan untuk objective Defeat baru.
- Scene `UNS_Demo` menyimpan `BackpackQuestPlacement`, `QuestManager`, instance prefab `Bear_Enemy`, dan instance `Ransel_Bram`; objective quest sekarang memuat urutan Defeat, Collect, lalu TalkTo. HUD hanya menampilkan compass/tracker jika quest aktif dan target terdaftar.
- Akar penyebab gejala berulang adalah flag penyelesaian Closing `Prince.Closing.QuestCompleted` yang tersimpan di PlayerPrefs. `PostQuestExploreMode` sebelumnya memakai flag persisten itu sendirian untuk menyembunyikan tas dan menonaktifkan seluruh BearAI, meskipun status quest in-memory pada sesi baru masih `NotStarted`. Dengan tas tidak aktif, `QuestManager.TryGetTarget` memang tidak dapat menemukan `bram_backpack`, sehingga warning HUD dan tracker kosong muncul bersamaan dengan tas hilang dan bear tidak menyerang.

## Perubahan

- Quest sekarang berurutan: `Kalahkan beruang yang menjaga ransel.`, `Ambil ransel Bram di dekat sungai.`, lalu `Kembalilah ke Bram dan serahkan ranselnya.` HUD, compass, dan peta tetap melacak target Collect ransel ketika objective aktif adalah Defeat.
- `BearAI` melaporkan kematian ke `QuestManager`; target yang sudah kalah dicatat selama sesi agar objective juga terselesaikan bila quest baru dimulai setelah beruang mati.
- `QuestPickup` memiliki `requireBearDefeated` (default `true`) dan referensi Inspector `bearGuard`. Jika referensi kosong, komponen mencari beruang dalam `BackpackQuestPlacement.guardAreaRadius` (atau satu-satunya beruang di scene). Saat beruang hidup prompt menampilkan satu pesan singkat; bila beruang tidak ditemukan, satu `Debug.LogWarning` dicatat dan ransel tetap dapat diambil. Compass tetap menargetkan ransel.
- Dialog Bram menambahkan satu baris kondisional saat beruang masih hidup: “Kau harus menghadapi beruang itu lebih dulu. Hati-hati.”
- `QuestManager.OnQuestCompleted` sekarang dipancarkan setelah quest benar-benar selesai saat turn-in. Trigger Closing mendengarkan event ini, menunggu dialog tertutup dan `DialogueInputLock` terlepas, menyimpan state kembali setelah lock dilepas, menunggu 1,8 detik waktu nyata, lalu memuat Closing melalui `SceneFader`. Penjaga satu-kali menghindari transisi ganda.
- Jika HUD meminta target yang belum masuk ke registry (misalnya saat urutan callback `OnEnable`/pergantian scene membuat registrasi terlewat), `QuestManager.TryGetTarget` kini melakukan pencarian target aktif di scene lalu mendaftarkannya sebelum menyatakan target tidak ada. ID yang belum ditemukan dicache agar pencarian tidak diulang tiap frame; cache miss dibersihkan saat target mendaftar atau scene baru dimuat. Ini menjaga tracker tetap berfungsi tanpa pencarian berulang.
- `PostQuestExploreMode` kini menjalankan penyembunyian tas dan penenangan beruang hanya bila flag Closing persisten aktif **dan** `QuestManager` pada sesi berjalan menyatakan quest `hunter_backpack` berstatus `Completed`. Flag lama dari sesi sebelumnya tidak lagi menyembunyikan target atau mematikan AI; warning yang jelas dicatat untuk flag stale.
- Menu **Tools > Prince > Fix Backpack Quest Flow** menyelaraskan definisi quest dan prefab, mendaftarkan quest, memasang referensi beruang jika tersedia, mengarahkan beruang ke jalur Bram, menambahkan instance backpack hanya jika belum ada, serta memastikan trigger dan fader tidak digandakan. Target ransel kemudian tersedia bagi compass/tracker. Jika scene hutan masih dirty, tool meminta persetujuan untuk menyimpan perubahan scene terlebih dahulu; memilih Cancel membatalkan setup tanpa menghapus perubahan. Backup data/prefab/scene dibuat satu kali sebelum perubahan tool.
- Menu **Tools > Prince > Setup Bear Enemy** kini mengarahkan beruang baru ke Bram/jalur kedatangan, bukan hanya ke ransel.
- Menu **Tools > Prince > Setup Closing Scene** juga memastikan `SceneFader` tersedia di scene hutan. Sebelumnya menu ini memasang trigger Closing tetapi tidak memasang fader, sehingga quest bisa selesai tanpa transisi.
- Sistem respawn, dialog lain, tampilan peta/menu, dan kombat selain notifikasi kematian beruang tidak diubah; penentuan target peta hanya disesuaikan untuk mempertahankan marker ransel selama objective Defeat. Tidak ada aset Asset Store atau Starter Assets yang diedit.

## Backup sebelum perubahan kode/dokumen

Salinan asli berekstensi `.bak` berada di `Assets/_Project/Docs/QuestFixBackup~/`:

- `QuestDefinition.cs.bak`
- `QuestManager.cs.bak`
- `QuestManager.cs.before-target-registry-fix.bak`
- `PostQuestExploreMode.cs.before-stale-completion-fix.bak`
- `QuestFix_Notes.md.before-stale-completion-fix.bak`
- `QuestFix_Notes.md.before-target-registry-fix.bak`
- `QuestPickup.cs.bak`
- `BearAI.cs.bak`
- `DialogueInputLock.cs.bak`
- `QuestDialogueUI.cs.bak`
- `QuestTrackingHUD.cs.bak`
- `MapSystem.cs.bak`
- `BackpackQuest_Notes.md.bak`
- `QuestCompleteTrigger.cs.bak`
- `PrinceDialogueQuestSetup.cs.bak`
- `PrinceClosingSceneSetup.cs.bak`
- `PrinceBackpackQuestFix.cs.bak`
- `PrinceBearEnemySetup.cs.bak`
- `QuestFix_Notes.md.bak`
- `TestQuest.yarn.bak`
- `ClosingScene_Notes.md.bak`

Saat menu perbaikan dijalankan untuk pertama kali, menu tersebut juga membuat `Quest_RanselSangPemburu.asset.bak`, `QuestItem_Backpack.prefab.bak`, dan `UNS_Demo.unity.bak` di folder yang sama sebelum menulis perubahan data itu.

## Verifikasi dan langkah manual

- Pemeriksaan statis dilakukan pada alur event, IDs objective/target, guard pickup, fungsi dialog, pemuatan Closing, konfigurasi menu, dan backup. Brace serta pemanggilan konfigurasi yang terkait diperiksa.
- Penanganan registry target divalidasi secara statis: cache tetap menjadi jalur cepat; pada cache miss, manager mencari target aktif dengan ID persis lalu mengisi registry. Misses tidak mengulang pencarian per frame dan direset saat scene dimuat.
- Kondisi post-quest divalidasi secara statis: PlayerPrefs completion flag saja tidak cukup; status quest aktif pada sesi berjalan juga harus `Completed` sebelum tas disembunyikan atau BearAI dinonaktifkan.
- Tidak membuka Unity, menjalankan Play Mode, menguji alur quest saat Play, atau memverifikasi pemuatan/fade scene secara runtime. Kompilasi dan import aktual Unity belum diverifikasi.
- Untuk menerapkan data serialized: buka proyek, tunggu kompilasi, lalu jalankan **Tools > Prince > Fix Backpack Quest Flow**. Jika muncul dialog `Unsaved Forest Scene`, pilih **Save and Continue**; **Cancel** menghentikan setup. Tool akan menambah backpack ke posisi yang tersimpan di `BackpackQuestPlacement` jika belum ada.
- Jika log menyebut `no SceneFader`, jalankan **Tools > Prince > Setup Closing Scene** sekali lagi setelah kompilasi; tool kini memasang fader di scene hutan secara idempoten.
- Untuk memulihkan setup scene yang terdeteksi sekarang, jalankan **Tools > Prince > Fix Backpack Quest Flow**. Tool akan membuat tas jika belum ada, memperbarui urutan objective, mengikat tas ke beruang yang sudah berada di scene, dan mengarahkan beruang ke jalur Bram. Jangan jalankan **Setup Bear Enemy** jika instance `Bear_Enemy` sudah ada; tool tersebut hanya diperlukan bila bear memang tidak ada.
- Setelah tool berhasil, periksa `QuestItem_Backpack` (`requireBearDefeated` dan `bearGuard`), definisi objective, dan arah hadap `Bear_Enemy`. Compass/tracker muncul setelah quest diterima Bram dan akan melacak posisi ransel. Verifikasi gameplay secara manual dengan urutan: terima quest dari Bram, dekati area tas dari jalur Bram dan pastikan bear mengejar/menyerang, kalahkan beruang, ambil ransel, kembali ke Bram, lanjutkan dialog sampai selesai, lalu pastikan Closing baru muncul setelah jeda dan fade.
- Untuk menguji ulang flag Closing, hentikan Play Mode dan gunakan **Tools > Prince > Reset Closing Quest Progress** sebelum memulai sesi baru. Guard tambahan tetap menjaga backpack/bear bila flag lama tertinggal, tetapi reset flag mencegah warning stale dan memberi pengujian yang bersih.
