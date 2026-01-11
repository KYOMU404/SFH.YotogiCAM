using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using System.Reflection;
using RenderHeads.Media.AVProVideo;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

namespace COM3D2.YotogiCamControl.Plugin
{
    [BepInPlugin("com.stay.fucking.hard", "YotogiCamControl", "1.7.0")]
    public class YotogiCamControl : BaseUnityPlugin
    {
        private bool isGuiActive = false;
        private Rect windowRect = new Rect(20, 20, 450, 700);
        private int windowId = 9999;
        private Vector2 scrollPosition = Vector2.zero;

        // UI State
        private int activeTab = 0;
        private string[] tabNames = { "Scene", "Screens", "Faces", "Kiss", "Video", "Prop TV", "Swap", "LookAt", "Misc" };
        private bool showLighting = true;
        private bool showPostFX = true;
        
        // Screens UI State
        private int selectedUIIndex = 0; 
        private bool showFaceCam = true;
        private bool showChestCam = false;
        private bool showPelvisCam = false;
        
        private bool showVideoControl = true;

        // Profile State
        private string profileNameInput = "default";
        private string profileStatusMsg = "";

        // Lighting
        private float lightIntensity = 0.95f;
        private Color lightColor = Color.white;
        private float shadowStrength = 0.098f;

        // Post Processing
        private float bloomValue = 0f;
        private float saturation = 1f;
        private float rgbRed = 1f;
        private float rgbGreen = 1f;
        private float rgbBlue = 1f;
        
        // Animation Speed
        private float animationSpeed = 100f;
        private bool syncAnimationSpeed = true;

        // Reflection for ColorCorrectionCurves
        private MonoBehaviour colorCorrection;
        private Type colorCorrectionType;
        private PropertyInfo saturationProp;
        private FieldInfo redChannelField, greenChannelField, blueChannelField;
        private MethodInfo updateTexturesMethod;

        // Screens
        private bool showScreens = true;
        private const float minScreenSize = 100f;
        private const float maxScreenSize = 600f;

        // Auto Kiss & Drool
        private KissManager kissManager;
        private FaceManager faceManager;
        private BodyReactionManager bodyReactionManager;
        
        // Prop TV
        private PropTVManager propTVManager;

        // Look At Enums
        public enum LookAtType
        {
            None,
            MainCamera,
            FaceScreen,
            ChestScreen,
            PelvisScreen,
            Master,
            OtherMaid
        }

        // Camera Settings Class
        [Serializable]
        public class CamSetting
        {
            public float fov = 45f;
            public float distance = 0.5f;
            public Vector3 offset = Vector3.zero;
            public float rotationX = 0f; // Yaw
            public float rotationY = 0f; // Pitch
            public bool invert = false;
        }

        private List<MaidScreenSet> maidScreens = new List<MaidScreenSet>();
        private VideoScreen videoScreen;

        // Video Settings
        private string videoPathInput = "";
        private bool showVideoScreen = true;
        private float videoScreenSize = 300f; 

        // File Browser State
        private bool showFileBrowser = false;
        private Rect fileBrowserRect = new Rect(450, 20, 400, 500);
        private InGameFileBrowser fileBrowser;
        private Action<string> onFileSelectedCallback;

        // Maid Swap Settings
        private int swappedIndex = -1;

        public void Awake()
        {
            videoScreen = new VideoScreen();
            videoScreen.Initialize();
            
            fileBrowser = new InGameFileBrowser();
            fileBrowser.Initialize();

            kissManager = new KissManager();
            faceManager = new FaceManager();
            bodyReactionManager = new BodyReactionManager();
            propTVManager = new PropTVManager(this);
        }

        public void OnDestroy()
        {
            foreach (var set in maidScreens)
            {
                set.Destroy();
            }
            maidScreens.Clear();
            videoScreen?.Destroy();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                isGuiActive = !isGuiActive;
                if (!isGuiActive) showFileBrowser = false; 
            }

            if (isGuiActive || showScreens)
            {
                UpdateScreens();
            }

            if (isGuiActive)
            {
                UpdateLighting();
                UpdatePostProcessing();
            }

            UpdateLookAt();
            
            // Animation Speed
            if (syncAnimationSpeed && GameMain.Instance != null && GameMain.Instance.CharacterMgr != null)
            {
                CharacterMgr charMgr = GameMain.Instance.CharacterMgr;
                for (int i = 0; i < charMgr.GetMaidCount(); i++)
                {
                    Maid m = charMgr.GetMaid(i);
                    if (m != null && m.Visible && m.body0 != null)
                    {
                        var anim = m.body0.GetAnimation();
                        if (anim != null && m.body0.LastAnimeFN != null && anim[m.body0.LastAnimeFN] != null)
                        {
                            anim[m.body0.LastAnimeFN].speed = animationSpeed / 100f;
                        }
                    }
                }
            }

            kissManager.Update();
            bodyReactionManager.Update();
            propTVManager.Update();
        }

