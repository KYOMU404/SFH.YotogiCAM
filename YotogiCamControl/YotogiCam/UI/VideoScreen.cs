using UnityEngine;
using RenderHeads.Media.AVProVideo;

namespace COM3D2.YotogiCamControl.Plugin.UI
{
    public class VideoScreen
    {
        public GameObject obj;
        public MediaPlayer mediaPlayer;
        public Rect windowRect = new Rect(300, 20, 200, 170);

        public void Initialize()
        {
            obj = new GameObject("YotogiVideoPlayer");
            GameObject.DontDestroyOnLoad(obj);
            mediaPlayer = obj.AddComponent<MediaPlayer>();
            mediaPlayer.m_AutoOpen = false;
            mediaPlayer.m_AutoStart = true;
            mediaPlayer.m_Loop = true;
        }

        public void Load(string path)
        {
            if (mediaPlayer == null) return;

            MediaPlayer.FileLocation location = MediaPlayer.FileLocation.AbsolutePathOrURL;
            if (string.IsNullOrEmpty(path)) return;

            mediaPlayer.OpenVideoFromFile(location, path, true);
        }

        public void Close()
        {
            if (mediaPlayer != null) mediaPlayer.CloseVideo();
        }

        public bool IsLoaded()
        {
            return mediaPlayer != null && mediaPlayer.VideoOpened;
        }

        public bool IsPlaying()
        {
            return mediaPlayer != null && mediaPlayer.Control != null && mediaPlayer.Control.IsPlaying();
        }

        public void Play() { mediaPlayer?.Play(); }
        public void Pause() { mediaPlayer?.Pause(); }
        public void Rewind() { mediaPlayer?.Rewind(true); }

        public void SetLooping(bool loop)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.m_Loop = loop;
                if (mediaPlayer.Control != null) mediaPlayer.Control.SetLooping(loop);
            }
        }
        public bool GetLooping() { return mediaPlayer != null && mediaPlayer.m_Loop; }

        public void SetVolume(float vol)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.m_Volume = vol;
                if (mediaPlayer.Control != null) mediaPlayer.Control.SetVolume(vol);
            }
        }
        public float GetVolume() { return mediaPlayer != null ? mediaPlayer.m_Volume : 1f; }

        public Texture GetTexture()
        {
            if (mediaPlayer != null && mediaPlayer.TextureProducer != null)
            {
                return mediaPlayer.TextureProducer.GetTexture();
            }
            return null;
        }

        public bool RequiresFlip()
        {
            if (mediaPlayer != null && mediaPlayer.TextureProducer != null)
            {
                return mediaPlayer.TextureProducer.RequiresVerticalFlip();
            }
            return false;
        }

        public void Destroy()
        {
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }
    }
}
