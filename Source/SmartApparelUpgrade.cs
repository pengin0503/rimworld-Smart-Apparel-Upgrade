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
        // Harmony owners the player explicitly accepted for automatic swaps. Outfit managers
        // detected by package or name can never be accepted here.
        public List<string> AllowedPatchOwners=new List<string>();
        public override void ExposeData()
        {
            Scribe_Values.Look(ref AutoWear,"autoWear",false);
            Scribe_Values.Look(ref PreferSameTypeAndStuff,"preferSameTypeAndStuff",true);
            Scribe_Values.Look(ref MinimumQualityGain,"minimumQualityGain",1);
            Scribe_Values.Look(ref MinimumConditionPercent,"minimumConditionPercent",70);
            Scribe_Values.Look(ref MinimumPerformanceGainPercent,"minimumPerformanceGainPercent",1);
            Scribe_Collections.Look(ref AllowedPatchOwners,"allowedPatchOwners",LookMode.Value);
            if(AllowedPatchOwners==null)AllowedPatchOwners=new List<string>();
            base.ExposeData();
        }
        public string Signature {get{return AutoWear+":"+PreferSameTypeAndStuff+":"+MinimumQualityGain+":"+MinimumConditionPercent+":"+MinimumPerformanceGainPercent+":"+string.Join(",",AllowedPatchOwners.OrderBy(x=>x));}}
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
    public sealed class PatchFinding
    {
        public string Method;
        public List<string> Owners;
    }
    public sealed class ApparelScanner : MapComponent
    {
        const int Interval=10000,MaxStock=10000,MaxComparisons=2500,MaxExtraSlotRows=150,ManagerRecheck=60000;
        static readonly StatDef[] protection={StatDefOf.ArmorRating_Sharp,StatDefOf.ArmorRating_Blunt,
            StatDefOf.ArmorRating_Heat,StatDefOf.Insulation_Cold,StatDefOf.Insulation_Heat};
        static readonly string[] watchedMethods={"TryGiveJob","ApparelScoreRaw","ApparelScoreGain","Wear","TryDrop","PawnCanWear","CanWearTogether"};
        // Vanilla ApparelScoreRaw reads this static, written only by TryGiveJob for whichever
        // pawn vanilla evaluated last (possibly on another map or season).
        static readonly FieldInfo neededWarmthField=typeof(JobGiver_OptimizeApparel).GetField("neededWarmth",BindingFlags.NonPublic|BindingFlags.Static);
        List<SwapStamp> stamps=new List<SwapStamp>();
        List<UpgradeChoice> choices=new List<UpgradeChoice>();
        public IReadOnlyList<UpgradeChoice> Choices {get{return choices;}}
        public bool Partial {get;private set;}
        public bool Failed {get;private set;}
        public int FailedItems {get;private set;}
        public int ScanVersion {get;private set;}
        public double ScanMilliseconds {get;private set;}
        public string AutoBlockKey {get;private set;}
        public string AutoBlockDetail {get;private set;}
        public static List<PatchFinding> PatchFindings {get;private set;}=new List<PatchFinding>();
        public int EligibleCount {get{return choices.Count(c=>c.SafeAuto);}}
        public string SnapshotSignature {get;private set;}
        int resumeAt=-1,pawnOffset,stockOffset;
        // Per-pawn position in that pawn's candidate list, so a capped scan resumes where
        // it stopped instead of re-checking the same first candidates forever.
        readonly Dictionary<int,int> cursors=new Dictionary<int,int>();
        bool threat;
        Dictionary<Pawn,Dictionary<Apparel,Garment>> profileCache;
        string observedSettings;
        static int managersCheckedAt=-ManagerRecheck;static string managersSignature;
        static string managerKey,managerDetail;
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
        void SetWarmth(Pawn pawn)
        {
            if(neededWarmthField==null||pawn.Map==null)return;
            neededWarmthField.SetValue(null,PawnApparelGenerator.CalculateNeededWarmth(pawn,pawn.Map.Tile,GenLocalDate.Twelfth(pawn)));
        }
        // CanWearTogether depends only on the two defs and the body; cache it for one scan
        // (not longer, in case another mod patches it with state-dependent logic).
        readonly Dictionary<long,bool> wearTogether=new Dictionary<long,bool>();
        bool WearTogether(ThingDef a,ThingDef b,BodyDef body)
        {
            long key=((long)a.shortHash<<32)|((long)(ushort)b.shortHash<<16)|(ushort)body.shortHash;
            bool value;
            if(!wearTogether.TryGetValue(key,out value))wearTogether[key]=value=ApparelUtility.CanWearTogether(a,b,body);
            return value;
        }
        bool CheaplyExcluded(Apparel worn,Apparel candidate,Pawn pawn,ApparelSettings s)
        {
            // Candidates that cannot pass Compare are skipped before they consume budget.
            if(WearTogether(worn.def,candidate.def,pawn.RaceProps.body))return true;
            if(candidate.MaxHitPoints>0&&(double)candidate.HitPoints/candidate.MaxHitPoints<Math.Max(0,Math.Min(100,s.MinimumConditionPercent))/100.0)return true;
            QualityCategory oldQuality,newQuality;
            return QualityUtility.TryGetQuality(worn,out oldQuality)&&QualityUtility.TryGetQuality(candidate,out newQuality)&&
                (int)newQuality-(int)oldQuality<Math.Max(0,s.MinimumQualityGain);
        }
        public void Scan(){Scan(false);}
        public void Scan(bool recheckManagers)
        {
            ApparelSettings settings=ApparelUpgradeMod.Settings;
            if(settings==null)return;
            var timer=Stopwatch.StartNew();
            object savedWarmth=neededWarmthField==null?null:neededWarmthField.GetValue(null);
            int failedItems=0;
            try
            {
                DetectManagers(recheckManagers);
                wearTogether.Clear();
                threat=GenHostility.AnyHostileActiveThreatToPlayer(map);
                profileCache=new Dictionary<Pawn,Dictionary<Apparel,Garment>>();
                var next=new List<UpgradeChoice>();bool partial=false;
                var allStock=map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>()
                    .Where(a=>a.Spawned&&a.Wearer==null).ToList();
                List<Apparel> stock;
                if(allStock.Count>MaxStock)
                {
                    partial=true;
                    if(stockOffset>=allStock.Count)stockOffset=0;
                    stock=allStock.Skip(stockOffset).Concat(allStock.Take(Math.Max(0,stockOffset+MaxStock-allStock.Count))).Take(MaxStock).ToList();
                }
                else {stock=allStock;stockOffset=0;}
                var byLayer=new Dictionary<ApparelLayerDef,List<Apparel>>();
                foreach(Apparel candidate in stock)
                    foreach(ApparelLayerDef layer in candidate.def.apparel.layers)
                    {List<Apparel> items;if(!byLayer.TryGetValue(layer,out items))byLayer[layer]=items=new List<Apparel>();items.Add(candidate);}
                var pawns=map.mapPawns.AllPawnsSpawned.Where(p=>!p.Dead&&p.IsFreeColonist&&p.apparel!=null&&p.outfits!=null).ToList();
                int budget=MaxComparisons,extraRows=MaxExtraSlotRows,paths=64;
                bool completedPass=true;
                var covered=new HashSet<Pawn>();
                if(pawnOffset>=pawns.Count)pawnOffset=0;
                for(int i=0;i<pawns.Count;i++)
                {
                    int index=(pawnOffset+i)%pawns.Count;Pawn pawn=pawns[index];
                    covered.Add(pawn);
                    bool stopped=false;
                    try
                    {
                        SetWarmth(pawn);
                        var scores=pawn.apparel.WornApparel.Select(a=>JobGiver_OptimizeApparel.ApparelScoreRaw(pawn,a)).ToList();
                        // Ordered (worn, candidate) pairs for this pawn; same type/material first.
                        var pairs=new List<KeyValuePair<Apparel,Apparel>>();
                        var paired=new HashSet<Apparel>();
                        foreach(Apparel worn in pawn.apparel.WornApparel)
                        {
                            var relevant=worn.def.apparel.layers.Where(l=>byLayer.ContainsKey(l)).SelectMany(l=>byLayer[l]).Distinct()
                                .Where(a=>!CheaplyExcluded(worn,a,pawn,settings))
                                .OrderByDescending(a=>settings.PreferSameTypeAndStuff&&a.def==worn.def&&a.Stuff==worn.Stuff);
                            foreach(Apparel candidate in relevant)if(paired.Add(candidate))pairs.Add(new KeyValuePair<Apparel,Apparel>(worn,candidate));
                        }
                        int cursor;cursors.TryGetValue(pawn.thingIDNumber,out cursor);
                        if(cursor>=pairs.Count)cursor=0;
                        for(int k=cursor;k<pairs.Count;k++)
                        {
                            if(--budget<0){partial=true;stopped=true;cursors[pawn.thingIDNumber]=k;break;}
                            var pair=pairs[k];
                            try{next.Add(Evaluate(pawn,pair.Key,pair.Value,scores,ref paths));}
                            catch(Exception e){failedItems++;next.Add(new UpgradeChoice{Pawn=pawn,Worn=pair.Key,Better=pair.Value,Reason="SAU_ReasonError",Detail=e.GetType().Name});}
                        }
                        if(!stopped)cursors[pawn.thingIDNumber]=0;
                        // Free slots: diagnostics only, from a separate small budget, and only for
                        // apparel the pawn's policy allows (other entries are not candidates).
                        if(!stopped&&extraRows>0&&pawn.outfits.CurrentApparelPolicy!=null)
                            foreach(Apparel candidate in stock)
                            {
                                if(extraRows<=0)break;
                                if(paired.Contains(candidate)||!pawn.outfits.CurrentApparelPolicy.filter.Allows(candidate))continue;
                                if(pawn.apparel.WornApparel.Any(a=>!ApparelUtility.CanWearTogether(a.def,candidate.def,pawn.RaceProps.body)))continue;
                                extraRows--;
                                string reason="SAU_ReasonExtraSlot";
                                if(!candidate.PawnCanWear(pawn,false)||!ApparelUtility.HasPartsToWear(pawn,candidate.def))reason="SAU_ReasonBody";
                                else if(candidate.IsForbidden(pawn))reason="SAU_ReasonForbidden";
                                else if(!pawn.CanReserve(candidate))reason="SAU_ReasonReserved";
                                else if(paths--<=0)reason="SAU_ReasonUnverified";
                                else if(!pawn.CanReach(candidate,PathEndMode.Touch,Danger.None))reason="SAU_ReasonReach";
                                next.Add(new UpgradeChoice{Pawn=pawn,Better=candidate,Reason=reason});
                            }
                    }
                    catch(Exception e)
                    {
                        failedItems++;
                        if(!Failed)Log.Warning("Smart Apparel Upgrade skipped "+pawn.LabelShort+": "+e.GetType().Name);
                    }
                    if(stopped){pawnOffset=index;completedPass=false;break;}
                }
                if(completedPass){pawnOffset=0;if(allStock.Count>MaxStock)stockOffset+=MaxStock;}
                // Rows of pawns this scan did not reach are kept for display only; automatic
                // orders always re-evaluate a row from scratch.
                foreach(UpgradeChoice old in choices)
                    if(old.Pawn!=null&&!covered.Contains(old.Pawn)&&pawns.Contains(old.Pawn))next.Add(old);
                choices=next;Partial=partial||paths<0;Failed=false;FailedItems=failedItems;ScanVersion++;
                SnapshotSignature=settings.Signature;
                int now=Find.TickManager.TicksGame;
                stamps.RemoveAll(s=>s==null||string.IsNullOrEmpty(s.PawnId)||((long)now-s.LastIssued)>600000);
            }
            catch(Exception e){if(!Failed)Log.Warning("Smart Apparel Upgrade scan incomplete: "+e.GetType().Name);Failed=true;}
            finally
            {
                if(neededWarmthField!=null&&savedWarmth!=null)neededWarmthField.SetValue(null,savedWarmth);
                ScanMilliseconds=timer.Elapsed.TotalMilliseconds;
            }
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
        // Defer for anything that needs care now: bleeding, medical rest, a non-chronic
        // condition awaiting treatment or a wound still healing. Chronic conditions (asthma,
        // ...) and permanent scars are lasting states, not reasons to wait forever.
        static bool NeedsCare(Pawn p)
        {
            if(p.health==null||p.health.hediffSet==null)return true;
            HediffSet set=p.health.hediffSet;
            // Vanilla ShouldSeekMedicalRest also fires for any tendable hediff, including
            // chronic ones; use its other parts and exclude chronic conditions explicitly.
            if(set.BleedRateTotal>0.0001f||set.InLabor()||set.HasTendedAndHealingInjury()||set.HasImmunizableNotImmuneHediff()||
                HealthAIUtility.ShouldHaveSurgeryDoneNow(p))return true;
            return set.hediffs.Any(h=>(h.TendableNow()&&!h.def.chronic)||(h is Hediff_Injury&&!h.IsPermanent()));
        }
        IdleState Activity(Pawn p)
        {
            // Idle colonists usually run the wander filler jobs; those carry no work to protect.
            return new IdleState {Idle=p.CurJob==null||p.CurJobDef==JobDefOf.Wait||p.CurJobDef==JobDefOf.Wait_MaintainPosture||
                p.CurJobDef==JobDefOf.Wait_Wander||p.CurJobDef==JobDefOf.GotoWander,
                Awake=p.Awake(),PlayerForced=p.CurJob!=null&&(p.CurJob.playerForced||p.CurJob.playerInterruptedForced),
                Queued=p.jobs==null||p.jobs.jobQueue.Count>0,
                Injured=NeedsCare(p),
                Threat=threat,Drafted=p.Drafted,Mental=p.InMentalState,Downed=p.Downed,Dead=p.Dead};
        }
        void DetectManagers(bool force)
        {
            ApparelSettings settings=ApparelUpgradeMod.Settings;
            int now=Find.TickManager.TicksGame;
            // Packages and Harmony patches do not change during play; inspect them rarely.
            if(force||now-managersCheckedAt>=ManagerRecheck||now<managersCheckedAt||managersSignature!=settings.Signature)
            {
                managersCheckedAt=now;managersSignature=settings.Signature;
                Inspect(settings);
            }
            AutoBlockKey=managerKey;AutoBlockDetail=managerDetail;
        }
        static void Inspect(ApparelSettings settings)
        {
            managerKey=null;managerDetail=null;
            var findings=new List<PatchFinding>();
            foreach(ModContentPack pack in LoadedModManager.RunningModsListForReading)
                if(Compatibility.IsManager(pack.PackageId,pack.Name))
                {managerKey="SAU_ManagerDetected";managerDetail=pack.Name;PatchFindings=findings;return;}
            try
            {
                Type harmony=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("HarmonyLib.Harmony",false)).FirstOrDefault(t=>t!=null);
                if(harmony==null){PatchFindings=findings;return;}
                MethodInfo get=harmony.GetMethod("GetPatchInfo",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(MethodBase)},null);
                if(get==null){managerKey="SAU_InspectionFailed";PatchFindings=findings;return;}
                foreach(Type t in new[]{typeof(JobGiver_OptimizeApparel),typeof(Pawn_ApparelTracker),typeof(Apparel),typeof(ApparelUtility)})
                    foreach(MethodInfo method in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static))
                    {
                        if(!watchedMethods.Contains(method.Name))continue;
                        object info=get.Invoke(null,new object[]{method});
                        if(info==null)continue;
                        var owners=(info.GetType().GetProperty("Owners")?.GetValue(info) as IEnumerable<string>)?.Distinct().ToList();
                        // An unreadable owner list cannot be accepted by the player.
                        if(owners==null||owners.Count==0)owners=new List<string>{"?"};
                        findings.Add(new PatchFinding{Method=t.Name+"."+method.Name,Owners=owners});
                    }
            }
            catch{managerKey="SAU_InspectionFailed";PatchFindings=findings;return;}
            PatchFindings=findings;
            var blocking=findings.Where(f=>f.Owners.Any(o=>o=="?"||!settings.AllowedPatchOwners.Contains(o))).ToList();
            if(blocking.Count>0)
            {
                managerKey="SAU_Patched";
                managerDetail=string.Join("; ",blocking.Select(f=>f.Method+" ("+string.Join(", ",f.Owners)+")"));
            }
        }
        void OrderIdleUpgrades()
        {
            DetectManagers(false);if(!string.IsNullOrEmpty(AutoBlockKey))return;
            threat=GenHostility.AnyHostileActiveThreatToPlayer(map);profileCache=null;
            object savedWarmth=neededWarmthField==null?null:neededWarmthField.GetValue(null);
            try
            {
                var issued=new HashSet<Pawn>();var claimed=new HashSet<Apparel>();int now=Find.TickManager.TicksGame;
                foreach(UpgradeChoice choice in choices.Where(c=>c.SafeAuto).OrderByDescending(c=>ApparelUpgradeMod.Settings.PreferSameTypeAndStuff&&c.Displaced.Count==1&&c.Worn.def==c.Better.def&&c.Worn.Stuff==c.Better.Stuff).ThenByDescending(c=>c.ScoreGain).ToList())
                {
                    Pawn pawn=choice.Pawn;
                    if(issued.Contains(pawn)||claimed.Contains(choice.Better)||!SwapSafety.CanAct(Activity(pawn)))continue;
                    UpgradeChoice fresh;
                    try
                    {
                        SetWarmth(pawn);
                        var scores=pawn.apparel.WornApparel.Select(a=>JobGiver_OptimizeApparel.ApparelScoreRaw(pawn,a)).ToList();int paths=1;
                        fresh=Evaluate(pawn,choice.Worn,choice.Better,scores,ref paths);
                    }
                    catch(Exception){continue;} // A failed recheck never orders.
                    if(!fresh.SafeAuto)continue;
                    // Only vanilla Wear performs removal/equip. No direct tracker mutation.
                    // Not TryTakeOrderedJob: it sets Job.playerForced, and JobDriver_Wear then marks the
                    // new apparel as forced (OutfitForcedHandler.SetForced), silently changing the
                    // player's outfit state and blocking later vanilla optimization.
                    Job job=JobMaker.MakeJob(JobDefOf.Wear,choice.Better);
                    job.playerForced=false;
                    pawn.jobs.StartJob(job,JobCondition.InterruptForced,null,false,true,null,JobTag.Misc);
                    if(pawn.CurJob==job)
                    {
                        issued.Add(pawn);claimed.Add(choice.Better);
                        stamps.RemoveAll(s=>s.PawnId==pawn.ThingID);
                        stamps.Add(new SwapStamp{PawnId=pawn.ThingID,TargetId=choice.Better.ThingID,RemovedId=choice.Worn.ThingID,RemovedIds=fresh.Displaced.Select(a=>a.ThingID).ToList(),LastIssued=now});
                        choice.Reason="SAU_ReasonOrdered";choice.SafeAuto=false;
                    }
                }
            }
            finally{if(neededWarmthField!=null&&savedWarmth!=null)neededWarmthField.SetValue(null,savedWarmth);}
        }
    }
}
