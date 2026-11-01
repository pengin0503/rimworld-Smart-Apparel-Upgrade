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

- No new qualified A load, generated map/finite game ticks, real completed swap, policy-change/gameplay acceptance, whole-map save/load, mid-save installation/removal or co-loaded manager/race/DLC test has yet run for this new DLL. Previous A evidence is historical only.
- The safety model tests do not exercise real Wear job effects, reservation API behavior, game-body compatibility or actual Scribe serialization. Stock-CLR Scribe integration in the preceding MOD failed on game ObjectPool/WaylithClr binding; this is not save/load proof here.
- Cross-type/material and multiple-removal automation remains intentionally unsupported. Custom equipped stat offsets/abilities and required environmental gear are not exhaustively modeled; conservative same-type/material automation still requires actual gameplay acceptance. Any injury (including a scar) defers automation.
- Diagnostics are bounded to 10,000 stock items / 2,500 comparisons / 64 path checks per scan, every 10,000 ticks with rotating pawn starts for truncated scans. High counts or unverified access disable auto for that snapshot. Missing-quality items and non-overlapping extra slots are not full individual diagnostic coverage.
- Manager-name safeguards may be overbroad; unknown or renamed managers that do not patch inspected methods may be missed. No compatibility claim is made from name tests.
- UI layout, 270+ MOD compatibility, actual scan allocation/time and TPS are unmeasured. No v1.0 release. Issue #1 stays open.

日本語: 入植者・衣服別の理由と性能比較、品質・耐久度・性能下限、強制着用・ロック・服装要件等の確認、再指示・逆交換防止、保存する履歴、ロード後の猶予、管理MOD検出による診断専用化を追加しました。実物DLL参照ビルドと24項目のモデルテスト・日英キー・開発版パッケージは合格しました。実際の着替え、セーブとロード、途中導入・削除、270以上のMOD、実UI・性能は未検証です。異種・異素材や複数脱衣の自動交換、特殊効果の網羅的評価は未対応のため正式版にはしません。
