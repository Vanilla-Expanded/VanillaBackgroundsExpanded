using RimWorld;
using UnityEngine;
using Verse;

namespace VBE
{
    public static class BackgroundController
    {
        private static BackgroundImageDef current;
        public static BackgroundImageDef Default;
        private static float transitionTime = float.MaxValue;
        private static float transitionPct = -1f;
        private static BackgroundImageDef transitionTo;
        private static bool initialized;
        public static bool PauseTransition;

        public static BackgroundImageDef Current
        {
            get => current ?? Default;
            set
            {
                if (current == value) return;
                current = value;
                BackgroundMain.overrideBGImage = Current.Texture;
            }
        }

        private static UI_BackgroundMain BackgroundMain => (UI_BackgroundMain) UIMenuBackgroundManager.background;

        public static void DoOverlay(Rect bgRect)
        {
            if (!initialized) return;
            if (PauseTransition)
            {
                if (transitionPct >= 0f && transitionTo is not null && transitionTo != Current)
                {
                    GUI.color = new Color(1f, 1f, 1f, transitionPct);
                    GUI.DrawTexture(bgRect, transitionTo.Texture, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }

                return;
            }

            if (!VBEMod.Settings.cycle) return;
            if (BackgroundMain.expansionImageFades is not null && ModLister.AllExpansions.Any(key => BackgroundMain.expansionImageFades[key] > 0f)) return;

            if (Time.time >= transitionTime)
            {
                transitionTo = GetNext();
                transitionPct = 0f;
                transitionTime = float.MaxValue;
            }

            if (transitionPct >= 0f && transitionTo is not null)
            {
                transitionPct += Time.deltaTime;
                if (transitionTo != Current)
                {
                    GUI.color = new Color(1f, 1f, 1f, transitionPct);
                    GUI.DrawTexture(bgRect, transitionTo.Texture, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }

                if (transitionPct >= 1f)
                {
                    Current = transitionTo;
                    transitionPct = -1f;
                    transitionTo = null;
                    transitionTime = Time.time + VBEMod.Settings.cycleTime.RandomInRange;
                }
            }
        }

        public static void Notify_SettingsChanged(VBESettings settings)
        {
            if (!settings.cycle)
            {
                transitionPct = -1f;
                if (transitionTo is not null) Current = transitionTo;
                transitionTo = null;
                transitionTime = float.MaxValue;
            }
            else
            {
                transitionPct = -1f;
                if (transitionTo is not null) Current = transitionTo;
                transitionTo = null;
                transitionTime = Time.time + settings.cycleTime.RandomInRange;
            }
        }

        private static BackgroundImageDef GetNext() => VBEMod.Settings.Enabled.TryRandomElement(out var random) ? random : Default;

        public static void Initialize()
        {
            var settings = VBEMod.Settings;
            settings.CheckInit();
            if (settings.randomize) Current = GetNext();

            if (settings.cycle) transitionTime = Time.time + settings.cycleTime.RandomInRange;

            initialized = true;
        }
    }
}