# Ransel Sang Pemburu

## Keputusan dan integrasi

- Quest lama `test_fetch` diubah menjadi `hunter_backpack`, dengan objective mengalahkan beruang penjaga, mengambil ransel, lalu mengembalikannya kepada Bram. Asset quest memakai GUID lama agar referensi yang sudah ada tetap tersambung.
- Bram menggunakan prefab NPC Guard yang sudah ada agar tampilannya berhelm; data `NPCInfo` pada instance scene diatur menjadi Bram, pemburu, dan node dialog `Bram_Start`.
- Ransel menggunakan prefab asli `Assets/LowPoly Fantasy Bags/Prefab/Bag.3.prefab`, bukan placeholder. Model tas diskalakan ke 0.1 agar proporsional, memakai material URP cokelat, dan menggunakan collider trigger sederhana.
- Pickup hanya aktif untuk progress quest saat state `Active`, sehingga ransel tidak hilang sebelum quest diterima. Secara default, ransel juga terkunci sampai beruang penjaga mati. Quest tidak memiliki state gagal; bila bear reference/objek tidak tersedia, ransel tetap bisa diambil dan warning dicatat untuk mencegah soft-lock.
- Quest memberi hadiah naratif “Peta Hutan” pada dialog dan notifikasi penyelesaian. Proyek belum memiliki inventory/reward runtime, jadi tidak ada item inventory yang diberikan.
- Tujuan ransel diinisialisasi 30 m ke arah hadap pemain dan Bram 2 m di depannya, lalu diproyeksikan ke Terrain aktif. Ini titik awal yang dapat diatur, bukan koordinat sungai yang diverifikasi secara visual. Sesuaikan penempatan ransel ke tepi sungai di Inspector.
- `guardAreaPosition` dan `guardAreaRadius` menentukan area pencarian beruang penjaga, selain referensi `bearGuard` yang bisa diatur pada prefab/instance pickup.
- Parser dialog Yarn yang sudah ada mendapat satu fungsi kondisi untuk menampilkan peringatan Bram saat beruang masih hidup. Quest, pickup, tracking compass, off-screen indicator, input lock, dan marker peta tetap memakai sistem yang sudah ada; compass/peta tetap mengarahkan ke ransel saat objective Defeat aktif.
- Closing hanya berjalan setelah quest selesai saat dialog turn-in Bram, dialog tertutup, input lock dilepas, dan jeda 1,8 detik; transisi menggunakan `SceneFader`.

## File yang dibuat atau diubah

- `Assets/_Project/Scripts/Runtime/QuestPickup.cs`
- `Assets/_Project/Scripts/Runtime/QuestDialogueUI.cs`
- `Assets/_Project/Scripts/Runtime/QuestTrackingHUD.cs`
- `Assets/_Project/Scripts/Runtime/MapSystem.cs`
- `Assets/_Project/Scripts/Runtime/BearAI.cs`
- `Assets/_Project/Scripts/Runtime/QuestManager.cs`
- `Assets/_Project/Scripts/Runtime/QuestDefinition.cs`
- `Assets/_Project/Scripts/Runtime/DialogueInputLock.cs`
- `Assets/_Project/Scripts/Runtime/UI/QuestCompleteTrigger.cs`
- `Assets/_Project/Scripts/Runtime/BackpackQuestPlacement.cs`
- `Assets/_Project/Scripts/Runtime/BackpackQuestPlacement.cs.meta`
- `Assets/_Project/Scripts/Editor/PrinceDialogueQuestSetup.cs`
- `Assets/_Project/Scripts/Editor/PrinceClosingSceneSetup.cs`
- `Assets/_Project/Scripts/Editor/PrinceBackpackQuestFix.cs`
- `Assets/_Project/Data/Quests/Quest_RanselSangPemburu.asset`
- `Assets/_Project/Data/Quests/Quest_RanselSangPemburu.asset.meta`
- `Assets/_Project/Dialogue/TestQuest.yarn`
- `Assets/_Project/Docs/BackpackQuest_Notes.md`

`Tools > Prince > Fix Backpack Quest Flow` memperbarui definisi quest, prefab `Assets/_Project/Prefabs/QuestItem_Backpack.prefab`, referensi beruang, trigger Closing, dan instance backpack di scene hutan tanpa menggandakan objek. Setup awal masih dapat membuat atau memperbarui material `Assets/_Project/Materials/QuestBackpack_Brown.mat`.

## Langkah manual untukku

1. Pastikan scene hutan tidak memiliki perubahan belum tersimpan.
2. Jalankan **Tools > Prince > Fix Backpack Quest Flow**. Tool membuat backup `.bak` satu kali sebelum mengubah data quest, prefab, dan scene.
3. Scene saat ini tidak memuat `BearAI`; bila ingin alur pertarungan normal, jalankan **Tools > Prince > Setup Bear Enemy**, lalu jalankan kembali **Fix Backpack Quest Flow**.
4. Sesuaikan posisi Bram, ransel, dan area penjaga melalui `BackpackQuest_Setup`; letakkan ransel dekat sungai.
5. Setelah kompilasi, uji dialog, beruang, pickup, objective, compass/off-screen indicator, marker peta, dan Closing secara manual.

## Batas validasi

Unity tidak dibuka dan menu setup tidak dijalankan selama pengerjaan ini. Tampilan, jarak interaksi aktual, titik sungai yang tepat, kompilasi/import oleh Unity, dan perilaku saat Play belum diverifikasi; lakukan pengujian manual di Unity setelah menjalankan menu.
