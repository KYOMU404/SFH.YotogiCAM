using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using COM3D2.YotogiCamControl.Plugin.Data;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class MasturbationManager
    {
        private YotogiCamControl plugin;

        // Animation Lists
        private string[] anims_weak = new string[] {
            "onani_1_f.anm", "onani_2_f.anm"
        };
        private string[] anims_strong = new string[] {
            "onani_3_f.anm"
        };
        private string[] anims_orgasm = new string[] {
            "onani_cli_2_f.anm", "onani_cli_3_f.anm"
        };

        public BasicVoiceSet[] bvs = new BasicVoiceSet[20];
        public string[] personalNames => VoiceRepository.personalNames;

        // State
        private int currentMode = 0; // 0: None, 1: Weak, 2: Strong, 3: Orgasm
        private float timer = 0f;
        private string searchText = "";

        public string[] sFaceAnimeStun = new string[] { "絶頂射精後１", "興奮射精後１", "エロメソ泣き", "エロ痛み２", "エロ我慢３", "引きつり笑顔", "エロ通常３", "泣き" };

        public MasturbationManager(YotogiCamControl plugin)
        {
            this.plugin = plugin;
            InitializeVoiceData();
        }

        private void InitializeVoiceData()
        {
            bvs = VoiceRepository.GetVoiceSets();
        }

        public void Update()
        {
            if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;
            Maid m = GameMain.Instance.CharacterMgr.GetMaid(1); // Sub Maid
            if (m == null || !m.Visible) return;

            if (currentMode > 0)
            {
                if (m.AudioMan != null && !m.AudioMan.audiosource.isPlaying)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        PlayNextVoice(m);
                    }
                }
            }
        }

        private void PlayNextVoice(Maid m)
        {
            int pIndex = Array.IndexOf(personalNames, m.status.personal.uniqueName);
            if (pIndex < 0 || pIndex >= bvs.Length) pIndex = 0; // Default or fallback

            string[] clips = null;
            int exciteLv = 0; // Simplified excite level logic for now, or fetch from m.status.currentExcite

            if (m.status.currentExcite >= 200) exciteLv = 3;
            else if (m.status.currentExcite >= 100) exciteLv = 2;
            else if (m.status.currentExcite >= 50) exciteLv = 1;

            // Adjust index based on array size
            int safeIndex = 0;

            if (currentMode == 1)
            { // Weak
                if (bvs[pIndex].sLoopVoice20Vibe.Length > 0)
                {
                    safeIndex = Mathf.Clamp(exciteLv, 0, bvs[pIndex].sLoopVoice20Vibe.Length - 1);
                    clips = bvs[pIndex].sLoopVoice20Vibe[safeIndex];
                }
            }
            else if (currentMode == 2)
            { // Strong
                if (bvs[pIndex].sLoopVoice30Vibe.Length > 0)
                {
                    safeIndex = Mathf.Clamp(exciteLv, 0, bvs[pIndex].sLoopVoice30Vibe.Length - 1);
                    clips = bvs[pIndex].sLoopVoice30Vibe[safeIndex];
                }
            }
            else if (currentMode == 3)
            { // Orgasm
                if (bvs[pIndex].sOrgasmVoice30Vibe.Length > 0)
                {
                    safeIndex = Mathf.Clamp(exciteLv, 0, bvs[pIndex].sOrgasmVoice30Vibe.Length - 1);
                    clips = bvs[pIndex].sOrgasmVoice30Vibe[safeIndex];
                }
            }

            if (clips != null && clips.Length > 0)
            {
                string clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                m.AudioMan.LoadPlay(clip, 0f, false, false); // Loop handled manually by timer
                timer = UnityEngine.Random.Range(2f, 5f); // Random delay between moans

                // Reset mode if orgasm finished?
                if (currentMode == 3)
                {
                    // Maybe switch to stop or weak after orgasm
                    // For now, keep playing until user stops
                }
            }
        }

        public void DrawUI()
        {
            Maid m = GameMain.Instance.CharacterMgr.GetMaid(1);
            if (m == null)
            {
                GUILayout.Label("Sub Maid (Maid 1) not found.");
                return;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label("Masturbation Control (Maid 1)");

            GUILayout.BeginHorizontal();

            // Weak Button
            GUI.backgroundColor = currentMode == 1 ? Color.green : Color.white;
            if (GUILayout.Button("WEAK", GUILayout.Height(40)))
            {
                SetMode(1, m);
            }

            // Strong Button
            GUI.backgroundColor = currentMode == 2 ? Color.yellow : Color.white;
            if (GUILayout.Button("STRONG", GUILayout.Height(40)))
            {
                SetMode(2, m);
            }

            // Orgasm Button
            GUI.backgroundColor = currentMode == 3 ? Color.red : Color.white;
            if (GUILayout.Button("ORGASM", GUILayout.Height(40)))
            {
                SetMode(3, m);
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("STOP", GUILayout.Height(40)))
            {
                SetMode(0, m);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Animations:");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Search:", GUILayout.Width(50));
                searchText = GUILayout.TextField(searchText);
                if (GUILayout.Button("X", GUILayout.Width(25))) searchText = "";
                GUILayout.EndHorizontal();

            // Animation Grid
            DrawAnimGrid(anims_weak, "Weak Animations", m);
            DrawAnimGrid(anims_strong, "Strong Animations", m);
            DrawAnimGrid(anims_orgasm, "Orgasm Animations", m);

            GUILayout.EndVertical();
        }

        private void DrawAnimGrid(string[] anims, string label, Maid m)
        {
                bool isSearching = !string.IsNullOrEmpty(searchText);

                // Pre-filter
                List<string> displayAnims = new List<string>();
                if (isSearching)
                {
                    foreach(string anim in anims)
                    {
                        if (anim.ToLower().Contains(searchText.ToLower())) displayAnims.Add(anim);
                    }
                    if (displayAnims.Count == 0) return; // Skip section if no matches
                }
                else
                {
                    displayAnims.AddRange(anims);
                }

            GUILayout.Label(label);
            int columns = 2;
            int current = 0;
            GUILayout.BeginHorizontal();
                foreach (string anim in displayAnims)
            {
                if (current >= columns)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    current = 0;
                }
                if (GUILayout.Button(anim.Replace(".anm", ""), GUILayout.Width(150)))
                {
                    PlayAnimation(m, anim);
                }
                current++;
            }
            GUILayout.EndHorizontal();
        }

        private void SetMode(int mode, Maid m)
        {
            currentMode = mode;
            if (mode == 0)
            {
                m.AudioMan.Stop();
                ToggleToy(false, m);
            }
            else
            {
                ToggleToy(true, m);
                PlayNextVoice(m);

                if (mode == 1) PlayAnimation(m, anims_weak[0]);
                if (mode == 2) PlayAnimation(m, anims_strong[0]);
                if (mode == 3)
                {
                    PlayAnimation(m, anims_orgasm[0]);
                }
            }
        }

        private void PlayAnimation(Maid m, string anim)
        {
            // Use Reflection to avoid CS0012 and try both FileSystems
            try
            {
                // Get GameUty type and FileSystems
                Type gameUtyType = Type.GetType("GameUty, Assembly-CSharp");
                object fs = null;
                object fsOld = null;

                if (gameUtyType != null)
                {
                    PropertyInfo fsProp = gameUtyType.GetProperty("FileSystem", BindingFlags.Public | BindingFlags.Static);
                    if (fsProp != null) fs = fsProp.GetValue(null, null);

                    PropertyInfo fsOldProp = gameUtyType.GetProperty("FileSystemOld", BindingFlags.Public | BindingFlags.Static);
                    if (fsOldProp != null) fsOld = fsOldProp.GetValue(null, null);
                }

                // Find the CrossFade overload that takes AFileSystemBase (using LINQ or simple iteration)
                // CrossFade(string fn, AFileSystemBase fileSystem, bool additive, bool loop, bool boAddQue, float val, float weight)
                MethodInfo method = null;
                foreach (MethodInfo mi in m.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (mi.Name == "CrossFade" && mi.GetParameters().Length == 7)
                    {
                        method = mi; // This should be the one
                        break;
                    }
                }

                if (method != null)
                {
                    // Try FileSystemOld first as these seem to be legacy animations
                    if (fsOld != null)
                    {
                        method.Invoke(m, new object[] { anim, fsOld, false, true, false, 0.5f, 1f });
                    }
                    else if (fs != null)
                    {
                        method.Invoke(m, new object[] { anim, fs, false, true, false, 0.5f, 1f });
                    }
                }
                else
                {
                    Debug.LogError("[MasturbationManager] CrossFade(7 params) method not found via Reflection.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[MasturbationManager] Error invoking CrossFade: " + ex.ToString());
            }
        }

        private void ToggleToy(bool active, Maid m)
        {
            if (active)
            {
                // Spawn vibe prop if not exists
                // m.SetProp(MPN.handitem, "HandItemR_AnalVibe_I_.menu", 0, true);
                // Or accVag_VibeBig_I_.menu depending on anim
            }
            else
            {
                m.SetProp(MPN.handitem, "", 0, true);
                m.SetProp(MPN.accvag, "", 0, true);
            }
        }
    }
}
