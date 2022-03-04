using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VBE
{
    [StaticConstructorOnStartup]
    public static class ModCompat
    {
        static ModCompat()
        {
            if (!ModLister.HasActiveModWithName("RimThemes")) return;
            VBEMod.Harm.Patch(AccessTools.Method(AccessTools.TypeByName("aRandomKiwi.RimThemes.LoaderGM"), "loadThemesTextures"),
                postfix: new HarmonyMethod(typeof(ModCompat), nameof(LoadRimThemesImages)));
        }

        public static void LoadRimThemesImages()
        {
            var themes = AccessTools.TypeByName("aRandomKiwi.RimThemes.Themes");
            var defaultIcon = (Texture2D) AccessTools.Field(AccessTools.TypeByName("aRandomKiwi.RimThemes.Loader"), "defaultIconTex")?.GetValue(null) ?? Texture2D.normalTexture;
            var textDB = (Dictionary<string, Dictionary<string, string>>) AccessTools.Field(themes, "DBText")?.GetValue(null);
            var texDB = (Dictionary<string, Dictionary<string, Dictionary<string, Texture2D>>>) AccessTools.Field(themes, "DBTex")?.GetValue(null);
            var iconDB = (Dictionary<string, Texture2D>) AccessTools.Field(themes, "DBTexThemeIcon")?.GetValue(null);
            var animatedBackgroundDB = (Dictionary<string, string>) AccessTools.Field(themes, "DBAnimatedBackground")?.GetValue(null);
            if (texDB is not null)
                foreach (var (key, dict) in texDB)
                    if (dict.ContainsKey("UI_BackgroundMain") && dict["UI_BackgroundMain"].ContainsKey("BGPlanet"))
                    {
                        var tex = dict["UI_BackgroundMain"]["BGPlanet"];
                        var icn = iconDB is not null && iconDB.TryGetValue(key, out var icon) ? icon : defaultIcon;
                        var arr = key.Split('§');
                        var defName = arr[1] + arr[0];
                        if (DefDatabase<BackgroundImageDef>.GetNamedSilentFail(defName) is not null) continue;
                        DefGenerator.AddImpliedDef(new BackgroundImageDef(tex, icn)
                        {
                            label = arr[1].Trim(),
                            description = textDB?[key]?["description"] ?? "VBE.NoDesc".Translate(),
                            defName = defName,
                            isTheme = true
                        });
                    }

            if (animatedBackgroundDB is not null)
                foreach (var (key, path) in animatedBackgroundDB)
                {
                    var icn = iconDB is not null && iconDB.TryGetValue(key, out var icon) ? icon : defaultIcon;
                    var arr = key.Split('§');
                    var defName = arr[1] + arr[0] + "_Animated";
                    if (DefDatabase<BackgroundImageDef>.GetNamedSilentFail(defName) is not null) continue;
                    DefGenerator.AddImpliedDef(new BackgroundImageDef(path, icn)
                    {
                        label = arr[1].Trim() + " (" + "VBE.Animated".Translate() + ")",
                        description = textDB?[key]?["description"] ?? "VBE.NoDesc".Translate(),
                        defName = defName,
                        isTheme = true,
                        animated = true,
                        previewFrame = 30
                    });
                }

            VBEMod.Settings.CheckInit();
            VBEMod.Instance.InitializeAnimated();
        }
    }
}