using System;
using System.Collections.Generic;
using UnityEngine;
using COM3D2.YotogiCamControl.Plugin.Data;
using COM3D2.YotogiCamControl.Plugin.Utils;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class AhegaoManager
    {
        private YotogiCamControl plugin;

        public class MaidState
        {
            // Mind Break Feature (Legacy)
            public bool mindBreakEnabled;
            public bool isMindBreakApplied;

            // Auto AHE Feature (YotogiSlider)
            public bool autoAheEnabled = false;
            public bool convulsionEnabled = false;

            public bool initialized = false;

            // State
            public float eyeY = 0f;
            public float fAheDefEye = 0f;
            public float fAheLastEye = 0f;
            public float fPassedTimeOnCommand = 0f;
            public int iOrgasmCount = 0;
            public int iLastExcite = 0;
            public int iAheOrgasmChain = 0;
            public bool bOrgasmAvailable = false;

            public float fEyePosToSliderMul = 5000f;

            public Dictionary<string, PlayAnime> pa = new Dictionary<string, PlayAnime>();

            public MaidState()
            {
                pa["AHE.継続.0"] = new PlayAnime("AHE.継続.0", 1, 0.00f, 0.75f);
                pa["AHE.絶頂.0"] = new PlayAnime("AHE.絶頂.0", 2, 6.00f, 9.00f);
                pa["AHE.痙攣.0"] = new PlayAnime("AHE.痙攣.0", 1, 0.00f, 9.00f, PlayAnime.Formula.Convulsion);
                pa["AHE.痙攣.1"] = new PlayAnime("AHE.痙攣.1", 1, 0.00f, 10.00f, PlayAnime.Formula.Convulsion);
                pa["AHE.痙攣.2"] = new PlayAnime("AHE.痙攣.2", 1, 0.00f, 11.00f, PlayAnime.Formula.Convulsion);
            }

            public int idxAheOrgasm
            {
                get { return (int)Math.Min(Math.Max(Math.Floor((iOrgasmCount - 1) / 3f), 0), 2); }
            }
        }

        private Dictionary<int, MaidState> maidStates = new Dictionary<int, MaidState>();
        private float timer = 0f;
        private int selectedMaidIndex = 0;

        public AhegaoManager(YotogiCamControl plugin)
        {
            this.plugin = plugin;
        }

        public void Update()
        {
            if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;

            for (int i = 0; i < GameMain.Instance.CharacterMgr.GetMaidCount(); i++)
            {
                Maid m = GameMain.Instance.CharacterMgr.GetMaid(i);
                if (m != null && m.Visible && m.body0 != null && m.body0.isLoadedBody)
                {
                    UpdateMaid(i, m);
                }
            }
        }

        private void UpdateMaid(int index, Maid m)
        {
            if (!maidStates.ContainsKey(index)) maidStates[index] = new MaidState();
            var state = maidStates[index];

            // Initialize
            if (!state.initialized)
            {
                if (m.body0 != null)
                {
                     state.fAheDefEye = m.body0.trsEyeL.localPosition.y * state.fEyePosToSliderMul;
                     state.eyeY = state.fAheDefEye;

                     // Check Orgasm ShapeKey
                     foreach(var slot in m.body0.EnumerateGoSlot())
                     {
                         if(slot.morph != null && slot.morph.hash.ContainsKey("orgasm"))
                         {
                             state.bOrgasmAvailable = true;
                             break;
                         }
                     }
                }

                SetupSetters(m, state);
                state.initialized = true;
            }

            // Mind Break Logic
            if (state.mindBreakEnabled)
            {
                if (!state.isMindBreakApplied) BreakMaid(m, state);
                if (m.AudioMan != null && !m.AudioMan.audiosource.isPlaying)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0f) PlayBrokenVoice(m);
                }
            }
            else
            {
                if (state.isMindBreakApplied) RestoreMaid(m, state);
            }

            // Auto AHE Logic
            if (state.autoAheEnabled)
            {
                state.fPassedTimeOnCommand += Time.deltaTime;

                if (state.pa["AHE.継続.0"].NowPlaying) state.pa["AHE.継続.0"].Update();

                if (state.pa["AHE.絶頂.0"].NowPlaying)
                {
                    state.pa["AHE.絶頂.0"].Update();
                    m.FaceBlend(YotogiSliderConstants.sAheOrgasmFaceBlend[state.idxAheOrgasm]);
                }

                for (int j = 0; j < 3; j++)
                {
                     if (state.pa["AHE.痙攣." + j].NowPlaying) state.pa["AHE.痙攣." + j].Update();
                }

                // Eye decrement when idle
                if (!state.pa["AHE.継続.0"].NowPlaying && !state.pa["AHE.絶頂.0"].NowPlaying)
                {
                    // If eyeY is above default, lower it
                    float decrement = (0.20f / 60f) * (int)(state.fPassedTimeOnCommand / 10);
                    if (state.eyeY > state.fAheDefEye)
                    {
                        UpdateMaidEyePosY(m, state, state.eyeY - decrement);
                    }
                }
            }
        }

        private void SetupSetters(Maid m, MaidState state)
        {
            state.pa["AHE.継続.0"].SetSetter((v) => UpdateMaidEyePosY(m, state, v));
            state.pa["AHE.絶頂.0"].SetSetter((v) => UpdateAheOrgasm(m, state, v));
            for(int i=0; i<3; i++)
            {
                state.pa["AHE.痙攣." + i].SetSetter((v) => UpdateOrgasmConvulsion(m, state, v));
            }
        }

        public void OnCommand(Maid maid, CommonCommandData data)
        {
            // Find Index
            int index = -1;
            for(int i=0; i<GameMain.Instance.CharacterMgr.GetMaidCount(); i++)
            {
                if (GameMain.Instance.CharacterMgr.GetMaid(i) == maid)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1) return;

            if (!maidStates.ContainsKey(index)) maidStates[index] = new MaidState();
            var state = maidStates[index];
            if(!state.initialized) { state.initialized = true; SetupSetters(maid, state); }

            if (!state.autoAheEnabled) return;

            // Reset time
            state.fPassedTimeOnCommand = 0f;
            state.fAheLastEye = maid.body0.trsEyeL.localPosition.y * state.fEyePosToSliderMul;

            // Stop animations
            for (int i = 0; i < 1; i++)
            {
                if (state.pa["AHE.絶頂." + i].NowPlaying) state.pa["AHE.絶頂." + i].Stop();
                if (state.pa["AHE.継続." + i].NowPlaying) state.pa["AHE.継続." + i].Stop();
            }

            // Capture Excite
            int excite = maid.status.currentExcite;
            int idx = state.idxAheOrgasm;

            if (data.basic.command_type == Yotogi.SkillCommandType.絶頂)
            {
                if (state.iLastExcite >= YotogiSliderConstants.iAheExcite[idx])
                {
                    state.pa["AHE.継続.0"].Play(state.fAheLastEye, YotogiSliderConstants.fAheOrgasmEyeMax[idx]);

                    float[] xFrom = { YotogiSliderConstants.fAheOrgasmEyeMax[idx], YotogiSliderConstants.fAheOrgasmSpeed[idx] };
                    float[] xTo = { YotogiSliderConstants.fAheOrgasmEyeMin[idx], 100f };

                    state.pa["AHE.絶頂.0"].Play(xFrom, xTo);

                    if (state.convulsionEnabled)
                    {
                        if (state.pa["AHE.痙攣." + idx].NowPlaying) state.iAheOrgasmChain++;
                        state.pa["AHE.痙攣." + idx].Play(0f, YotogiSliderConstants.fAheOrgasmConvulsion[idx]);
                    }

                    state.iOrgasmCount++;

                    // Face Anime
                    maid.FaceAnime(YotogiSliderConstants.sAheOrgasmFace[idx], 5f, 0);
                }
            }
            else
            {
                if (excite >= YotogiSliderConstants.iAheExcite[idx])
                {
                    float to = YotogiSliderConstants.fAheNormalEyeMax[idx] * (excite - YotogiSliderConstants.iAheExcite[idx]) / (300f - YotogiSliderConstants.iAheExcite[idx]);
                    state.pa["AHE.継続.0"].Play(state.fAheLastEye, to);
                }
                else
                {
                     state.pa["AHE.継続.0"].Play(state.fAheLastEye, state.fAheDefEye);
                }
            }

            state.iLastExcite = excite;
        }

        // --- Updaters ---

        private void UpdateMaidEyePosY(Maid m, MaidState state, float value)
        {
            if (value < state.fAheDefEye) value = state.fAheDefEye;
            state.eyeY = value;

            Vector3 vl = m.body0.trsEyeL.localPosition;
            Vector3 vr = m.body0.trsEyeR.localPosition;
            m.body0.trsEyeL.localPosition = new Vector3(vl.x, value / state.fEyePosToSliderMul, vl.z);
            m.body0.trsEyeR.localPosition = new Vector3(vr.x, -value / state.fEyePosToSliderMul, vr.z);
        }

        private void UpdateAheOrgasm(Maid m, MaidState state, float[] x)
        {
            UpdateMaidEyePosY(m, state, x[0]);
            // Motion speed update could go here if we want to change animation speed during orgasm
        }

        private void UpdateOrgasmConvulsion(Maid m, MaidState state, float value)
        {
            if (state.bOrgasmAvailable)
            {
                 m.body0.VertexMorph_FromProcItem("orgasm", value / 100f);
            }
        }

        // --- Legacy Logic ---

        private void BreakMaid(Maid m, MaidState state)
        {
            state.isMindBreakApplied = true;
            m.SetProp(MPN.eye_hi, "", 0, true);
            m.SetProp(MPN.eye_hi_r, "", 0, true);

            if (plugin.masturbationManager != null && plugin.masturbationManager.sFaceAnimeStun.Length > 0)
            {
                string[] faces = plugin.masturbationManager.sFaceAnimeStun;
                string face = faces[UnityEngine.Random.Range(0, faces.Length)];
                m.FaceAnime(face, 1f, 0);
            }

            m.body0.boHeadToCam = false;
            m.body0.boEyeToCam = false;

            PlayBrokenVoice(m);
        }

        private void RestoreMaid(Maid m, MaidState state)
        {
            state.isMindBreakApplied = false;
            m.FaceAnime("通常", 1f, 0);
            m.body0.boHeadToCam = true;
            m.body0.boEyeToCam = true;
            m.ResetProp(MPN.eye_hi);
            m.ResetProp(MPN.eye_hi_r);
            m.AllProcPropSeqStart();

            if (m.AudioMan != null) m.AudioMan.Stop();
        }

        private void PlayBrokenVoice(Maid m)
        {
            if (plugin.masturbationManager == null) return;

            var mm = plugin.masturbationManager;
            int pIndex = Array.IndexOf(VoiceRepository.personalNames, m.status.personal.uniqueName);
            if (pIndex < 0 || pIndex >= mm.bvs.Length) pIndex = 0;

            if (mm.bvs[pIndex].sLoopVoice40Vibe.Length > 0)
            {
                int idx = Mathf.Clamp(3, 0, mm.bvs[pIndex].sLoopVoice40Vibe.Length - 1);
                string clip = mm.bvs[pIndex].sLoopVoice40Vibe[idx];
                m.AudioMan.LoadPlay(clip, 0f, false, false);
                timer = UnityEngine.Random.Range(3f, 6f);
            }
        }

        // --- UI ---

        public void DrawUI()
        {
            GUILayout.BeginVertical("box");

            if (GameMain.Instance.CharacterMgr == null) return;
            int count = GameMain.Instance.CharacterMgr.GetMaidCount();
            List<int> validMaids = new List<int>();
            for(int i=0; i<count; i++)
            {
                if (GameMain.Instance.CharacterMgr.GetMaid(i) != null) validMaids.Add(i);
            }

            if (validMaids.Count == 0)
            {
                GUILayout.Label("No Maids");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Maid:");
            if (selectedMaidIndex >= validMaids.Count) selectedMaidIndex = 0;
            string[] names = new string[validMaids.Count];
            for(int i=0; i<validMaids.Count; i++) names[i] = "Maid " + validMaids[i];

            selectedMaidIndex = GUILayout.Toolbar(selectedMaidIndex, names);
            GUILayout.EndHorizontal();

            int realIndex = validMaids[selectedMaidIndex];
            Maid m = GameMain.Instance.CharacterMgr.GetMaid(realIndex);

            if (!maidStates.ContainsKey(realIndex)) maidStates[realIndex] = new MaidState();
            var state = maidStates[realIndex];
            if (!state.initialized && m.body0 != null && m.body0.isLoadedBody) { state.initialized = true; SetupSetters(m, state); }

            GUILayout.Space(5);
            GUILayout.Label("YotogiSlider Ahegao Features:");
            state.autoAheEnabled = GUILayout.Toggle(state.autoAheEnabled, "Enable Auto Ahegao (Eye Roll / Orgasm Face)");
            if (state.autoAheEnabled)
            {
                state.convulsionEnabled = GUILayout.Toggle(state.convulsionEnabled, "Enable Orgasm Convulsions");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Eye Y", GUILayout.Width(60));
                float newVal = GUILayout.HorizontalSlider(state.eyeY, state.fAheDefEye, 100f);
                GUILayout.Label(newVal.ToString("F1"), GUILayout.Width(30));
                GUILayout.EndHorizontal();

                if (newVal != state.eyeY)
                {
                    UpdateMaidEyePosY(m, state, newVal);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Legacy Mind Break:");
            state.mindBreakEnabled = GUILayout.Toggle(state.mindBreakEnabled, "Enable Mind Break (Dead Eyes / Stun)");

            GUILayout.EndVertical();
        }
    }
}
