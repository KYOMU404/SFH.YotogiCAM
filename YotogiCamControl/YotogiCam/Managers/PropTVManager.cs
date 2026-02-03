using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using BepInEx;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class PropTVManager
    {
        private YotogiCamControl plugin;
        private GameObject tvObject;
        private ShitsumuRoomMonitorControl monitorControl;
        private string currentVideoPath = "";
        private Vector3 currentPos = new Vector3(-1.36f, 0.7625f, -2.15f); // Default from script
        private Vector3 currentRot = new Vector3(0f, -38f, 0f); // Default from script
        private string profileName = "tv_default";
        private string statusMsg = "";
        private bool shouldBeSpawned = false;
        private float volume = 1.0f;

        public PropTVManager(YotogiCamControl plugin)
        {
            this.plugin = plugin;
        }

        public void Update()
        {
            if (tvObject != null)
            {
                if (monitorControl == null)
                {
                    monitorControl = tvObject.GetComponentInChildren<ShitsumuRoomMonitorControl>();
                }

                if (tvObject == null || tvObject.Equals(null))
                {
                    tvObject = null;
                    monitorControl = null;
                }
            }

            // Persistence Logic: Respawn if it was supposed to be there but isn't
            if (shouldBeSpawned && tvObject == null)
            {
                // Check if we are in a valid state to spawn (e.g. Yotogi or Room)
                // GameMain.Instance.BgMgr should be present
                if (GameMain.Instance.BgMgr != null)
                {
                    SpawnTV();
                    if (tvObject != null && !string.IsNullOrEmpty(currentVideoPath))
                    {
                        LoadVideo(currentVideoPath);
                    }
                }
            }
        }

        public void DrawUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("TV Prop Control");

            if (tvObject == null)
            {
                if (GUILayout.Button("Spawn TV Prop"))
                {
                    SpawnTV();
                }
            }
            else
            {
                if (GUILayout.Button("Remove TV Prop"))
                {
                    RemoveTV();
                }

                GUILayout.Space(10);
                GUILayout.Label("Transform:");

                GUILayout.Label($"Position: {currentPos}");
                DrawVector3Control("Pos", ref currentPos, -10f, 10f);

                GUILayout.Label($"Rotation: {currentRot}");
                DrawVector3Control("Rot", ref currentRot, -180f, 180f);

                // Apply Transform
                if (tvObject != null)
                {
                    tvObject.transform.position = currentPos;
                    tvObject.transform.rotation = Quaternion.Euler(currentRot);
                }

                GUILayout.Space(10);
                GUILayout.Label("Video:");
                GUILayout.BeginHorizontal();
                currentVideoPath = GUILayout.TextField(currentVideoPath);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    plugin.OpenFileBrowser((path) => {
                        currentVideoPath = path;
                        LoadVideo(currentVideoPath);
                    });
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Load & Play")) LoadVideo(currentVideoPath);
                if (GUILayout.Button("Play")) { if (monitorControl) monitorControl.Play(); }
                if (GUILayout.Button("Pause")) { if (monitorControl) monitorControl.Pause(); }
                if (GUILayout.Button("Stop")) { if (monitorControl) monitorControl.Stop(); }
                GUILayout.EndHorizontal();

                // Volume
                GUILayout.BeginHorizontal();
                GUILayout.Label("Volume", GUILayout.Width(50));
                float newVol = GUILayout.HorizontalSlider(volume, 0f, 1f);
                if (newVol != volume)
                {
                    volume = newVol;
                    ApplyVolume();
                }
                GUILayout.Label(volume.ToString("F2"), GUILayout.Width(35));
                GUILayout.EndHorizontal();

                // Next / Prev Buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<< Prev Video")) PlayNextVideo(false);
                if (GUILayout.Button("Next Video >>")) PlayNextVideo(true);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Profiles (Pos/Rot/VidPath):");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            profileName = GUILayout.TextField(profileName, GUILayout.Width(100));
            if (GUILayout.Button("Save")) SaveProfile(profileName);
            if (GUILayout.Button("Load")) LoadProfile(profileName);
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(statusMsg)) GUILayout.Label(statusMsg);

            GUILayout.EndVertical();
        }

        private void PlayNextVideo(bool next)
        {
            if (string.IsNullOrEmpty(currentVideoPath)) return;

            try
            {
                string dir = Path.GetDirectoryName(currentVideoPath);
                if (!Directory.Exists(dir)) return;

                string[] filters = { ".mp4", ".avi", ".mkv", ".mov", ".webm", ".ine" };
                string[] files = Directory.GetFiles(dir)
                    .Where(f => filters.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f)
                    .ToArray();

                if (files.Length == 0) return;

                int index = Array.IndexOf(files, currentVideoPath);
                if (index == -1) index = 0;
                else
                {
                    if (next) index++; else index--;
                    if (index >= files.Length) index = 0;
                    if (index < 0) index = files.Length - 1;
                }

                currentVideoPath = files[index];
                LoadVideo(currentVideoPath);
            }
            catch (Exception ex)
            {
                statusMsg = "Next/Prev Error: " + ex.Message;
            }
        }

        private void DrawVector3Control(string prefix, ref Vector3 vec, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(prefix + " X", GUILayout.Width(40));
            vec.x = GUILayout.HorizontalSlider(vec.x, min, max);
            string xs = GUILayout.TextField(vec.x.ToString("F3"), GUILayout.Width(50));
            float.TryParse(xs, out vec.x);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(prefix + " Y", GUILayout.Width(40));
            vec.y = GUILayout.HorizontalSlider(vec.y, min, max);
            string ys = GUILayout.TextField(vec.y.ToString("F3"), GUILayout.Width(50));
            float.TryParse(ys, out vec.y);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(prefix + " Z", GUILayout.Width(40));
            vec.z = GUILayout.HorizontalSlider(vec.z, min, max);
            string zs = GUILayout.TextField(vec.z.ToString("F3"), GUILayout.Width(50));
            float.TryParse(zs, out vec.z);
            GUILayout.EndHorizontal();
        }

        private void SpawnTV()
        {
            if (GameMain.Instance.BgMgr == null) return;

            string prefabName = "Odogu_ShinShitsumu_Monitor";
            string name = "YotogiTVProp";

            // AddPrefabToBg(string src, string name, string dest, Vector3 pos, Vector3 rot)
            // dest is optional, passing empty string.
            tvObject = GameMain.Instance.BgMgr.AddPrefabToBg(prefabName, name, "", currentPos, currentRot);

            if (tvObject != null)
            {
                // Ensure rotation is applied correctly as AddPrefabToBg might take Euler angles in vector3
                tvObject.transform.position = currentPos;
                tvObject.transform.rotation = Quaternion.Euler(currentRot);

                monitorControl = tvObject.GetComponentInChildren<ShitsumuRoomMonitorControl>();
                if (monitorControl == null)
                {
                    statusMsg = "Warning: Monitor Control script not found on prefab.";
                }
                else
                {
                    statusMsg = "Spawned TV Prop.";
                    shouldBeSpawned = true;
                }
                ApplyVolume();
            }
            else
            {
                statusMsg = "Failed to spawn prefab.";
            }
        }

        private void RemoveTV()
        {
            if (GameMain.Instance.BgMgr == null) return;

            // DelPrefabFromBg takes the 'name' assigned during creation
            GameMain.Instance.BgMgr.DelPrefabFromBg("YotogiTVProp");
            tvObject = null;
            monitorControl = null;
            shouldBeSpawned = false;
            statusMsg = "Removed TV Prop.";
        }

        private void LoadVideo(string path)
        {
            if (monitorControl != null && !string.IsNullOrEmpty(path))
            {
                bool success = monitorControl.LoadMovie(path, true);
                statusMsg = success ? "Loaded video." : "Failed to load video.";
                ApplyVolume();
            }
        }

        private void ApplyVolume()
        {
            if (tvObject != null)
            {
                // Try AudioSource on root
                AudioSource audio = tvObject.GetComponent<AudioSource>();
                if (audio != null) audio.volume = volume;

                // Try AudioSource in children
                AudioSource[] audios = tvObject.GetComponentsInChildren<AudioSource>();
                foreach(var a in audios) a.volume = volume;
            }
        }

        private void SaveProfile(string name)
        {
            try
            {
                string path = Path.Combine(Paths.ConfigPath, "YotogiCamControl_TVProfiles");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                TVProfile profile = new TVProfile
                {
                    posX = currentPos.x,
                    posY = currentPos.y,
                    posZ = currentPos.z,
                    rotX = currentRot.x,
                    rotY = currentRot.y,
                    rotZ = currentRot.z,
                    videoPath = currentVideoPath
                };

                string filePath = Path.Combine(path, name + ".xml");
                XmlSerializer serializer = new XmlSerializer(typeof(TVProfile));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, profile);
                }
                statusMsg = "Saved: " + name;
            }
            catch (Exception ex)
            {
                statusMsg = "Save Error: " + ex.Message;
                Debug.LogError(ex);
            }
        }

        private void LoadProfile(string name)
        {
            try
            {
                string path = Path.Combine(Path.Combine(Paths.ConfigPath, "YotogiCamControl_TVProfiles"), name + ".xml");
                if (!File.Exists(path))
                {
                    statusMsg = "Profile not found.";
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(TVProfile));
                TVProfile profile;
                using (StreamReader reader = new StreamReader(path))
                {
                    profile = (TVProfile)serializer.Deserialize(reader);
                }

                currentPos = new Vector3(profile.posX, profile.posY, profile.posZ);
                currentRot = new Vector3(profile.rotX, profile.rotY, profile.rotZ);
                currentVideoPath = profile.videoPath;

                // Apply if spawned
                if (tvObject != null)
                {
                    tvObject.transform.position = currentPos;
                    tvObject.transform.rotation = Quaternion.Euler(currentRot);
                    if (!string.IsNullOrEmpty(currentVideoPath)) LoadVideo(currentVideoPath);
                }
                else
                {
                    SpawnTV();
                    if (!string.IsNullOrEmpty(currentVideoPath)) LoadVideo(currentVideoPath);
                }

                statusMsg = "Loaded: " + name;
            }
            catch (Exception ex)
            {
                statusMsg = "Load Error: " + ex.Message;
                Debug.LogError(ex);
            }
        }

        [Serializable]
        public class TVProfile
        {
            public float posX, posY, posZ;
            public float rotX, rotY, rotZ;
            public string videoPath;
        }
    }
}
