# Apparel manager compatibility investigation

2026-11-01. The private R2 `rimworld-mod-index.zip` was read first (186,980 bytes, schema 1). No Colony Wardrobe / Outfit Manager / wardrobe-name match was present in that snapshot. No Workshop or PackageId was invented or used to fetch an unconfirmed R2 prefix.

- Outfit Manager: author-linked source [About.xml](https://github.com/LordKuper/rimworld-outfitmanager/blob/master/About/About.xml) confirms `LordKuper.OutfitManager` and Workshop 1984694952. Its current inspected metadata declares 1.6, not a proven 1.7 build. [JobGiverPatch.cs](https://github.com/LordKuper/rimworld-outfitmanager/blob/master/Source/OutfitManager/Patches/JobGiverPatch.cs) patches `JobGiver_OptimizeApparel.ApparelScoreRaw` to inject work-dependent scoring. Directly competing with its choices is a concrete integration risk. Exact package match disables our auto mode.
- Colony Wardrobe: no indexed PackageId was available, and the author Workshop page could not be retrieved. Its PackageId and implementation remain unverified. The runtime metadata display-name safeguard catches `wardrobe` / `outfit manager` without pretending to know a package ID. This may also block cosmetic wardrobe mods conservatively.
- Other managers: runtime Harmony inspection checks relevant apparel scoring, wear/drop, body compatibility and wearability methods. Any patch metadata or inspection failure disables auto mode. Harmony is inspected through reflection and is not a mandatory dependency. We install no patches. A manager that replaces logic without those patches and without recognizable metadata may remain undetected.

These are source inspection and conservative fallback rules, **not** co-loaded gameplay acceptance. Translated/renamed mods, race equipment, DLC requirements, dynamic patches and special apparel effects still need real-game tests. Diagnostics may reflect patched scores; we do not overwrite another mod's apparel management.

日本語: 非公開索引を先に検索しましたが対象MODの名前は見つかりませんでした。Outfit Managerは作者のAbout.xmlでPackageIdを確認し、服装評価へのパッチも確認しました。Colony WardrobeのPackageId・実装は未確認のため推測せず、実行時の表示名と関連Harmonyパッチによる保守的な診断専用化を実装しました。同時導入の実ゲーム検証は未完了で、互換性を保証するものではありません。
