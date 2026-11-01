using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Pengin.SmartApparelUpgrade
{
    public sealed class ApparelSettings : ModSettings
    {
        public bool AutoWear=false,PreferSameTypeAndStuff=true;
        public int MinimumQualityGain=1,MinimumConditionPercent=70,MinimumPerformanceGainPercent=1;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref AutoWear,"autoWear",false);
            Scribe_Values.Look(ref PreferSameTypeAndStuff,"preferSameTypeAndStuff",true);
            Scribe_Values.Look(ref MinimumQualityGain,"minimumQualityGain",1);
            Scribe_Values.Look(ref MinimumConditionPercent,"minimumConditionPercent",70);
            Scribe_Values.Look(ref MinimumPerformanceGainPercent,"minimumPerformanceGainPercent",1);
            base.ExposeData();
        }
        public string Signature {get{return AutoWear+":"+PreferSameTypeAndStuff+":"+MinimumQualityGain+":"+MinimumConditionPercent+":"+MinimumPerformanceGainPercent;}}
    }
    public sealed class SwapStamp : IExposable
    {
        public string PawnId,TargetId,RemovedId;
        public int LastIssued;
        public List<string> RemovedIds=new List<string>();
        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnId,"pawn","");Scribe_Values.Look(ref TargetId,"target","");
            Scribe_Values.Look(ref RemovedId,"removed","");Scribe_Values.Look(ref LastIssued,"last",0);
            Scribe_Collections.Look(ref RemovedIds,"removedIds",LookMode.Value);
            if(RemovedIds==null)RemovedIds=new List<string>();
        }
    }
    public sealed class UpgradeChoice
    {
        public Pawn Pawn;public Apparel Worn,Better;
        public List<Apparel> Displaced;
        public string Reason,Detail;
        public bool SafeAuto;
        public double ScoreGain;
        public string Label {get{return Pawn.LabelShort+": "+(Displaced==null?(Worn==null?"SAU_EmptySlot".Translate().ToString():Worn.LabelShort):string.Join(" + ",Displaced.Select(a=>a.LabelShort)))+" → "+Better.LabelShort;}}
    }
    public sealed class ApparelScanner : MapComponent
    {
        const int Interval=10000,MaxStock=10000,MaxComparisons=2500;
        static readonly StatDef[] protection={StatDefOf.ArmorRating_Sharp,StatDefOf.ArmorRating_Blunt,
            StatDefOf.ArmorRating_Heat,StatDefOf.Insulation_Cold,StatDefOf.Insulation_Heat};
        List<SwapStamp> stamps=new List<SwapStamp>();
        List<UpgradeChoice> choices=new List<UpgradeChoice>();
        public IReadOnlyList<UpgradeChoice> Choices {get{return choices;}}
        public bool Partial {get;private set;}
        public bool Failed {get;private set;}
        public double ScanMilliseconds {get;private set;}
        public string AutoBlockKey {get;private set;}
        public string AutoBlockDetail {get;private set;}
        public int EligibleCount {get{return choices.Count(c=>c.SafeAuto);}}
        public string SnapshotSignature {get;private set;}
        int resumeAt=-1,pawnOffset;
        bool threat;
        Dictionary<Pawn,Dictionary<Apparel,Garment>> profileCache;
        string observedSettings;
        public ApparelScanner(Map map):base(map){}
        public override void ExposeData()
        {
            base.ExposeData();Scribe_Collections.Look(ref stamps,"swapHistory",LookMode.Deep);
            if(stamps==null)stamps=new List<SwapStamp>();
        }
        public override void MapComponentTick()
        {
            ApparelSettings settings=ApparelUpgradeMod.Settings;if(settings==null)return;
            int now=Find.TickManager.TicksGame;
            if(resumeAt<0||observedSettings!=settings.Signature)
            {resumeAt=now+Interval;observedSettings=settings.Signature;}
            if(now%Interval!=map.uniqueID%Interval)return;
            Scan();
            if(settings.AutoWear&&!Failed&&now>=resumeAt)
            {
                try{OrderIdleUpgrades();}
                catch(Exception e)
                {
                    Failed=true;
                    Log.Warning("Smart Apparel Upgrade order deferred after failure: "+e.GetType().Name);
                }
            }
        }
        public void Scan()
        {
            if(ApparelUpgradeMod.Settings==null)return;
            var timer=Stopwatch.StartNew();
            try
            {
                DetectManagers();
                threat=GenHostility.AnyHostileActiveThreatToPlayer(map);
                profileCache=new Dictionary<Pawn,Dictionary<Apparel,Garment>>();
                var next=new List<UpgradeChoice>();bool partial=false;
                var allStock=map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>()
                    .Where(a=>a.Spawned&&a.Wearer==null).ToList();
                if(allStock.Count>MaxStock)partial=true;
                var stock=allStock.Take(MaxStock).ToList();
                var byLayer=new Dictionary<ApparelLayerDef,List<Apparel>>();
                foreach(Apparel candidate in stock)
                    foreach(ApparelLayerDef layer in candidate.def.apparel.layers)
                    {List<Apparel> items;if(!byLayer.TryGetValue(layer,out items))byLayer[layer]=items=new List<Apparel>();items.Add(candidate);}
                var pawns=map.mapPawns.AllPawnsSpawned.Where(p=>!p.Dead&&p.IsFreeColonist&&p.apparel!=null&&p.outfits!=null).ToList();
                int budget=MaxComparisons,paths=64;
                for(int i=0;i<pawns.Count;i++)
                {
                    int index=(pawnOffset+i)%pawns.Count;Pawn pawn=pawns[index];
                    var scores=pawn.apparel.WornApparel.Select(a=>JobGiver_OptimizeApparel.ApparelScoreRaw(pawn,a)).ToList();
                    var seen=new HashSet<Apparel>();
                    foreach(Apparel worn in pawn.apparel.WornApparel)
                    {
                        var relevant=worn.def.apparel.layers.Where(l=>byLayer.ContainsKey(l)).SelectMany(l=>byLayer[l]).Distinct()
                            .OrderByDescending(a=>ApparelUpgradeMod.Settings.PreferSameTypeAndStuff&&a.def==worn.def&&a.Stuff==worn.Stuff);
                        foreach(Apparel candidate in relevant)
                        {
                            if(--budget<0){partial=true;break;}
                            QualityCategory oldQuality,newQuality;
                            if(QualityUtility.TryGetQuality(worn,out oldQuality)&&QualityUtility.TryGetQuality(candidate,out newQuality)&&newQuality<oldQuality)continue;
                            if(ApparelUtility.CanWearTogether(worn.def,candidate.def,pawn.RaceProps.body))continue;
                            if(!seen.Add(candidate))continue;
                            next.Add(Evaluate(pawn,worn,candidate,scores,ref paths));
                        }
                        if(budget<0)break;
                    }
                    if(budget>=0)
                        foreach(Apparel candidate in stock.Where(a=>!seen.Contains(a)))
                        {
                            if(--budget<0){partial=true;break;}
                            if(pawn.apparel.WornApparel.Any(a=>!ApparelUtility.CanWearTogether(a.def,candidate.def,pawn.RaceProps.body)))continue;
                            string reason="SAU_ReasonExtraSlot";
                            if(pawn.outfits.CurrentApparelPolicy==null||!pawn.outfits.CurrentApparelPolicy.filter.Allows(candidate))reason="SAU_ReasonPolicy";
                            else if(!candidate.PawnCanWear(pawn,false)||!ApparelUtility.HasPartsToWear(pawn,candidate.def))reason="SAU_ReasonBody";
                            else if(candidate.IsForbidden(pawn))reason="SAU_ReasonForbidden";
                            else if(!pawn.CanReserve(candidate))reason="SAU_ReasonReserved";
                            else if(paths--<=0)reason="SAU_ReasonUnverified";
                            else if(!pawn.CanReach(candidate,PathEndMode.Touch,Danger.None))reason="SAU_ReasonReach";
                            next.Add(new UpgradeChoice{Pawn=pawn,Better=candidate,Reason=reason});
                        }
                    if(budget<0){pawnOffset=(index+1)%pawns.Count;break;}
                }
                choices=next;Partial=partial||paths<0;Failed=false;
                SnapshotSignature=ApparelUpgradeMod.Settings.Signature;
                int now=Find.TickManager.TicksGame;
                stamps.RemoveAll(s=>s==null||string.IsNullOrEmpty(s.PawnId)||((long)now-s.LastIssued)>600000);
            }
            catch(Exception e){if(!Failed)Log.Warning("Smart Apparel Upgrade scan incomplete: "+e.GetType().Name);Failed=true;}
            finally{ScanMilliseconds=timer.Elapsed.TotalMilliseconds;}
        }
        Garment Profile(Pawn pawn,Apparel item)
        {
            Dictionary<Apparel,Garment> cache=null;Garment cached;
            if(profileCache!=null)
            {
                if(!profileCache.TryGetValue(pawn,out cache))profileCache[pawn]=cache=new Dictionary<Apparel,Garment>();
                if(cache.TryGetValue(item,out cached))return cached;
            }
            QualityCategory quality;
            bool hasQuality=QualityUtility.TryGetQuality(item,out quality);
            var profile=new Garment{Quality=hasQuality?(int)quality:-1,Condition=item.MaxHitPoints>0?(double)item.HitPoints/item.MaxHitPoints:0,
                Score=JobGiver_OptimizeApparel.ApparelScoreRaw(pawn,item),Stats=protection.Select(s=>(double)item.GetStatValue(s)).ToArray()};
            if(cache!=null)cache[item]=profile;return profile;
        }
        UpgradeChoice Evaluate(Pawn pawn,Apparel worn,Apparel candidate,List<float> scores,ref int paths)
        {
            var row=new UpgradeChoice{Pawn=pawn,Worn=worn,Better=candidate,Reason="SAU_ReasonUnknown"};
            if(candidate.Destroyed||!candidate.Spawned||candidate.Wearer!=null||worn.Wearer!=pawn||pawn.Map!=map||candidate.Map!=map)return row;
            var displaced=pawn.apparel.WornApparel.Where(a=>!ApparelUtility.CanWearTogether(a.def,candidate.def,pawn.RaceProps.body)).ToList();
            row.Displaced=displaced;
            Garment old=SwapSafety.Combine(displaced.Select(a=>Profile(pawn,a))),better=Profile(pawn,candidate);
            if(old==null||better==null||better.Quality<0){row.Reason="SAU_ReasonUnknownQuality";return row;}
            row.Detail="SAU_Profile".Translate(old.Quality,better.Quality,(old.Condition*100).ToString("0"),(better.Condition*100).ToString("0"),
                old.Score.ToString("0.000"),better.Score.ToString("0.000"))+"\n"+
                "SAU_Stats".Translate(string.Join("/",old.Stats.Select(x=>x.ToString("0.00"))),string.Join("/",better.Stats.Select(x=>x.ToString("0.00"))));
            if(pawn.apparel.IsLocked(worn)){row.Reason="SAU_ReasonLocked";return row;}
            if(pawn.outfits.forcedHandler!=null&&!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(worn))
            {row.Reason="SAU_ReasonForced";return row;}
            ApparelPolicy policy=pawn.outfits.CurrentApparelPolicy;
            if(policy==null||policy.filter==null||!policy.filter.Allows(candidate)){row.Reason="SAU_ReasonPolicy";return row;}
            if(candidate.WornByCorpse){row.Reason="SAU_ReasonTainted";return row;}
            if(!candidate.PawnCanWear(pawn,false)||!ApparelUtility.HasPartsToWear(pawn,candidate.def))
            {row.Reason="SAU_ReasonBody";return row;}
            if(pawn.apparel.WouldReplaceLockedApparel(candidate)){row.Reason="SAU_ReasonLocked";return row;}
            if(displaced.Any(a=>pawn.apparel.IsLocked(a)||(pawn.outfits.forcedHandler!=null&&!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(a))))
            {row.Reason="SAU_ReasonForced";return row;}
            foreach(ApparelRequirementWithSource requirement in pawn.apparel.AllRequirements)
                if(!requirement.requirement.AllowedForPawn(pawn,candidate.def,false)||
                    (displaced.Any(a=>requirement.requirement.RequiredForPawn(pawn,a.def,false))&&!requirement.requirement.RequiredForPawn(pawn,candidate.def,false)))
                {row.Reason="SAU_ReasonRequirement";return row;}
            bool sameCoverage=new HashSet<BodyPartGroupDef>(candidate.def.apparel.bodyPartGroups).SetEquals(displaced.SelectMany(a=>a.def.apparel.bodyPartGroups))&&
                new HashSet<ApparelLayerDef>(candidate.def.apparel.layers).SetEquals(displaced.SelectMany(a=>a.def.apparel.layers));
            if(!sameCoverage){row.Reason="SAU_ReasonCoverage";return row;}
            double net=JobGiver_OptimizeApparel.ApparelScoreGain(pawn,candidate,scores);row.ScoreGain=net;
            ApparelSettings s=ApparelUpgradeMod.Settings;
            SwapReason reason=SwapSafety.Compare(old,better,Math.Max(0,s.MinimumQualityGain),
                Math.Max(0,Math.Min(100,s.MinimumConditionPercent))/100.0,Math.Max(0,s.MinimumPerformanceGainPercent),net);
            if(reason!=SwapReason.None){row.Reason="SAU_Reason"+reason;return row;}
            if(candidate.IsForbidden(pawn)){row.Reason="SAU_ReasonForbidden";return row;}
            if(!pawn.CanReserve(candidate)){row.Reason="SAU_ReasonReserved";return row;}
            if(paths--<=0){row.Reason="SAU_ReasonUnverified";return row;}
            if(!pawn.CanReach(candidate,PathEndMode.Touch,Danger.None)){row.Reason="SAU_ReasonReach";return row;}
            if(!OrdinaryGear(candidate)||displaced.Any(a=>!OrdinaryGear(a)))
            {row.Reason="SAU_ReasonSpecial";return row;}
            // Unknown stat directions cannot be treated as improvements. Preserve all equipped offsets.
            var offsets=displaced.Concat(new[]{candidate}).SelectMany(a=>a.def.equippedStatOffsets??new List<StatModifier>())
                .Select(m=>m.stat).Distinct();
            foreach(StatDef stat in offsets)
                if(!SwapSafety.SameFinite(displaced.Sum(a=>(double)StatWorker.StatOffsetFromGear(a,stat)),StatWorker.StatOffsetFromGear(candidate,stat)))
                {row.Reason="SAU_ReasonSpecial";return row;}
            if(displaced.Count!=1||worn.def!=candidate.def||worn.Stuff!=candidate.Stuff)
            {
                double mass=candidate.GetStatValue(StatDefOf.Mass),oldMass=displaced.Sum(a=>(double)a.GetStatValue(StatDefOf.Mass));
                double wear=candidate.def.apparel.wearPerDay,oldWear=displaced.Min(a=>(double)a.def.apparel.wearPerDay);
                if(double.IsNaN(mass)||double.IsInfinity(mass)||double.IsNaN(oldMass)||double.IsInfinity(oldMass)||
                    double.IsNaN(wear)||double.IsInfinity(wear)||double.IsNaN(oldWear)||double.IsInfinity(oldWear)||
                    mass>oldMass+1e-6||
                    candidate.MaxHitPoints<displaced.Max(a=>a.MaxHitPoints)||
                    wear>oldWear+1e-6)
                {row.Reason="SAU_ReasonRegression";return row;}
            }
            if(!string.IsNullOrEmpty(AutoBlockKey)){row.Reason="SAU_ReasonManager";return row;}
            if(!SwapSafety.CanAct(Activity(pawn))){row.Reason="SAU_ReasonBusy";return row;}
            SwapStamp stamp=stamps.FirstOrDefault(x=>x.PawnId==pawn.ThingID);
            if(stamp!=null&&stamp.RemovedIds.Concat(new[]{stamp.RemovedId}).Any(id=>!SwapSafety.CooldownAllows(Find.TickManager.TicksGame,stamp.LastIssued,candidate.ThingID,id)))
            {row.Reason="SAU_ReasonCooldown";return row;}
            row.Reason="SAU_ReasonEligible";row.SafeAuto=true;return row;
        }
        static bool OrdinaryGear(Apparel item)
        {
            if(item.GetType()!=typeof(Apparel))return false;
            string[] passive={"CompQuality","CompColorable","CompForbiddable","CompStyleable"};
            return item.AllComps.All(c=>c.GetType().Assembly==typeof(Apparel).Assembly&&passive.Contains(c.GetType().Name));
        }
        IdleState Activity(Pawn p)
        {
            return new IdleState {Idle=p.CurJob==null||p.CurJobDef==JobDefOf.Wait||p.CurJobDef==JobDefOf.Wait_MaintainPosture,
                Awake=p.Awake(),PlayerForced=p.CurJob!=null&&(p.CurJob.playerForced||p.CurJob.playerInterruptedForced),
                Queued=p.jobs==null||p.jobs.jobQueue.Count>0,Injured=p.health==null||p.health.HasHediffsNeedingTend(false)||p.health.hediffSet.hediffs.Any(h=>h is Hediff_Injury),
                Threat=threat,Drafted=p.Drafted,Mental=p.InMentalState,Downed=p.Downed,Dead=p.Dead};
        }
        void DetectManagers()
        {
            AutoBlockKey=null;AutoBlockDetail=null;
            foreach(ModContentPack pack in LoadedModManager.RunningModsListForReading)
                if(Compatibility.IsManager(pack.PackageId,pack.Name))
                {AutoBlockKey="SAU_ManagerDetected";AutoBlockDetail=pack.Name;return;}
            try
            {
                Type harmony=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("HarmonyLib.Harmony",false)).FirstOrDefault(t=>t!=null);
                if(harmony==null)return;
                MethodInfo get=harmony.GetMethod("GetPatchInfo",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(MethodBase)},null);
                if(get==null){AutoBlockKey="SAU_InspectionFailed";return;}
                foreach(Type t in new[]{typeof(JobGiver_OptimizeApparel),typeof(Pawn_ApparelTracker),typeof(Apparel),typeof(ApparelUtility)})
                    foreach(MethodInfo method in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static))
                        if(new[]{"TryGiveJob","ApparelScoreRaw","ApparelScoreGain","Wear","TryDrop","PawnCanWear","CanWearTogether"}.Contains(method.Name)&&get.Invoke(null,new object[]{method})!=null)
                        {AutoBlockKey="SAU_Patched";AutoBlockDetail=t.Name+"."+method.Name;return;}
            }
            catch{AutoBlockKey="SAU_InspectionFailed";}
        }
        void OrderIdleUpgrades()
        {
            DetectManagers();if(!string.IsNullOrEmpty(AutoBlockKey))return;
            threat=GenHostility.AnyHostileActiveThreatToPlayer(map);profileCache=null;
            var issued=new HashSet<Pawn>();var claimed=new HashSet<Apparel>();int now=Find.TickManager.TicksGame;
            foreach(UpgradeChoice choice in choices.Where(c=>c.SafeAuto).OrderByDescending(c=>ApparelUpgradeMod.Settings.PreferSameTypeAndStuff&&c.Displaced.Count==1&&c.Worn.def==c.Better.def&&c.Worn.Stuff==c.Better.Stuff).ThenByDescending(c=>c.ScoreGain))
            {
                Pawn pawn=choice.Pawn;
                if(issued.Contains(pawn)||claimed.Contains(choice.Better)||!SwapSafety.CanAct(Activity(pawn)))continue;
                var scores=pawn.apparel.WornApparel.Select(a=>JobGiver_OptimizeApparel.ApparelScoreRaw(pawn,a)).ToList();int paths=1;
                UpgradeChoice fresh=Evaluate(pawn,choice.Worn,choice.Better,scores,ref paths);
                if(!fresh.SafeAuto)continue;
                // Only vanilla Wear performs removal/equip. No direct tracker mutation.
                if(pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Wear,choice.Better),JobTag.Misc))
                {
                    issued.Add(pawn);claimed.Add(choice.Better);
                    stamps.RemoveAll(s=>s.PawnId==pawn.ThingID);
                    stamps.Add(new SwapStamp{PawnId=pawn.ThingID,TargetId=choice.Better.ThingID,RemovedId=choice.Worn.ThingID,RemovedIds=fresh.Displaced.Select(a=>a.ThingID).ToList(),LastIssued=now});
                    choice.Reason="SAU_ReasonOrdered";choice.SafeAuto=false;
                }
            }
        }
    }
    public sealed class ApparelUpgradeMod : Mod
    {
        public static ApparelSettings Settings;
        Vector2 scroll;float controlsHeight=500;
        public ApparelUpgradeMod(ModContentPack pack):base(pack){Settings=GetSettings<ApparelSettings>();}
        public override string SettingsCategory(){return "Smart Apparel Upgrade";}
        public override void DoSettingsWindowContents(Rect rect)
        {
            ApparelScanner scanner=Find.CurrentMap==null?null:Find.CurrentMap.GetComponent<ApparelScanner>();
            int count=scanner==null?0:scanner.Choices.Count;const float rowHeight=136;
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
                if(list.ButtonText("SAU_Scan".Translate()))scanner.Scan();
                list.Label("SAU_Count".Translate(scanner.Choices.Count,scanner.EligibleCount));
                if(scanner.Partial)list.Label("SAU_Partial".Translate());
                if(scanner.Failed)list.Label("SAU_Failed".Translate());
                if(scanner.SnapshotSignature!=Settings.Signature)list.Label("SAU_Stale".Translate());
                if(!string.IsNullOrEmpty(scanner.AutoBlockKey))list.Label(scanner.AutoBlockKey.Translate()+" "+scanner.AutoBlockDetail);
                list.Label("SAU_ScanTime".Translate(scanner.ScanMilliseconds.ToString("0.00")));
            }
            controlsHeight=list.CurHeight+10;list.End();
            if(scanner!=null)
                for(int i=0;i<scanner.Choices.Count;i++)
                {
                    float y=controlsHeight+i*rowHeight;
                    if(y+rowHeight<scroll.y||y>scroll.y+rect.height)continue;
                    UpgradeChoice c=scanner.Choices[i];
                    Widgets.Label(new Rect(0,y,content.width-80,rowHeight-4),c.Label+"\n"+c.Reason.Translate()+"\n"+c.Detail);
                    if(c.Better!=null&&c.Better.Spawned&&Widgets.ButtonText(new Rect(content.width-76,y+4,73,28),"SAU_Jump".Translate()))
                        CameraJumper.TryJump(c.Better);
                }
            Widgets.EndScrollView();
        }
    }
}
