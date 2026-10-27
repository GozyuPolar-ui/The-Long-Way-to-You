# Ransel Sang Pemburu

## Keputusan dan integrasi

- Quest lama `test_fetch` diubah menjadi `hunter_backpack`, dengan objective mengambil ransel lalu kembali bicara kepada Bram. Asset quest memakai GUID lama agar referensi yang sudah ada tetap tersambung.
- Bram menggunakan prefab NPC Guard yang sudah ada agar tampilannya berhelm; data `NPCInfo` pada instance scene diatur menjadi Bram, pemburu, dan node dialog `Bram_Start`.
- Ransel menggunakan prefab asli `Assets/LowPoly Fantasy Bags/Prefab/Bag.3.prefab`, bukan placeholder. Model tas diskalakan ke 0.1 agar proporsional, memakai material URP cokelat, dan menggunakan collider trigger sederhana.
- Pickup hanya aktif untuk progress quest saat state `Active`, sehingga ransel tidak hilang sebelum quest diterima. Quest tidak memiliki state gagal dan tidak bergantung pada babi hutan.
- Quest memberi hadiah naratif “Peta Hutan” pada dialog dan notifikasi penyelesaian. Proyek belum memiliki inventory/reward runtime, jadi tidak ada item inventory yang diberikan.
- Tujuan ransel diinisialisasi 30 m ke arah hadap pemain dan Bram 2 m di depannya, lalu diproyeksikan ke Terrain aktif. Ini titik awal yang dapat diatur, bukan koordinat sungai yang diverifikasi secara visual. Sesuaikan penempatan ransel ke tepi sungai di Inspector.
- `guardAreaPosition` dan `guardAreaRadius` hanya menjadi gizmo editor sebagai titik/radius opsional untuk penempatan babi hutan di masa depan.
- Parser dialog Yarn yang sudah ada mendapat satu perintah `goto` untuk menghubungkan pilihan deskripsi ransel kembali ke cabang penerimaan/penolakan. Quest, pickup, tracking compass, off-screen indicator, input lock, dan marker peta tetap memakai sistem yang sudah ada.

## File yang dibuat atau diubah

- `Assets/_Project/Scripts/Runtime/QuestPickup.cs`
- `Assets/_Project/Scripts/Runtime/QuestDialogueUI.cs`
- `Assets/_Project/Scripts/Runtime/QuestTrackingHUD.cs`
- `Assets/_Project/Scripts/Runtime/BackpackQuestPlacement.cs`
- `Assets/_Project/Scripts/Runtime/BackpackQuestPlacement.cs.meta`
- `Assets/_Project/Scripts/Editor/PrinceDialogueQuestSetup.cs`
- `Assets/_Project/Data/Quests/Quest_RanselSangPemburu.asset`
- `Assets/_Project/Data/Quests/Quest_RanselSangPemburu.asset.meta`
- `Assets/_Project/Dialogue/TestQuest.yarn`
- `Assets/_Project/Docs/BackpackQuest_Notes.md`

Tool akan membuat atau memperbarui prefab `Assets/_Project/Prefabs/QuestItem_Backpack.prefab` dan material `Assets/_Project/Materials/QuestBackpack_Brown.mat` saat dijalankan.

## Langkah manual untukku

1. Buka scene permainan yang memuat `PlayerPrince`.
2. Jalankan menu **Tools > Prince > Setup Backpack Quest** satu kali.
3. Simpan scene aktif saat Unity menandainya berubah.
4. Pilih `BackpackQuest_Setup`, sesuaikan posisi Bram, ransel, dan area babi hutan; pindahkan ransel ke dekat sungai.
5. Di Unity, uji seluruh cabang dialog, pickup sebelum/sesudah menerima quest, kedua objective, compass/off-screen indicator, marker peta, dan notifikasi selesai.

## Batas validasi

Setup tidak dibuka atau dijalankan di Unity selama pengerjaan ini. Tampilan, jarak interaksi aktual, titik sungai yang tepat, kompilasi oleh Unity, dan perilaku saat Play belum diverifikasi; lakukan pengujian manual di Unity setelah menjalankan menu.
