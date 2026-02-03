using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using COM3D2.YotogiCamControl.Plugin.UI;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class LUTManager
    {
        private YotogiCamControl plugin;
        private List<string> lutFiles = new List<string>();
        private string selectedLutName = "None";
        private Vector2 scrollPos;
        private bool showLutList = false;
        private float lutIntensity = 100f;

        // Component references
        private MonoBehaviour amplifyColorEffect;
        private Type amplifyType;
        private bool effectsInitialized = false;
        private bool searchAttempted = false;

        public LUTManager(YotogiCamControl plugin)
        {
            this.plugin = plugin;
            RefreshLutList();
        }

        public void Update()
        {
            if (!effectsInitialized && !searchAttempted && GameMain.Instance != null && GameMain.Instance.MainCamera != null)
            {
                InitializeEffects();
            }
        }

        private void InitializeEffects()
        {
            searchAttempted = true; // Prevent spamming
            Camera cam = null;
            if (GameMain.Instance.MainCamera != null)
            {
                cam = GameMain.Instance.MainCamera.camera;
            }

            if (cam == null) return;

            // 1. Try to find existing AmplifyColorEffect on camera
            MonoBehaviour[] components = cam.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour comp in components)
            {
                if (comp != null && (comp.GetType().Name.Contains("AmplifyColor") || comp.GetType().Name == "ColorCorrectionLookup"))
                {
                    amplifyColorEffect = comp;
                    amplifyType = comp.GetType();
                    effectsInitialized = true;
                    Debug.Log($"YotogiCamControl: Found existing {amplifyType.Name} on Camera.");
                    return;
                }
            }

            // 2. Comprehensive Search across ALL assemblies
            if (amplifyColorEffect == null)
            {
                Debug.Log("YotogiCamControl: LUT Effect not found on camera. Searching ALL assemblies for Types...");
                Type targetType = null;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        // Optimization: Skip obviously irrelevant assemblies if needed, but safer to check all for now
                        Type[] types = asm.GetTypes();
                        foreach (Type t in types)
                        {
                            if (t.Name.Equals("AmplifyColorEffect", StringComparison.OrdinalIgnoreCase) ||
                                t.Name.Equals("AmplifyColorBase", StringComparison.OrdinalIgnoreCase) ||
                                t.Name.Equals("ColorCorrectionLookup", StringComparison.OrdinalIgnoreCase))
                            {
                                targetType = t;
                                Debug.Log($"YotogiCamControl: Found suitable Type: {t.FullName} in {asm.GetName().Name}");
                                break;
                            }
                        }
                    }
                    catch { /* Ignore assembly load errors */ }

                    if (targetType != null) break;
                }

                if (targetType != null)
                {
                    Debug.Log($"YotogiCamControl: Adding component {targetType.Name}...");
                    amplifyColorEffect = (MonoBehaviour)cam.gameObject.AddComponent(targetType);
                    amplifyType = targetType;
                    effectsInitialized = true;
                }
                else
                {
                    // 3. Fallback: Check standard COM3D2 CMSystem if available
                    if (GameMain.Instance.CMSystem != null)
                    {
                         // Sometimes stored here, but usually just reflects camera component.
                    }

                    Debug.LogWarning("YotogiCamControl: Could not find ANY suitable LUT Effect Type in loaded assemblies.");
                }
            }
        }

        public void DrawUI()
        {
            GUILayout.BeginVertical(StyleManager.BoxStyle);
            if (GUILayout.Button(showLutList ? "Hide LUT Library" : "Show LUT Library"))
            {
                showLutList = !showLutList;
                if (showLutList) RefreshLutList();
            }

            GUILayout.Label($"Current LUT: {selectedLutName}");

            // Intensity Slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Intensity", GUILayout.Width(60));
            float newIntensity = GUILayout.HorizontalSlider(lutIntensity, 0f, 100f);
            GUILayout.Label(newIntensity.ToString("F0") + "%", GUILayout.Width(40));
            if (newIntensity != lutIntensity)
            {
                lutIntensity = newIntensity;
                UpdateIntensity();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset LUT"))
            {
                ApplyLUT(null);
            }

            if (showLutList)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));

                if (lutFiles.Count == 0)
                {
                    GUILayout.Label("No .png files found in YotogiCamControl/LUTs/");
                }
                else
                {
                    foreach (string file in lutFiles)
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (GUILayout.Button(name))
                        {
                            ApplyLUT(file);
                        }
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private void UpdateIntensity()
        {
            if (amplifyColorEffect != null && amplifyType != null)
            {
                var blendProp = amplifyType.GetProperty("BlendAmount");
                if (blendProp != null)
                {
                    blendProp.SetValue(amplifyColorEffect, lutIntensity / 100f, null);
                }
            }
        }

        private void RefreshLutList()
        {
            lutFiles.Clear();
            string path = Path.Combine(Paths.PluginPath, "YotogiCamControl/LUTs");

            // Check adjacent to DLL as fallback
            if (!Directory.Exists(path))
            {
                string dllDir = Path.GetDirectoryName(typeof(YotogiCamControl).Assembly.Location);
                string altPath = Path.Combine(dllDir, "LUTs");
                if (Directory.Exists(altPath)) path = altPath;
            }

            if (Directory.Exists(path))
            {
                lutFiles.AddRange(Directory.GetFiles(path, "*.png"));
            }
        }

        private void ApplyLUT(string path)
        {
            if (amplifyColorEffect == null) InitializeEffects();

            if (amplifyColorEffect == null)
            {
                NotificationManager.Show("Error: LUT Effect not found/supported.", 4f, Color.red);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    // Reset
                    var lutProp = amplifyType.GetProperty("LutTexture");
                    if (lutProp != null) lutProp.SetValue(amplifyColorEffect, null, null);

                    var blendProp = amplifyType.GetProperty("BlendAmount");
                    if (blendProp != null) blendProp.SetValue(amplifyColorEffect, 0f, null);

                    selectedLutName = "None";
                    NotificationManager.Show("LUT Reset", 2f, Color.white);
                }
                else
                {
                    byte[] data = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(256, 16, TextureFormat.RGB24, false);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.LoadImage(data);

                    var lutProp = amplifyType.GetProperty("LutTexture");
                    if (lutProp != null) lutProp.SetValue(amplifyColorEffect, tex, null);

                    // Update intensity (apply slider value)
                    UpdateIntensity();

                    // Enable if disabled
                    if (!amplifyColorEffect.enabled) amplifyColorEffect.enabled = true;

                    selectedLutName = Path.GetFileNameWithoutExtension(path);
                    NotificationManager.Show($"Applied LUT: {selectedLutName}", 2f, Color.green);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("YotogiCamControl: LUT Apply Error: " + ex.ToString());
                NotificationManager.Show("Error applying LUT", 3f, Color.red);
            }
        }
    }
}
