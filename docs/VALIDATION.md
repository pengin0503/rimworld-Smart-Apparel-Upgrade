# Validation / 検証記録

## Verified / 確認済み

- **Game build:** RimWorld 1.7.4985 rev1269, Linux x86_64 WaylithClr (private game snapshot).
- **Source commit tested:** `d0f18e4e62456fc27c5b2f459beaf89d321643c5`.
- **Public-reference build:** `dotnet build` with .NET SDK 10 and `Krafs.Rimworld.Ref 1.7.4985-unstable` succeeded (0 warnings and 0 errors).
- **Real game-code load (A):** `status=passed`, `exitCode=0`, `reasonCodes=[]` for SmartApparelUpgrade.
- **Evidence:** https://github.com/pengin0503/RimWorld-for-mod-test/actions/runs/41275665138
- **Method:** isolated A-level comparison via `RimWorld-for-mod-test`. Original game content stayed in a private R2-backed validation snapshot; no game DLL was published.

## Not yet verified / 未確認

- This host **does not run the normal playable RimWorld application**; no in-game UI interaction, time-dependent job behavior or save/reload verification was performed.
- Per-pawn exclusion explanations, wear-job effect and no-loop validation, outfit-manager compatibility and gameplay/save-load verification.
- **A=passed does not mean that the mod is a completed, gameplay-verified release.** Avoid claiming v1.0 readiness until the remaining items are independently validated.

日本語: Linux版のゲーム本体（1.7.4985 rev1269）でAレベルのMOD読み込み検証に合格しました。通常のゲーム起動・画面操作・セーブとロード・長時間プレイでの動作は未検証です。完成版として扱わないでください。


## 2026-11-01 safety implementation (development)

- SDK 10.0.111 / .NET 10.0.11, Linux x86_64; Release build against exact private 1.7.4985 rev1269 DLLs succeeds with 0 warnings/errors.
- `dotnet run --project Tests/Tests.csproj -c Release`: 24 assertions pass after observing the absent safety model fail. Quality/condition/armor/insulation/score regressions, invalid numeric values, verified manager identity and conservative display-name detection, manual/queued/useful jobs, injury, drafting, sleep, mental state, threats, cooldowns/reversal and future timestamps are covered at the model level.
- Model benchmark: 100,000 numeric comparisons in 11.09 ms in one Release run. This excludes game scores/stats, scanning, pathfinding, GUI, other MODs and actual Wear jobs; it is not scan/TPS overhead.
- Metadata and English/Japanese parity pass for 42 used keys. Development packaging includes only metadata, our DLL, both languages and README.
- Self-review only, no subagents: checked all displaced garments, forced/locked restrictions, required apparel, atomic scan publication, immediate pre-order validation, snapshot-local stat caching, explicit partial results and patch-inspection failure fallback.

### Implemented safeguards

The comparison inspects vanilla raw/net apparel scores and five armor/insulation stats with no regression allowed. The adapter checks policy, `PawnCanWear`, body parts, requirement restrictions, every displaced garment, exact coverage/layers, forbidden/reserved/reachable state and current activity. A vanilla Wear order is issued only for a single same-Def/same-material replacement, after rechecking live state. It is not reported as completed wearing. Cooldowns and removed-item reversal history use primitive serialized state; an additional 10,000-tick grace applies after load/settings changes.

Related managers or relevant Harmony patches force diagnostics-only mode; failed patch inspection also fails closed. No Harmony patches are installed. See [COMPATIBILITY.md](COMPATIBILITY.md) for source attribution and missing Colony Wardrobe metadata. The private mod index was consulted before public author metadata, and no unverified PackageId was adopted.

### Release blockers / 正式版までの残作業

- At the initial checkpoint, fresh A/map/tick checks had not run. Final evidence below updates those scopes; real completed swaps, policy-change acceptance, whole-map save/load, installation/removal and manager/race/DLC gameplay remain unverified.
- The safety model tests do not exercise real Wear job effects, reservation API behavior, game-body compatibility or actual Scribe serialization. The initial direct Scribe attempt failed on missing game dependencies/uninitialized prefs; the final isolated settings/history checks below pass, without proving whole-map save/load.
- Cross-type/material and multiple-removal automation remains intentionally unsupported. Custom equipped stat offsets/abilities and required environmental gear are not exhaustively modeled; conservative same-type/material automation still requires actual gameplay acceptance. Any injury (including a scar) defers automation.
- Diagnostics are bounded to 10,000 stock items / 2,500 comparisons / 64 path checks per scan, every 10,000 ticks with rotating pawn starts for truncated scans. High counts or unverified access disable auto for that snapshot. Missing-quality items and non-overlapping extra slots are not full individual diagnostic coverage.
- Manager-name safeguards may be overbroad; unknown or renamed managers that do not patch inspected methods may be missed. No compatibility claim is made from name tests.
- UI layout, 270+ MOD compatibility, actual scan allocation/time and TPS are unmeasured. No v1.0 release. Issue #1 stays open.

日本語: 入植者・衣服別の理由と性能比較、品質・耐久度・性能下限、強制着用・ロック・服装要件等の確認、再指示・逆交換防止、保存する履歴、ロード後の猶予、管理MOD検出による診断専用化を追加しました。実物DLL参照ビルドと24項目のモデルテスト・日英キー・開発版パッケージは合格しました。実際の着替え、セーブとロード、途中導入・削除、270以上のMOD、実UI・性能は未検証です。異種・異素材や複数脱衣の自動交換、特殊効果の網羅的評価は未対応のため正式版にはしません。

## Final private-runtime evidence (2026-11-01)

