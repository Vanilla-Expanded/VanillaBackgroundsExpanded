using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.Video;
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
        private static VideoPlayer videoPlayer;
        private static VideoPlayer videoPlayerTransition;

        private static int index;

        public static BackgroundImageDef Current
        {
            get => current ?? Default;
            set
            {
                if (current == value) return;
                if (value is null) return;
                if (value.animated)
                {
                    if (!VBEMod.Settings.allowAnimated)
                    {
                        Log.Error("[VBE] Tried to load animated background while that setting is disabled");
                        return;
                    }

                    if (value.Video.NullOrEmpty())
                    {
                        Log.Error("[VBE] Tried to switch to invalid Background");
                        return;
                    }
                }

                if (current is not null && current.animated && !value.animated)
                {
                    videoPlayer.Stop();
                    videoPlayer.enabled = false;
                }

                var idx = VBEMod.Settings.Enabled.ToList().IndexOf(value);
                if (idx >= 0) index = idx;

                current = value;
                if (Current.animated)
                {
                    videoPlayer ??= Utils.MakeVideoPlayer();
                    if (videoPlayer.url == Current.Video && videoPlayer.isPrepared)
                        videoPlayer.Play();
                    else
                    {
                        videoPlayer.enabled = true;
                        videoPlayer.sendFrameReadyEvents = false;
                        videoPlayer.frame = 0;
                        videoPlayer.url = Current.Video;
                        videoPlayer.time = 0f;
                        videoPlayer.Prepare();
                    }
                }
                else BackgroundMain.overrideBGImage = Current.Texture;
            }
        }


        private static UI_BackgroundMain BackgroundMain => (UI_BackgroundMain) UIMenuBackgroundManager.background;


        public static void DoOverlay()
        {
            if (!initialized) return;
            if (Current.animated)
            {
                if (videoPlayer.isPlaying) Utils.DrawBG(videoPlayer.texture);
                else if (videoPlayer.isPrepared) videoPlayer.Play();
            }

            if (!VBEMod.Settings.cycle) return;
            if (BackgroundMain.expansionImageFades is not null && ModLister.AllExpansions.Any(key => BackgroundMain.expansionImageFades[key] > 0f)) return;

            if (Time.time >= transitionTime)
            {
                transitionTo = GetNext();
                transitionTime = float.MaxValue;
                if (transitionTo.animated)
                {
                    videoPlayerTransition ??= Utils.MakeVideoPlayer();
                    videoPlayerTransition.frame = 0;
                    videoPlayerTransition.url = transitionTo.Video;
                    videoPlayerTransition.prepareCompleted += source =>
                    {
                        transitionPct = 0f;
                        source.Play();
                    };
                    videoPlayerTransition.Prepare();
                }
                else transitionPct = 0f;
            }

            if (transitionPct >= 0f && transitionTo is not null)
            {
                transitionPct += Time.deltaTime;
                if (transitionTo != Current)
                {
                    GUI.color = new Color(1f, 1f, 1f, transitionPct);
                    Utils.DrawBG(transitionTo.animated ? videoPlayerTransition.texture : transitionTo.Texture);
                    GUI.color = Color.white;
                }

                if (transitionPct >= 1f && !PauseTransition)
                {
                    if (transitionTo.animated)
                    {
                        Object.Destroy(videoPlayer);
                        videoPlayer = videoPlayerTransition;
                        videoPlayerTransition = null;
                        Current = transitionTo;
                    }
                    else Current = transitionTo;

                    transitionPct = -1f;
                    transitionTo = null;
                    transitionTime = Time.time + VBEMod.Settings.cycleTime.RandomInRange;
                }
            }
        }

        public static void Notify_SettingsChanged(VBESettings settings)
        {
            if (settings.cycle)
            {
                if (settings.Allowed(Current))
                {
                    transitionPct = -1f;
                    if (transitionTo is not null && settings.Allowed(transitionTo)) Current = transitionTo;
                    transitionTo = null;
                    transitionTime = Time.time + settings.cycleTime.RandomInRange;
                }
                else
                {
                    transitionTo = GetNext();
                    transitionTime = float.MaxValue;
                    transitionPct = 0f;
                }
            }
            else
            {
                transitionPct = -1f;
                if (transitionTo is not null && settings.Allowed(transitionTo)) Current = transitionTo;
                transitionTo = null;
                transitionTime = float.MaxValue;
                if (!settings.Allowed(transitionTo)) Current = GetNext();
            }
        }

        private static BackgroundImageDef GetNext()
        {
            var settings = VBEMod.Settings;
            if (!settings.current.NullOrEmpty()) return DefDatabase<BackgroundImageDef>.GetNamedSilentFail(settings.current);
            if (settings.randomize)
                return settings.Enabled.TryRandomElement(out var random) ? random : Default;
            var list = settings.Enabled.ToList();
            return list[Utils.Wrap(index + 1, 0, list.Count - 1)];
        }

        public static void Initialize()
        {
            var settings = VBEMod.Settings;
            settings.CheckInit();
            Current = GetNext();

            if (settings.cycle) transitionTime = Time.time + settings.cycleTime.RandomInRange;

            initialized = true;
        }
    }
}