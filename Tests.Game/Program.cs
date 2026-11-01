using System;
using System.IO;
using System.Reflection;
using Pengin.SmartApparelUpgrade;
using Verse;
class Program
{
    static void Main()
    {
        _=ParseHelper.FromString<string>("fixture");
        // Real default PrefsData for DeepProfiler; no engine or playable-map initialization.
        typeof(Prefs).GetField("data",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new PrefsData());
        string path=Path.Combine(Path.GetTempPath(),"sau-"+Guid.NewGuid()+".xml");
        try
        {
            var settings=new ApparelSettings {AutoWear=true,PreferSameTypeAndStuff=false,MinimumQualityGain=3,MinimumConditionPercent=95,MinimumPerformanceGainPercent=8};
            var stamp=new SwapStamp {PawnId="pawn",TargetId="new",RemovedId="old",RemovedIds=new System.Collections.Generic.List<string>{"old","second"},LastIssued=100};
            Scribe.saver.InitSaving(path,"fixture");
            Scribe.EnterNode("settings");settings.ExposeData();Scribe.ExitNode();
            Scribe.EnterNode("history");stamp.ExposeData();Scribe.ExitNode();
            Scribe.saver.FinalizeSaving();
            settings=new ApparelSettings();stamp=new SwapStamp();
            Scribe.loader.InitLoading(path);
            Scribe.EnterNode("settings");settings.ExposeData();Scribe.ExitNode();
            Scribe.EnterNode("history");stamp.ExposeData();Scribe.ExitNode();
            Scribe.loader.FinalizeLoading();
            if(!settings.AutoWear||settings.PreferSameTypeAndStuff||settings.MinimumQualityGain!=3||settings.MinimumConditionPercent!=95||settings.MinimumPerformanceGainPercent!=8||stamp.PawnId!="pawn"||stamp.TargetId!="new"||stamp.RemovedId!="old"||stamp.LastIssued!=100||stamp.RemovedIds.Count!=2||stamp.RemovedIds[1]!="second")
                throw new Exception("Apparel settings/history Scribe round trip failed");
            if(SwapSafety.CooldownAllows(200,stamp.LastIssued,"new",stamp.RemovedId))
                throw new Exception("Saved cooldown not respected");
            Console.WriteLine("PASS real-game Scribe apparel settings/history round trip and cooldown (not map save/load)");
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
}
