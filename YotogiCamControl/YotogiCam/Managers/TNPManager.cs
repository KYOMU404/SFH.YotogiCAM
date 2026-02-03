using System;
using UnityEngine;
using COM3D2.YotogiCamControl.Plugin.UI;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class TNPManager
    {
        private YotogiCamControl plugin;
        private string[] penisModels = {
            "bone_tinpo_z5_i_.menu",
            "bone_tinpo_z6_i_.menu",
            "bone_tinpo_z7_i_.menu",
            "bone_tinpo_z8_i_.menu"
        };

        private string statusMessage = "";

        public TNPManager(YotogiCamControl plugin)
        {
            this.plugin = plugin;
        }

        public void DrawUI()
        {
            GUILayout.BeginVertical(StyleManager.BoxStyle);
            GUILayout.Label("TNP (Male Model Control)");

            if (GameMain.Instance.CharacterMgr == null)
            {
                GUILayout.Label("Error: CharacterMgr is null");
                GUILayout.EndVertical();
                return;
            }

            // Get the Man (Maid 0 is usually male in yotogi if configured, but GetMan(0) is safer)
            Maid man = GameMain.Instance.CharacterMgr.GetMan(0);

            if (man == null)
            {
                GUILayout.Label("No Man found in scene.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Select Model:");

            foreach (string modelMenu in penisModels)
            {
                if (GUILayout.Button(modelMenu))
                {
                    ChangePenisModel(man, modelMenu);
                }
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(10);
                GUILayout.Label(statusMessage);
            }

            GUILayout.EndVertical();
        }

        private void ChangePenisModel(Maid man, string menuFile)
        {
            try
            {
                // MAN Category moza corresponds to MPN.moza
                Debug.Log($"[YotogiCamControl] TNP: Setting {menuFile} to Man {man.status.fullNameEnStyle}");

                // SetProp(MPN mpn, string filename, int subPropId, bool temp)
                man.SetProp(MPN.moza, menuFile, 0, false);

                // Force Update
                man.AllProcProp();

                // Ensure visibility
                if (man.body0 != null)
                {
                    man.body0.SetMask(MPN.moza, true);
                    // Force rebuild of parts if needed
                    man.body0.FixMaskFlag();
                }

                statusMessage = "Applied: " + menuFile;
            }
            catch (Exception ex)
            {
                statusMessage = "Error: " + ex.Message;
                Debug.LogError("YotogiCamControl TNP Error: " + ex.ToString());
            }
        }
    }
}