- [Pinned private-host run](https://github.com/pengin0503/RimWorld-for-mod-test/actions/runs/41278778403) completed successfully. Only safe rebuilt summaries were printed; raw game files, saves and private logs were not published.
- Fresh qualified **A=passed**, exit 0, no reason codes, for the source revision in [runtime-smoke.json](evidence/2026-11-01-runtime-smoke.json).
- Generated-map finite-tick smoke: actual game tick and independent registered `fixture.counter` both equal **0, 60, 600, 12,000**. **D stage=completed**, but **overall=incomplete** with `experimental`, `dNotQualified`, `mapNotQualified`. The host CI success means these limited expectations were met; it is not promotion to qualified D or game feature acceptance. No new errors/warnings were reported by that comparison.
- The registered observer measures its fixture counter, not our work diagnosis, feed accuracy, apparel effects or scan timing. The fixture map and Core/Japanese conditions do not represent a populated or 270+ MOD colony. No TPS claim is made.
- Local model benchmark conditions: Linux x86_64, AMD EPYC 9V74, 9 logical CPUs exposed, SDK 10.0.111 / .NET 10.0.11, Release, single-run measurements with game API/GUI/pathfinding excluded. No before/after optimization claim.

### Primitive Scribe integration

`dotnet run --project Tests.Game/Tests.Game.csproj -c Release -p:RimWorldManaged=<matching-private-game-dir> -p:GameExtraManaged=<matching-private-extra-dir>` exercises the real game Scribe under stock .NET for isolated primitive data. The extra directory contains only matching R2 ObjectPool and Steamworks managed dependencies. The fixture supplies the game's real default `PrefsData` because normal engine startup is absent. These files are test inputs only and are never committed or packaged.

**PASS:** all five settings and SwapStamp pawn/target/removed IDs and tick round trip; restored cooldown blocks immediate retry. This does not verify a whole map or completed Wear job. Static index comparison: 122 member signatures and 75 types matched; 183 external-framework references remain outside the indexed assembly set, with no indexed game-reference mismatch. Final self-review additionally contained exceptions during optional order issuance.

**Still blocked for v1.0:** normal populated gameplay, full-map save/load and installation/removal, actual task/feeding/wear acceptance, real outfit-manager/race/DLC interactions, 270+ MOD compatibility and actual large-map scan/TPS measurement. Earlier implementation gaps and scenario limitations remain; Issue #1 is open and no v1.0 release is created.

日本語: 最新コミットで実ゲーム読み込みAは合格し、実ゲームTickと独立fixtureが12,000 Tickまで一致しました。ただしD・マップ生成は未適格化で総合判定はincompleteです。実物Scribeによる限定的なデータ往復も合格しましたが、通常プレイやマップ全体の保存・読み込み、実際の業務動作、270以上のMOD環境・実TPSの検証にはなりません。Issue #1と正式版の保留を維持します。


## Continued implementation: replacement coverage (2026-11-01)

This section supersedes the earlier same-type-only and blanket partial-scan restrictions. It does not upgrade the old private-host evidence to evidence for this revision.

- Implemented cross-type/material and multiple-removal eligibility using summed removed raw scores and five protection/insulation stats, maximum removed quality/condition, exact union coverage/layers, and all removed-item locks, forced wear and apparel requirements. Cross-type/material and multiple removals additionally reject greater mass, lower maximum HP and faster wear. All automatic replacements reject custom apparel classes/non-passive components and changed or nonfinite equipped stat offsets; unknown stat directions are never guessed.
- Actual order priority honors the same-type/material setting. Partial scans may order only individually verified candidates after fresh full revalidation, avoiding global shutdown simply because another pawn/candidate exceeded budget. Unchecked paths remain ineligible. No higher scan frequency or budgets were introduced.
- Unknown-quality comparisons are visible. Additional nonconflicting slots show policy/body/access reasons or explain that there is no worn garment to upgrade; no automatic extra-slot filling. Multiple-removal IDs persist with a legacy single-ID fallback, and reversal checks consider every removed garment.
- Focused red check first: build failed because the new aggregate helper was absent. After implementation: 29 model assertions pass. Single Release run: 100,000 numeric comparisons in 14.25 ms (Linux x86_64 / .NET 10.0.11; excludes game stats, paths, scanning and TPS). This is not an optimization claim.
- Exact private 1.7.4985 rev1269 references compile successfully; real-game isolated Scribe settings/history round trip passes including both removed IDs and restored cooldown. Metadata and English/Japanese parity pass (44 used keys). Development zip contains five files, no private game assemblies.
- Self-review: all displaced apparel, required gear, finite offsets, conservative functional-gear guard, live rechecking, bounded diagnostics and legacy-save fallback. No subagents or new broad test framework.

日本語: 異種・異素材・複数脱衣を、脱衣対象すべての合計性能と被覆・要件を比較する安全判定へ追加しました。特殊装備や装備補正の変更は診断専用です。同種・同素材優先を自動指示へ反映し、個別確認済み候補が他の未確認候補で一律停止しないよう修正しました。品質不明・追加スロットも理由を表示します。29項目、限定的な実物Scribe、日英キー、配布構成の確認は合格です。

Remaining release acceptance: actual completed Wear jobs (including these new replacement shapes), populated whole-map save/load and installation/removal, real manager/race/DLC/270+ MOD combinations, visible UI review and measured large-colony scan/TPS. These require a representative running game/fixture that is not available in this local stock-.NET environment. Do not equate compilation, isolated Scribe or the previous empty-map tick smoke with these checks. Issue #1 remains open; development artifacts only, no v1.0.
