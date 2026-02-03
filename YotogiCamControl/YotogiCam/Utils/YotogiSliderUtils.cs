using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yotogis;
using HarmonyLib;

namespace COM3D2.YotogiCamControl.Plugin.Utils
{
    // Enums
    public enum TunLevel
    {
        None = -1,
        Friction = 0,
        Petting = 1,
        Nip = 2,
    }

    public enum KupaLevel
    {
        None = -1,
        Sex = 0,
        Vibe = 1,
    }

    // Constants
    public static class YotogiSliderConstants
    {
        // AutoAHE Defaults
        public static readonly int[] iAheExcite = new int[] { 267, 233, 200 };
        public static readonly float[] fAheNormalEyeMax = new float[] { 40f, 45f, 50f };
        public static readonly float[] fAheOrgasmEyeMax = new float[] { 50f, 60f, 70f };
        public static readonly float[] fAheOrgasmEyeMin = new float[] { 30f, 35f, 40f };
        public static readonly float[] fAheOrgasmSpeed = new float[] { 90f, 80f, 70f };
        public static readonly float[] fAheOrgasmConvulsion = new float[] { 60f, 80f, 100f };
        public static readonly string[] sAheOrgasmFace = new string[] { "エロ放心", "エロ好感３", "通常射精後１" };
        public static readonly string[] sAheOrgasmFaceBlend = new string[] { "頬１涙１", "頬２涙２", "頬３涙３よだれ" };

        // AutoKUPA Defaults
        public static readonly int[] iKupaValue = { 100, 50 };
        public static readonly int[] iAnalKupaValue = { 100, 50 };
    }

    // TBodyExtensions
    public static class TBodyExtensions
    {
        static FieldInfo TBody_goSlot_FieldInfo;
        static FieldInfo Slot_m_slots_FieldInfo;
        static bool isCom3d25 = false;

        static TBodyExtensions()
        {
            try
            {
                TBody_goSlot_FieldInfo = AccessTools.Field(typeof(TBody), "goSlot");

                if (TBody_goSlot_FieldInfo != null && TBody_goSlot_FieldInfo.FieldType != typeof(List<TBodySkin>))
                {
                    isCom3d25 = true;
                    Slot_m_slots_FieldInfo = AccessTools.Field(
                        TBody_goSlot_FieldInfo.FieldType,
                        "m_slots");
                }
            }
            catch (Exception) { }
        }

        static List<TBodySkin> GoSlot_20(TBody tbody)
        {
            return TBody_goSlot_FieldInfo.GetValue(tbody) as List<TBodySkin>;
        }

        static List<List<TBodySkin>> GoSlot_25(TBody tbody)
        {
            var goSlot_obj = TBody_goSlot_FieldInfo.GetValue(tbody);
            return Slot_m_slots_FieldInfo.GetValue(goSlot_obj) as List<List<TBodySkin>>;
        }

        public static IEnumerable<TBodySkin> EnumerateGoSlot(this TBody tbody)
        {
            if (isCom3d25)
            {
                if (Slot_m_slots_FieldInfo == null) yield break;
                var list = GoSlot_25(tbody);
                if (list != null)
                {
                    foreach (var slot in list)
                    {
                        if (slot != null && slot.Count > 0) yield return slot[0];
                    }
                }
            }
            else
            {
                if (TBody_goSlot_FieldInfo == null) yield break;
                var list = GoSlot_20(tbody);
                if (list != null)
                {
                    foreach (var slot in list)
                    {
                        yield return slot;
                    }
                }
            }
        }

        public static bool VertexMorph_FromProcItem(this TBody body, string sTag, float f)
        {
            bool bFace = false;
            var i = 0;
            foreach (var slot in body.EnumerateGoSlot())
            {
                TMorph morph = slot.morph;
                if (morph != null)
                {
                    if (morph.Contains(sTag))
                    {
                        if (i == 1)
                        {
                            bFace = true;
                        }
                        if (slot.morph.hash.ContainsKey(sTag))
                        {
                            int h = (int)slot.morph.hash[sTag];
                            slot.morph.SetBlendValues(h, f);
                            slot.morph.FixBlendValues();
                        }
                    }
                }

                i++;
            }
            return bFace;
        }
    }

