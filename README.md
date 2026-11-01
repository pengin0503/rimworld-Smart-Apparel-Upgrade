# Smart Apparel Upgrade

[日本語](#日本語) | [English](#english)

## 日本語

Smart Apparel Upgrade は RimWorld 1.7 用のMODです。入植者ごとに、より良い衣服の候補と、それを着る・着ない理由を表示します。安全条件をすべて満たす場合に限り、自動で着替えさせることもできます（**初期OFF**）。Harmony は不要です。

### 診断

入植者・衣服ごとに次の項目を比較します。

- 品質、耐久度、バニラの服装スコア
- 鋭利・打撃・熱の防御、寒冷・高温の断熱
- 種類、素材、被覆部位、レイヤー

候補ごとに次の条件を確認し、理由を表示します。

- 服装ポリシー
- 着用できる身体・種族・DLC（イデオロギーの服装要件を含む）
- 強制着用・ロック、死体が着ていた服
- 予約、禁止、到達性

そのほか、品質が分からない衣服や、交換対象のない追加スロット（服装ポリシーで許可されたもの）も理由付きで表示します。追加スロットは診断のみで、自動では着せません。一覧は、自動交換の対象 → 対処できる理由 → その他の順に並び、入植者で絞り込めます。改善にならない行は既定で隠します。候補の位置へ移動できます。

### 自動交換（任意・初期OFF）

設定で有効にした場合だけ、次の条件をすべて満たす候補をバニラの Wear Job で着替えさせます。

- 新しい服が、脱ぐことになる**すべての**服を合計した防御・断熱・服装スコアを下回らない。品質と耐久度は、脱ぐ服の最高値を下回らない。
- 被覆部位とレイヤーが変わらない。異種・異素材・複数脱衣では、重量・最大耐久値・消耗速度も悪化しない。
- 品質差、耐久度、性能改善幅が設定の下限を満たし、バニラの評価でも改善になる。
- 特殊なコンポーネント・独自クラス・装備補正値の変化がない、通常の装備に限る。効果が分からない装備は対象外です。
- 入植者が暇（待機・散歩中）で起きており、次のどれにも当てはまらない。
  - 手動指示・予約済みのJob
  - 徴兵、治療が必要な状態（出血・安静が必要・慢性ではない未治療の症状・治癒中の負傷。古い傷跡や喘息などの慢性疾患は含まない）、精神状態、ダウン
  - 戦闘の脅威
- 同じ入植者への再指示は1日空ける。脱いだ服を着直す指示は3日空ける（履歴はセーブに保存）。

その他の動作:

- 同種・同素材の候補を優先します（設定で変更できます）。
- Jobの発行直前に、装備状態と全条件をもう一度確認します。バニラの服装スコアは、入植者ごとの必要保温度で計算します。
- 自動で着た服は「強制着用」になりません。以後もバニラの服の最適化の対象です。
- ロード直後と設定変更後は、一定の猶予期間を置いてから自動交換を始めます。
- 次の場合は自動交換を止め、診断専用になります。
  - Outfit Manager（`LordKuper.OutfitManager`）や、名前に wardrobe / outfit manager を含む服装管理MODを検出したとき
  - 服装関連メソッドへのHarmonyパッチを検出したとき（設定画面に、パッチを当てているメソッドと所有者をすべて表示します。所有者ごとに、自己責任で自動交換を許可することもできます。服装管理MODの検出は解除できません）
  - パッチの検査に失敗したとき

### 使い方

1. リリースのZIPを展開した `SmartApparelUpgrade` フォルダを RimWorld の `Mods` フォルダに置き、有効化します。
2. 「設定」→「MOD設定」→「Smart Apparel Upgrade」で「候補を検索」を押し、診断結果を確認します。
3. 必要なら最低品質差・耐久度・改善幅を調整し、自動交換を有効にします。

途中導入・途中削除ができます。交換履歴はマップ単位でセーブに保存され、以前の開発版の履歴形式も読み込めます。

### 制限事項と未検証範囲

- **通常のゲームプレイでの長時間の動作確認は行っていません。** 確認した範囲は `docs/VALIDATION.md` を参照してください。公開前に確認したのは次の範囲です。
  - 実物のゲームDLLを参照したビルド
  - バニラの Wear Job と強制着用処理のIL確認
  - 実ゲームの有限Tick実行で、自動交換による着替えが完了し、強制着用にならないこと（v1.0.1）
  - 静的な参照照合
  - モデル単体の確認
  - 限定的なセーブ形式の確認
- 服装管理MODの検出は名前と関連パッチに基づく保守的なもので、互換性を保証しません。Colony Wardrobe の PackageId は確認できていません。
- 1回の検査には上限（在庫10,000着、比較2,500件、追加スロットの診断150件）があり、超えた場合は「不完全」と表示します。改善にならない候補は上限を使う前に除き、続きは次回以降の検査で順に調べます。確認できなかった候補で自動交換することはありません。評価できなかった候補は理由付きで表示し、自動交換の対象から外します。
- 大規模コロニーでの検査時間・TPSへの影響と、多数のMODとの互換性は計測・検証していません。

## English

Smart Apparel Upgrade is a RimWorld 1.7 mod. It lists better apparel for each colonist and explains why each candidate is or is not worn. Optionally, it can have colonists change clothes automatically when every safety condition holds (**OFF by default**). Harmony is not required.

### Diagnostics

For each colonist and garment it compares:

- quality, condition and the vanilla apparel score
- sharp/blunt/heat armor and cold/heat insulation
- type, material, covered body parts and layers

For each candidate it checks the following and shows the reason:

- apparel policy
- body, race and DLC wearability (including ideology apparel requirements)
- forced or locked apparel, and apparel worn by a corpse
- reservations, forbidden state and reachability

Apparel of unknown quality and additional free slots with no garment to replace (policy-allowed items) are also shown with a reason. Free slots are diagnostic only and are never filled automatically. The list sorts automatic candidates first, then reasons you can act on, then the rest, and can be filtered by colonist. Rows that are not improvements are hidden by default. You can jump to a candidate's location.

### Automatic replacement (optional, OFF by default)

Only when enabled, a vanilla Wear job is issued for candidates that meet every condition below:

- The new garment is not worse than the **combined** armor, insulation and apparel score of every garment it would remove. Its quality and condition are not below the highest of the removed garments.
- Coverage and layers are unchanged. For cross-type, cross-material or multiple removals, mass, maximum hit points and wear rate must not get worse.
- Quality, condition and performance gains meet your thresholds, and vanilla scoring also sees an improvement.
- Only ordinary apparel is eligible: no special components, custom classes or changed equipped stat offsets. Gear with unknown effects is excluded.
- The colonist is idle (waiting or wandering) and awake, and none of the following applies:
  - a manual or queued job
  - drafted, a condition needing care (bleeding, medical rest, an untreated non-chronic condition or a still-healing wound; old scars and chronic conditions such as asthma do not count), in a mental state or downed
  - a combat threat
- At least one day passes between orders for the same colonist, and three days before a removed garment can be re-targeted. The history is stored in the save.

Other behavior:

- Same-type/material candidates are preferred (configurable).
- Apparel state and every condition are rechecked right before the job is issued. Vanilla apparel scores are computed with each colonist's own warmth needs.
- Automatically worn apparel is **not** marked as forced, so vanilla outfit optimization still applies to it later.
- Automation waits a grace interval after loading and after settings changes.
- Automation stops and the mod becomes diagnostics-only when:
  - Outfit Manager (`LordKuper.OutfitManager`) or an outfit-manager mod whose name contains wardrobe / outfit manager is detected
  - a Harmony patch on relevant apparel methods is detected (the settings page lists every patched method and owner; you can allow automatic swaps per owner at your own risk; outfit-manager detection cannot be overridden)
  - patch inspection fails

### Usage

1. Extract the release ZIP, place the `SmartApparelUpgrade` folder in RimWorld's `Mods` folder and enable it.
2. Open Settings → Mod Settings → Smart Apparel Upgrade, press "Scan for upgrades" and review the diagnostics.
3. Optionally adjust the quality, condition and gain thresholds, then enable automatic replacement.

The mod can be added to or removed from an existing save. Replacement history is stored per map, and history saved by earlier development builds still loads.

### Limitations and unverified scope

- **Not yet tested in long normal gameplay.** See `docs/VALIDATION.md` for exactly what was checked. Before release, the following were checked:
  - a build against the real game DLLs
  - an IL review of vanilla Wear and forced-apparel handling
  - a bounded real-game run in which an automatic swap completed without marking the apparel forced (v1.0.1)
  - static reference matching
  - model unit checks
  - limited save-format checks
- Outfit-manager detection is a conservative name and patch check, not a compatibility guarantee. The Colony Wardrobe PackageId has not been confirmed.
- Each scan has caps (10,000 stored garments, 2,500 comparisons, 150 extra-slot rows). When they are reached the result is labeled partial. Candidates that cannot be improvements are dropped before using the budget, and later scans continue where the previous one stopped. Candidates that were not checked are never used for automatic replacement. Candidates that cannot be evaluated are shown with a reason and excluded from automation.
- Scan time and TPS impact in large colonies, and compatibility with large mod lists, have not been measured or verified.
