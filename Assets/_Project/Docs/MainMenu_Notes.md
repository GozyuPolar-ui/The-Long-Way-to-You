# Main Menu: The Long Way to You

## Hasil inspeksi dan keputusan

- Scene permainan yang dipakai adalah `Assets/_Project/Scene/UNS_Demo.unity`; scene menu yang diperbarui adalah `Assets/_Project/Scene/Main-Menu.unity`.
- Belum ditemukan sistem save/load, pause menu, atau AudioMixer di proyek sebelum menu dibuat. Save ringan disimpan sebagai JSON (`prince-save.json`) di `Application.persistentDataPath`, berisi nama scene, posisi, dan rotasi pemain.
- Belum ditemukan font Cinzel atau sprite Kenney Fantasy UI Borders. UI memakai sprite-less frame sederhana berwarna emas redup dan font TMP `LiberationSans SDF` yang sudah ada.
- Ada musik menu di `Assets/_Project/Audio/MAIN MENU/royaltyfreemusicstudio-medieval-fantasy-music-462199.mp3`; musik tersebut dipasang sebagai BGM menu berulang.
- Panorama memakai Terrain game yang sama dan dua prefab Spruce yang sudah ada. Terrain detail/tree rendering bawaan dimatikan pada scene menu; 12 pohon latar ditempatkan sebagai static batching, collider dan real-time shadows dimatikan. Tidak menyalin player, NPC, atau sistem gameplay ke menu.
- Karena tidak ada mixer proyek dan membuat asset mixer Unity secara manual rentan tidak valid, slider master/musik/efek memakai `AudioListener` dan volume `AudioSource` berbasis channel. Pengaturan disimpan di `PlayerPrefs` dan diterapkan kembali ketika scene berubah.
- Sensitivitas kamera disimpan di `PlayerPrefs` dan diterapkan melalui adapter runtime pada input player; file Starter Assets tidak diubah.
- Menu dibangun oleh satu tool Editor yang meregenerasi scene `Main-Menu`, mengatur urutan Build Settings agar menu index 0 dan scene `UNS_Demo` index 1, lalu mempertahankan scene lain tanpa entri ganda.

## File yang dibuat

- `Assets/_Project/Scripts/Runtime/UI/MenuUIFactory.cs`
- `Assets/_Project/Scripts/Runtime/UI/MenuButtonFX.cs`
- `Assets/_Project/Scripts/Runtime/UI/MenuCameraDrift.cs`
- `Assets/_Project/Scripts/Runtime/UI/SceneFader.cs`
- `Assets/_Project/Scripts/Runtime/UI/SaveGameService.cs`
- `Assets/_Project/Scripts/Runtime/UI/MenuSettings.cs`
- `Assets/_Project/Scripts/Runtime/UI/SettingsPanel.cs`
- `Assets/_Project/Scripts/Runtime/UI/MainMenuController.cs`
- `Assets/_Project/Scripts/Editor/PrinceMainMenuSetup.cs`
- `Assets/_Project/Scripts/Runtime/UI.meta`
- Meta untuk script C# baru.

Tool memperbarui `Assets/_Project/Scene/Main-Menu.unity` dan entry Build Settings; tool tidak mengubah scene game.

## Parameter yang dapat diatur

- Di `MenuCameraDrift`: kecepatan orbit (`degreesPerSecond`) dan sudut orbit (`orbitAngle`).
- Di `MainMenuController`: scene awal (`gameplaySceneName`), durasi fade judul, tingkat peredupan latar.
- Di `SceneFader`: durasi transisi hitam.
- Pengguna dapat mengubah rentang slider kamera dan volume di `SettingsPanel`/`MenuSettings`; nilai disimpan dengan key `Prince.*` di PlayerPrefs.
- Kepadatan panorama saat ini dibatasi 12 Spruce; warna kabut, intensitas cahaya, lokasi pohon, dan jarak/fog adalah konstanta pada `PrinceMainMenuSetup`.

## Langkah manual untukku

1. Buka proyek di Unity; pastikan editor sudah selesai mengimpor script.
2. Jalankan **Tools > Prince > Setup Main Menu**. Tool membaca `UNS_Demo` secara additive untuk mengambil Terrain/posisi pemain dan memperbarui `Assets/_Project/Scene/Main-Menu.unity`.
3. Periksa Build Settings: `Main-Menu` harus index 0 dan `UNS_Demo` index 1; menu tetap mempertahankan scene lain di belakangnya.
4. Jalankan build atau Play dari Build Settings untuk memeriksa transisi, navigasi keyboard/gamepad, pengaturan, simpan/muat posisi, BGM, dan konfirmasi.
5. Jika ingin tampilan font lebih fantasi, tambahkan font Cinzel berlisensi gratis, lalu buat TMP Font Asset lewat **Window > TextMeshPro > Font Asset Creator** dan assign ke field `menuFont` pada MainMenuController setelah tool membuat scene.

## Belum dapat diuji

Menu ini hanya diperiksa secara statis dalam sesi pengerjaan: Unity tidak dibuka, Play Mode tidak dijalankan, dan screenshot tidak diambil. Tampilan komposisi panorama/UI, hasil kompilasi oleh Unity, navigasi perangkat aktual, operasi save JSON saat keluar, kualitas audio, serta transisi saat Play masih perlu diuji manual. Belum ada asset AudioMixer; volume musik/efek saat ini bekerja lewat `AudioSource` dan master lewat `AudioListener`.
