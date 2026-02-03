using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
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
