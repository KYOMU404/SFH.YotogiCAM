using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using COM3D2.YotogiCamControl.Plugin.Utils;
using COM3D2.YotogiCamControl.Plugin.UI;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class KupaManager
    {
        private YotogiCamControl plugin;

        public class MaidState
        {
            public bool initialized = false;
            public bool isVisible = false;

            // Sliders
            public float kupa = 0f;
            public float analKupa = 0f;
            public float kupaLevel = 70f;
            public float labiaKupa = 0f;
            public float vaginaKupa = 0f;
            public float nyodoKupa = 0f;
            public float suji = 0f;
            public float clitoris = 0f;

            // Toggles
            public bool autoKupaEnabled = true;

            // Logic State
            public bool bKupaFuck = false;
            public bool bAnalKupaFuck = false;
            public int iKupaMin = 0;
            public int iAnalKupaMin = 0;

            // Waiting
            public float fPassedTimeOnAutoKupaWaiting = 0f;
            public float fPassedTimeOnAutoAnalKupaWaiting = 0f;
            public int iKupaWaitingValue = 5;
            public int iAnalKupaWaitingValue = 5;

            // Animations
            public Dictionary<string, PlayAnime> pa = new Dictionary<string, PlayAnime>();

            // Availability
            public bool bKupaAvailable = false;
            public bool bAnalKupaAvailable = false;
            public bool bLabiaKupaAvailable = false;
            public bool bVaginaKupaAvailable = false;
            public bool bNyodoKupaAvailable = false;
            public bool bSujiAvailable = false;
            public bool bClitorisAvailable = false;

            public MaidState()
            {
                // Initialize PlayAnime instances
                pa["KUPA.挿入.0"] = new PlayAnime("KUPA.挿入.0", 1, 0.50f, 1.50f);
                pa["KUPA.挿入.1"] = new PlayAnime("KUPA.挿入.1", 1, 1.50f, 2.50f);
                pa["KUPA.止める"] = new PlayAnime("KUPA.止める", 1, 0.00f, 2.00f);
                pa["AKPA.挿入.0"] = new PlayAnime("AKPA.挿入.0", 1, 0.50f, 1.50f);
                pa["AKPA.挿入.1"] = new PlayAnime("AKPA.挿入.1", 1, 1.50f, 2.50f);
                pa["AKPA.止める"] = new PlayAnime("AKPA.止める", 1, 0.00f, 2.00f);
                pa["KUPACL.剥く.0"] = new PlayAnime("KUPACL.剥く.0", 1, 0.00f, 0.30f);
                pa["KUPACL.剥く.1"] = new PlayAnime("KUPACL.剥く.1", 1, 0.20f, 0.60f);
                pa["KUPACL.被る"] = new PlayAnime("KUPACL.被る", 1, 0.00f, 0.40f);
            }
        }

        private Dictionary<int, MaidState> maidStates = new Dictionary<int, MaidState>();
        private int selectedMaidIndex = 0;

        public KupaManager(YotogiCamControl plugin)
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

            if (!state.initialized)
            {
                CheckAvailability(m, state);
                SetupSetters(m, state);
                state.initialized = true;
            }

            if (!state.autoKupaEnabled) return;

            // Update Animations
            foreach (var kvp in state.pa)
            {
                if (kvp.Value.NowPlaying)
                {
                    kvp.Value.Update();
                }
            }

            // Waiting Motion
            bool updated = false;
            string[] animNames = {
                "KUPA.挿入.0", "KUPA.挿入.1", "KUPA.止める",
                "AKPA.挿入.0", "AKPA.挿入.1", "AKPA.止める",
                "KUPACL.剥く.0", "KUPACL.剥く.1", "KUPACL.被る",
            };
            foreach (var name in animNames)
            {
                if (state.pa[name].NowPlaying) updated = true;
            }

            if (state.bKupaAvailable && state.iKupaWaitingValue > 0)
            {
                if (!updated && state.kupa > 0)
                {
                    state.fPassedTimeOnAutoKupaWaiting += Time.deltaTime;
                    float f2rad = 180f * state.fPassedTimeOnAutoKupaWaiting * Mathf.Deg2Rad;
                    // Note: SyncMotionSpeed logic omitted for simplicity or assuming standard speed 1.0 if not accessing animation speed
                    float freq = 1f;
                    float value = state.kupa + state.iKupaWaitingValue * (1f + Mathf.Sin(freq * f2rad)) / 2f;
                    m.body0.VertexMorph_FromProcItem("kupa", value / 100f);
                }
                else
                {
                    state.fPassedTimeOnAutoKupaWaiting = 0;
                }
            }
            if (state.bAnalKupaAvailable && state.iAnalKupaWaitingValue > 0)
            {
                if (!updated && state.analKupa > 0)
                {
                    state.fPassedTimeOnAutoAnalKupaWaiting += Time.deltaTime;
                    float f2rad = 180f * state.fPassedTimeOnAutoAnalKupaWaiting * Mathf.Deg2Rad;
                    float freq = 1f;
                    float value = state.analKupa + state.iAnalKupaWaitingValue * (1f + Mathf.Sin(freq * f2rad)) / 2f;
                    m.body0.VertexMorph_FromProcItem("analkupa", value / 100f);
                }
                else
                {
                    state.fPassedTimeOnAutoAnalKupaWaiting = 0;
                }
            }
        }

        private void CheckAvailability(Maid m, MaidState state)
        {
            if (m.body0 == null) return;
            // Assuming we check slot 0 (body) for morphs

            // Note: GetGoSlot is extension or native. TBodyExtensions provides it.
            // But TBodyExtensions from YotogiSlider was not strictly ported, I implemented EnumerateGoSlot.
            // I'll use EnumerateGoSlot to find morphs.

            foreach(var s in m.body0.EnumerateGoSlot())
            {
                if (s.morph != null && s.morph.hash != null)
                {
                    if (s.morph.hash.ContainsKey("kupa")) state.bKupaAvailable = true;
                    if (s.morph.hash.ContainsKey("analkupa")) state.bAnalKupaAvailable = true;
                    if (s.morph.hash.ContainsKey("labiakupa")) state.bLabiaKupaAvailable = true;
                    if (s.morph.hash.ContainsKey("vaginakupa")) state.bVaginaKupaAvailable = true;
                    if (s.morph.hash.ContainsKey("nyodokupa")) state.bNyodoKupaAvailable = true;
                    if (s.morph.hash.ContainsKey("suji")) state.bSujiAvailable = true;
                    if (s.morph.hash.ContainsKey("clitoris")) state.bClitorisAvailable = true;
                }
            }
        }

        private void SetupSetters(Maid m, MaidState state)
        {
            foreach (var kvp in state.pa)
            {
                var p = kvp.Value;
                if (p.Contains("KUPA") && !p.Contains("KUPACL")) p.SetSetter((v) => UpdateShapeKeyKupa(m, state, v));
                if (p.Contains("AKPA")) p.SetSetter((v) => UpdateShapeKeyAnalKupa(m, state, v));
                if (p.Contains("KUPACL")) p.SetSetter((v) => UpdateShapeKeyClitoris(m, state, v));
            }
        }

        public void OnCommand(Maid maid, CommonCommandData data)
        {
            // Find maid index
            int index = -1;
            for(int i=0; i<GameMain.Instance.CharacterMgr.GetMaidCount(); i++)
            {
                if (GameMain.Instance.CharacterMgr.GetMaid(i) == maid)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1) return; // Should not happen if maid is valid

            if (!maidStates.ContainsKey(index)) maidStates[index] = new MaidState();
            var state = maidStates[index];
            if (!state.initialized) { CheckAvailability(maid, state); SetupSetters(maid, state); state.initialized = true; }

            if (!state.autoKupaEnabled) return;

            // Logic ported from YotogiSlider playAnimeOnCommand

            // Kupa
            float fromKupa = state.kupa;
            int kupaLvl = (int)CheckCommandKupaLevel(data.basic);
            if (kupaLvl >= 0)
            {
                int target = (int)(YotogiSliderConstants.iKupaValue[kupaLvl] * state.kupaLevel / 100f);
                if (fromKupa < target)
                {
                    state.pa["KUPA.挿入." + kupaLvl].Play(fromKupa, target);
                }
                state.bKupaFuck = true;
            }
            else if (state.bKupaFuck && CheckCommandKupaStop(data.basic))
            {
                state.pa["KUPA.止める"].Play(fromKupa, state.iKupaMin);
                state.bKupaFuck = false;
            }

            // Anal Kupa
            float fromAnal = state.analKupa;
            int analLvl = (int)CheckCommandAnalKupaLevel(data.basic);
            if (analLvl >= 0)
            {
                int target = (int)(YotogiSliderConstants.iAnalKupaValue[analLvl] * state.kupaLevel / 100f);
                if (fromAnal < target)
                {
                    state.pa["AKPA.挿入." + analLvl].Play(fromAnal, target);
                }
                state.bAnalKupaFuck = true;
            }
            else if (state.bAnalKupaFuck && CheckCommandAnalKupaStop(data.basic))
            {
                state.pa["AKPA.止める"].Play(fromAnal, state.iAnalKupaMin);
                state.bAnalKupaFuck = false;
            }

            // Clitoris
            if (state.bClitorisAvailable)
            {
                // Simplified excite check (assuming exite is accessible via maid.status)
                float excite = maid.status.currentExcite;
                float offset = 0f;
                float clitorisLong = 30f;
                if (excite < 120f) { offset = 0f; clitorisLong = 30f; }
                else if (excite < 210f) { offset = 40f; clitorisLong = 30f; }
                else if (excite < 300f) { offset = 70f; clitorisLong = 40f; }
                else { offset = 100f; clitorisLong = 50f; }

                if (data.basic.name.Contains("クリトリス") || data.basic.name.Contains("オナニー") || data.basic.group_name.Contains("バイブを舐めさせる")
                    || data.basic.group_name.Contains("オナニー") || (data.basic.group_name.StartsWith("洗い") && (data.basic.name.Contains("洗わせる") || data.basic.name.Contains("たわし洗い"))))
                {
                    if (!state.pa["KUPACL.剥く.1"].NowPlaying)
                    {
                        state.pa["KUPACL.剥く.1"].Play(0f + offset, clitorisLong + offset);
                    }
                }
                else
                {
                    if (!state.pa["KUPACL.剥く.0"].NowPlaying && !state.pa["KUPACL.剥く.1"].NowPlaying
                       && (data.basic.command_type == Yotogi.SkillCommandType.絶頂 || data.basic.name.Contains("強く責める"))
                       && state.clitoris < (clitorisLong - 10f + offset))
                    {
                        state.pa["KUPACL.剥く.0"].Play(0f + offset, clitorisLong + offset);
                    }
                    else if (!state.pa["KUPACL.被る"].NowPlaying && data.basic.command_type == Yotogi.SkillCommandType.止める
                            && state.clitoris > (clitorisLong - 10f + offset))
                    {
                        state.pa["KUPACL.被る"].Play(clitorisLong + offset, 0f + offset);
                    }
                }
            }
        }

        // --- Setters / Morph Updaters ---

        private void UpdateShapeKeyKupa(Maid m, MaidState state, float value)
        {
            state.kupa = value;
            m.body0.VertexMorph_FromProcItem("kupa", value / 100f);
        }

        private void UpdateShapeKeyAnalKupa(Maid m, MaidState state, float value)
        {
            state.analKupa = value;
            m.body0.VertexMorph_FromProcItem("analkupa", value / 100f);
        }

        private void UpdateShapeKeyClitoris(Maid m, MaidState state, float value)
        {
            state.clitoris = value;
            m.body0.VertexMorph_FromProcItem("clitoris", value / 100f);
        }

        private void UpdateShapeKeyLabia(Maid m, MaidState state, float value)
        {
            state.labiaKupa = value;
            m.body0.VertexMorph_FromProcItem("labiakupa", value / 100f);
        }

        private void UpdateShapeKeyVagina(Maid m, MaidState state, float value)
        {
            state.vaginaKupa = value;
            m.body0.VertexMorph_FromProcItem("vaginakupa", value / 100f);
        }

        private void UpdateShapeKeyNyodo(Maid m, MaidState state, float value)
        {
            state.nyodoKupa = value;
            m.body0.VertexMorph_FromProcItem("nyodokupa", value / 100f);
        }

        private void UpdateShapeKeySuji(Maid m, MaidState state, float value)
        {
            state.suji = value;
            m.body0.VertexMorph_FromProcItem("suji", value / 100f);
        }

        // --- Logic Checks (Ported) ---

        private KupaLevel CheckCommandKupaLevel(CommonCommandData.Basic cmd)
        {
            if (cmd.command_type == Yotogi.SkillCommandType.挿入)
            {
                if (cmd.group_name.Contains("素股")) return KupaLevel.None;
                if (!cmd.group_name.Contains("アナル"))
                {
                    string[] t0 = { "セックス", "太バイブ", "正常位", "後背位", "騎乗位", "立位", "側位", "座位", "駅弁", "寝バック" };
                    if (t0.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Sex;

                    string[] t1 = { "愛撫", "オナニー", "バイブ", "シックスナイン", "ポーズ維持プレイ", "磔プレイ" };
                    if (t1.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Vibe;
                }
                else
                {
                    if (cmd.group_name.Contains("アナルバイブ責めセックス")) return KupaLevel.Sex;
                }
            }
            else if (cmd.group_name.Contains("三角木馬"))
            {
                if (cmd.name.Contains("肩を押す")) return KupaLevel.Vibe;
            }
            else if (cmd.group_name.Contains("まんぐり"))
            {
                if (cmd.name.Contains("愛撫") || cmd.name.Contains("クンニ")) return KupaLevel.Vibe;
            }
            if (!cmd.group_name.Contains("アナル"))
            {
                if (cmd.name.Contains("指を増やして")) return KupaLevel.Sex;
                if (cmd.group_name.Contains("バイブ") || cmd.group_name.Contains("オナニー"))
                {
                    if (cmd.name == "イカせる") return KupaLevel.Vibe;
                }
            }
            return KupaLevel.None;
        }

        private KupaLevel CheckCommandAnalKupaLevel(CommonCommandData.Basic cmd)
        {
            if (cmd.group_name.StartsWith("アナルバイブ責めセックス")) return KupaLevel.None;
            if (cmd.command_type == Yotogi.SkillCommandType.挿入)
            {
                string[] t0 = { "アナルセックス", "アナル正常位", "アナル後背位", "アナル騎乗位", "2穴", "4P", "アナル処女喪失", "アナル処女再喪失", "アナル寝バック", "アナル駅弁", "アナル座位"};
                if (t0.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Sex;

                string[] t1 = { "アナルバイブ", "アナルオナニー" };
                if (t1.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Vibe;
            }
            if (cmd.group_name.Contains("アナルバイブ"))
            {
                if (cmd.name == "イカせる") return KupaLevel.Vibe;
            }
            return KupaLevel.None;
        }

        private bool CheckCommandKupaStop(CommonCommandData.Basic cmd)
        {
            if (cmd.group_name == "まんぐり返しアナルセックス")
            {
                if (cmd.name.Contains("責める")) return true;
            }
            return CheckCommandAnyKupaStop(cmd);
        }

        private bool CheckCommandAnalKupaStop(CommonCommandData.Basic cmd)
        {
            if (cmd.command_type == Yotogi.SkillCommandType.絶頂)
            {
                if (cmd.group_name.Contains("オナニー")) return true;
            }
            return CheckCommandAnyKupaStop(cmd);
        }

        private bool CheckCommandAnyKupaStop(CommonCommandData.Basic cmd)
        {
            if (cmd.command_type == Yotogi.SkillCommandType.止める) return true;
            if (cmd.command_type == Yotogi.SkillCommandType.絶頂)
            {
                if (cmd.group_name.Contains("愛撫")) return true;
                if (cmd.group_name.Contains("まんぐり")) return true;
                if (cmd.group_name.Contains("シックスナイン")) return true;
                if (cmd.name.Contains("外出し")) return true;
            }
            else
            {
                if (cmd.name.Contains("頭を撫でる")) return true;
                if (cmd.name.Contains("口を責める")) return true;
                if (cmd.name.Contains("クリトリスを責めさせる")) return true;
                if (cmd.name.Contains("バイブを舐めさせる")) return true;
                if (cmd.name.Contains("擦りつける")) return true;
                if (cmd.name.Contains("放尿させる")) return true;
            }
            return false;
        }

        // --- UI ---

        public void DrawUI()
        {
            GUILayout.BeginVertical(StyleManager.BoxStyle);

            if (GameMain.Instance.CharacterMgr == null)
            {
                GUILayout.Label("Character Manager is null");
                GUILayout.EndVertical();
                return;
            }

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
            string[] names = validMaids.Select(i => "Maid " + i).ToArray();
            selectedMaidIndex = GUILayout.Toolbar(selectedMaidIndex, names);
            GUILayout.EndHorizontal();

            int realIndex = validMaids[selectedMaidIndex];
            Maid m = GameMain.Instance.CharacterMgr.GetMaid(realIndex);

            if (!maidStates.ContainsKey(realIndex)) maidStates[realIndex] = new MaidState();
            var state = maidStates[realIndex];
            if (!state.initialized && m.body0 != null && m.body0.isLoadedBody)
            {
                CheckAvailability(m, state);
                SetupSetters(m, state);
                state.initialized = true;
            }

            // Controls
            state.autoKupaEnabled = GUILayout.Toggle(state.autoKupaEnabled, "Enable Auto Kupa Logic");

            GUILayout.Space(5);

            if (state.bKupaAvailable)
            {
                DrawSlider("Kupa", ref state.kupa, 0f, 150f, (v) => UpdateShapeKeyKupa(m, state, v));
            }
            if (state.bAnalKupaAvailable)
            {
                DrawSlider("Anal Kupa", ref state.analKupa, 0f, 150f, (v) => UpdateShapeKeyAnalKupa(m, state, v));
            }

            if (state.bKupaAvailable || state.bAnalKupaAvailable)
            {
                DrawSlider("Kupa Level %", ref state.kupaLevel, 0f, 100f, null);
            }

            if (state.bLabiaKupaAvailable) DrawSlider("Labia", ref state.labiaKupa, 0f, 150f, (v) => UpdateShapeKeyLabia(m, state, v));
            if (state.bVaginaKupaAvailable) DrawSlider("Vagina", ref state.vaginaKupa, 0f, 150f, (v) => UpdateShapeKeyVagina(m, state, v));
            if (state.bNyodoKupaAvailable) DrawSlider("Nyodo", ref state.nyodoKupa, 0f, 150f, (v) => UpdateShapeKeyNyodo(m, state, v));
            if (state.bSujiAvailable) DrawSlider("Suji", ref state.suji, 0f, 150f, (v) => UpdateShapeKeySuji(m, state, v));
            if (state.bClitorisAvailable) DrawSlider("Clitoris", ref state.clitoris, 0f, 150f, (v) => UpdateShapeKeyClitoris(m, state, v));

            GUILayout.EndVertical();
        }

        private void DrawSlider(string label, ref float val, float min, float max, Action<float> onChange)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(80));
            float newVal = GUILayout.HorizontalSlider(val, min, max);
            GUILayout.Label(newVal.ToString("F0"), GUILayout.Width(30));
            if (GUILayout.Button("R", GUILayout.Width(20))) newVal = 0f;
            GUILayout.EndHorizontal();

            if (newVal != val)
            {
                val = newVal;
                if (onChange != null) onChange(val);
            }
        }
    }
}
