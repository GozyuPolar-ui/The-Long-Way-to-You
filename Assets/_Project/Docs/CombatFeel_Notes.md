# Combat Feel

## Kondisi awal dan fitur yang sudah tersedia

- Kombo pemain sudah memakai `CrossFadeInFixedTime`; transisi Animator pemain sekitar 0,08–0,10 detik dan controller beruang juga sudah memiliki transisi singkat. Animator Controller tidak diubah.
- Klip serangan pemain sudah memiliki event suara dan awal/akhir hitbox. Mekanisme event itu tetap menjadi sumber utama jendela hit; nilai normalized time di settings menjadi fallback.
- `PlayerCombat` sudah memiliki buffer kombo 0,85 detik. Implementasi baru menggunakan buffer bertenggat yang lebih singkat dari `CombatFeelSettings` saat aktif; fallback lama tetap dipakai saat toggle buffer dimatikan.
- Beruang sudah memiliki telegraph serangan sekitar 0,8 detik, reaksi hit, health bar, dan knockback dasar. `BearAI` dan Animator Controller tidak diubah.
- `ThirdPersonController` Starter Assets sudah memakai smoothing akselerasi/deselerasi dan rotasi (`SpeedChangeRate`/`RotationSmoothTime`); komponen tambahan untuk gerak dasar tidak diperlukan.
- Tidak ada aksi dodge/roll yang ditemukan. Input Jump yang sudah ada dapat membatalkan serangan setelah cancel window terbuka. Tidak ada Avatar Mask/lapisan tubuh atas baru agar resolver tarik/sarung tetap aman.

## Implementasi

- `CombatFeelSettings` adalah sumber parameter, preset Standard/Lenient, serta toggle fitur dan API aksesibilitas.
- Komponen baru: `CombatFeel`, `AttackBuffer`, `HitStop`, `AttackLunge`, `CameraImpulse`, `EnemyPoise`, `HitFlash`, dan `SwordTrail`.
- Hook minimal ditambahkan ke `PlayerCombat`, `WeaponHitbox`, dan `WeaponAudio`; `PlayerHealth` hanya menerapkan skala damage beruang saat preset Lenient dan toggle `enemyDamageScalingEnabled` aktif.
- Knockback poise dan animasi hit dapat memberi respons berjenjang, tetapi tidak mengubah state/AI beruang secara logis. Interupsi AI tidak ditambahkan karena itu memerlukan perubahan pada `BearAI`.
- Skala damage beruang Lenient diimplementasikan lewat hook gated pada `PlayerHealth`. Tidak ada perubahan pada nilai damage atau logika serangan di `BearAI`.
- Telegraph beruang yang sudah ada (sekitar 0,8 detik) dipertahankan; nilainya tidak diubah atau dipetakan ulang ke preset karena `BearAI` harus tetap tidak tersentuh.
- Panel Pengaturan tidak diubah. `CombatFeelSettings` menyediakan API publik untuk camera shake, screen flash, hit stop, dan visual effects.
- Tidak ada scene atau Animator Controller yang diedit. Tool Editor **Tools > Prince > Setup Combat Feel** membuat settings dan material bila belum ada, lalu mengonfigurasi prefab pemain dan beruang secara idempoten.

## Parameter dan nilai awal

Tool membuat asset `Assets/_Project/Data/CombatFeel/CombatFeelSettings.asset` dengan preset **Lenient**:

- Input buffer: 0,22 detik (Standard: 0,15).
- Kecepatan serangan: 1,15 / 1,18 / 1,22 untuk Attack 1–3; cancel window dimajukan pada Lenient.
- Jendela hit-stop: Light 0,05; Medium 0,07; Finisher 0,10 detik; kecepatan Animator saat berhenti 0,02.
- Lunge: 0,28 / 0,35 / 0,45 unit; jarak pemeriksaan tanah 1,5 unit.
- Auto-face Lenient: radius 5,5 unit, sudut 115°, kecepatan putar 630°/detik.
- Poise: threshold Medium 1, Finisher 2; multiplier poise beruang 2.
- Damage beruang ke pemain pada Lenient: multiplier 0,8. Toggle `enemyDamageScalingEnabled` dapat mematikannya.
- Impulse kamera: 0,18 / 0,28 / 0,35; durasi 0,25 detik; intensitas aksesibilitas 65%.
- Flash 0,08 detik; pitch audio ±7%; slow motion finisher 0,25× selama 0,4 detik, default mati.
- Jejak pedang, hit flash, partikel tumbukan, debu lari, hit-stop, dan efek lain memiliki toggle terpisah di settings. Efek visual aktif secara default.

Semua timer efek memakai waktu tak berskala. Hit-stop tidak memakai `Time.timeScale`.

## File lama yang disentuh dan backup

Sebelum perubahan, salinan asli disimpan di `Assets/_Project/Docs/CombatBackup~`:

- `PlayerCombat.cs.bak`
- `WeaponHitbox.cs.bak`
- `WeaponAudio.cs.bak`
- `PlayerHealth.cs.bak`

Hook yang ditambahkan hanya menghubungkan fitur opsional Combat Feel. Dengan toggle fitur dimatikan, hook terkait mempertahankan jalur perilaku lama.

## Batas verifikasi

Verifikasi dilakukan statis. Tanpa membuka Unity atau Play Mode, tidak dapat dipastikan rasa gerak, sinkronisasi event animasi, hit-stop pada FPS runtime, respons Cinemachine Impulse, tampilan trail/partikel/flash, atau hasil pemuatan/konfigurasi prefab di Editor.

## Langkah manual

1. Buka proyek di Unity dan tunggu kompilasi selesai.
2. Jalankan **Tools > Prince > Setup Combat Feel** sekali. Periksa Console untuk pesan keberhasilan atau error; tool aman dijalankan ulang.
3. Buka `CombatFeelSettings.asset` dan sesuaikan preset, profil serangan, atau toggle. Untuk mematikan fitur, nonaktifkan field toggle-nya; untuk mematikan semua tambahan, matikan semua toggle fitur dan accessibility.
4. Verifikasi referensi `CombatFeel`, `AttackBuffer`, `HitStop`, `AttackLunge`, `CameraImpulse`, `EnemyPoise`, `HitFlash`, serta `SwordTrail` pada prefab pemain/beruang. Jangan mengubah Animator Controller atau scene Main-Menu/Closing.
5. Setelah fitur diuji di proyek kerja, file lama dapat dipulihkan satu per satu dengan mengganti file aktif memakai file `.bak` yang cocok di `Assets/_Project/Docs/CombatBackup~`. File `.bak` adalah salinan sebelum perubahan dan tidak perlu diimpor sebagai asset Unity.
