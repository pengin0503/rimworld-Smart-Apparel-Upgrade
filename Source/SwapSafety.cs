using System;
using System.Collections.Generic;

namespace Pengin.SmartApparelUpgrade
{
    public enum SwapReason {None,Invalid,Quality,Condition,Regression,Gain}
    public sealed class Garment
    {
        public int Quality;
        public double Condition,Score;
        public double[] Stats;
    }
    public sealed class IdleState
    {
        public bool Idle,Awake,PlayerForced,Queued,Injured,Threat,Drafted,Mental,Downed,Dead;
    }
    public static class SwapSafety
    {
        static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value);}
        public static bool SameFinite(double old,double next){return Finite(old)&&Finite(next)&&Math.Abs(old-next)<=1e-6;}
        public static Garment Combine(IEnumerable<Garment> garments)
        {
            var result=new Garment {Quality=-1,Stats=new double[5]};bool any=false;
            foreach(Garment garment in garments)
            {
                if(garment==null||garment.Quality<0||garment.Stats==null||garment.Stats.Length!=5)return null;
                any=true;result.Quality=Math.Max(result.Quality,garment.Quality);
                result.Condition=Math.Max(result.Condition,garment.Condition);result.Score+=garment.Score;
                for(int i=0;i<5;i++)result.Stats[i]+=garment.Stats[i];
            }
            return any?result:null;
        }
        public static SwapReason Compare(Garment old,Garment candidate,int minimumQuality,
            double minimumCondition,double minimumGainPercent,double vanillaNetGain)
        {
            if(old==null||candidate==null||old.Quality<0||candidate.Quality<0||old.Stats==null||candidate.Stats==null||old.Stats.Length!=5||
                candidate.Stats.Length!=5||!Finite(old.Score)||!Finite(candidate.Score)||
                !Finite(old.Condition)||!Finite(candidate.Condition)||!Finite(vanillaNetGain)||
                !Finite(minimumCondition)||!Finite(minimumGainPercent))return SwapReason.Invalid;
            if(candidate.Quality-old.Quality<Math.Max(0,minimumQuality))return SwapReason.Quality;
            if(candidate.Condition<minimumCondition||candidate.Condition+1e-6<old.Condition)return SwapReason.Condition;
            for(int i=0;i<5;i++)
            {
                if(!Finite(old.Stats[i])||!Finite(candidate.Stats[i]))return SwapReason.Invalid;
                if(candidate.Stats[i]+1e-6<old.Stats[i])return SwapReason.Regression;
            }
            double threshold=Math.Max(.001,Math.Abs(old.Score)*Math.Max(0,minimumGainPercent)/100);
            if(candidate.Score-old.Score<threshold||vanillaNetGain<=.001)return SwapReason.Gain;
            return SwapReason.None;
        }
        public static bool CanAct(IdleState s)
        {
            return s!=null&&s.Idle&&s.Awake&&!s.PlayerForced&&!s.Queued&&!s.Injured&&!s.Threat&&
                !s.Drafted&&!s.Mental&&!s.Downed&&!s.Dead;
        }
        public static bool CooldownAllows(int now,int last,string target,string lastRemoved)
        {
            long elapsed=(long)now-last;
            return elapsed>=60000&&(target!=lastRemoved||elapsed>=180000);
        }
    }
    public static class Compatibility
    {
        public static bool IsManager(string packageId,string displayName)
        {
            if(string.Equals(packageId,"LordKuper.OutfitManager",StringComparison.OrdinalIgnoreCase))return true;
            // Display-name safeguards, deliberately not invented PackageIds.
            string text=(displayName??"").ToLowerInvariant().Replace(" ","").Replace("-","");
            return text.Contains("wardrobe")||text.Contains("outfitmanager")||text.Contains("chooseyouroutfit");
        }
    }
}
