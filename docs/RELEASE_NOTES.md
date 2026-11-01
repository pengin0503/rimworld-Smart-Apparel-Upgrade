Smart Apparel Upgrade v1.0.1 for RimWorld 1.7 (built against 1.7.4985). No dependencies.

## 日本語

**修正**
- 古い傷跡や、喘息などの慢性疾患がある入植者が、自動交換の対象から永久に外れていました。出血・安静が必要・慢性ではない未治療の症状・治癒中の負傷があるときだけ保留します。
- 検査上限に達すると、同じ候補ばかり調べていました。在庫と入植者ごとの候補の位置を次の検査へ引き継ぎ、改善にならない候補は先に除きます。追加スロットの診断には別の上限を設けました。
- 1件の例外で検査全体が止まらないようにしました（評価できなかった候補は理由付きで表示し、自動交換しません）。
- バニラの服装スコアを、入植者ごとの必要保温度で計算します。

**追加**
- 監視対象のメソッドにパッチを当てているMODを、すべて一覧表示します。所有者ごとに、自己責任で自動交換を許可できます（服装管理MODの検出は解除できません）。
- 診断一覧の並べ替え（交換可能 → 対処できる理由 → その他）、入植者での絞り込み、改善にならない行を隠す機能。

**確認した範囲**: 実ゲーム（Linux、1.7.4985 rev1269）で次を確認しました。長時間の通常プレイは未確認です。
- このMODの自動交換で入植者が傑作の服に着替え、強制着用にならないこと。
- バニラのプレイヤー指示では強制着用になること（v1.0.0の不具合の原因）。

## English

**Fixed**
- Colonists with old scars or chronic conditions such as asthma were excluded from automatic swaps forever. Automation now waits only for bleeding, medical rest, untreated non-chronic conditions or healing wounds.
- Capped scans kept re-checking the same candidates. The stock window and each colonist's candidate position carry over between scans, non-improvements are dropped first, and extra-slot diagnostics have their own budget.
- One exception no longer stops the whole scan (candidates that cannot be evaluated are shown with a reason and never ordered).
- Vanilla apparel scores use each colonist's own warmth needs.

**Added**
- Every mod patching a watched method is listed, and each owner can be allowed for automatic swaps at your own risk (outfit-manager detection cannot be overridden).
- Diagnostics sorting (eligible, then actionable reasons, then the rest), a colonist filter and a "hide non-improvements" option.

**Checked**: in the real game (Linux, 1.7.4985 rev1269) the following were checked. Long normal gameplay is not verified.
- An automatic swap by this mod dressed a colonist in an excellent shirt without marking it forced.
- The vanilla player-order path does mark it forced (the cause of the v1.0.0 defect).

The DLL in this ZIP was built in CI against public reference assemblies (Krafs.Rimworld.Ref 1.7.4985).
