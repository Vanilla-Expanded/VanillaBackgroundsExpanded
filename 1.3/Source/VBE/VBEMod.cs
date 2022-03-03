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
        private float lastHeight;
        private Vector2 scrollPos;

        public VBEMod(ModContentPack content) : base(content)
        {
            Harm = new Harmony("vanillaexpanded.backgrounds");
            LongEventHandler.ExecuteWhenFinished(() =>
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

                Settings = GetSettings<VBESettings>();

                BackgroundController.Initialize();
            });
            HarmonyPatches.DoPatches(Harm);
        }

        public override string SettingsCategory() => "Vanilla Backgrounds Expanded";

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
            listing.CheckboxLabeled("Randomize", ref Settings.randomize, "Randomize background on game start");
            listing.CheckboxLabeled("Cycle", ref Settings.cycle, "Cycle background occasionally while in game");
            if (Settings.cycle)
            {
                listing.Label("Cycle time:");
                Widgets.FloatRange(listing.GetRect(28f), (int) listing.CurHeight, ref Settings.cycleTime, 0.1f, 60f);
            }

            listing.Label("Toggle Backgrounds Being Shown ->");

            listing.End();

            var width = (inRect.width - 35f) / 3f;
            var height = 0f;
            var viewRect = new Rect(0, 0, inRect.width - 15f, lastHeight);
            lastHeight = 5f;
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            var curPos = new Vector2(5f, 5f);
            foreach (var def in DefDatabase<BackgroundImageDef>.AllDefs)
            {
                var enabled = Settings.enabled[def];
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
                Settings.enabled[def] = enabled;
            }

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
        public BackgroundImageDef current;
        public bool cycle = true;
        public FloatRange cycleTime = new(5f, 10f);
        public Dictionary<BackgroundImageDef, bool> enabled;
        public bool randomize = true;
        public IEnumerable<BackgroundImageDef> Enabled => enabled.Where(kv => kv.Value).Select(kv => kv.Key);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cycle, "cycle", true);
            Scribe_Values.Look(ref randomize, "randomize", true);
            Scribe_Values.Look(ref cycleTime, "cycleTime", new FloatRange(5f, 10f));
            Scribe_Defs.Look(ref current, "current");
            Scribe_Collections.Look(ref enabled, "enabled", LookMode.Def, LookMode.Value);
        }
    }

    public class BackgroundImageDef : Def
    {
        private Texture2D icon;
        public string iconPath;
        public string path;
        private Texture2D texture;
        public Texture2D Texture => texture ??= ContentFinder<Texture2D>.Get(path);
        public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);
    }
}