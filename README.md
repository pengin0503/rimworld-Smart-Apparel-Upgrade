# Smart Apparel Upgrade

[日本語](#日本語) | [English](#english)

## 日本語

RimWorld 1.7で、入植者・衣服ごとに交換候補と着替えない理由を表示します。品質・耐久度・バニラの服装スコアに加え、鋭利・打撃・熱の防御、寒冷・高温の断熱、服装ポリシー、着用可能な身体・種族・DLC要件、部位・レイヤー、強制着用・ロック、予約・禁止・到達性を確認します。対象位置へ移動できます。

最低品質差、耐久度、スコア改善率、同種・同素材を優先する表示を設定できます。自動交換は**初期OFF**です。有効時も、同種・同素材の1着置換だけをバニラWear Jobで指示します。複数の服を外す交換、部位・レイヤー変更、異種・異素材は診断専用です。確認できない特殊効果の安全性を推測しません。

手動・予約済み・有用な作業、睡眠、徴兵、負傷、精神状態、戦闘の脅威がある場合は保留します。再指示・逆交換の待機時間を保存し、ロード・設定変更後にも猶予期間を設けます。服装管理MODの名前や関連メソッドへのHarmonyパッチ、パッチ検査失敗を検出すると診断専用にします。名称による検出は保守的で、互換性を保証するものではありません。

**開発版です。実際の着替え完了、マップ保存・読み込み、途中導入・削除、他MODとの実動作、UI配置、実スキャン時間・TPSへの影響は未検証です。** 指示成功を着替え完了と表示しません。走査上限に達した場合は不完全と表示し、自動指示を行いません。

### 利用方法

1.7用開発版をModsフォルダに配置し、「設定」→「MOD設定」→「Smart Apparel Upgrade」で再走査します。最初は診断専用で確認し、重要なセーブで自動交換を有効にしないでください。

## English

Displays replacement candidates and individual reasons why RimWorld 1.7 colonists do not wear them. It checks quality, condition, vanilla apparel score, sharp/blunt/heat armor, cold/heat insulation, apparel policy, body/race/DLC wearability requirements, body groups/layers, forced/locked apparel, reservation, forbidden state and reachability. Locate candidate items from the list.

Configure minimum quality difference, condition and score improvement, plus a preference for same-type/material diagnostics. Automatic replacement is **OFF by default**. When enabled, it only orders vanilla Wear jobs for single same-type/material replacements. Multiple removals, changed coverage/layers and different types/materials remain diagnostic-only; unverified special effects are not assumed safe.

Manual, queued or useful jobs, sleep, drafting, injury, mental states and combat threats defer automation. Persistent cooldown/reversal guards and a load/settings grace interval limit retries. Manager display names, Harmony patches to relevant apparel methods or patch-inspection failure force diagnostics-only mode. Name detection is conservative and does not establish runtime compatibility.

**Development build. Actual completed swaps, full-map save/load, installation/removal, other MOD gameplay, UI layout and actual scan/TPS overhead are unverified.** Issuing a job is never reported as completed wearing. Budget-limited scans are labeled partial and do not issue automatic orders.

### Usage

Install a 1.7 development build in Mods, then open Settings → Mod Settings → Smart Apparel Upgrade and rescan. Start with diagnostics only; do not enable automation on important saves.
