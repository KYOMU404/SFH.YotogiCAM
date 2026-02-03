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

            if (GUILayout.Button("Debug: Log All Characters"))
            {
                LogAllCharacters();
            }

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
                        string name = GetBestName(m) + " (Man " + i + ")";
                        if (!m.Visible) name += " (Hidden)";
                        candidateNames.Add("[M] " + name);
                    }
                }

                // Check Maids (Sometimes men are loaded here in mods)
                for (int i = 0; i < maidCount; i++)
                {
                    Maid m = GameMain.Instance.CharacterMgr.GetMaid(i);
                    if (m != null && m.body0 != null)
                    {
                        candidates.Add(m);
                        string name = GetBestName(m) + " (Maid " + i + ")";
                        if (!m.Visible) name += " (Hidden)";
                        candidateNames.Add("[Maid] " + name);
                    }
                }

                if (candidates.Count > 0)
                {
                    if (selectedManIndex >= candidates.Count) selectedManIndex = 0;

                    GUILayout.Label("Select Character:");
                    // Use SelectionGrid if too many items
                    if (candidateNames.Count > 6)
                        selectedManIndex = GUILayout.SelectionGrid(selectedManIndex, candidateNames.ToArray(), 2);
                    else
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
                GUILayout.Label($"Target: {GetBestName(man)} (Visible: {man.Visible})");
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

        private string GetBestName(Maid m)
        {
            if (m == null || m.status == null) return "Unknown";
            if (!string.IsNullOrEmpty(m.status.fullNameEnStyle)) return m.status.fullNameEnStyle;
            if (!string.IsNullOrEmpty(m.status.fullNameJpStyle)) return m.status.fullNameJpStyle;
            if (!string.IsNullOrEmpty(m.status.nickName)) return m.status.nickName;
            return "No Name";
        }

        private Maid FindMaidByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            name = name.ToLower();

            CharacterMgr cm = GameMain.Instance.CharacterMgr;

            // Helper to check
            bool Check(Maid m) {
                if (m == null || m.status == null) return false;
                if (!string.IsNullOrEmpty(m.status.fullNameEnStyle) && m.status.fullNameEnStyle.ToLower().Contains(name)) return true;
                if (!string.IsNullOrEmpty(m.status.fullNameJpStyle) && m.status.fullNameJpStyle.ToLower().Contains(name)) return true;
                if (!string.IsNullOrEmpty(m.status.nickName) && m.status.nickName.ToLower().Contains(name)) return true;
                if (!string.IsNullOrEmpty(m.status.firstName) && m.status.firstName.ToLower().Contains(name)) return true;
                if (!string.IsNullOrEmpty(m.status.lastName) && m.status.lastName.ToLower().Contains(name)) return true;
                return false;
            }

            // Search Men
            for(int i=0; i<cm.GetManCount(); i++)
            {
                if (Check(cm.GetMan(i))) return cm.GetMan(i);
            }

            // Search Maids
            for (int i = 0; i < cm.GetMaidCount(); i++)
            {
                if (Check(cm.GetMaid(i))) return cm.GetMaid(i);
            }

            return null;
        }

        private void LogAllCharacters()
        {
            Debug.Log("[YotogiCamControl] --- Character Dump ---");
            if (GameMain.Instance.CMSystem != null)
            {
                Debug.Log("Player Name (CMSystem): " + GameMain.Instance.CMSystem.PlayerName);
            }
            CharacterMgr cm = GameMain.Instance.CharacterMgr;
            for(int i=0; i<cm.GetManCount(); i++)
            {
                Maid m = cm.GetMan(i);
                if (m != null) Debug.Log($"Man {i}: {GetBestName(m)} (Vis: {m.Visible})");
                else Debug.Log($"Man {i}: NULL");
            }
            for(int i=0; i<cm.GetMaidCount(); i++)
            {
                Maid m = cm.GetMaid(i);
                if (m != null) Debug.Log($"Maid {i}: {GetBestName(m)} (Vis: {m.Visible})");
                else Debug.Log($"Maid {i}: NULL");
            }
            Debug.Log("[YotogiCamControl] ----------------------");
        }

        private void ChangePenisModel(Maid man, string menuFile)
        {
            try
            {
                // MAN Category moza corresponds to MPN.moza
                Debug.Log($"[YotogiCamControl] TNP: Setting {menuFile} to Man {GetBestName(man)}");

                // SetProp(MPN mpn, string filename, int subPropId, bool temp)
                man.SetProp(MPN.moza, menuFile, 0, false);

                // Force Update
                man.AllProcProp();

                // Ensure visibility (FALSE means unmasked/visible)
                if (man.body0 != null)
                {
                    man.body0.SetMask(TBody.SlotID.moza, false);
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
