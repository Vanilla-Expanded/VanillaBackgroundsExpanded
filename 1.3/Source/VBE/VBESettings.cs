using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VBE
{
    public class VBESettings : ModSettings
    {
        public bool allowAnimated = true;
        public string current;
        public bool cycle = true;
        public FloatRange cycleTime = new(5f, 10f);
        public Dictionary<string, bool> enabled;
        public bool randomize = true;

        public IEnumerable<BackgroundImageDef> Enabled => from kv in enabled
            where kv.Value
            let def = DefDatabase<BackgroundImageDef>.GetNamedSilentFail(kv.Key)
            where def is not null && (allowAnimated || !def.animated)
            orderby def.isCore descending, def.isVanilla descending, def.isTheme, def.label
            select def;


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cycle, "cycle", true);
            Scribe_Values.Look(ref randomize, "randomize", true);
            Scribe_Values.Look(ref allowAnimated, "allowAnimated", true);
            Scribe_Values.Look(ref cycleTime, "cycleTime", new FloatRange(5f, 10f));
            Scribe_Values.Look(ref current, "current");
            Scribe_Collections.Look(ref enabled, "enabled", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) CheckInit();
        }

        public void CheckInit()
        {
            enabled ??= DefDatabase<BackgroundImageDef>.AllDefs.ToDictionary(def => def.defName, _ => true);

            foreach (var def in DefDatabase<BackgroundImageDef>.AllDefs)
                if (!enabled.ContainsKey(def.defName))
                    enabled.Add(def.defName, true);

            if (randomize || cycle) current = null;
        }

        public bool Allowed(BackgroundImageDef def) =>
            def is not null && enabled[def.defName] && (!def.animated || allowAnimated) && (current.NullOrEmpty() || current == def.defName);
    }
}