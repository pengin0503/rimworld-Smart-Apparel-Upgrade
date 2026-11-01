Smart Apparel Upgrade v1.0.0 for RimWorld 1.7 (built against 1.7.4985). No dependencies.

## 日本語

入植者ごとに改善候補の衣服と、着る・着ない理由を表示するMODです。任意の自動交換は**初期OFF**です。

**主な機能**
- 品質・耐久度・服装スコア・防御・断熱・素材・種類・被覆部位・レイヤーの比較
- 服装ポリシー、身体・種族・DLC要件、強制着用・ロック、予約・禁止・到達性の確認と理由表示
- 同種・同素材の優先、最低品質差・耐久度・改善幅の設定
- 異種・異素材・複数脱衣は、脱ぐ服すべての合計性能と被覆・レイヤー・要件を比較
- 品質不明・追加スロットの診断（追加スロットは自動で着せない）
- 自動交換はバニラの Wear Job で行い、入植者が暇なときだけ、発行直前に再確認して実行。特殊・未知の装備は対象外
- 再指示・着直しの待機時間を持つ交換履歴（旧形式の履歴も読み込み可）
- Outfit Manager などの服装管理MODや、関連するHarmonyパッチを検出すると診断専用に切り替え

**今回の修正**: 自動交換のJobがプレイヤー指示扱いで発行されていたため、着た服がバニラ側で「強制着用」に設定され、以後の自動最適化が止まる問題がありました。プレイヤー指示ではないJobとして発行するよう修正しました。また、散歩中の暇な入植者も自動交換の対象にしました。

**導入**: ZIPを展開し、`SmartApparelUpgrade` フォルダを `Mods` に置いて有効化します。

**未検証範囲・既知の制限**
- 通常のゲームプレイでの動作（実際の着替え完了を含む）は確認していません。確認済みなのは、実物ゲームDLLを参照したビルド、バニラの Wear Job のIL確認、静的な参照照合、モデル単体の確認、限定的なセーブ形式の確認です。
- 服装管理MODの検出は名前とパッチによる保守的なもので、互換性を保証しません。Colony Wardrobe の PackageId は未確認です。
- 大規模コロニーでの検査時間・TPS、多数MOD環境での互換性は未計測・未検証です。
- このZIPに含まれるDLLは、公開参照アセンブリ（Krafs.Rimworld.Ref 1.7.4985）でCIビルドしたものです。

## English

Shows better apparel candidates for each colonist and why each one is or is not worn. Optional automatic replacement is **OFF by default**.

**Features**
- Compares quality, condition, apparel score, armor, insulation, material, type, coverage and layers
- Checks apparel policy, body/race/DLC requirements, forced/locked apparel, reservations, forbidden state and reachability, with reasons
- Same-type/material preference; minimum quality, condition and gain settings
- Cross-type, cross-material and multiple removals are compared against the combined performance, coverage, layers and requirements of every removed garment
- Diagnostics for unknown quality and additional slots (free slots are never filled automatically)
- Automatic replacement uses vanilla Wear jobs, only while the colonist is idle, with a full recheck right before the job. Special or unknown gear is excluded
- Replacement history with retry and re-wear cooldowns; history from earlier builds still loads
- Switches to diagnostics-only when Outfit Manager or another outfit manager, or a related Harmony patch, is detected

**Fixed in this release**: automatic Wear jobs were issued as player orders. Vanilla therefore marked the new apparel as forced, which stopped later automatic outfit optimization. Jobs are now issued as non-player jobs. Wandering idle colonists are now also eligible.

**Installation**: extract the ZIP, place the `SmartApparelUpgrade` folder in `Mods` and enable it.

**Unverified scope and known limitations**
- Not tested in normal gameplay, including actual completed swaps. Checked: a build against the real game DLLs, an IL review of vanilla Wear, static reference matching, model unit checks and limited save-format checks.
- Outfit-manager detection is a conservative name and patch check, not a compatibility guarantee. The Colony Wardrobe PackageId is unconfirmed.
- Scan time/TPS in large colonies and compatibility with large mod lists are not measured or verified.
- The DLL in this ZIP was built in CI against public reference assemblies (Krafs.Rimworld.Ref 1.7.4985).
