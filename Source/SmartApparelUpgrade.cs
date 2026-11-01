using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Pengin.SmartApparelUpgrade
{
    public sealed class ApparelSettings : ModSettings
    {
        public bool AutoWear = false;
        public int MinimumQualityGain = 1;
        public int MinimumConditionPercent = 70;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref AutoWear, "autoWear", false);
            Scribe_Values.Look(ref MinimumQualityGain, "minimumQualityGain", 1);
            Scribe_Values.Look(ref MinimumConditionPercent, "minimumConditionPercent", 70);
            base.ExposeData();
        }
    }

    public sealed class UpgradeChoice
    {
        public Pawn Pawn;
        public Apparel Worn;
        public Apparel Better;
        public int QualityGain;
        public string Label
        {
            get { return Pawn.LabelShort + ": " + Worn.LabelShort + " -> " + Better.LabelShort + " (+" + QualityGain + ")"; }
        }
    }

    public sealed class ApparelScanner : MapComponent
    {
        private readonly Dictionary<string, int> lastOrders = new Dictionary<string, int>();
        private List<UpgradeChoice> choices = new List<UpgradeChoice>();
        public IReadOnlyList<UpgradeChoice> Choices { get { return choices; } }
        public int RejectedPolicy { get; private set; }
        public int RejectedCondition { get; private set; }
        public int RejectedAccess { get; private set; }
        public int RejectedQuality { get; private set; }

        public ApparelScanner(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 10000 != map.uniqueID % 10000) return;
            Scan();
            if (ApparelUpgradeMod.Settings != null && ApparelUpgradeMod.Settings.AutoWear)
                OrderIdleUpgrades();
        }

        public void Scan()
        {
            choices = new List<UpgradeChoice>();
            RejectedPolicy = RejectedCondition = RejectedAccess = RejectedQuality = 0;
            ApparelSettings settings = ApparelUpgradeMod.Settings;
            if (settings == null || !map.IsPlayerHome) return;

            // Group by apparel Def first: avoid a full scan of every item for every worn garment.
            var stock = map.listerThings.AllThings.OfType<Apparel>()
                .Where(a => a.Spawned && a.Wearer == null && !a.WornByCorpse)
                .Take(10000).GroupBy(a => a.def).ToDictionary(g => g.Key, g => g.ToList());

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || !pawn.IsFreeColonist || pawn.apparel == null || pawn.outfits == null) continue;
                ApparelPolicy policy = pawn.outfits.CurrentApparelPolicy;
                if (policy == null || policy.filter == null) continue;
                foreach (Apparel worn in pawn.apparel.WornApparel)
                {
                    if (pawn.apparel.IsLocked(worn) ||
                        (pawn.outfits.forcedHandler != null && pawn.outfits.forcedHandler.IsForced(worn)))
                        continue;

                    QualityCategory oldQuality;
                    if (!QualityUtility.TryGetQuality(worn, out oldQuality)) continue;
                    List<Apparel> similar;
                    if (!stock.TryGetValue(worn.def, out similar)) continue;
                    Apparel best = null;
                    int bestGain = 0;
                    foreach (Apparel candidate in similar)
                    {
                        if (candidate.Stuff != worn.Stuff) continue;
                        if (!policy.filter.Allows(candidate)) { RejectedPolicy++; continue; }
                        if (candidate.MaxHitPoints <= 0 ||
                            100f * candidate.HitPoints / candidate.MaxHitPoints < settings.MinimumConditionPercent ||
                            (worn.MaxHitPoints > 0 &&
                             (float)candidate.HitPoints / candidate.MaxHitPoints < (float)worn.HitPoints / worn.MaxHitPoints))
                        { RejectedCondition++; continue; }
                        QualityCategory quality;
                        if (!QualityUtility.TryGetQuality(candidate, out quality)) continue;
                        int gain = (int)quality - (int)oldQuality;
                        if (gain < settings.MinimumQualityGain || gain <= bestGain)
                        { RejectedQuality++; continue; }
                        if (candidate.IsForbidden(pawn) || !pawn.CanReserve(candidate) ||
                            !pawn.CanReach(candidate, PathEndMode.Touch, Danger.Some))
                        { RejectedAccess++; continue; }
                        best = candidate;
                        bestGain = gain;
                    }
                    if (best != null)
                        choices.Add(new UpgradeChoice { Pawn = pawn, Worn = worn, Better = best, QualityGain = bestGain });
                }
            }
        }

        private void OrderIdleUpgrades()
        {
            var issued = new HashSet<string>();
            var claimed = new HashSet<string>();
            int now = Find.TickManager.TicksGame;
            foreach (UpgradeChoice choice in choices)
            {
                Pawn pawn = choice.Pawn;
                if (pawn.Drafted || pawn.Downed || pawn.InMentalState || pawn.Dead) continue;
                // Only when the pawn is not performing useful work.
                if (pawn.CurJob != null && pawn.CurJobDef != JobDefOf.Wait &&
                    pawn.CurJobDef != JobDefOf.Wait_MaintainPosture) continue;
                if (!choice.Better.Spawned || issued.Contains(pawn.ThingID) ||
                    claimed.Contains(choice.Better.ThingID)) continue;
                int last;
                if (lastOrders.TryGetValue(pawn.ThingID, out last) && now - last < 30000) continue;
                // Let vanilla JobDriver_Wear handle garment removal and addition.
                if (pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Wear, choice.Better), JobTag.Misc))
                {
                    issued.Add(pawn.ThingID);
                    claimed.Add(choice.Better.ThingID);
                    lastOrders[pawn.ThingID] = now;
                }
            }
        }
    }

    public sealed class ApparelUpgradeMod : Mod
    {
        public static ApparelSettings Settings;
        private Vector2 scroll;
        public ApparelUpgradeMod(ModContentPack pack) : base(pack)
        {
            Settings = GetSettings<ApparelSettings>();
        }
        public override string SettingsCategory() { return "Smart Apparel Upgrade"; }

        public override void DoSettingsWindowContents(Rect rect)
        {
            var list = new Listing_Standard();
            list.Begin(new Rect(rect.x, rect.y, rect.width, 180f));
            list.Label("SAU_Description".Translate());
            list.CheckboxLabeled("SAU_Auto".Translate(), ref Settings.AutoWear);
            list.Label("SAU_MinimumQuality".Translate(Settings.MinimumQualityGain));
            Settings.MinimumQualityGain = (int)list.Slider(Settings.MinimumQualityGain, 1, 5);
            list.Label("SAU_MinimumHP".Translate(Settings.MinimumConditionPercent));
            Settings.MinimumConditionPercent = (int)list.Slider(Settings.MinimumConditionPercent, 1, 100);
            list.End();
            Map current = Find.CurrentMap;
            if (current == null) return;
            ApparelScanner scanner = current.GetComponent<ApparelScanner>();
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + 185, 180, 30), "SAU_Scan".Translate()))
                scanner.Scan();
            Widgets.Label(new Rect(rect.x + 185, rect.y + 190, rect.width - 190, 28),
                "SAU_Count".Translate(scanner.Choices.Count));
            Widgets.Label(new Rect(rect.x, rect.y + 219, rect.width, 28),
                "SAU_Excluded".Translate(scanner.RejectedPolicy, scanner.RejectedCondition,
                                          scanner.RejectedAccess, scanner.RejectedQuality));
            Rect view = new Rect(rect.x, rect.y + 250, rect.width, Math.Max(80f, rect.height - 253f));
            Rect content = new Rect(0, 0, Math.Max(100f, view.width - 20), Math.Max(view.height, scanner.Choices.Count * 33f));
            Widgets.BeginScrollView(view, ref scroll, content);
            for (int i = 0; i < scanner.Choices.Count; i++)
                Widgets.Label(new Rect(0, i * 33f, content.width, 31f), scanner.Choices[i].Label);
            Widgets.EndScrollView();
        }
    }
}
