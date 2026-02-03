using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;
using Yotogis;
using COM3D2.YotogiCamControl.Plugin.Data;
using COM3D2.YotogiCamControl.Plugin.Managers;
using COM3D2.YotogiCamControl.Plugin.UI;
using COM3D2.YotogiCamControl.Plugin.Utils;

namespace COM3D2.YotogiCamControl.Plugin
{
    [BepInPlugin("com.stay.fucking.hard", "YotogiCamControl", "1.7.1")]
    public class YotogiCamControl : BaseUnityPlugin
    {
        public static YotogiCamControl Instance { get; private set; }

        private bool isGuiActive = false;
        private Rect windowRect = new Rect(20, 20, 550, 700);
        private int windowId = 9999;
        private Vector2 scrollPosition = Vector2.zero;

        // Configuration
        private ConfigEntry<KeyboardShortcut> toggleKeyConfig;
        private ConfigEntry<Vector2> windowPositionConfig;

        // UI State
        private int activeTab = 0;
        private string[] tabNames = { "Scene", "Screens", "Faces", "Kiss", "Video", "Prop TV", "VideoCam", "Swap", "LookAt", "Misc", "Masturbation", "Ahegao", "Kupa", "TNP" };
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

        // Overlay Texture
        private Texture2D overlayTexture;
        private Texture2D overlayTextureMain;
        private bool showMainOverlay = false;

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
#pragma warning disable 0414
        private bool reflectionInitialized = false;
#pragma warning restore 0414

        // Screens
        private bool showScreens = true;
        private const float minScreenSize = 100f;
        private const float maxScreenSize = 600f;

        // Managers
        public KissManager kissManager;
        public FaceManager faceManager;
        public BodyReactionManager bodyReactionManager;
        public PropTVManager propTVManager;
        public VideoCamManager videoCamManager;
        public LUTManager lutManager;
        public MasturbationManager masturbationManager;
        public AhegaoManager ahegaoManager;
        public KupaManager kupaManager;
        public TNPManager tnpManager;

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
            Instance = this;
            Logger.LogInfo("YotogiCamControl: Awake started");
            try
            {
                // Init Configuration
                toggleKeyConfig = Config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.F6), "Key to toggle the UI");
                windowPositionConfig = Config.Bind("UI", "WindowPosition", new Vector2(20, 20), "Saved window position");

                // Restore window position
                windowRect.x = windowPositionConfig.Value.x;
                windowRect.y = windowPositionConfig.Value.y;

                videoScreen = new VideoScreen();
                videoScreen.Initialize();

                fileBrowser = new InGameFileBrowser();
                fileBrowser.Initialize();

                kissManager = new KissManager();
                faceManager = new FaceManager();
                bodyReactionManager = new BodyReactionManager();
                propTVManager = new PropTVManager(this);
                videoCamManager = new VideoCamManager(this);
                lutManager = new LUTManager(this);
                masturbationManager = new MasturbationManager(this);
                ahegaoManager = new AhegaoManager(this);
                kupaManager = new KupaManager(this);
                tnpManager = new TNPManager(this);

                // Harmony
                Harmony.CreateAndPatchAll(typeof(YotogiCamControl));

                // Load Overlay Textures
                string dllDir = Path.GetDirectoryName(Info.Location);

                // Load Overlay 1
                try {
                    string overlayPath = Path.Combine(Paths.PluginPath, "YotogiCamControl/Overlay.png");
                    Logger.LogInfo("YotogiCamControl: Checking for Overlay at " + overlayPath);
                    if (!File.Exists(overlayPath))
                    {
                         overlayPath = Path.Combine(dllDir, "Overlay.png");
                         Logger.LogInfo("YotogiCamControl: Checking for Overlay at " + overlayPath);
                    }

                    if (File.Exists(overlayPath))
                    {
                        byte[] fileData = File.ReadAllBytes(overlayPath);
                        overlayTexture = new Texture2D(2, 2);
                        overlayTexture.LoadImage(fileData);
                        Logger.LogInfo("YotogiCamControl: Loaded Overlay.png");
                    }
                    else
                    {
                        Logger.LogWarning("YotogiCamControl: Overlay.png not found.");
                    }
                } catch (Exception e) {
                    Logger.LogError("YotogiCamControl: Failed to load overlay: " + e.Message);
                }