    // PlayAnime Class
    public class PlayAnime
    {
        public enum Formula
        {
            Linear,
            Quadratic,
            Convulsion
        }

        private float[] value;
        private float[] vFrom;
        private float[] vTo;
        private Formula type;
        private int num;
        private bool play = false;
        private float passedTime = 0f;
        private float startTime = 0f;
        private float finishTime = 0f;
        public float progress { get { return (passedTime - startTime) / (finishTime - startTime); } }

        private Action<float> setValue0 = null;
        private Action<float[]> setValue = null;

        public string Name;
        public string Key { get { return (Name.Split('.'))[0]; } }
        public bool NowPlaying { get { return play && (passedTime < finishTime); } }
        public bool SetterExist { get { return (num == 1) ? (setValue0 != null) : (setValue != null); } }


        public PlayAnime(string name, int n, float st, float ft) : this(name, n, st, ft, Formula.Linear) { }
        public PlayAnime(string name, int n, float st, float ft, Formula t)
        {
            Name = name;
            num = n;
            value = new float[n];
            vFrom = new float[n];
            vTo = new float[n];
            startTime = st;
            finishTime = ft;
            type = t;
        }

        public bool IsKye(string s) { return s == Key; }
        public bool Contains(string s) { return Name.Contains(s); }

        public void SetFrom(float vform) { vFrom[0] = vform; }
        public void SetTo(float vto) { vTo[0] = vto; }
        public void SetSetter(Action<float> func) { setValue0 = func; }
        public void Set(float vform, float vto) { SetFrom(vform); SetTo(vto); }

        public void SetFrom(float[] vform) { if (vform.Length == num) Array.Copy(vform, vFrom, num); }
        public void SetTo(float[] vto) { if (vto.Length == num) Array.Copy(vto, vTo, num); }
        public void SetSetter(Action<float[]> func) { setValue = func; }
        public void Set(float[] vform, float[] vto) { SetFrom(vform); SetTo(vto); }

        public void Play()
        {
            if (SetterExist)
            {
                passedTime = 0f;
                play = true;
            }
        }
        public void Play(float vform, float vto) { Set(vform, vto); Play(); }
        public void Play(float[] vform, float[] vto) { Set(vform, vto); Play(); }

        public void Stop() { play = false; }

        public void Update()
        {
            if (play)
            {
                bool change = false;

                for (int i = 0; i < num; i++)
                {
                    if (vFrom[i] == vTo[i]) continue;

                    if (passedTime >= finishTime)
                    {
                        Stop();
                    }
                    else if (passedTime >= startTime)
                    {
                        switch (type)
                        {
                            case Formula.Linear:
                                {
                                    value[i] = vFrom[i] + (vTo[i] - vFrom[i]) * progress;
                                    change = true;
                                }
                                break;

                            case Formula.Quadratic:
                                {
                                    value[i] = vFrom[i] + (vTo[i] - vFrom[i]) * Mathf.Pow(progress, 2);
                                    change = true;
                                }
                                break;

                            case Formula.Convulsion:
                                {
                                    float t = Mathf.Pow(progress + 0.05f * UnityEngine.Random.value, 2f) * 2f * Mathf.PI * 6f;

                                    value[i] = (vTo[i] - vFrom[i])
                                            * Mathf.Clamp(Mathf.Clamp(Mathf.Pow((Mathf.Cos(t - Mathf.PI / 2f) + 1f) / 2f, 3f) * Mathf.Pow(1f - progress, 2f) * 4f, 0f, 1f)
                                                            + Mathf.Sin(t * 3f) * 0.1f * Mathf.Pow(1f - progress, 3f), 0f, 1f);

                                    if (progress < 0.03f) value[i] *= Mathf.Pow(1f - (0.03f - progress) * 33f, 2f);
                                    change = true;

                                }
                                break;

                            default: break;
                        }
                    }
                }

                if (change)
                {
                    if (num == 1) setValue0(value[0]);
                    else setValue(value);
                }
            }

            passedTime += Time.deltaTime;
        }
    }

