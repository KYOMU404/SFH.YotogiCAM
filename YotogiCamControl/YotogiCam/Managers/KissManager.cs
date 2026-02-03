using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
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
            foreach (var kvp in maidKissingState) if (kvp.Value) anyKissing = true;
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
}