                // Load Overlay 2 (Main Screen)
                try {
                    string overlayPath2 = Path.Combine(Paths.PluginPath, "YotogiCamControl/Overlay2.png");
                    Logger.LogInfo("YotogiCamControl: Checking for Overlay2 at " + overlayPath2);
                    if (!File.Exists(overlayPath2))
                    {
                         overlayPath2 = Path.Combine(dllDir, "Overlay2.png");
                         Logger.LogInfo("YotogiCamControl: Checking for Overlay2 at " + overlayPath2);
                    }

                    if (File.Exists(overlayPath2))
                    {
                        byte[] fileData = File.ReadAllBytes(overlayPath2);
                        overlayTextureMain = new Texture2D(2, 2);
                        overlayTextureMain.LoadImage(fileData);
                         Logger.LogInfo("YotogiCamControl: Loaded Overlay2.png");
                    }
                    else
                    {
                        Logger.LogWarning("YotogiCamControl: Overlay2.png not found.");
                    }
                } catch (Exception e) {
                    Logger.LogError("YotogiCamControl: Failed to load overlay2: " + e.Message);
                }

                Logger.LogInfo("YotogiCamControl: Initialization successful");
            }
            catch (Exception ex)
            {
                Logger.LogError("YotogiCamControl: Fatal error during initialization: " + ex.ToString());
            }
        }

        public void OnDestroy()
        {
            // Save window position
            windowPositionConfig.Value = new Vector2(windowRect.x, windowRect.y);

            foreach (var set in maidScreens)
            {
                set.Destroy();
            }
            maidScreens.Clear();
            videoScreen?.Destroy();
        }

        public void Update()
        {
            try
            {
                if (toggleKeyConfig.Value.IsDown())
                {
                    Logger.LogInfo("YotogiCamControl: Toggle Key Pressed");
                    isGuiActive = !isGuiActive;
                    if (!isGuiActive)
                    {
                        showFileBrowser = false;
                        // Save config when closing
                        windowPositionConfig.Value = new Vector2(windowRect.x, windowRect.y);
                    }
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

                // Centralized Maid Update Loop
                if (GameMain.Instance != null && GameMain.Instance.CharacterMgr != null)
                {
                    CharacterMgr charMgr = GameMain.Instance.CharacterMgr;
                    for (int i = 0; i < charMgr.GetMaidCount(); i++)
                    {
                        Maid m = charMgr.GetMaid(i);
                        if (m != null && m.Visible && m.body0 != null)
                        {
                            // Animation Speed
                            if (syncAnimationSpeed)
                            {
                                var anim = m.body0.GetAnimation();
                                if (anim != null && m.body0.LastAnimeFN != null && anim[m.body0.LastAnimeFN] != null)
                                {
                                    anim[m.body0.LastAnimeFN].speed = animationSpeed / 100f;
                                }

                                // Ahe Morph Logic
                                if (animationSpeed >= 100f)
                                {
                                    float aheVal = (animationSpeed - 100f) / 100f;
                                    if (m.body0.Face != null && m.body0.Face.morph != null)
                                    {
                                        TMorph morph = m.body0.Face.morph;
                                        if (morph.hash.ContainsKey("ahe"))
                                        {
                                            morph.SetBlendValues((int)morph.hash["ahe"], aheVal);
                                        }
                                    }
                                }
                            }

                            // Update Managers per Maid
                            if (ahegaoManager != null) ahegaoManager.Update(i, m);
                            if (kupaManager != null) kupaManager.Update(i, m);
                        }
                    }
                }

                if (kissManager != null) kissManager.Update();
                if (bodyReactionManager != null) bodyReactionManager.Update();
                if (propTVManager != null) propTVManager.Update();
                if (videoCamManager != null) videoCamManager.Update();
                if (lutManager != null) lutManager.Update();
                if (faceManager != null) faceManager.Update();
                if (masturbationManager != null) masturbationManager.Update();

                NotificationManager.Update();
            }
            catch (Exception ex)
            {
                Logger.LogError($"YotogiCamControl: Update Error: {ex.Message}");
            }
        }

        public void OnGUI()
        {
            if (isGuiActive)
            {
                windowRect = GUI.Window(windowId, windowRect, DrawWindow, "Yotogi Cam Control");

                // Ensure window stays on screen
                windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
                windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);

                if (showFileBrowser)
                {
                    fileBrowserRect = GUI.Window(windowId + 100, fileBrowserRect, DrawFileBrowserWindow, "File Browser");
                }
            }

            if (showScreens)
            {
                DrawScreens();
            }

            // Main Overlay Logic
            if (showMainOverlay && overlayTextureMain != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTextureMain, ScaleMode.StretchToFill);
            }

            NotificationManager.Draw();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            // Use SelectionGrid with 4 columns to prevent tabs from being too narrow
            activeTab = GUILayout.SelectionGrid(activeTab, tabNames, 4, StyleManager.TabStyle);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            switch (activeTab)
            {
                case 0: DrawSceneTab(); break;
                case 1: DrawScreensTab(); break;
                case 2: faceManager.DrawUI(); break;
                case 3: kissManager.DrawUI(); break;
                case 4: DrawVideoTab(); break;
                case 5: propTVManager.DrawUI(); break;
                case 6: videoCamManager.DrawUI(); break;
                case 7: DrawSwapTab(); break;
                case 8: DrawLookAtTab(); break;
                case 9: DrawMiscTab(); break;
                case 10: masturbationManager.DrawUI(); break;
                case 11: ahegaoManager.DrawUI(); break;
                case 12: kupaManager.DrawUI(); break;
                case 13: tnpManager.DrawUI(); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        // ... [OMITTING UNCHANGED METHODS FOR BREVITY IN PLANNING, BUT WRITING FULL CONTENT] ...
        // I will write the full content in the tool call to ensure consistency.

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
            GUILayout.BeginVertical(StyleManager.BoxStyle);
            bodyReactionManager.DrawUI();
            GUILayout.EndVertical();
        }

        private void DrawSceneTab()
        {
            if (DrawHeader("Lighting", ref showLighting))
            {
                GUILayout.BeginVertical(StyleManager.BoxStyle);
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
                GUILayout.BeginVertical(StyleManager.BoxStyle);
                DrawSliderWithReset("Bloom", ref bloomValue, 0f, 100f, 0f, "F0");
                DrawSliderWithReset("Saturation", ref saturation, 0f, 5f, 1f);

                GUILayout.Label("Color Grading");
                DrawSliderWithReset("Red", ref rgbRed, 0f, 5f, 1f);
                DrawSliderWithReset("Green", ref rgbGreen, 0f, 5f, 1f);
                DrawSliderWithReset("Blue", ref rgbBlue, 0f, 5f, 1f);

                GUILayout.Space(5);
                lutManager.DrawUI();

                GUILayout.EndVertical();
            }

            GUILayout.Space(5);
            GUILayout.BeginVertical(StyleManager.BoxStyle);
            GUILayout.Label("Animation");
            // Max speed capped to 130 per instructions
            DrawSliderWithReset("Speed %", ref animationSpeed, 0f, 130f, 100f, "F0");
            syncAnimationSpeed = GUILayout.Toggle(syncAnimationSpeed, "Sync Animation Speed");
            GUILayout.EndVertical();
        }

        private void DrawScreensTab()
        {
            GUILayout.BeginVertical(StyleManager.BoxStyle);
            showScreens = GUILayout.Toggle(showScreens, "Show Screens Enabled");
            showMainOverlay = GUILayout.Toggle(showMainOverlay, "Show REC Overlay on Main Camera");

            GUILayout.Space(5);
            GUILayout.Label("Profile Management:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            profileNameInput = GUILayout.TextField(profileNameInput, GUILayout.Width(100));
            if (GUILayout.Button("Save")) SaveProfile(profileNameInput);
            if (GUILayout.Button("Load")) LoadProfile(profileNameInput);
            GUILayout.EndHorizontal();

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
                        GUILayout.BeginVertical(StyleManager.BoxStyle);
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
                        GUILayout.BeginVertical(StyleManager.BoxStyle);
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
                        GUILayout.BeginVertical(StyleManager.BoxStyle);
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
                for (int i = 0; i < maidScreens.Count; i++)
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
                NotificationManager.Show($"Saved Profile: {name}", 2f, Color.green);
            }
            catch (Exception e)
            {
                NotificationManager.Show($"Error Saving: {e.Message}", 4f, Color.red);
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
                    NotificationManager.Show($"Profile not found: {name}", 3f, Color.yellow);
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

                    for (int i = 0; i < container.profiles.Count && i < maidScreens.Count; i++)
                    {
                        maidScreens[i].ApplyProfileData(container.profiles[i]);
                    }
                    NotificationManager.Show($"Loaded Profile: {name}", 2f, Color.green);
                }
                else
                {
                    NotificationManager.Show("Error: Invalid XML", 4f, Color.red);
                    Debug.LogError("[YotogiCamControl] Failed to parse profile container.");
                }
            }
            catch (Exception e)
            {
                NotificationManager.Show($"Error Loading: {e.Message}", 4f, Color.red);
                Debug.LogError("[YotogiCamControl] Load Error: " + e.ToString());
            }
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

                    GUILayout.BeginVertical(StyleManager.BoxStyle);

                    // Eyes Only Toggle
                    bool newEyesOnly = GUILayout.Toggle(currentSet.eyesOnly, "Eyes Only (No Head Movement)");
                    if (newEyesOnly != currentSet.eyesOnly) currentSet.eyesOnly = newEyesOnly;

                    GUILayout.Label($"Target for Maid {actualMaidIndex}:");

                    bool isLocked = currentSet.lookAtType != LookAtType.None;

                    void DrawLookToggle(string label, LookAtType type, int targetIndex = -1)
                    {
                        bool isActive = currentSet.lookAtType == type && currentSet.lookAtTargetIndex == targetIndex;
                        bool isDisabled = isLocked && !isActive;

                        GUI.enabled = !isDisabled;
                        if (GUILayout.Button(label, isActive ? StyleManager.ActiveButtonStyle : StyleManager.ButtonStyle))
                        {
                            if (!isActive)
                            {
                                currentSet.lookAtType = type;
                                currentSet.lookAtTargetIndex = targetIndex;
                            }
                            else
                            {
                                currentSet.lookAtType = LookAtType.None;
                                currentSet.lookAtTargetIndex = -1;
                            }
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
                    foreach (int otherIndex in activeMaidIndices)
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

                    if (set.eyesOnly)
                    {
                        maid.body0.boHeadToCam = false;
                        maid.body0.boEyeToCam = true;
                    }
                    else
                    {
                        maid.body0.boHeadToCam = true;
                        maid.body0.boEyeToCam = true;
                    }
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
                GUILayout.BeginVertical(StyleManager.BoxStyle);
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

            GUILayout.BeginVertical(StyleManager.BoxStyle);
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
            FieldInfo ymMaidField = ReflectionUtils.GetField(typeof(YotogiManager), "maid_");
            if (ymMaidField != null) ymMaidField.SetValue(YotogiManager.instans, newMaid);

            FieldInfo playSkillArrField = ReflectionUtils.GetField(typeof(YotogiManager), "play_skill_array_");
            if (playSkillArrField != null)
            {
                object arrObj = playSkillArrField.GetValue(YotogiManager.instans);
                if (arrObj is object[] arr)
                {
                    foreach (object playingSkillData in arr)
                    {
                        if (playingSkillData == null) continue;
                        FieldInfo skillPairField = ReflectionUtils.GetField(playingSkillData.GetType(), "skill_pair");
                        if (skillPairField != null)
                        {
                            object skillPair = skillPairField.GetValue(playingSkillData);
                            if (skillPair != null)
                            {
                                FieldInfo spMaidField = ReflectionUtils.GetField(skillPair.GetType(), "maid");
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
                FieldInfo pmMaidField = ReflectionUtils.GetField(typeof(YotogiPlayManager), "maid_");
                if (pmMaidField != null) pmMaidField.SetValue(pm, newMaid);

                FieldInfo pmReplaceArrField = ReflectionUtils.GetField(typeof(YotogiPlayManager), "replace_personal_target_maid_array_");
                if (pmReplaceArrField != null)
                {
                    Maid[] repArr = (Maid[])pmReplaceArrField.GetValue(pm);
                    if (repArr != null && repArr.Length > 0)
                    {
                        if (repArr[0] == oldMaid) repArr[0] = newMaid;
                    }
                }
            }

            UndressingManager um = ReflectionUtils.GetField(typeof(YotogiPlayManager), "undressing_mgr_")?.GetValue(pm) as UndressingManager;
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

            FieldInfo breatheField = ReflectionUtils.GetField(typeof(YotogiPlayManager), "breatheObjects");
            if (breatheField != null) breatheField.SetValue(pm, newBreatheList);

            GameObject[] liquidObjs = new GameObject[3];
            string[] liquidNames = { "夜伽_愛液1", "夜伽_愛液2", "夜伽_愛液3" };
            string[] liquidPaths = { "Particle/pPistonEasy_cm3D2", "Particle/pPistonNormal_cm3D2", "Particle/pPistonHard_cm3D2" };

            for (int i = 0; i < 3; i++)
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

            FieldInfo liquidField = ReflectionUtils.GetField(typeof(YotogiPlayManager), "loveLiquidObjects");
            if (liquidField != null) liquidField.SetValue(pm, liquidObjs);
        }

        private void RefreshManagers(Maid newMaid)
        {
            YotogiPlayManager pm = YotogiManager.instans.play_mgr;
            if (pm == null) return;

            FieldInfo paramBarField = ReflectionUtils.GetField(typeof(YotogiPlayManager), "param_basic_bar_");
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

            FieldInfo paramViewerField = ReflectionUtils.GetField(typeof(YotogiPlayManager), "paramenter_viewer_");
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
            GUILayout.BeginHorizontal(StyleManager.BoxStyle);
            if (GUILayout.Button(state ? "\u25BC " + text : "\u25B6 " + text, StyleManager.HeaderStyle))
            {
                state = !state;
            }
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
                if (!reflectionInitialized)
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
                    reflectionInitialized = true;
                }

                if (colorCorrection != null)
                {
                    if (saturationProp != null) saturationProp.SetValue(colorCorrection, saturation, null);

                    if (redChannelField != null) redChannelField.SetValue(colorCorrection, AnimationCurve.Linear(0, 0, 1, rgbRed));
                    if (greenChannelField != null) greenChannelField.SetValue(colorCorrection, AnimationCurve.Linear(0, 0, 1, rgbGreen));
                    if (blueChannelField != null) blueChannelField.SetValue(colorCorrection, AnimationCurve.Linear(0, 0, 1, rgbBlue));

                    if (updateTexturesMethod != null) updateTexturesMethod.Invoke(colorCorrection, null);

                    if (colorCorrection.enabled == false) colorCorrection.enabled = true;
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
                    set.faceRect = GUI.Window(baseId + i * 10 + 0, set.faceRect, (id) => DrawTextureWindow(id, set.faceCam.renderTexture, false, false, set.faceSize, overlayTexture), "Face - Maid " + i);

                if (set.chestEnabled)
                    set.chestRect = GUI.Window(baseId + i * 10 + 1, set.chestRect, (id) => DrawTextureWindow(id, set.chestCam.renderTexture, false, false, set.chestSize, overlayTexture), "Chest - Maid " + i);

                if (set.pelvisEnabled)
                    set.pelvisRect = GUI.Window(baseId + i * 10 + 2, set.pelvisRect, (id) => DrawTextureWindow(id, set.pelvisCam.renderTexture, false, false, set.pelvisSize, overlayTexture), "Pelvis - Maid " + i);

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

        private void DrawTextureWindow(int id, Texture tex, bool isVideo, bool flipVideo, float width, Texture2D overlay = null)
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

                if (overlay != null)
                {
                     GUI.DrawTexture(contentRect, overlay, ScaleMode.StretchToFill);
                }
            }
            GUI.DragWindow();
        }

        // --- Harmony Patches ---

        [HarmonyPatch(typeof(YotogiPlayManager), "OnClickCommand")]
        [HarmonyPostfix]
        public static void OnClickCommand_Postfix(Skill.Data.Command.Data command_data)
        {
            if (Instance == null) return;
            try
            {
                CommonCommandData commonData = new CommonCommandData(command_data);

                // Get the active maid. YotogiManager.instans.play_mgr.maid_ is typically the main maid involved in yotogi.
                Maid activeMaid = AccessTools.Field(typeof(YotogiPlayManager), "maid_").GetValue(YotogiManager.instans.play_mgr) as Maid;

                if (activeMaid != null)
                {
                    if (Instance.ahegaoManager != null) Instance.ahegaoManager.OnCommand(activeMaid, commonData);
                    if (Instance.kupaManager != null) Instance.kupaManager.OnCommand(activeMaid, commonData);
                }
            }
            catch(Exception e)
            {
                 Instance.Logger.LogError("Error in OnClickCommand_Postfix: " + e.ToString());
            }
        }
    }
}
