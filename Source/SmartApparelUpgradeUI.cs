using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Pengin.SmartApparelUpgrade
{
    public sealed class ApparelUpgradeMod : Mod
    {
        public static ApparelSettings Settings;
        Vector2 scroll;float controlsHeight=500;
        // Rows that are not improvements at all; hidden by default to keep actionable rows visible.
        static readonly HashSet<string> nonImprovements=new HashSet<string>{"SAU_ReasonQuality","SAU_ReasonGain","SAU_ReasonRegression","SAU_ReasonCondition"};
        // Reasons the player can act on (unforbid, unlock, change policy, ...) sort before the rest.
        static readonly HashSet<string> actionable=new HashSet<string>{"SAU_ReasonForbidden","SAU_ReasonReserved","SAU_ReasonReach","SAU_ReasonPolicy",
            "SAU_ReasonLocked","SAU_ReasonForced","SAU_ReasonBusy","SAU_ReasonCooldown","SAU_ReasonManager","SAU_ReasonOrdered"};
        bool hideNonImprovements=true;int pawnFilter=-1;
        List<UpgradeChoice> view;int viewVersion=-1;bool viewHide;int viewPawn=-2;ApparelScanner viewScanner;
        public ApparelUpgradeMod(ModContentPack pack):base(pack){Settings=GetSettings<ApparelSettings>();}
        public override string SettingsCategory(){return "Smart Apparel Upgrade";}
        static int Rank(UpgradeChoice c){return c.SafeAuto?0:actionable.Contains(c.Reason)?1:nonImprovements.Contains(c.Reason)?3:2;}
        public override void DoSettingsWindowContents(Rect rect)
        {
            ApparelScanner scanner=Find.CurrentMap==null?null:Find.CurrentMap.GetComponent<ApparelScanner>();
            List<Pawn> pawns=scanner==null?new List<Pawn>():scanner.Choices.Select(c=>c.Pawn).Where(p=>p!=null).Distinct().OrderBy(p=>p.LabelShort).ToList();
            if(pawnFilter>=pawns.Count)pawnFilter=-1;
            if(scanner!=null&&(view==null||viewVersion!=scanner.ScanVersion||viewHide!=hideNonImprovements||viewPawn!=pawnFilter||viewScanner!=scanner))
            {
                Pawn only=pawnFilter<0?null:pawns[pawnFilter];
                view=scanner.Choices.Where(c=>(only==null||c.Pawn==only)&&(!hideNonImprovements||!nonImprovements.Contains(c.Reason)))
                    .OrderBy(Rank).ThenByDescending(c=>c.ScoreGain).ToList();
                viewVersion=scanner.ScanVersion;viewHide=hideNonImprovements;viewPawn=pawnFilter;viewScanner=scanner;
            }
            int count=scanner==null||view==null?0:view.Count;const float rowHeight=136;
            var content=new Rect(0,0,Math.Max(100,rect.width-20),Math.Max(rect.height,controlsHeight+count*rowHeight));
            Widgets.BeginScrollView(rect,ref scroll,content);
            var list=new Listing_Standard();list.Begin(new Rect(0,0,content.width,10000));
            list.Label("SAU_Description".Translate());
            list.CheckboxLabeled("SAU_Auto".Translate(),ref Settings.AutoWear);
            list.CheckboxLabeled("SAU_PreferSame".Translate(),ref Settings.PreferSameTypeAndStuff);
            list.Label("SAU_MinimumQuality".Translate(Settings.MinimumQualityGain));Settings.MinimumQualityGain=(int)list.Slider(Settings.MinimumQualityGain,0,5);
            list.Label("SAU_MinimumHP".Translate(Settings.MinimumConditionPercent));Settings.MinimumConditionPercent=(int)list.Slider(Settings.MinimumConditionPercent,1,100);
            list.Label("SAU_MinimumPerformance".Translate(Settings.MinimumPerformanceGainPercent));Settings.MinimumPerformanceGainPercent=(int)list.Slider(Settings.MinimumPerformanceGainPercent,0,50);
            if(scanner!=null)
            {
                if(list.ButtonText("SAU_Scan".Translate()))scanner.Scan(true);
                list.Label("SAU_Count".Translate(scanner.Choices.Count,scanner.EligibleCount));
                if(scanner.Partial)list.Label("SAU_Partial".Translate());
                if(scanner.FailedItems>0)list.Label("SAU_FailedItems".Translate(scanner.FailedItems));
                if(scanner.Failed)list.Label("SAU_Failed".Translate());
                if(scanner.SnapshotSignature!=Settings.Signature)list.Label("SAU_Stale".Translate());
                if(!string.IsNullOrEmpty(scanner.AutoBlockKey))list.Label(scanner.AutoBlockKey.Translate()+" "+scanner.AutoBlockDetail);
                // Every patch on a watched method, with an explicit per-owner opt-in.
                if(ApparelScanner.PatchFindings.Count>0)
                {
                    list.Label("SAU_PatchHeader".Translate());
                    foreach(PatchFinding finding in ApparelScanner.PatchFindings)
                        list.Label("  "+finding.Method+": "+string.Join(", ",finding.Owners));
                    foreach(string owner in ApparelScanner.PatchFindings.SelectMany(f=>f.Owners).Distinct().Where(o=>o!="?").OrderBy(o=>o))
                    {
                        bool allowed=Settings.AllowedPatchOwners.Contains(owner),before=allowed;
                        list.CheckboxLabeled("SAU_AllowOwner".Translate(owner),ref allowed);
                        if(allowed!=before){if(allowed)Settings.AllowedPatchOwners.Add(owner);else Settings.AllowedPatchOwners.Remove(owner);}
                    }
                }
                list.Label("SAU_ScanTime".Translate(scanner.ScanMilliseconds.ToString("0.00")));
                list.CheckboxLabeled("SAU_HideNonImprovements".Translate(),ref hideNonImprovements);
                if(list.ButtonText(pawnFilter<0?"SAU_AllPawns".Translate().ToString():"SAU_PawnFilter".Translate(pawns[pawnFilter].LabelShort).ToString()))
                    pawnFilter=pawnFilter+1>=pawns.Count?-1:pawnFilter+1;
            }
            controlsHeight=list.CurHeight+10;list.End();
            if(scanner!=null&&view!=null)
                for(int i=0;i<view.Count;i++)
                {
                    float y=controlsHeight+i*rowHeight;
                    if(y+rowHeight<scroll.y||y>scroll.y+rect.height)continue;
                    UpgradeChoice c=view[i];
                    Widgets.Label(new Rect(0,y,content.width-80,rowHeight-4),c.Label+"\n"+c.Reason.Translate()+"\n"+c.Detail);
                    if(c.Better!=null&&c.Better.Spawned&&Widgets.ButtonText(new Rect(content.width-76,y+4,73,28),"SAU_Jump".Translate()))
                    {
                        Find.WindowStack.TryRemove(typeof(Dialog_ModSettings));
                        CameraJumper.TryJump(c.Better);
                    }
                }
            Widgets.EndScrollView();
        }
    }
}
