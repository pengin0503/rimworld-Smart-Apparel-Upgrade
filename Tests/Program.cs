using System;
using Pengin.SmartApparelUpgrade;

static class Program
{
    static int count;
    static void Check(bool v,string name){if(!v)throw new Exception(name);count++;}
    static Garment G(int q,double hp,double score,params double[] stats){return new Garment{Quality=q,Condition=hp,Score=score,Stats=stats};}
    static void Main()
    {
        var old=G(2,.8,2,1,1,1,1,1);var good=G(3,1,3,2,2,2,2,2);
        Check(SwapSafety.Compare(old,good,1,.7,1,.5)==SwapReason.None,"strictly improving replacement allowed");
        Check(SwapSafety.Compare(old,G(4,1,4,.5,2,2,2,2),1,.7,1,.5)==SwapReason.Regression,"quality cannot compensate for armor loss");
        Check(SwapSafety.Compare(old,G(3,.75,3,2,2,2,2,2),1,.7,1,.5)==SwapReason.Condition,"condition must not regress");
        Check(SwapSafety.Compare(old,G(3,1,2.001,2,2,2,2,2),1,.7,5,.001)==SwapReason.Gain,"minimum improvement enforced");
        Check(SwapSafety.Compare(old,good,2,.7,1,.5)==SwapReason.Quality,"minimum quality difference enforced");
        Check(SwapSafety.Compare(old,G(3,1,double.NaN,2,2,2,2,2),1,.7,1,.5)==SwapReason.Invalid,"invalid modded stats fail closed");
        Check(SwapSafety.Compare(old,good,1,.7,1,-1)==SwapReason.Gain,"negative vanilla net gain is rejected");
        Check(SwapSafety.Compare(old,G(3,1,3,2,2,2,2,.5),1,.7,1,.5)==SwapReason.Regression,"cold or heat insulation cannot regress");
        Check(Compatibility.IsManager("LordKuper.OutfitManager","renamed"),"verified Outfit Manager package id");
        Check(Compatibility.IsManager("unknown","Colony Wardrobe — Outfit Manager"),"unverified package uses observed display-name safeguard");
        Check(!Compatibility.IsManager("other","Better Clothes Textures"),"unrelated apparel art mod is not name-blocked");
        var state=new IdleState {Idle=true,Awake=true};
        Check(SwapSafety.CanAct(state),"idle awake pawn may act");
        state.PlayerForced=true;Check(!SwapSafety.CanAct(state),"manual wait is protected");state.PlayerForced=false;
        state.Queued=true;Check(!SwapSafety.CanAct(state),"queued jobs are protected");state.Queued=false;
        state.Injured=true;Check(!SwapSafety.CanAct(state),"injuries are protected");state.Injured=false;
        state.Threat=true;Check(!SwapSafety.CanAct(state),"combat threat is protected");state.Threat=false;
        state.Drafted=true;Check(!SwapSafety.CanAct(state),"drafted pawn is protected");state.Drafted=false;
        state.Mental=true;Check(!SwapSafety.CanAct(state),"mental state is protected");state.Mental=false;
        state.Awake=false;Check(!SwapSafety.CanAct(state),"sleep is protected");state.Awake=true;
        state.Idle=false;Check(!SwapSafety.CanAct(state),"useful work is protected");
        Check(!SwapSafety.CooldownAllows(100,90,"a","b"),"cooldown blocks repeated order");
        Check(!SwapSafety.CooldownAllows(200,300,"a","b"),"future save timestamp fails closed");
        Check(!SwapSafety.CooldownAllows(70000,100,"old","old"),"recently removed garment cannot reverse immediately");
        Check(SwapSafety.CooldownAllows(70000,100,"new","old"),"different monotonic upgrade after cooldown");
        var aggregate=SwapSafety.Combine(new[]{old,G(1,.6,1,.2,.2,.2,.2,.2)});
        Check(aggregate.Quality==2&&aggregate.Condition==.8&&aggregate.Score==3&&Math.Abs(aggregate.Stats[0]-1.2)<1e-9,"all removed garments contribute to baseline");
        Check(SwapSafety.Compare(aggregate,good,1,.7,1,.5)==SwapReason.Gain,"replacement must beat combined raw score");
        Check(SwapSafety.Compare(G(-1,.8,2,1,1,1,1,1),good,1,.7,1,.5)==SwapReason.Invalid,"unknown quality cannot authorize automation");
        Check(SwapSafety.Combine(new[]{old,(Garment)null})==null,"missing removed profile fails closed");
        Check(!SwapSafety.SameFinite(double.NaN,1)&&!SwapSafety.SameFinite(1,2)&&SwapSafety.SameFinite(1,1),"equipped offsets must be finite and preserved");
        var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<100000;i++)SwapSafety.Compare(old,good,1,.7,1,.5);
        Console.WriteLine($"PASS {count} safety assertions; 100,000 comparisons: {timer.Elapsed.TotalMilliseconds:F2} ms (model only)");
    }
}
