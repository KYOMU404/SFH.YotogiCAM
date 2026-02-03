using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using BepInEx;
using COM3D2.YotogiCamControl.Plugin.UI;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class VideoCamManager
    {
        private YotogiCamControl plugin;
        private GameObject camObject;
        private Vector3 currentPos = new Vector3(0f, 1.5f, 0.5f);
        private Vector3 currentRot = new Vector3(0f, 180f, 0f);
        private string profileName = "videocam_default";
        private string statusMsg = "";
        private bool shouldBeSpawned = false;

        private string foundPrefabName = "";
        private bool isAssetBundle = false;
        private string assetBundleName = "";

        public VideoCamManager(YotogiCamControl plugin)
        {
            this.plugin = plugin;
        }

        public void Update()
        {
            // Persistence Logic: Respawn if it was supposed to be there but isn't
            if (shouldBeSpawned && camObject == null)
            {
                if (GameMain.Instance != null && GameMain.Instance.BgMgr != null)
                {
                    SpawnCam();
                }
            }
        }

        public void DrawUI()
        {
            GUILayout.BeginVertical(StyleManager.BoxStyle);
            GUILayout.Label("Video Cam Control");

            if (camObject == null)
            {
                if (GUILayout.Button("Spawn Video Cam"))
                {
                    FindAndSpawnCam();
                }
            }
            else
            {
                if (GUILayout.Button("Remove Video Cam"))
                {
                    RemoveCam();
                }

                GUILayout.Space(10);
                GUILayout.Label("Transform:");

                GUILayout.Label($"Position: {currentPos}");
                DrawVector3Control("Pos", ref currentPos, -10f, 10f);

                GUILayout.Label($"Rotation: {currentRot}");
                DrawVector3Control("Rot", ref currentRot, -180f, 180f);

                // Apply Transform
                if (camObject != null)
                {
                    camObject.transform.position = currentPos;
                    camObject.transform.rotation = Quaternion.Euler(currentRot);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Profiles:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            profileName = GUILayout.TextField(profileName, GUILayout.Width(100));
            if (GUILayout.Button("Save")) SaveProfile(profileName);
            if (GUILayout.Button("Load")) LoadProfile(profileName);
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(statusMsg)) GUILayout.Label(statusMsg);

            GUILayout.EndVertical();
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

        private void FindAndSpawnCam()
        {
            if (GameMain.Instance.BgMgr == null) return;

            // Search if we haven't found it yet or forced retry
            try
            {
                PhotoBGObjectData.Create(); // Ensure data is loaded
                if (PhotoBGObjectData.data != null)
                {
                    var targetItem = PhotoBGObjectData.data.FirstOrDefault(d => d.name == "ハメ撮りカメラ");
                    if (targetItem != null)
                    {
                        foundPrefabName = targetItem.create_prefab_name;
                        assetBundleName = targetItem.create_asset_bundle_name;
                        isAssetBundle = !string.IsNullOrEmpty(assetBundleName);

                        // If prefab name is empty but asset bundle exists, we might need special handling
                        // If both empty, it's a problem.
                    }
                    else
                    {
                        Debug.LogWarning("YotogiCamControl: 'ハメ撮りカメラ' not found in PhotoBGObjectData. Trying default 'Odogu_VideoCam'.");
                        foundPrefabName = "Odogu_VideoCam";
                        isAssetBundle = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("YotogiCamControl: Error searching PhotoBGObjectData: " + ex.Message);
                foundPrefabName = "Odogu_VideoCam"; // Fallback
            }

            SpawnCam();
        }

        private void SpawnCam()
        {
            if (GameMain.Instance.BgMgr == null) return;
            if (string.IsNullOrEmpty(foundPrefabName) && string.IsNullOrEmpty(assetBundleName))
            {
                statusMsg = "No prefab found to spawn.";
                return;
            }

            string spawnName = "YotogiVideoCamProp";

            try
            {
                if (isAssetBundle && !string.IsNullOrEmpty(assetBundleName))
                {
                    // Logic derived from PhotoBGObjectData.Instantiate:
                    // GameObject gameObject3 = GameMain.Instance.BgMgr.CreateAssetBundle(create_asset_bundle_name);
                    GameObject bundleObj = GameMain.Instance.BgMgr.CreateAssetBundle(assetBundleName);
                    if (bundleObj != null)
                    {
                        camObject = UnityEngine.Object.Instantiate(bundleObj);
                        if (camObject != null)
                        {
                             camObject.name = spawnName;
                             // We need to manage it. AddPrefabToBg adds to a list and manages it.
                             // Instantiate just creates it. We should try to see if AddPrefabToBg can handle asset bundles or if we just manually manage it.
                             // AddPrefabToBg signature: (string src, string name, string dest, Vector3 pos, Vector3 rot)
                             // It loads from Resources usually.
                             // Since we manually instantiated, we just set transform.
                        }
                    }
                    else
                    {
                        statusMsg = "Failed to load AssetBundle: " + assetBundleName;
                        return;
                    }
                }
                else
                {
                    // Resource based
                    // AddPrefabToBg loads from "Prefab/" + src usually
                    camObject = GameMain.Instance.BgMgr.AddPrefabToBg(foundPrefabName, spawnName, "", currentPos, currentRot);
                }

                if (camObject != null)
                {
                    camObject.transform.position = currentPos;
                    camObject.transform.rotation = Quaternion.Euler(currentRot);

                    // Add Collider if missing (logic from PhotoBGObjectData.Instantiate)
                    if (camObject.GetComponentInChildren<BoxCollider>() == null)
                    {
                         MeshRenderer renderer = camObject.GetComponentInChildren<MeshRenderer>(true);
                         if (renderer != null)
                         {
                             renderer.gameObject.AddComponent<BoxCollider>();
                         }
                    }

                    shouldBeSpawned = true;
                    statusMsg = "Spawned Video Cam.";
                }
                else
                {
                    statusMsg = "Failed to spawn prefab: " + foundPrefabName;
                }
            }
            catch (Exception ex)
            {
                statusMsg = "Error spawning: " + ex.Message;
                Debug.LogError("VideoCamManager: " + ex.ToString());
            }
        }

        private void RemoveCam()
        {
            if (GameMain.Instance.BgMgr == null) return;

            // If we used AddPrefabToBg
            GameMain.Instance.BgMgr.DelPrefabFromBg("YotogiVideoCamProp");

            // If we manually instantiated (AssetBundle path), DelPrefabFromBg might not catch it if not added to internal lists.
            if (camObject != null)
            {
                UnityEngine.Object.Destroy(camObject);
                camObject = null;
            }

            shouldBeSpawned = false;
            statusMsg = "Removed Video Cam.";
        }

        private void SaveProfile(string name)
        {
            try
            {
                string path = Path.Combine(Paths.ConfigPath, "YotogiCamControl_VideoCamProfiles");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                VideoCamProfile profile = new VideoCamProfile
                {
                    posX = currentPos.x,
                    posY = currentPos.y,
                    posZ = currentPos.z,
                    rotX = currentRot.x,
                    rotY = currentRot.y,
                    rotZ = currentRot.z
                };

                string filePath = Path.Combine(path, name + ".xml");
                XmlSerializer serializer = new XmlSerializer(typeof(VideoCamProfile));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, profile);
                }
                statusMsg = "Saved: " + name;
                NotificationManager.Show($"Saved: {name}", 2f, Color.green);
            }
            catch (Exception ex)
            {
                statusMsg = "Save Error: " + ex.Message;
                Debug.LogError(ex);
                NotificationManager.Show($"Error: {ex.Message}", 4f, Color.red);
            }
        }

        private void LoadProfile(string name)
        {
            try
            {
                string path = Path.Combine(Path.Combine(Paths.ConfigPath, "YotogiCamControl_VideoCamProfiles"), name + ".xml");
                if (!File.Exists(path))
                {
                    statusMsg = "Profile not found.";
                    NotificationManager.Show("Profile not found.", 3f, Color.yellow);
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(VideoCamProfile));
                VideoCamProfile profile;
                using (StreamReader reader = new StreamReader(path))
                {
                    profile = (VideoCamProfile)serializer.Deserialize(reader);
                }

                currentPos = new Vector3(profile.posX, profile.posY, profile.posZ);
                currentRot = new Vector3(profile.rotX, profile.rotY, profile.rotZ);

                // Apply if spawned
                if (camObject != null)
                {
                    camObject.transform.position = currentPos;
                    camObject.transform.rotation = Quaternion.Euler(currentRot);
                }
                else
                {
                    FindAndSpawnCam();
                }

                statusMsg = "Loaded: " + name;
                NotificationManager.Show($"Loaded: {name}", 2f, Color.green);
            }
            catch (Exception ex)
            {
                statusMsg = "Load Error: " + ex.Message;
                Debug.LogError(ex);
                NotificationManager.Show($"Error: {ex.Message}", 4f, Color.red);
            }
        }

        [Serializable]
        public class VideoCamProfile
        {
            public float posX, posY, posZ;
            public float rotX, rotY, rotZ;
        }
    }
}
