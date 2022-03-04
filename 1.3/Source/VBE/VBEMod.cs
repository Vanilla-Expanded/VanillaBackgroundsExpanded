using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VBE
{
    public class VBEMod : Mod
    {
        public static Harmony Harm;
        public static VBESettings Settings;
        public static VBEMod Instance;
        private float lastHeight;
        private Vector2 scrollPos;

        public VBEMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Harm = new Harmony("vanillaexpanded.backgrounds");
            Settings = GetSettings<VBESettings>();
            LongEventHandler.ExecuteWhenFinished(Initialize);
            HarmonyPatches.DoPatches(Harm);
        }

        public void Initialize()
        {
            foreach (var def in ModLister.AllExpansions)
            {
                var bgDef = new BackgroundImageDef
                {
                    label = def.label,
                    description = def.description,
                    defName = def.defName,
                    path = def.backgroundPath,
                    iconPath = def.iconPath
                };
                DefGenerator.AddImpliedDef(bgDef);
                if (def.isCore) BackgroundController.Default = bgDef;
            }

            if (ModLister.HasActiveModWithName("RimThemes")) ModCompat.LoadRimThemesImages();

            Settings.CheckInit();

            BackgroundController.Initialize();
        }

        public override string SettingsCategory() => "VBE".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            BackgroundController.PauseTransition = true;
            var leftRect = inRect.LeftPartPixels(120f);
            Widgets.DrawLineVertical(leftRect.xMax + 15f, leftRect.yMin, leftRect.height);
            inRect.xMin += 150f;
            Widgets.DrawMenuSection(inRect);
            inRect = inRect.ContractedBy(7f);
            var listing = new Listing_Standard();
            listing.Begin(leftRect);
            listing.CheckboxLabeled("VBE.Randomize".Translate(), ref Settings.randomize, "VBE.Randomize.Desc".Translate());
            listing.CheckboxLabeled("VBE.Cycle".Translate(), ref Settings.cycle, "VBE.Cycle.Desc".Translate());
            if (Settings.cycle)
            {
                listing.Label("VBE.CycleTime".Translate());
                Widgets.FloatRange(listing.GetRect(28f), (int) listing.CurHeight, ref Settings.cycleTime, 0.1f, 60f);
            }

            listing.Label("VBE.ToggleBackgrounds".Translate());

            listing.End();

            var width = (inRect.width - 37f) / 3f;
            var height = 0f;
            var viewRect = new Rect(0, 0, inRect.width - 17f, lastHeight);
            lastHeight = 5f;
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            var curPos = new Vector2(5f, 5f);
            foreach (var def in DefDatabase<BackgroundImageDef>.AllDefs)
            {
                var enabled = Settings.enabled[def.defName];
                if (curPos.x + width > viewRect.xMax)
                {
                    curPos.x = 5f;
                    curPos.y += height + 5f;
                    lastHeight += height + 5f;
                    height = 0f;
                }

                var myHeight = (float) def.Texture.height / def.Texture.width * width;
                height = Mathf.Max(height, myHeight);
                var rect = new Rect(curPos, new Vector2(width, myHeight));
                GUI.DrawTexture(rect, def.Texture, ScaleMode.ScaleToFit);
                if (Widgets.ButtonInvisible(rect)) enabled = !enabled;
                Widgets.DrawHighlightIfMouseover(rect);
                TooltipHandler.TipRegion(rect, () => $"{def.LabelCap}\n\n{def.description}", def.shortHash);
                Widgets.Checkbox(new Vector2(rect.xMax - 24f, rect.yMax - 24f), ref enabled);
                curPos.x += width + 5f;
                Settings.enabled[def.defName] = enabled;
            }

            lastHeight += height;

            Widgets.EndScrollView();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            BackgroundController.PauseTransition = false;
            BackgroundController.Notify_SettingsChanged(Settings);
        }
    }

    public class VBESettings : ModSettings
    {
        public string current;
        public bool cycle = true;
        public FloatRange cycleTime = new(5f, 10f);
        public Dictionary<string, bool> enabled;
        public bool randomize = true;

        public IEnumerable<BackgroundImageDef> Enabled =>
            enabled.Where(kv => kv.Value).Select(kv => DefDatabase<BackgroundImageDef>.GetNamedSilentFail(kv.Key)).Where(def => def is not null);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cycle, "cycle", true);
            Scribe_Values.Look(ref randomize, "randomize", true);
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
        }
    }

    public class BackgroundImageDef : Def
    {
        private Texture2D icon;
        public string iconPath;
        public string path;
        private Texture2D texture;

        public BackgroundImageDef()
        {
        }

        public BackgroundImageDef(Texture2D tex, Texture2D icn = null)
        {
            texture = tex;
            icon = icn;
        }

        public Texture2D Texture => texture ??= ContentFinder<Texture2D>.Get(path);
        public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);
    }
}