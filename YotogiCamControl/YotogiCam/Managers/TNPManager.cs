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
        private string searchName = "Tovar";
        private bool searchMode = false;

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

            // Search Mode Toggle
            GUILayout.BeginHorizontal();
            GUILayout.Label("Find Man Name:", GUILayout.Width(100));
            searchName = GUILayout.TextField(searchName);
            if (GUILayout.Button("Search", GUILayout.Width(60))) searchMode = true;
            GUILayout.EndHorizontal();

            Maid man = null;

            if (searchMode && !string.IsNullOrEmpty(searchName))
            {
                man = FindMaidByName(searchName);
                if (man == null)
                {
                    GUILayout.Label("Character '" + searchName + "' not found.");
                }
                else
                {
                    GUILayout.Label("Found: " + man.status.fullNameEnStyle);
                }
            }

            if (man == null)
            {
                // Fallback to Selector
                int manCount = GameMain.Instance.CharacterMgr.GetManCount();
                int maidCount = GameMain.Instance.CharacterMgr.GetMaidCount();

                System.Collections.Generic.List<Maid> candidates = new System.Collections.Generic.List<Maid>();
                System.Collections.Generic.List<string> candidateNames = new System.Collections.Generic.List<string>();

                // Check Men
                for (int i = 0; i < manCount; i++)
                {
                    Maid m = GameMain.Instance.CharacterMgr.GetMan(i);
                    if (m != null && m.body0 != null)
                    {
                        candidates.Add(m);
                        string name = !string.IsNullOrEmpty(m.status.fullNameEnStyle) ? m.status.fullNameEnStyle : "Man " + i;
                        if (!m.Visible) name += " (Hidden)";
                        candidateNames.Add("[M] " + name);
                    }
                }

                // Check Maids (Sometimes men are loaded here in mods)
                for (int i = 0; i < maidCount; i++)
                {
                    Maid m = GameMain.Instance.CharacterMgr.GetMaid(i);
                    if (m != null && m.body0 != null && m.boMan)
                    {
                        candidates.Add(m);
                        string name = !string.IsNullOrEmpty(m.status.fullNameEnStyle) ? m.status.fullNameEnStyle : "Maid " + i;
                        if (!m.Visible) name += " (Hidden)";
                        candidateNames.Add("[F->M] " + name);
                    }
                }

                if (candidates.Count > 0)
                {
                    if (selectedManIndex >= candidates.Count) selectedManIndex = 0;

                    GUILayout.Label("Select Character:");
                    selectedManIndex = GUILayout.Toolbar(selectedManIndex, candidateNames.ToArray());
                    man = candidates[selectedManIndex];
                }
                else
                {
                    GUILayout.Label("No male characters found.");
                    GUILayout.EndVertical();
                    return;
                }
            }

            if (man != null)
            {
                GUILayout.Space(5);
                GUI.color = Color.green;
                GUILayout.Label($"Target: {man.status.fullNameEnStyle} (Visible: {man.Visible})");
                GUI.color = Color.white;

                GUILayout.Label("Select Penis Model:");

                foreach (string modelMenu in penisModels)
                {
                    if (GUILayout.Button(modelMenu))
                    {
                        ChangePenisModel(man, modelMenu);
                    }
                }
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(10);
                GUILayout.Label(statusMessage);
            }

            GUILayout.EndVertical();
        }

        private Maid FindMaidByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            name = name.ToLower();

            CharacterMgr cm = GameMain.Instance.CharacterMgr;

            // Search Men
            for(int i=0; i<cm.GetManCount(); i++)
            {
                Maid m = cm.GetMan(i);
                if (m != null && !string.IsNullOrEmpty(m.status.fullNameEnStyle) && m.status.fullNameEnStyle.ToLower().Contains(name)) return m;
                if (m != null && !string.IsNullOrEmpty(m.status.fullNameJpStyle) && m.status.fullNameJpStyle.ToLower().Contains(name)) return m;
            }

            // Search Maids
            for (int i = 0; i < cm.GetMaidCount(); i++)
            {
                Maid m = cm.GetMaid(i);
                if (m != null && !string.IsNullOrEmpty(m.status.fullNameEnStyle) && m.status.fullNameEnStyle.ToLower().Contains(name)) return m;
                if (m != null && !string.IsNullOrEmpty(m.status.fullNameJpStyle) && m.status.fullNameJpStyle.ToLower().Contains(name)) return m;
            }

            return null;
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