        public void OnGUI()
        {
            if (isGuiActive)
            {
                windowRect = GUI.Window(windowId, windowRect, DrawWindow, "Yotogi Cam Control");
                
                if (showFileBrowser)
                {
                    fileBrowserRect = GUI.Window(windowId + 100, fileBrowserRect, DrawFileBrowserWindow, "File Browser");
                }
            }

            if (showScreens)
            {
                DrawScreens();
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            
            activeTab = GUILayout.Toolbar(activeTab, tabNames);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            switch (activeTab)
            {
                case 0: DrawSceneTab(); break;
                case 1: DrawScreensTab(); break;
                case 2: faceManager.DrawUI(); break;
                case 3: kissManager.DrawUI(); break;
                case 4: DrawVideoTab(); break;
                case 5: propTVManager.DrawUI(); break;
                case 6: DrawSwapTab(); break;
                case 7: DrawLookAtTab(); break;
                case 8: DrawMiscTab(); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawFileBrowserWindow(int id)
        {
            fileBrowser.Draw(fileBrowserRect, (path) => {
                if (onFileSelectedCallback != null)
                {
                    onFileSelectedCallback(path);
                }
                else 
                {
                    // Default fallback
                    videoPathInput = path;
                    videoScreen.Load(videoPathInput);
                }
                showFileBrowser = false; 
                onFileSelectedCallback = null;
            }, () => {
                showFileBrowser = false; 
                onFileSelectedCallback = null;
            });
            GUI.DragWindow();
        }
        
        public void OpenFileBrowser(Action<string> onSelected)
        {
            onFileSelectedCallback = onSelected;
            showFileBrowser = true;
        }

        private void DrawMiscTab()
        {
            GUILayout.BeginVertical("box");
            bodyReactionManager.DrawUI();
            GUILayout.EndVertical();
        }

        private void DrawSceneTab()
        {
            if (DrawHeader("Lighting", ref showLighting))
            {
                GUILayout.BeginVertical("box");
                DrawSliderWithReset("Intensity", ref lightIntensity, 0f, 3f, 0.95f);
                DrawSliderWithReset("Shadow", ref shadowStrength, 0f, 1f, 0.098f);

                GUILayout.Label("Light Color");
                GUILayout.BeginHorizontal();
                GUILayout.Label("R", GUILayout.Width(15));
                lightColor.r = GUILayout.HorizontalSlider(lightColor.r, 0f, 1f);
                if (GUILayout.Button("R", GUILayout.Width(20))) lightColor.r = 1f;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("G", GUILayout.Width(15));
                lightColor.g = GUILayout.HorizontalSlider(lightColor.g, 0f, 1f);
                if (GUILayout.Button("R", GUILayout.Width(20))) lightColor.g = 1f;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("B", GUILayout.Width(15));
                lightColor.b = GUILayout.HorizontalSlider(lightColor.b, 0f, 1f);
                if (GUILayout.Button("R", GUILayout.Width(20))) lightColor.b = 1f;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            if (DrawHeader("Post Processing", ref showPostFX))
            {
                GUILayout.BeginVertical("box");
                DrawSliderWithReset("Bloom", ref bloomValue, 0f, 100f, 0f, "F0");
                DrawSliderWithReset("Saturation", ref saturation, 0f, 5f, 1f);
                
                GUILayout.Label("Color Grading");
                DrawSliderWithReset("Red", ref rgbRed, 0f, 5f, 1f);
                DrawSliderWithReset("Green", ref rgbGreen, 0f, 5f, 1f);
                DrawSliderWithReset("Blue", ref rgbBlue, 0f, 5f, 1f);
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Animation");
            DrawSliderWithReset("Speed %", ref animationSpeed, 0f, 500f, 100f, "F0");
            syncAnimationSpeed = GUILayout.Toggle(syncAnimationSpeed, "Sync Animation Speed");
            GUILayout.EndVertical();
        }

        private void DrawScreensTab()
        {
            GUILayout.BeginVertical("box");
            showScreens = GUILayout.Toggle(showScreens, "Show Screens Enabled");
            
            GUILayout.Space(5);
            GUILayout.Label("Profile Management:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            profileNameInput = GUILayout.TextField(profileNameInput, GUILayout.Width(100));
            if (GUILayout.Button("Save")) SaveProfile(profileNameInput);
            if (GUILayout.Button("Load")) LoadProfile(profileNameInput);
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(profileStatusMsg)) GUILayout.Label(profileStatusMsg);
            
            GUILayout.EndVertical();

            GUILayout.Space(5);

            List<int> activeMaidIndices = GetActiveMaidIndices();

            if (activeMaidIndices.Count > 0)
            {
                string[] maidNames = activeMaidIndices.Select(i => "Maid " + i).ToArray();
                GUILayout.Label("Select Maid Controls:");
                
                if (selectedUIIndex >= activeMaidIndices.Count) selectedUIIndex = 0;
                
                selectedUIIndex = GUILayout.Toolbar(selectedUIIndex, maidNames);

                if (selectedUIIndex >= 0 && selectedUIIndex < activeMaidIndices.Count)
                {
                    int actualMaidIndex = activeMaidIndices[selectedUIIndex];
                    MaidScreenSet currentSet = maidScreens[actualMaidIndex];

                    if (DrawHeader($"Face Camera (Maid {actualMaidIndex})", ref showFaceCam))
                    {
                        GUILayout.BeginVertical("box");
                        currentSet.faceEnabled = GUILayout.Toggle(currentSet.faceEnabled, "Enable Screen");
                        if (currentSet.faceEnabled)
                        {
                            DrawSliderWithReset("Screen Size", ref currentSet.faceSize, minScreenSize, maxScreenSize, 200f);
                            DrawCamSettingControls(currentSet.faceSet, 30f, 0.35f);
                        }
                        GUILayout.EndVertical();
                    }

                    if (DrawHeader($"Chest Camera (Maid {actualMaidIndex})", ref showChestCam))
                    {
                        GUILayout.BeginVertical("box");
                        currentSet.chestEnabled = GUILayout.Toggle(currentSet.chestEnabled, "Enable Screen");
                        if (currentSet.chestEnabled)
                        {
                            DrawSliderWithReset("Screen Size", ref currentSet.chestSize, minScreenSize, maxScreenSize, 200f);
                            DrawCamSettingControls(currentSet.chestSet, 45f, 0.4f);
                        }
                        GUILayout.EndVertical();
                    }

                    if (DrawHeader($"Pelvis Camera (Maid {actualMaidIndex})", ref showPelvisCam))
                    {
                        GUILayout.BeginVertical("box");
                        currentSet.pelvisEnabled = GUILayout.Toggle(currentSet.pelvisEnabled, "Enable Screen");
                        if (currentSet.pelvisEnabled)
                        {
                            DrawSliderWithReset("Screen Size", ref currentSet.pelvisSize, minScreenSize, maxScreenSize, 200f);
                            DrawCamSettingControls(currentSet.pelvisSet, 45f, 0.4f);
                        }
                        GUILayout.EndVertical();
                    }
                }
            }
            else
            {
                GUILayout.Label("No Maids Detected.");
            }
        }

        private void SaveProfile(string name)
        {
            try 
            {
                string path = Path.Combine(Paths.ConfigPath, "YotogiCamControl_Profiles");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                
                List<MaidProfileData> dataList = new List<MaidProfileData>();
                for(int i=0; i<maidScreens.Count; i++)
                {
                    dataList.Add(maidScreens[i].ToProfileData());
                }
                
                ProfileContainer container = new ProfileContainer { profiles = dataList };
                string filePath = Path.Combine(path, name + ".xml");

                XmlSerializer serializer = new XmlSerializer(typeof(ProfileContainer));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, container);
                }

                Debug.Log($"[YotogiCamControl] Saved Profile '{name}' to {filePath}");
                profileStatusMsg = "Saved: " + name;
            }
            catch(Exception e) 
            { 
                profileStatusMsg = "Error: " + e.Message; 
                Debug.LogError("[YotogiCamControl] Save Error: " + e.ToString());
            }
        }

        private void LoadProfile(string name)
        {
            try 
            {
                string path = Path.Combine(Path.Combine(Paths.ConfigPath, "YotogiCamControl_Profiles"), name + ".xml");
                if (!File.Exists(path)) 
                {
                    profileStatusMsg = "Not Found: " + name;
                    Debug.LogWarning($"[YotogiCamControl] Profile not found at: {path}");
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(ProfileContainer));
                ProfileContainer container;
                
                using (StreamReader reader = new StreamReader(path))
                {
                    container = (ProfileContainer)serializer.Deserialize(reader);
                }
                
                if (container != null && container.profiles != null)
                {
                    Debug.Log($"[YotogiCamControl] Loaded {container.profiles.Count} profiles.");
                    
                    while (maidScreens.Count < container.profiles.Count) maidScreens.Add(new MaidScreenSet());
                    
                    for(int i=0; i<container.profiles.Count && i < maidScreens.Count; i++)
                    {
                        maidScreens[i].ApplyProfileData(container.profiles[i]);
                    }
                    profileStatusMsg = "Loaded: " + name;
                }
                else
                {
                    profileStatusMsg = "Error: Invalid XML";
                    Debug.LogError("[YotogiCamControl] Failed to parse profile container.");
                }
            }
            catch(Exception e) 
            { 
                profileStatusMsg = "Error: " + e.Message; 
                Debug.LogError("[YotogiCamControl] Load Error: " + e.ToString());
            }
        }

        [Serializable]
        public class ProfileContainer
        {
            public List<MaidProfileData> profiles;
        }

        [Serializable]
        public class MaidProfileData
        {
            public bool faceEnabled, chestEnabled, pelvisEnabled;
            public float faceSize, chestSize, pelvisSize;
            public CamSetting faceSet, chestSet, pelvisSet;
        }

        private void DrawLookAtTab()
        {
            List<int> activeMaidIndices = GetActiveMaidIndices();

            if (activeMaidIndices.Count > 0)
            {
                GUILayout.Label("Select Maid to Control:");
                
                if (selectedUIIndex >= activeMaidIndices.Count) selectedUIIndex = 0;
                selectedUIIndex = GUILayout.Toolbar(selectedUIIndex, activeMaidIndices.Select(i => "Maid " + i).ToArray());

                if (selectedUIIndex >= 0 && selectedUIIndex < activeMaidIndices.Count)
                {
                    int actualMaidIndex = activeMaidIndices[selectedUIIndex];
                    MaidScreenSet currentSet = maidScreens[actualMaidIndex];
                    
                    GUILayout.BeginVertical("box");
                    GUILayout.Label($"Target for Maid {actualMaidIndex}:");

                    bool isLocked = currentSet.lookAtType != LookAtType.None;

                    void DrawLookToggle(string label, LookAtType type, int targetIndex = -1)
                    {
                        bool isActive = currentSet.lookAtType == type && currentSet.lookAtTargetIndex == targetIndex;
                        bool isDisabled = isLocked && !isActive;

                        GUI.enabled = !isDisabled;
                        if (GUILayout.Toggle(isActive, label, "button"))
                        {
                            if (!isActive)
                            {
                                currentSet.lookAtType = type;
                                currentSet.lookAtTargetIndex = targetIndex;
                            }
                        }
                        else if (isActive)
                        {
                            currentSet.lookAtType = LookAtType.None;
                            currentSet.lookAtTargetIndex = -1;
                        }
                        GUI.enabled = true;
                    }

                    DrawLookToggle("Main Camera", LookAtType.MainCamera);
                    DrawLookToggle("Face Monitor", LookAtType.FaceScreen);
                    DrawLookToggle("Chest Monitor", LookAtType.ChestScreen);
                    DrawLookToggle("Pelvis Monitor", LookAtType.PelvisScreen);
                    DrawLookToggle("Master", LookAtType.Master);

                    GUILayout.Space(5);
                    GUILayout.Label("Other Maids:");
                    foreach(int otherIndex in activeMaidIndices)
                    {
                        if (otherIndex == actualMaidIndex) continue;
                        DrawLookToggle($"Maid {otherIndex}", LookAtType.OtherMaid, otherIndex);
                    }

                    GUILayout.EndVertical();
                }
            }
            else
            {
                GUILayout.Label("No Maids Detected.");
            }
        }

        private void UpdateLookAt()
        {
            if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;

            for (int i = 0; i < maidScreens.Count; i++)
            {
                MaidScreenSet set = maidScreens[i];
                if (!set.IsActive || set.lookAtType == LookAtType.None) continue;

                Maid maid = GameMain.Instance.CharacterMgr.GetMaid(i);
                if (maid == null || maid.body0 == null) continue;

                Transform target = null;

                switch (set.lookAtType)
                {
                    case LookAtType.MainCamera:
                        if (GameMain.Instance.MainCamera != null) 
                            target = GameMain.Instance.MainCamera.transform;
                        break;
                    case LookAtType.FaceScreen:
                        if (set.faceCam != null && set.faceCam.obj != null) 
                            target = set.faceCam.obj.transform;
                        break;
                    case LookAtType.ChestScreen:
                        if (set.chestCam != null && set.chestCam.obj != null) 
                            target = set.chestCam.obj.transform;
                        break;
                    case LookAtType.PelvisScreen:
                        if (set.pelvisCam != null && set.pelvisCam.obj != null) 
                            target = set.pelvisCam.obj.transform;
                        break;
                    case LookAtType.Master:
                        Maid man = GameMain.Instance.CharacterMgr.GetMan(0);
                        if (man != null && man.body0 != null) 
                            target = man.body0.trsHead;
                        break;
                    case LookAtType.OtherMaid:
                        Maid other = GameMain.Instance.CharacterMgr.GetMaid(set.lookAtTargetIndex);
                        if (other != null && other.body0 != null) 
                            target = other.body0.trsHead;
                        break;
                }

                if (target != null)
                {
                    maid.body0.trsLookTarget = target;
                    maid.body0.boHeadToCam = true;
                    maid.body0.boEyeToCam = true;
                }
            }
        }

        private List<int> GetActiveMaidIndices()
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < maidScreens.Count; i++)
            {
                if (maidScreens[i].IsActive) indices.Add(i);
            }
            return indices;
        }

        private void DrawVideoTab()
        {
            if (DrawHeader("Video Player", ref showVideoControl))
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("Video File Path:");
                
                GUILayout.BeginHorizontal();
                videoPathInput = GUILayout.TextField(videoPathInput);
                if (GUILayout.Button(showFileBrowser ? "Close Browser" : "Browse", GUILayout.Width(100)))
                {
                    OpenFileBrowser((path) => {
                        videoPathInput = path;
                        videoScreen.Load(videoPathInput);
                    });
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Load"))
                {
                    videoScreen.Load(videoPathInput);
                }
                if (GUILayout.Button("Close"))
                {
                    videoScreen.Close();
                }
                GUILayout.EndHorizontal();

                if (videoScreen.IsLoaded())
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Controls:");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(videoScreen.IsPlaying() ? "Pause" : "Play"))
                    {
                        if (videoScreen.IsPlaying()) videoScreen.Pause(); else videoScreen.Play();
                    }
                    if (GUILayout.Button("Rewind"))
                    {
                        videoScreen.Rewind();
                    }
                    GUILayout.EndHorizontal();

                    bool loop = videoScreen.GetLooping();
                    if (GUILayout.Toggle(loop, "Loop") != loop)
                    {
                        videoScreen.SetLooping(!loop);
                    }

                    float vol = videoScreen.GetVolume();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Volume", GUILayout.Width(50));
                    float newVol = GUILayout.HorizontalSlider(vol, 0f, 1f);
                    GUILayout.EndHorizontal();
                    if (newVol != vol) videoScreen.SetVolume(newVol);
                }

                GUILayout.Space(5);
                showVideoScreen = GUILayout.Toggle(showVideoScreen, "Show Video Screen");
                if (showVideoScreen)
                {
                    DrawSliderWithReset("Video Screen Size", ref videoScreenSize, minScreenSize, maxScreenSize * 1.5f, 300f);
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawSwapTab()
        {
            if (YotogiManager.instans == null)
            {
                GUILayout.Label("Not in Yotogi Mode");
                return;
            }

            GUILayout.BeginVertical("box");
            if (swappedIndex != -1)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Status: SWAPPED");
                GUI.color = Color.white;
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("<< REVERT TO ORIGINAL >>", GUILayout.Height(30))) RevertMaids();
                GUI.backgroundColor = Color.white;
                GUILayout.Space(10);
            }
            else
            {
                GUILayout.Label("Status: Original");
                GUILayout.Space(10);
            }

            GUILayout.Label("Select Target Maid to Swap with Main:");

            CharacterMgr cm = GameMain.Instance.CharacterMgr;
            int count = cm.GetMaidCount();
            
            for (int i = 0; i < count; i++)
            {
                Maid m = cm.GetMaid(i);
                if (m != null)
                {
                    string name = m.status.fullNameJpStyle;
                    
                    string extra = "";
                    if (swappedIndex == -1 && i == 0) extra = " [MAIN]";
                    if (swappedIndex != -1 && i == 0) extra = " [CURRENT]";
                    if (i == swappedIndex) extra = " [ORIGIN]";

                    if (i == 0) GUI.enabled = false;
                    
                    if (GUILayout.Button($"{name} (Slot {i}){extra}"))
                    {
                        SwapMaids(i);
                    }
                    
                    if (i == 0) GUI.enabled = true;
                }
            }
            GUILayout.EndVertical();
        }

        private void SwapMaids(int targetIndex)
        {
            if (swappedIndex != -1) RevertMaids();
            PerformSwap(targetIndex);
            swappedIndex = targetIndex;
        }

        private void RevertMaids()
        {
            if (swappedIndex == -1) return;
            PerformSwap(swappedIndex);
            swappedIndex = -1;
        }

        private void PerformSwap(int index)
        {
            CharacterMgr cm = GameMain.Instance.CharacterMgr;
            Maid oldMaid = cm.GetMaid(0);
            Maid newMaid = cm.GetMaid(index);

            if (oldMaid == null || newMaid == null) return;

            try 
            {
                Vector3 tempPos = oldMaid.gameObject.transform.position;
                Quaternion tempRot = oldMaid.gameObject.transform.rotation;

                oldMaid.gameObject.transform.position = newMaid.gameObject.transform.position;
                oldMaid.gameObject.transform.rotation = newMaid.gameObject.transform.rotation;

                newMaid.gameObject.transform.position = tempPos;
                newMaid.gameObject.transform.rotation = tempRot;

                cm.SwapActiveSlot(0, index, false);

                UpdateDeepReferences(oldMaid, newMaid);
                UpdateParticles(newMaid, oldMaid);
                RefreshManagers(newMaid);
                UpdateCamera(newMaid);
            }
            catch (Exception ex)
            {
                Debug.LogError("YotogiCamControl Swap Error: " + ex.ToString());
            }
        }

        private void UpdateDeepReferences(Maid oldMaid, Maid newMaid)
        {
            FieldInfo ymMaidField = GetField(typeof(YotogiManager), "maid_");
            if (ymMaidField != null) ymMaidField.SetValue(YotogiManager.instans, newMaid);

            FieldInfo playSkillArrField = GetField(typeof(YotogiManager), "play_skill_array_");
            if (playSkillArrField != null)
            {
                object arrObj = playSkillArrField.GetValue(YotogiManager.instans);
                if (arrObj is object[] arr)
                {
                    foreach (object playingSkillData in arr)
                    {
                        if (playingSkillData == null) continue;
                        FieldInfo skillPairField = GetField(playingSkillData.GetType(), "skill_pair");
                        if (skillPairField != null)
                        {
                            object skillPair = skillPairField.GetValue(playingSkillData);
                            if (skillPair != null)
                            {
                                FieldInfo spMaidField = GetField(skillPair.GetType(), "maid");
                                if (spMaidField != null)
                                {
                                    Maid currentPairMaid = (Maid)spMaidField.GetValue(skillPair);
                                    if (currentPairMaid == oldMaid)
                                    {
                                        spMaidField.SetValue(skillPair, newMaid);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            YotogiPlayManager pm = YotogiManager.instans.play_mgr;
            if (pm != null)
            {
                FieldInfo pmMaidField = GetField(typeof(YotogiPlayManager), "maid_");
                if (pmMaidField != null) pmMaidField.SetValue(pm, newMaid);

                FieldInfo pmReplaceArrField = GetField(typeof(YotogiPlayManager), "replace_personal_target_maid_array_");
                if (pmReplaceArrField != null)
                {
                    Maid[] repArr = (Maid[])pmReplaceArrField.GetValue(pm);
                    if (repArr != null && repArr.Length > 0)
                    {
                        if (repArr[0] == oldMaid) repArr[0] = newMaid;
                    }
                }
            }

            UndressingManager um = GetField(typeof(YotogiPlayManager), "undressing_mgr_")?.GetValue(pm) as UndressingManager;
            if (um != null)
            {
                List<Maid> subMaids = new List<Maid>();
                CharacterMgr cm = GameMain.Instance.CharacterMgr;
                for (int i = 1; i < cm.GetMaidCount(); i++)
                {
                    Maid m = cm.GetMaid(i);
                    if (m != null && m.Visible) subMaids.Add(m);
                }
                
                um.SetMaid(newMaid, subMaids.ToArray());
            }
        }

        private void UpdateParticles(Maid newMaid, Maid oldMaid)
        {
            YotogiPlayManager pm = YotogiManager.instans.play_mgr;
            if (pm == null) return;

            List<GameObject> newBreatheList = new List<GameObject>();
            CharacterMgr cm = GameMain.Instance.CharacterMgr;
            
            for (int i = 0; i < cm.GetMaidCount(); i++)
            {
                Maid m = cm.GetMaid(i);
                if (m != null && m.Visible)
                {
                    GameObject breatheObj = m.GetPrefab("夜伽_吐息");
                    if (breatheObj == null)
                    {
                        m.AddPrefab("Particle/pToiki", "夜伽_吐息", "Bip01 Head", new Vector3(0.042f, 0.076f, 0f), new Vector3(-90f, 90f, 0f));
                        breatheObj = m.GetPrefab("夜伽_吐息");
                    }
                    if (breatheObj != null) newBreatheList.Add(breatheObj);
                }
            }
            
            FieldInfo breatheField = GetField(typeof(YotogiPlayManager), "breatheObjects");
            if (breatheField != null) breatheField.SetValue(pm, newBreatheList);

            GameObject[] liquidObjs = new GameObject[3];
            string[] liquidNames = { "夜伽_愛液1", "夜伽_愛液2", "夜伽_愛液3" };
            string[] liquidPaths = { "Particle/pPistonEasy_cm3D2", "Particle/pPistonNormal_cm3D2", "Particle/pPistonHard_cm3D2" };
            
            for(int i=0; i<3; i++)
            {
                GameObject obj = newMaid.GetPrefab(liquidNames[i]);
                if (obj == null)
                {
                    newMaid.AddPrefab(liquidPaths[i], liquidNames[i], "_IK_vagina", new Vector3(0f, 0f, 0.01f), new Vector3(0f, -180f, 90f));
                    obj = newMaid.GetPrefab(liquidNames[i]);
                }
                liquidObjs[i] = obj;
                if (obj != null)
                {
                    ParticleSystem ps = obj.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.startDelay = 0f; 
                    }
                    obj.SetActive(false); 
                }
            }

            FieldInfo liquidField = GetField(typeof(YotogiPlayManager), "loveLiquidObjects");
            if (liquidField != null) liquidField.SetValue(pm, liquidObjs);
        }

        private void RefreshManagers(Maid newMaid)
        {
            YotogiPlayManager pm = YotogiManager.instans.play_mgr;
            if (pm == null) return;

            FieldInfo paramBarField = GetField(typeof(YotogiPlayManager), "param_basic_bar_");
            if (paramBarField != null)
            {
                object barObj = paramBarField.GetValue(pm);
                if (barObj != null)
                {
                    MethodInfo setMaid = barObj.GetType().GetMethod("SetMaid");
                    if (setMaid != null) setMaid.Invoke(barObj, new object[] { newMaid });
                    
                    MethodInfo updateView = barObj.GetType().GetMethod("UpdateView");
                    if (updateView != null) updateView.Invoke(barObj, null);
                }
            }

            FieldInfo paramViewerField = GetField(typeof(YotogiPlayManager), "paramenter_viewer_");
            if (paramViewerField != null)
            {
                object viewerObj = paramViewerField.GetValue(pm);
                if (viewerObj != null)
                {
                    MethodInfo setMaid = viewerObj.GetType().GetMethod("SetMaid");
                    if (setMaid != null) setMaid.Invoke(viewerObj, new object[] { newMaid });
                    
                    MethodInfo updateCommon = viewerObj.GetType().GetMethod("UpdateTextCommon");
                    if (updateCommon != null) updateCommon.Invoke(viewerObj, null);
                    
                    MethodInfo updateParam = viewerObj.GetType().GetMethod("UpdateTextParam");
                    if (updateParam != null) updateParam.Invoke(viewerObj, null);
                }
            }

            pm.UpdateCommand();
        }

        private void UpdateCamera(Maid targetMaid)
        {
            if (GameMain.Instance.MainCamera != null)
            {
                Vector3 targetPos = targetMaid.gameObject.transform.position;
                targetPos.y += 0.8f; 
                
                GameMain.Instance.MainCamera.SetTargetPos(targetPos);
            }
        }

        private FieldInfo GetField(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        private void DrawCamSettingControls(CamSetting setting, float defaultFov, float defaultDist)
        {
            DrawSliderWithReset("FOV", ref setting.fov, 1f, 120f, defaultFov, "F0");
            DrawSliderWithReset("Distance", ref setting.distance, 0.05f, 2f, defaultDist);
            DrawSliderWithReset("Rotation X", ref setting.rotationX, -180f, 180f, 0f, "F0");
            DrawSliderWithReset("Rotation Y", ref setting.rotationY, -90f, 90f, 0f, "F0");
            setting.invert = GUILayout.Toggle(setting.invert, "Invert View");
        }

        private void DrawSliderWithReset(string label, ref float value, float min, float max, float defaultValue, string format = "F2")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(70));
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.Label(value.ToString(format), GUILayout.Width(35));
            if (GUILayout.Button("R", GUILayout.Width(20)))
            {
                value = defaultValue;
            }
            GUILayout.EndHorizontal();
        }

        private bool DrawHeader(string text, ref bool state)
        {
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button(state ? "\u25BC" : "\u25B6", GUILayout.Width(20)))
            {
                state = !state;
            }
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
            return state;
        }

        private void UpdateLighting()
        {
            if (GameMain.Instance.MainLight != null)
            {
                GameMain.Instance.MainLight.SetIntensity(lightIntensity);
                GameMain.Instance.MainLight.SetColor(lightColor);
                GameMain.Instance.MainLight.SetShadowStrength(shadowStrength);
            }
        }

        private void UpdatePostProcessing()
        {
            if (GameMain.Instance.CMSystem != null)
            {
                GameMain.Instance.CMSystem.BloomValue = (int)bloomValue;
                GameMain.Instance.CMSystem.Bloom = bloomValue > 0;
            }

            if (GameMain.Instance.MainCamera != null)
            {
                if (colorCorrection == null)
                {
                    Component[] components = GameMain.Instance.MainCamera.GetComponents<MonoBehaviour>();
                    foreach (var c in components)
                    {
                        if (c.GetType().Name == "ColorCorrectionCurves")
                        {
                            colorCorrection = (MonoBehaviour)c;
                            colorCorrectionType = c.GetType();
                            saturationProp = colorCorrectionType.GetProperty("saturation");
                            redChannelField = colorCorrectionType.GetField("redChannel");
                            greenChannelField = colorCorrectionType.GetField("greenChannel");
                            blueChannelField = colorCorrectionType.GetField("blueChannel");
                            updateTexturesMethod = colorCorrectionType.GetMethod("UpdateTextures");
                            break;
                        }
                    }
                }

                if (colorCorrection != null)
                {
                    if (saturationProp != null) saturationProp.SetValue(colorCorrection, saturation, null);
                    
                    if (redChannelField != null) redChannelField.SetValue(colorCorrection, AnimationCurve.Linear(0, 0, 1, rgbRed));
                    if (greenChannelField != null) greenChannelField.SetValue(colorCorrection, AnimationCurve.Linear(0, 0, 1, rgbGreen));
                    if (blueChannelField != null) blueChannelField.SetValue(colorCorrection, AnimationCurve.Linear(0, 0, 1, rgbBlue));

                    if (updateTexturesMethod != null) updateTexturesMethod.Invoke(colorCorrection, null);

                    if (!colorCorrection.enabled) colorCorrection.enabled = true;
                }
            }
        }

        private void UpdateScreens()
        {
            if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;
            CharacterMgr charMgr = GameMain.Instance.CharacterMgr;

            while (maidScreens.Count < charMgr.GetMaidCount())
            {
                maidScreens.Add(new MaidScreenSet());
            }

            for (int i = 0; i < charMgr.GetMaidCount(); i++)
            {
                Maid maid = charMgr.GetMaid(i);
                MaidScreenSet set = maidScreens[i];

                if (maid != null && maid.Visible && maid.body0 != null && maid.body0.isLoadedBody)
                {
                    if (!set.IsInitialized) set.Initialize(i);
                    set.Update(maid);
                }
                else
                {
                    set.SetActive(false);
                }
            }
        }

        private void DrawScreens()
        {
            int baseId = 10000;
            float currentRightEdge = Screen.width - 20;

            for (int i = 0; i < maidScreens.Count; i++)
            {
                MaidScreenSet set = maidScreens[i];
                if (!set.IsInitialized || !set.IsActive) continue;

                float maxW = Mathf.Max(set.faceSize, Mathf.Max(set.chestSize, set.pelvisSize));
                float currentX = currentRightEdge - maxW;

                set.faceRect.width = set.faceSize; set.faceRect.height = set.faceSize * 0.75f + 25;
                set.chestRect.width = set.chestSize; set.chestRect.height = set.chestSize * 0.75f + 25;
                set.pelvisRect.width = set.pelvisSize; set.pelvisRect.height = set.pelvisSize * 0.75f + 25;

                if (!set.layoutInitialized)
                {
                    set.faceRect.x = currentX;
                    set.chestRect.x = currentX;
                    set.pelvisRect.x = currentX;

                    float startY = 20;
                    set.faceRect.y = startY;
                    startY += set.faceRect.height + 10;
                    
                    set.chestRect.y = startY;
                    startY += set.chestRect.height + 10;
                    
                    set.pelvisRect.y = startY;

                    set.layoutInitialized = true;
                }
                
                if (set.faceEnabled)
                    set.faceRect = GUI.Window(baseId + i * 10 + 0, set.faceRect, (id) => DrawTextureWindow(id, set.faceCam.renderTexture, false, false, set.faceSize), "Face - Maid " + i);
                
                if (set.chestEnabled)
                    set.chestRect = GUI.Window(baseId + i * 10 + 1, set.chestRect, (id) => DrawTextureWindow(id, set.chestCam.renderTexture, false, false, set.chestSize), "Chest - Maid " + i);
                
                if (set.pelvisEnabled)
                    set.pelvisRect = GUI.Window(baseId + i * 10 + 2, set.pelvisRect, (id) => DrawTextureWindow(id, set.pelvisCam.renderTexture, false, false, set.pelvisSize), "Pelvis - Maid " + i);

                currentRightEdge = currentX - 20;
            }

            if (showVideoScreen && videoScreen != null && videoScreen.IsLoaded())
            {
                float vidHeight = videoScreenSize * 0.75f + 25;
                videoScreen.windowRect.width = videoScreenSize; 
                videoScreen.windowRect.height = vidHeight;
                videoScreen.windowRect = GUI.Window(9000, videoScreen.windowRect, (id) => DrawTextureWindow(id, videoScreen.GetTexture(), true, videoScreen.RequiresFlip(), videoScreenSize), "Video Player");
            }
        }

        private void DrawTextureWindow(int id, Texture tex, bool isVideo, bool flipVideo, float width)
        {
            if (tex != null)
            {
                Rect contentRect = new Rect(0, 25, width, width * 0.75f);
                if (flipVideo)
                {
                    GUIUtility.ScaleAroundPivot(new Vector2(1, -1), contentRect.center);
                }
                GUI.DrawTexture(contentRect, tex, ScaleMode.ScaleToFit);
                if (flipVideo)
                {
                    GUIUtility.ScaleAroundPivot(new Vector2(1, -1), contentRect.center);
                }
            }
            GUI.DragWindow();
        }

        // --- Inner Classes ---

        public class InGameFileBrowser
        {
            private string currentDirectory;
            private string[] currentDirectories;
            private string[] currentFiles;
            private Vector2 scrollPos;
            
            private string[] filters = { ".mp4", ".avi", ".mkv", ".mov", ".webm", ".ine" };

            public void Initialize()
            {
                currentDirectory = Directory.GetCurrentDirectory();
                Refresh();
            }

            private void Refresh()
            {
                try 
                {
                    currentDirectories = Directory.GetDirectories(currentDirectory);
                    currentFiles = Directory.GetFiles(currentDirectory)
                        .Where(f => filters.Contains(Path.GetExtension(f).ToLower()))
                        .ToArray();
                }
                catch (Exception) 
                {
                    currentDirectory = Directory.GetDirectoryRoot(currentDirectory);
                    currentDirectories = new string[0];
                    currentFiles = new string[0];
                    try {
                        currentDirectories = Directory.GetDirectories(currentDirectory);
                        currentFiles = Directory.GetFiles(currentDirectory)
                            .Where(f => filters.Contains(Path.GetExtension(f).ToLower()))
                            .ToArray();
                    } catch {}
                }
            }

            public void Draw(Rect rect, Action<string> onFileSelected, Action onCancel)
            {
                GUILayout.BeginVertical();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Root", GUILayout.Width(50)))
                {
                    currentDirectory = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
                    Refresh();
                }
                if (GUILayout.Button("Up", GUILayout.Width(50)))
                {
                    DirectoryInfo parent = Directory.GetParent(currentDirectory);
                    if (parent != null)
                    {
                        currentDirectory = parent.FullName;
                        Refresh();
                    }
                }
                GUILayout.Label(currentDirectory, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(25))) onCancel();
                GUILayout.EndHorizontal();

                scrollPos = GUILayout.BeginScrollView(scrollPos);

                GUI.backgroundColor = Color.yellow;
                if (currentDirectories != null) {
                    foreach (var dir in currentDirectories)
                    {
                        if (GUILayout.Button("[" + Path.GetFileName(dir) + "]"))
                        {
                            currentDirectory = dir;
                            Refresh();
                        }
                    }
                }
                GUI.backgroundColor = Color.white;

                if (currentFiles != null) {
                    foreach (var file in currentFiles)
                    {
                        if (GUILayout.Button(Path.GetFileName(file)))
                        {
                            onFileSelected(file);
                        }
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
        }

        private class VideoScreen
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
                    if(mediaPlayer.Control != null) mediaPlayer.Control.SetLooping(loop);
                }
            }
            public bool GetLooping() { return mediaPlayer != null && mediaPlayer.m_Loop; }

            public void SetVolume(float vol) 
            { 
                if (mediaPlayer != null) 
                {
                    mediaPlayer.m_Volume = vol; 
                    if(mediaPlayer.Control != null) mediaPlayer.Control.SetVolume(vol);
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
                    
                    // Sync position if changed via script/game (optional) or enforce from UI
                    // For now, we assume UI drives the object.
                    
                    // If object was destroyed (e.g. scene change)
                    if (tvObject == null || tvObject.Equals(null))
                    {
                        tvObject = null;
                        monitorControl = null;
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
                    if (GUILayout.Button("Play")) { if(monitorControl) monitorControl.Play(); }
                    if (GUILayout.Button("Pause")) { if(monitorControl) monitorControl.Pause(); }
                    if (GUILayout.Button("Stop")) { if(monitorControl) monitorControl.Stop(); }
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
                if(!string.IsNullOrEmpty(statusMsg)) GUILayout.Label(statusMsg);

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
                    }
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
                statusMsg = "Removed TV Prop.";
            }

            private void LoadVideo(string path)
            {
                if (monitorControl != null && !string.IsNullOrEmpty(path))
                {
                    bool success = monitorControl.LoadMovie(path, true);
                    statusMsg = success ? "Loaded video." : "Failed to load video.";
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
                        posX = currentPos.x, posY = currentPos.y, posZ = currentPos.z,
                        rotX = currentRot.x, rotY = currentRot.y, rotZ = currentRot.z,
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
                catch(Exception ex)
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
                        if(!string.IsNullOrEmpty(currentVideoPath)) LoadVideo(currentVideoPath);
                    }
                    else
                    {
                        // Auto spawn on load? Maybe better to let user click spawn.
                        // But let's spawn it for convenience.
                        SpawnTV();
                        // LoadVideo is called in SpawnTV if we move lines, but SpawnTV uses currentPos/Rot.
                        // We need to load video after spawn.
                        if(!string.IsNullOrEmpty(currentVideoPath)) LoadVideo(currentVideoPath);
                    }

                    statusMsg = "Loaded: " + name;
                }
                catch(Exception ex)
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

        private class MaidScreenSet
        {
            public bool IsInitialized = false;
            public bool layoutInitialized = false;
            public bool IsActive = false;
            public SubCamera faceCam;
            public SubCamera chestCam;
            public SubCamera pelvisCam;
            
            public bool faceEnabled = true;
            public bool chestEnabled = true;
            public bool pelvisEnabled = true;

            public CamSetting faceSet = new CamSetting { distance = 0.35f, fov = 30f };
            public CamSetting chestSet = new CamSetting { distance = 0.4f, fov = 45f };
            public CamSetting pelvisSet = new CamSetting { distance = 0.4f, fov = 45f, offset = new Vector3(0, 0.05f, 0) };

            public float faceSize = 200f;
            public float chestSize = 200f;
            public float pelvisSize = 200f;

            public Rect faceRect;
            public Rect chestRect;
            public Rect pelvisRect;

            public LookAtType lookAtType = LookAtType.None;
            public int lookAtTargetIndex = -1;

            private int maidIndex;

            public void Initialize(int index)
            {
                maidIndex = index;
                faceCam = new SubCamera("FaceCam_" + index);
                chestCam = new SubCamera("ChestCam_" + index);
                pelvisCam = new SubCamera("PelvisCam_" + index);
                
                faceRect = new Rect(0, 0, faceSize, faceSize * 0.75f + 25);
                chestRect = new Rect(0, 0, chestSize, chestSize * 0.75f + 25);
                pelvisRect = new Rect(0, 0, pelvisSize, pelvisSize * 0.75f + 25);

                IsInitialized = true;
                layoutInitialized = false;
            }

            public void Update(Maid maid)
            {
                IsActive = true;
                
                Transform faceT = maid.body0.trsHead;
                Transform chestT = maid.body0.Spine1a; 
                Transform pelvisT = maid.body0.GetBone("_IK_vagina");
                if (pelvisT == null) pelvisT = maid.body0.Pelvis;

                if(faceEnabled) faceCam.Update(faceT, faceSet); else faceCam.SetActive(false);
                if(chestEnabled) chestCam.Update(chestT, chestSet); else chestCam.SetActive(false);
                if(pelvisEnabled) pelvisCam.Update(pelvisT, pelvisSet); else pelvisCam.SetActive(false);
            }

            public void SetActive(bool active)
            {
                IsActive = active;
                if (!active)
                {
                    faceCam?.SetActive(false);
                    chestCam?.SetActive(false);
                    pelvisCam?.SetActive(false);
                }
            }

            public MaidProfileData ToProfileData()
            {
                return new MaidProfileData {
                    faceEnabled = faceEnabled, chestEnabled = chestEnabled, pelvisEnabled = pelvisEnabled,
                    faceSize = faceSize, chestSize = chestSize, pelvisSize = pelvisSize,
                    faceSet = faceSet, chestSet = chestSet, pelvisSet = pelvisSet
                };
            }

            public void ApplyProfileData(MaidProfileData data)
            {
                faceEnabled = data.faceEnabled; chestEnabled = data.chestEnabled; pelvisEnabled = data.pelvisEnabled;
                faceSize = data.faceSize; chestSize = data.chestSize; pelvisSize = data.pelvisSize;
                faceSet = data.faceSet; chestSet = data.chestSet; pelvisSet = data.pelvisSet;
                layoutInitialized = false;
            }

            public void Destroy()
            {
                faceCam?.Destroy();
                chestCam?.Destroy();
                pelvisCam?.Destroy();
            }
        }

        private class SubCamera
        {
            public GameObject obj;
            public Camera cam;
            public RenderTexture renderTexture;
            private string name;

            public SubCamera(string name)
            {
                this.name = name;
            }

            public void Update(Transform target, CamSetting setting)
            {
                if (target == null) return;

                if (obj == null)
                {
                    if (renderTexture != null)
                    {
                        renderTexture.Release();
                        renderTexture = null;
                    }

                    obj = new GameObject(name);
                    cam = obj.AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.Color;
                    cam.backgroundColor = Color.black; 

                    if (GameMain.Instance.MainCamera != null)
                    {
                        Camera mainCam = GameMain.Instance.MainCamera.GetComponent<Camera>();
                        if (mainCam != null)
                        {
                            cam.CopyFrom(mainCam);
                            cam.rect = new Rect(0,0,1,1);
                        }
                    }
                    
                    cam.cullingMask &= ~(1 << LayerMask.NameToLayer("UI")); 
                    cam.cullingMask &= ~(1 << LayerMask.NameToLayer("NGUI"));
                    
                    renderTexture = new RenderTexture(512, 512, 24);
                    cam.targetTexture = renderTexture;
                }

                if (!obj.activeSelf) obj.SetActive(true);

                Quaternion targetRot = target.rotation;
                Quaternion orbitRot = Quaternion.Euler(setting.rotationY, setting.rotationX, 0);
                Vector3 offsetDir = orbitRot * Vector3.forward;
                
                Vector3 pos = target.position + (targetRot * offsetDir * setting.distance) + setting.offset;
                
                obj.transform.position = pos;
                
                obj.transform.LookAt(target, target.up);

                if (setting.invert)
                {
                    obj.transform.Rotate(0, 0, 180, Space.Self);
                }

                cam.fieldOfView = setting.fov;
            }

            public void SetActive(bool active)
            {
                if (obj != null) obj.SetActive(active);
            }

            public void Destroy()
            {
                if (obj != null) UnityEngine.Object.Destroy(obj);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture = null;
                }
            }
        }

        // --- Face Manager Class ---
        public class FaceManager
        {
            private Dictionary<string, Dictionary<string, string>> categories = new Dictionary<string, Dictionary<string, string>>
            {
                { "Common", new Dictionary<string, string> {
                    {"Normal", "通常"}, {"Angry", "怒り"}, {"Smile", "笑顔"}, {"Gentle Smile", "微笑み"},
                    {"Sad", "悲しみ２"}, {"Crying", "泣き"}, {"Embarrassed", "恥ずかしい"}, {"Shy", "照れ"},
                    {"Default", "デフォ"}, {"Ahegao", "エロ絶頂"}, {"Enjoying", "エロ好感３"}, {"Really Enjoying", "エロ絶頂"}
                }},
                { "Erotic", new Dictionary<string, string> {
                    {"Ero Normal 1", "エロ通常１"}, {"Ero Normal 2", "エロ通常２"}, {"Ero Normal 3", "エロ通常３"},
                    {"Ero Shy 1", "エロ羞恥１"}, {"Ero Shy 2", "エロ羞恥２"}, {"Ero Shy 3", "エロ羞恥３"},
                    {"Ero Excited 0", "エロ興奮０"}, {"Ero Excited 1", "エロ興奮１"}, {"Ero Excited 2", "エロ興奮２"}, {"Ero Excited 3", "エロ興奮３"},
                    {"Ero Nervous", "エロ緊張"}, {"Ero Expectation", "エロ期待"},
                    {"Ero Like 1", "エロ好感１"}, {"Ero Like 2", "エロ好感２"}, {"Ero Like 3", "エロ好感３"},
                    {"Ero Endure 1", "エロ我慢１"}, {"Ero Endure 2", "エロ我慢２"}, {"Ero Endure 3", "エロ我慢３"},
                    {"Ero Disgust 1", "エロ嫌悪１"}, {"Ero Fear", "エロ怯え"},
                    {"Ero Pain 1", "エロ痛み１"}, {"Ero Pain 2", "エロ痛み２"}, {"Ero Pain 3", "エロ痛み３"},
                    {"Ero Sobbing", "エロメソ泣き"}, {"Ero Orgasm", "エロ絶頂"},
                    {"Ero Pain Endure", "エロ痛み我慢"}, {"Ero Pain Endure 2", "エロ痛み我慢２"}, {"Ero Pain Endure 3", "エロ痛み我慢３"},
                    {"Ero Trance", "エロ放心"}, {"Horny", "発情"}
                }},
                { "Action", new Dictionary<string, string> {
                    {"After Ejac 1", "通常射精後１"}, {"After Ejac 2", "通常射精後２"},
                    {"After Exc Ejac 1", "興奮射精後１"}, {"After Exc Ejac 2", "興奮射精後２"},
                    {"After Org Ejac 1", "絶頂射精後１"}, {"After Org Ejac 2", "絶頂射精後２"},
                    {"Lick Affection", "エロ舐め愛情"}, {"Lick Pleasure", "エロ舐め快楽"},
                    {"Lick Disgust", "エロ舐め嫌悪"}, {"Lick Normal", "エロ舐め通常"},
                    {"Kiss", "接吻"},
                    {"BJ Affection", "エロフェラ愛情"}, {"BJ Pleasure", "エロフェラ快楽"},
                    {"BJ Disgust", "エロフェラ嫌悪"}, {"BJ Normal", "エロフェラ通常"},
                    {"Tongue Torture", "エロ舌責"}, {"Tongue Torture Pleasure", "エロ舌責快楽"}
                }},
                { "Closed/Other", new Dictionary<string, string> {
                    {"Closed Lick Aff", "閉じ舐め愛情"}, {"Closed Lick Pleasure", "閉じ舐め快楽"},
                    {"Closed Lick Disgust", "閉じ舐め嫌悪"}, {"Closed Lick Normal", "閉じ舐め通常"},
                    {"Closed BJ Aff", "閉じフェラ愛情"}, {"Closed BJ Pleasure", "閉じフェラ快楽"},
                    {"Closed BJ Disgust", "閉じフェラ嫌悪"}, {"Closed BJ Normal", "閉じフェラ通常"},
                    {"Closed Eyes", "閉じ目"}, {"Eyes Mouth Closed", "目口閉じ"}, {"Mouth Open", "口開け"}
                }},
                { "Expressions", new Dictionary<string, string> {
                    {"Blank", "きょとん"}, {"Staring", "ジト目"}, {"Ahhn", "あーん"}, {"Sigh", "ためいき"},
                    {"Smug", "ドヤ顔"}, {"Grin", "にっこり"}, {"Surprised", "びっくり"}, {"Pout", "ぷんすか"},
                    {"Squeeze Eyelids", "まぶたギュ"}, {"Muu", "むー"}, {"Twitched Smile", "引きつり笑顔"}, {"Question", "疑問"},
                    {"Bitter Smile", "苦笑い"}, {"Troubled", "困った"}, {"Thinking", "思案伏せ目"}, {"Slightly Angry", "少し怒り"},
                    {"Seduction", "誘惑"}, {"Sulking", "拗ね"}, {"Kindness", "優しさ"}, {"Sleeping", "居眠り安眠"},
                    {"Eyes Wide Open", "目を見開いて"}, {"Afterglow Weak", "余韻弱"},
                    {"Shy Shout", "照れ叫び"}, {"Wink Shy", "ウインク照れ"}, {"Grin Shy", "にっこり照れ"}
                }},
                { "Dance", new Dictionary<string, string> {
                    {"Dance Closed", "ダンス目つむり"}, {"Dance Yawn", "ダンスあくび"}, {"Dance Surprised", "ダンスびっくり"},
                    {"Dance Smile", "ダンス微笑み"}, {"Dance Open", "ダンス目あけ"}, {"Dance Closed 2", "ダンス目とじ"},
                    {"Dance Wink", "ダンスウインク"}, {"Dance Kiss", "ダンスキス"}, {"Dance Stare", "ダンスジト目"},
                    {"Dance Troubled", "ダンス困り顔"}, {"Dance Serious", "ダンス真剣"}, {"Dance Sorrow", "ダンス憂い"},
                    {"Dance Seduction", "ダンス誘惑"}
                }},
                { "Blends (Cheek/Tears)", new Dictionary<string, string> {
                    {"C0 T0", "頬０涙０"}, {"C0 T1", "頬０涙１"}, {"C0 T2", "頬０涙２"}, {"C0 T3", "頬０涙３"},
                    {"C1 T0", "頬１涙０"}, {"C1 T1", "頬１涙１"}, {"C1 T2", "頬１涙２"}, {"C1 T3", "頬１涙３"},
                    {"C2 T0", "頬２涙０"}, {"C2 T1", "頬２涙１"}, {"C2 T2", "頬２涙２"}, {"C2 T3", "頬２涙３"},
                    {"C3 T0", "頬３涙０"}, {"C3 T1", "頬３涙１"}, {"C3 T2", "頬３涙２"}, {"C3 T3", "頬３涙３"}
                }},
                { "Blends (Drool)", new Dictionary<string, string> {
                    {"Drool Only", "追加よだれ"},
                    {"C0 T0 Drool", "頬０涙０よだれ"}, {"C0 T1 Drool", "頬０涙１よだれ"}, {"C0 T2 Drool", "頬０涙２よだれ"}, {"C0 T3 Drool", "頬０涙３よだれ"},
                    {"C1 T0 Drool", "頬１涙０よだれ"}, {"C1 T1 Drool", "頬１涙１よだれ"}, {"C1 T2 Drool", "頬１涙２よだれ"}, {"C1 T3 Drool", "頬１涙３よだれ"},
                    {"C2 T0 Drool", "頬２涙０よだれ"}, {"C2 T1 Drool", "頬２涙１よだれ"}, {"C2 T2 Drool", "頬２涙２よだれ"}, {"C2 T3 Drool", "頬２涙３よだれ"},
                    {"C3 T0 Drool", "頬３涙０よだれ"}, {"C3 T1 Drool", "頬３涙１よだれ"}, {"C3 T2 Drool", "頬３涙２よだれ"}, {"C3 T3 Drool", "頬３涙３よだれ"}
                }}
            };

            private Vector2 scrollPos;
            private int selectedMaidIndex = 0;
            private bool[] categoryFoldouts;

            public FaceManager()
            {
                categoryFoldouts = new bool[categories.Count];
                for(int i=0; i<categoryFoldouts.Length; i++) categoryFoldouts[i] = true;
            }

            public void DrawUI()
            {
                if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;
                
                int maidCount = GameMain.Instance.CharacterMgr.GetMaidCount();
                if (maidCount == 0) 
                {
                    GUILayout.Label("No Maids.");
                    return;
                }

                GUILayout.BeginHorizontal();
                for (int i = 0; i < maidCount; i++)
                {
                    Maid m = GameMain.Instance.CharacterMgr.GetMaid(i);
                    if (m != null && m.Visible)
                    {
                        if (GUILayout.Toggle(selectedMaidIndex == i, "Maid " + i, "button")) selectedMaidIndex = i;
                    }
                }
                GUILayout.EndHorizontal();

                scrollPos = GUILayout.BeginScrollView(scrollPos);
                GUILayout.BeginVertical();
                
                int catIndex = 0;
                foreach (var cat in categories)
                {
                    // Category Header
                    if (GUILayout.Button(cat.Key, "box"))
                    {
                        categoryFoldouts[catIndex] = !categoryFoldouts[catIndex];
                    }

                    if (categoryFoldouts[catIndex])
                    {
                        int columns = 3;
                        int current = 0;
                        GUILayout.BeginHorizontal();
                        foreach (var kvp in cat.Value)
                        {
                            if (current >= columns)
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                current = 0;
                            }
                            if (GUILayout.Button(kvp.Key))
                            {
                                ApplyFace(kvp.Value);
                            }
                            current++;
                        }
                        GUILayout.EndHorizontal();
                    }
                    catIndex++;
                    GUILayout.Space(5);
                }

                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            private void ApplyFace(string faceName)
            {
                Maid m = GameMain.Instance.CharacterMgr.GetMaid(selectedMaidIndex);
                if (m != null)
                {
                    if (faceName.Contains("頬") || faceName.Contains("涙") || faceName.Contains("よだれ"))
                    {
                        m.FaceBlend(faceName);
                    }
                    else
                    {
                        m.FaceAnime(faceName, 1f, 0);
                    }
                }
            }
        }

        // --- Kiss Manager Class ---
        public class KissManager
        {
            public bool autoKissEnabled = false;
            public bool droolEffectEnabled = true;
            public bool isKissing = false;
            
            // Distances
            private const float kissDistClose = 0.3f;
            private const float kissDistVeryClose = 0.15f;
            private const float kissDistOffset = 0.05f;
            private const float checkOffsetHead = -0.01f;

            // State
            private Dictionary<int, bool> maidKissingState = new Dictionary<int, bool>();
            private Dictionary<int, string> originalFace = new Dictionary<int, string>();
            private Dictionary<int, string> originalBlend = new Dictionary<int, string>();
            
            private Dictionary<string, string[][]> voiceDB = new Dictionary<string, string[][]>();

            public KissManager()
            {
                LoadVoiceDB();
            }

            public void DrawUI()
            {
                GUILayout.BeginVertical("box");
                autoKissEnabled = GUILayout.Toggle(autoKissEnabled, "Auto Kiss Enabled (VR)");
                droolEffectEnabled = GUILayout.Toggle(droolEffectEnabled, "Drool Effect (Kiss/BJ/Close)");
                
                if (autoKissEnabled)
                {
                    GUILayout.Label($"Kiss State: {(isKissing ? "Kissing" : "None")}");
                }
                GUILayout.EndVertical();
            }

            public void Update()
            {
                if (!autoKissEnabled && !droolEffectEnabled) return;
                if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;

                // Check distance
                Transform cameraT = GameMain.Instance.MainCamera.transform;
                if (GameMain.Instance.OvrMgr != null && GameMain.Instance.OvrMgr.EyeAnchor != null)
                {
                    cameraT = GameMain.Instance.OvrMgr.EyeAnchor;
                }

                CharacterMgr cm = GameMain.Instance.CharacterMgr;
                for (int i = 0; i < cm.GetMaidCount(); i++)
                {
                    Maid m = cm.GetMaid(i);
                    if (m != null && m.Visible && m.body0 != null && m.body0.trsHead != null)
                    {
                        float dist = Vector3.Distance(cameraT.position, m.body0.trsHead.position);
                        bool isClose = dist < kissDistVeryClose;
                        
                        bool isKissSkill = IsKissOrBJSkill(m);

                        // Drool Logic
                        if (droolEffectEnabled)
                        {
                            if (isClose || isKissSkill)
                            {
                                ApplyDrool(m);
                            }
                        }

                        // Auto Kiss Logic
                        if (autoKissEnabled)
                        {
                            if (!maidKissingState.ContainsKey(i)) maidKissingState[i] = false;

                            if (isClose && !maidKissingState[i])
                            {
                                StartKiss(m, i);
                            }
                            else if (!isClose && maidKissingState[i])
                            {
                                StopKiss(m, i);
                            }
                        }
                    }
                }
            }

            private void StartKiss(Maid m, int index)
            {
                maidKissingState[index] = true;
                isKissing = true;
                
                if (!originalFace.ContainsKey(index)) originalFace[index] = m.ActiveFace;
                if (!originalBlend.ContainsKey(index)) originalBlend[index] = m.FaceName3;

                m.FaceAnime("接吻", 1f, 0); 

                PlayKissVoice(m);
            }

            private void StopKiss(Maid m, int index)
            {
                maidKissingState[index] = false;
                
                bool anyKissing = false;
                foreach(var kvp in maidKissingState) if(kvp.Value) anyKissing = true;
                isKissing = anyKissing;

                if (originalFace.ContainsKey(index)) m.FaceAnime(originalFace[index], 1f, 0);
                
                // Stop kissing sound
                if (m.AudioMan != null) m.AudioMan.Stop();
            }

            private void ApplyDrool(Maid m)
            {
                string current = m.FaceName3;
                if (string.IsNullOrEmpty(current)) current = "頬０涙０";
                if (!current.Contains("よだれ"))
                {
                    m.FaceBlend(current + "よだれ");
                }
            }

            private bool IsKissOrBJSkill(Maid m)
            {
                if (m.body0 == null || m.body0.LastAnimeFN == null) return false;
                string anim = m.body0.LastAnimeFN.ToLower();
                
                if (anim.Contains("fera")) return true;
                if (anim.Contains("sixnine")) return true;
                if (anim.Contains("_ir_")) return true;
                if (anim.Contains("_kuti")) return true;
                if (anim.Contains("housi")) return true;
                if (anim.Contains("kiss")) return true;
                
                return false; 
            }

            private void PlayKissVoice(Maid m)
            {
                string p = m.status.personal.uniqueName; 
                
                if (voiceDB.ContainsKey(p))
                {
                    string[][] levels = voiceDB[p];
                    int exciteLv = 0; 
                    if (m.status.currentExcite >= 200) exciteLv = 3;
                    else if (m.status.currentExcite >= 150) exciteLv = 2;
                    else if (m.status.currentExcite >= 100) exciteLv = 1;
                    
                    if (exciteLv < levels.Length)
                    {
                        string[] clips = levels[exciteLv];
                        if (clips.Length > 0)
                        {
                            string clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                            m.AudioMan.LoadPlay(clip, 0f, false, true); 
                        }
                    }
                }
            }

            private void LoadVoiceDB()
            {
                // Muku
                voiceDB["Muku"] = new string[][] {
                    new string[] { "H0_00093.ogg", "H0_00094.ogg" },
                    new string[] { "H0_00095.ogg", "H0_00096.ogg" },
                    new string[] { "H0_00097.ogg", "H0_00253.ogg" },
                    new string[] { "H0_00254.ogg", "H0_00255.ogg" }
                };
                
                // Majime
                voiceDB["Majime"] = new string[][] {
                    new string[] { "H1_00265.ogg", "H1_00266.ogg" },
                    new string[] { "H1_00267.ogg", "H1_00268.ogg" },
                    new string[] { "H1_00269.ogg", "H1_00270.ogg" },
                    new string[] { "H1_00271.ogg", "H1_00272.ogg" }
                };
                
                // Rindere
                voiceDB["Rindere"] = new string[][] {
                    new string[] { "H2_00067.ogg", "H2_00068.ogg" },
                    new string[] { "H2_00069.ogg", "H2_00070.ogg" },
                    new string[] { "H2_00071.ogg", "H2_00072.ogg" },
                    new string[] { "H2_00073.ogg", "H2_00074.ogg" }
                };

                // Silent
                voiceDB["Silent"] = new string[][] {
                    new string[] { "H3_00566.ogg", "H3_00567.ogg" },
                    new string[] { "H3_00568.ogg", "H3_00569.ogg" },
                    new string[] { "H3_00570.ogg", "H3_00571.ogg" },
                    new string[] { "H3_00572.ogg", "H3_00573.ogg" }
                };

                // Devilish
                voiceDB["Devilish"] = new string[][] {
                    new string[] { "H4_00901.ogg", "H4_00902.ogg" },
                    new string[] { "H4_00903.ogg", "H4_00904.ogg" },
                    new string[] { "H4_00905.ogg", "H4_00906.ogg" },
                    new string[] { "H4_00907.ogg", "H4_00908.ogg" }
                };

                // Ladylike
                voiceDB["Ladylike"] = new string[][] {
                    new string[] { "H5_00640.ogg", "H5_00641.ogg" },
                    new string[] { "H5_00642.ogg", "H5_00643.ogg" },
                    new string[] { "H5_00644.ogg", "H5_00645.ogg" },
                    new string[] { "H5_00646.ogg", "H5_00647.ogg" }
                };

                // Secretary
                voiceDB["Secretary"] = new string[][] {
                    new string[] { "H6_00206.ogg", "H6_00207.ogg" },
                    new string[] { "H6_00208.ogg", "H6_00209.ogg" },
                    new string[] { "H6_00210.ogg", "H6_00211.ogg" },
                    new string[] { "H6_00212.ogg", "H6_00213.ogg" }
                };

                // Sister
                voiceDB["Sister"] = new string[][] {
                    new string[] { "H7_02810.ogg", "H7_02811.ogg" },
                    new string[] { "H7_02812.ogg", "H7_02813.ogg" },
                    new string[] { "H7_02814.ogg", "H7_02815.ogg" },
                    new string[] { "H7_02816.ogg", "H7_02817.ogg" }
                };

                // Curtness
                voiceDB["Curtness"] = new string[][] {
                    new string[] { "H8_01179.ogg", "H8_01180.ogg" },
                    new string[] { "H8_01181.ogg", "H8_01182.ogg" },
                    new string[] { "H8_01183.ogg", "H8_01184.ogg" },
                    new string[] { "H8_01185.ogg", "H8_01186.ogg" }
                };

                // Missy
                voiceDB["Missy"] = new string[][] {
                    new string[] { "H9_00618.ogg", "H9_00619.ogg" },
                    new string[] { "H9_00620.ogg", "H9_00621.ogg" },
                    new string[] { "H9_00622.ogg", "H9_00623.ogg" },
                    new string[] { "H9_00624.ogg", "H9_00625.ogg" }
                };

                // Childhood
                voiceDB["Childhood"] = new string[][] {
                    new string[] { "H10_03889.ogg", "H10_03890.ogg" },
                    new string[] { "H10_03891.ogg", "H10_03892.ogg" },
                    new string[] { "H10_03893.ogg", "H10_03894.ogg" },
                    new string[] { "H10_03895.ogg", "H10_03896.ogg" }
                };

                // Masochist
                voiceDB["Masochist"] = new string[][] {
                    new string[] { "H11_00713.ogg", "H11_00714.ogg" },
                    new string[] { "H11_00715.ogg", "H11_00716.ogg" },
                    new string[] { "H11_00717.ogg", "H11_00718.ogg" },
                    new string[] { "H11_00719.ogg", "H11_00720.ogg" }
                };

                // Crafty
                voiceDB["Crafty"] = new string[][] {
                    new string[] { "H12_01253.ogg", "H12_01254.ogg" },
                    new string[] { "H12_01255.ogg", "H12_01256.ogg" },
                    new string[] { "H12_01257.ogg", "H12_01258.ogg" },
                    new string[] { "H12_01259.ogg", "H12_01260.ogg" }
                };

                // Friendly
                voiceDB["Friendly"] = new string[][] {
                    new string[] { "V1_00530.ogg", "V1_00531.ogg" },
                    new string[] { "V1_00532.ogg", "V1_00533.ogg" },
                    new string[] { "V1_00534.ogg", "V1_00535.ogg" },
                    new string[] { "V1_00536.ogg", "V1_00537.ogg" }
                };

                // Dame
                voiceDB["Dame"] = new string[][] {
                    new string[] { "V0_00528.ogg", "V0_00529.ogg" },
                    new string[] { "V0_00530.ogg", "V0_00531.ogg" },
                    new string[] { "V0_00532.ogg", "V0_00533.ogg" },
                    new string[] { "V0_00534.ogg", "V0_00535.ogg" }
                };

                // Gal
                voiceDB["Gal"] = new string[][] {
                    new string[] { "H13_01084.ogg", "H13_01085.ogg" },
                    new string[] { "H13_01086.ogg", "H13_01087.ogg" },
                    new string[] { "H13_01088.ogg", "H13_01089.ogg" },
                    new string[] { "H13_01090.ogg", "H13_01091.ogg" }
                };

                // Strong
                voiceDB["Strong"] = new string[][] {
                    new string[] { "H14_02544.ogg", "H14_02545.ogg", "H14_ecafe_00644.ogg", "H14_ecafe_00645.ogg" },
                    new string[] { "H14_02546.ogg", "H14_02547.ogg", "H14_ecafe_00646.ogg", "H14_ecafe_00647.ogg" },
                    new string[] { "H14_02548.ogg", "H14_02549.ogg", "H14_ecafe_00648.ogg", "H14_ecafe_00649.ogg" },
                    new string[] { "H14_02550.ogg", "H14_02551.ogg", "H14_ecafe_00650.ogg", "H14_ecafe_00651.ogg" }
                };

                // Elegant
                voiceDB["Elegant"] = new string[][] {
                    new string[] { "H15_00418.ogg", "H15_00419.ogg" },
                    new string[] { "H15_00420.ogg", "H15_00421.ogg" },
                    new string[] { "H15_00422.ogg", "H15_00423.ogg" },
                    new string[] { "H15_00424.ogg", "H15_00425.ogg" }
                };

                // Familiarity
                voiceDB["Familiarity"] = new string[][] {
                    new string[] { "H16_00379.ogg", "H16_00380.ogg" },
                    new string[] { "H16_00381.ogg", "H16_00382.ogg" },
                    new string[] { "H16_00383.ogg", "H16_00384.ogg" },
                    new string[] { "H16_00385.ogg", "H16_00386.ogg" }
                };
                
                // Pride
                voiceDB["Pride"] = new string[][] {
                    new string[] { "s0_01276.ogg", "s0_01277.ogg" },
                    new string[] { "s0_01284.ogg", "s0_01285.ogg" },
                    new string[] { "s0_01280.ogg", "s0_01281.ogg" },
                    new string[] { "s0_01288.ogg", "s0_01289.ogg" }
                };
                
                // Cool
                voiceDB["Cool"] = new string[][] {
                    new string[] { "s1_02349.ogg", "s1_02350.ogg" },
                    new string[] { "s1_02357.ogg", "s1_02358.ogg" },
                    new string[] { "s1_02353.ogg", "s1_02354.ogg" },
                    new string[] { "s1_02361.ogg", "s1_02362.ogg" }
                };
                
                // Pure
                voiceDB["Pure"] = new string[][] {
                    new string[] { "s2_01190.ogg", "s2_01191.ogg" },
                    new string[] { "s2_01198.ogg", "s2_01199.ogg" },
                    new string[] { "s2_01194.ogg", "s2_01195.ogg" },
                    new string[] { "s2_01202.ogg", "s2_01203.ogg" }
                };
                
                // Yandere
                voiceDB["Yandere"] = new string[][] {
                    new string[] { "s3_12044.ogg", "s3_02728.ogg" },
                    new string[] { "s3_02735.ogg", "s3_02736.ogg" },
                    new string[] { "s3_02731.ogg", "s3_02732.ogg" },
                    new string[] { "s3_02739.ogg", "s3_02740.ogg" }
                };
                
                // Anesan
                voiceDB["Anesan"] = new string[][] {
                    new string[] { "s4_08167.ogg", "s4_08168.ogg" },
                    new string[] { "s4_08175.ogg", "s4_08176.ogg" },
                    new string[] { "s4_08171.ogg", "s4_08172.ogg" },
                    new string[] { "s4_08179.ogg", "s4_08180.ogg" }
                };
                
                // Genki
                voiceDB["Genki"] = new string[][] {
                    new string[] { "s5_04087.ogg", "s5_04088.ogg" },
                    new string[] { "s5_04095.ogg", "s5_04096.ogg" },
                    new string[] { "s5_04091.ogg", "s5_04092.ogg" },
                    new string[] { "s5_04099.ogg", "s5_04100.ogg" }
                };
                
                // Sadist
                voiceDB["Sadist"] = new string[][] {
                    new string[] { "s6_02219.ogg", "s6_02220.ogg" },
                    new string[] { "s6_02227.ogg", "s6_02228.ogg" },
                    new string[] { "s6_02223.ogg", "s6_02224.ogg" },
                    new string[] { "s6_02231.ogg", "s6_02232.ogg" }
                };
            }
        }

        // --- Body Reaction Manager Class ---
        public class BodyReactionManager
        {
            public bool isSweatEnabled = true;
            public bool isNippleEnabled = true;
            public bool isClitorisEnabled = true;
            public bool isCheekEnabled = true;

            private HashSet<TMorph> morphsToFix = new HashSet<TMorph>();

            public void DrawUI()
            {
                isSweatEnabled = GUILayout.Toggle(isSweatEnabled, "Enable Sweat Mod");
                isNippleEnabled = GUILayout.Toggle(isNippleEnabled, "Enable Nipple Erection");
                isClitorisEnabled = GUILayout.Toggle(isClitorisEnabled, "Enable Clitoris Erection");
                isCheekEnabled = GUILayout.Toggle(isCheekEnabled, "Enable Dynamic Blush");
            }

            public void Update()
            {
                if (!isSweatEnabled && !isNippleEnabled && !isClitorisEnabled && !isCheekEnabled) return;
                if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;

                CharacterMgr cm = GameMain.Instance.CharacterMgr;
                Maid mainMaid = cm.GetMaid(0);
                if (mainMaid == null) return;

                float mainExcite = (float)mainMaid.status.currentExcite;

                for (int i = 0; i < cm.GetMaidCount(); i++)
                {
                    Maid m = cm.GetMaid(i);
                    if (m != null && m.Visible && m.body0 != null)
                    {
                        ApplyReactions(m, mainExcite);
                    }
                }

                // Batch fix blend values
                foreach (TMorph tm in morphsToFix)
                {
                    tm.FixBlendValues();
                }
                morphsToFix.Clear();
            }

            private void ApplyReactions(Maid maid, float excite)
            {
                float rate = Mathf.Clamp01(excite / 300f);

                // Sweat
                if (isSweatEnabled)
                {
                    float dryVal = (1.0f - rate);
                    float swetVal = rate;
                    float swetSmallVal = rate;
                    
                    float swetTareVal = 0f;
                    if (excite >= 150f) swetTareVal = Mathf.Clamp01((excite - 150f) / 150f);

                    float swetBigVal = 0f;
                    if (excite >= 200f) swetBigVal = Mathf.Clamp01((excite - 200f) / 100f);

                    SetMorphValue(maid.body0, "dry", dryVal);
                    SetMorphValue(maid.body0, "swet", swetVal);
                    SetMorphValue(maid.body0, "swet_small", swetSmallVal);
                    SetMorphValue(maid.body0, "swet_tare", swetTareVal);
                    SetMorphValue(maid.body0, "swet_big", swetBigVal);
                }

                // Nipples (tits_chikubi_cow)
                if (isNippleEnabled)
                {
                    // Max value is 25 (0.25f) at max excite (300)
                    SetMorphValue(maid.body0, "tits_chikubi_cow", rate * 0.25f);
                }

                // Clitoris
                if (isClitorisEnabled)
                {
                    SetMorphValue(maid.body0, "clitoris", rate);
                }

                // Cheeks (Hoho/Sekimen)
                if (isCheekEnabled)
                {
                    SetMorphValue(maid.body0, "hoho", rate);
                }
            }

            private void SetMorphValue(TBody body, string tag, float value)
            {
                if (body == null || body.goSlot == null) return;

                for (int i = 0; i < body.goSlot.Count; i++)
                {
                    TMorph morph = body.goSlot[i].morph;
                    if (morph != null && morph.Contains(tag))
                    {
                        morph.SetBlendValues((int)morph.hash[tag], value);
                        if (!morphsToFix.Contains(morph))
                        {
                            morphsToFix.Add(morph);
                        }
                    }
                }
            }
        }
    }
}