    // CommonCommandData Class
    public class CommonCommandData
    {
        public class Basic
        {
            private static readonly Dictionary<YotogiOld.SkillCommandType, Yotogi.SkillCommandType> dicSkillCommandType =
                new Dictionary<YotogiOld.SkillCommandType, Yotogi.SkillCommandType>()
                {
                    { YotogiOld.SkillCommandType.単発, Yotogi.SkillCommandType.単発 },
                    { YotogiOld.SkillCommandType.単発_挿入, Yotogi.SkillCommandType.単発_挿入 },
                    { YotogiOld.SkillCommandType.挿入, Yotogi.SkillCommandType.挿入 },
                    { YotogiOld.SkillCommandType.止める, Yotogi.SkillCommandType.止める },
                    { YotogiOld.SkillCommandType.絶頂, Yotogi.SkillCommandType.絶頂 },
                    { YotogiOld.SkillCommandType.継続, Yotogi.SkillCommandType.継続 }
                };

            public string name { get; private set; }
            public string group_name { get; private set; }
            public Yotogi.SkillCommandType command_type { get; private set; }

            public Basic(Skill.Data.Command.Data.Basic basic)
            {
                SetBasic(basic);
            }

            public Basic(Skill.Old.Data.Command.Data.Basic basic)
            {
                SetBasic(basic);
            }

            public void SetBasic(Skill.Data.Command.Data.Basic basic)
            {
                this.name = basic.name;
                this.group_name = basic.group_name;
                this.command_type = basic.command_type;
            }

            public void SetBasic(Skill.Old.Data.Command.Data.Basic basic)
            {
                this.name = basic.name;
                this.group_name = basic.group_name;
                this.command_type = dicSkillCommandType[basic.command_type];
            }
        }

        public class Status
        {
            public int frustration { get; private set; }

            public Status(Skill.Data.Command.Data.Status status)
            {
            }

            public Status(Skill.Old.Data.Command.Data.Status status)
            {
                SetStatus(status);
            }

            public void SetStatus(Skill.Data.Command.Data.Status status)
            {
            }

            public void SetStatus(Skill.Old.Data.Command.Data.Status status)
            {
                this.frustration = status.frustration;
            }
        }

        private readonly Basic basic_;
        private readonly Status status_;

        public Basic basic { get { return basic_; } }
        public Status status { get { return status_; } }

        public string skillName { get; private set; }

        public CommonCommandData(Skill.Data.Command.Data data)
        {
            this.basic_ = new Basic(data.basic);
            this.status_ = new Status(data.status);
            this.skillName = data.basic.skill.name;
        }

        public CommonCommandData(Skill.Old.Data.Command.Data data)
        {
            this.basic_ = new Basic(data.basic);
            this.status_ = new Status(data.status);

            Skill.Old.Data skillOldData = Skill.Old.Get(data.basic.skill_id);
            this.skillName = (skillOldData != null) ? skillOldData.name : string.Empty;
        }

        public void SetData(Skill.Data.Command.Data data)
        {
            this.basic.SetBasic(data.basic);
            this.status.SetStatus(data.status);
            this.skillName = data.basic.skill.name;
        }

        public void SetData(Skill.Old.Data.Command.Data data)
        {
            this.basic.SetBasic(data.basic);
            this.status.SetStatus(data.status);

            Skill.Old.Data skillOldData = Skill.Old.Get(data.basic.skill_id);
            this.skillName = (skillOldData != null) ? skillOldData.name : string.Empty;
        }
    }
}
