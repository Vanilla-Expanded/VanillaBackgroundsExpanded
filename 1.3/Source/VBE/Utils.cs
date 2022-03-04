using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace VBE
{
    public static class Utils
    {
        public static VideoPlayer MakeVideoPlayer()
        {
            var videoPlayer = Current.Root_Entry.gameObject.AddComponent<VideoPlayer>();
            videoPlayer.enabled = true;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.playOnAwake = true;
            videoPlayer.errorReceived += delegate(VideoPlayer source, string message) { Log.Error($"[VBE] Error in video player {source}: " + message + " "); };
            return videoPlayer;
        }

        public static void DrawBG(Texture texture)
        {
            GUI.DrawTexture(GetBGRect(texture), texture, ScaleMode.ScaleToFit);
        }

        public static Rect GetBGRect(Texture texture)
        {
            var vector = new Vector2(texture.width, texture.height);
            Rect rect;
            if (UI.screenWidth > UI.screenHeight * (vector.x / vector.y))
            {
                float width = UI.screenWidth;
                var num2 = UI.screenWidth * (vector.y / vector.x);
                rect = new Rect(0f, (UI.screenHeight - num2) * 0.5f, width, num2);
            }
            else
            {
                float height = UI.screenHeight;
                var num = UI.screenHeight * (vector.x / vector.y);
                rect = new Rect((UI.screenWidth - num) * 0.5f, 0f, num, height);
            }

            return rect;
        }

        public static int Wrap(int value, int min, int max)
        {
            if (value < min) value = max - (min - value);
            if (value > max) value = min + (value - max);
            return value;
        }
    }
}