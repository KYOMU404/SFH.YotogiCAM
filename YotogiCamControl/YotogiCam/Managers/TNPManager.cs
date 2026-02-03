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
        private int selectedManIndex = 0;

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

            // Man Selector
            int manCount = GameMain.Instance.CharacterMgr.GetManCount();
            if (manCount == 0)
            {
                GUILayout.Label("No Men found in scene.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Select Man:");

            // Build list of valid men
            System.Collections.Generic.List<int> validIndices = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<string> manNames = new System.Collections.Generic.List<string>();

            for(int i=0; i<manCount; i++)
            {
                Maid m = GameMain.Instance.CharacterMgr.GetMan(i);
                if (m != null && m.body0 != null)
                {
                    validIndices.Add(i);
                    string name = !string.IsNullOrEmpty(m.status.fullNameEnStyle) ? m.status.fullNameEnStyle : "Man " + i;
                    manNames.Add(name);
                }
            }

            if (validIndices.Count == 0)
            {
                GUILayout.Label("No valid Men loaded.");
                GUILayout.EndVertical();
                return;
            }

            if (selectedManIndex >= validIndices.Count) selectedManIndex = 0;
            selectedManIndex = GUILayout.Toolbar(selectedManIndex, manNames.ToArray());

            int realManIndex = validIndices[selectedManIndex];
            Maid man = GameMain.Instance.CharacterMgr.GetMan(realManIndex);

            GUILayout.Space(5);
            GUILayout.Label($"Target: {man.status.fullNameEnStyle}");
            GUILayout.Label("Select Penis Model:");

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
                    man.body0.SetMask(TBody.SlotID.moza, true);
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
