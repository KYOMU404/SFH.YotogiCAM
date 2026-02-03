using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.Managers
{
    public class FaceManager
    {
        private Dictionary<string, Dictionary<string, string>> categories = new Dictionary<string, Dictionary<string, string>>
        {
            { "Common", new Dictionary<string, string> {
                {"Normal", "通常"}, {"Angry", "怒り"}, {"Smile", "笑顔"}, {"Gentle Smile", "微笑み"},
                {"Sad", "悲しみ２"}, {"Crying", "泣き"}, {"Embarrassed", "恥ずかしい"}, {"Shy", "照れ"},
                {"Default", "デフォ"}, {"Ahegao", "エロ絶頂"}, {"Enjoying", "エロ好感３"}, {"Really Enjoying", "エロ絶頂"}
            }},
            { "Erotic", new Dictionary<string, string> {
                {"Ero Normal 1", "エロ通常１"}, {"Ero Normal 2", "エロ通常２"}, {"Ero Normal 3", "エロ通常３"},
                {"Ero Shy 1", "エロ羞恥１"}, {"Ero Shy 2", "エロ羞恥２"}, {"Ero Shy 3", "エロ羞恥３"},
                {"Ero Excited 0", "エロ興奮０"}, {"Ero Excited 1", "エロ興奮１"}, {"Ero Excited 2", "エロ興奮２"}, {"Ero Excited 3", "エロ興奮３"},
                {"Ero Nervous", "エロ緊張"}, {"Ero Expectation", "エロ期待"},
                {"Ero Like 1", "エロ好感１"}, {"Ero Like 2", "エロ好感２"}, {"Ero Like 3", "エロ好感３"},
                {"Ero Endure 1", "エロ我慢１"}, {"Ero Endure 2", "エロ我慢２"}, {"Ero Endure 3", "エロ我慢３"},
                {"Ero Disgust 1", "エロ嫌悪１"}, {"Ero Fear", "エロ怯え"},
                {"Ero Pain 1", "エロ痛み１"}, {"Ero Pain 2", "エロ痛み２"}, {"Ero Pain 3", "エロ痛み３"},
                {"Ero Sobbing", "エロメソ泣き"}, {"Ero Orgasm", "エロ絶頂"},
                {"Ero Pain Endure", "エロ痛み我慢"}, {"Ero Pain Endure 2", "エロ痛み我慢２"}, {"Ero Pain Endure 3", "エロ痛み我慢３"},
                {"Ero Trance", "エロ放心"}, {"Horny", "発情"}
            }},
            { "Action", new Dictionary<string, string> {
                {"After Ejac 1", "通常射精後１"}, {"After Ejac 2", "通常射精後２"},
                {"After Exc Ejac 1", "興奮射精後１"}, {"After Exc Ejac 2", "興奮射精後２"},
                {"After Org Ejac 1", "絶頂射精後１"}, {"After Org Ejac 2", "絶頂射精後２"},
                {"Lick Affection", "エロ舐め愛情"}, {"Lick Pleasure", "エロ舐め快楽"},
                {"Lick Disgust", "エロ舐め嫌悪"}, {"Lick Normal", "エロ舐め通常"},
                {"Kiss", "接吻"},
                {"BJ Affection", "エロフェラ愛情"}, {"BJ Pleasure", "エロフェラ快楽"},
                {"BJ Disgust", "エロフェラ嫌悪"}, {"BJ Normal", "エロフェラ通常"},
                {"Tongue Torture", "エロ舌責"}, {"Tongue Torture Pleasure", "エロ舌責快楽"}
            }},
            { "Closed/Other", new Dictionary<string, string> {
                {"Closed Lick Aff", "閉じ舐め愛情"}, {"Closed Lick Pleasure", "閉じ舐め快楽"},
                {"Closed Lick Disgust", "閉じ舐め嫌悪"}, {"Closed Lick Normal", "閉じ舐め通常"},
                {"Closed BJ Aff", "閉じフェラ愛情"}, {"Closed BJ Pleasure", "閉じフェラ快楽"},
                {"Closed BJ Disgust", "閉じフェラ嫌悪"}, {"Closed BJ Normal", "閉じフェラ通常"},
                {"Closed Eyes", "閉じ目"}, {"Eyes Mouth Closed", "目口閉じ"}, {"Mouth Open", "口開け"}
            }},
            { "Expressions", new Dictionary<string, string> {
                {"Blank", "きょとん"}, {"Staring", "ジト目"}, {"Ahhn", "あーん"}, {"Sigh", "ためいき"},
                {"Smug", "ドヤ顔"}, {"Grin", "にっこり"}, {"Surprised", "びっくり"}, {"Pout", "ぷんすか"},
                {"Squeeze Eyelids", "まぶたギュ"}, {"Muu", "むー"}, {"Twitched Smile", "引きつり笑顔"}, {"Question", "疑問"},
                {"Bitter Smile", "苦笑い"}, {"Troubled", "困った"}, {"Thinking", "思案伏せ目"}, {"Slightly Angry", "少し怒り"},
                {"Seduction", "誘惑"}, {"Sulking", "拗ね"}, {"Kindness", "優しさ"}, {"Sleeping", "居眠り安眠"},
                {"Eyes Wide Open", "目を見開いて"}, {"Afterglow Weak", "余韻弱"},
                {"Shy Shout", "照れ叫び"}, {"Wink Shy", "ウインク照れ"}, {"Grin Shy", "にっこり照れ"}
            }},
            { "Dance", new Dictionary<string, string> {
                {"Dance Closed", "ダンス目つむり"}, {"Dance Yawn", "ダンスあくび"}, {"Dance Surprised", "ダンスびっくり"},
                {"Dance Smile", "ダンス微笑み"}, {"Dance Open", "ダンス目あけ"}, {"Dance Closed 2", "ダンス目とじ"},
                {"Dance Wink", "ダンスウインク"}, {"Dance Kiss", "ダンスキス"}, {"Dance Stare", "ダンスジト目"},
                {"Dance Troubled", "ダンス困り顔"}, {"Dance Serious", "ダンス真剣"}, {"Dance Sorrow", "ダンス憂い"},
                {"Dance Seduction", "ダンス誘惑"}
            }},
            { "Blends (Cheek/Tears)", new Dictionary<string, string> {
                {"C0 T0", "頬０涙０"}, {"C0 T1", "頬０涙１"}, {"C0 T2", "頬０涙２"}, {"C0 T3", "頬０涙３"},
                {"C1 T0", "頬１涙０"}, {"C1 T1", "頬１涙１"}, {"C1 T2", "頬１涙２"}, {"C1 T3", "頬１涙３"},
                {"C2 T0", "頬２涙０"}, {"C2 T1", "頬２涙１"}, {"C2 T2", "頬２涙２"}, {"C2 T3", "頬２涙３"},
                {"C3 T0", "頬３涙０"}, {"C3 T1", "頬３涙１"}, {"C3 T2", "頬３涙２"}, {"C3 T3", "頬３涙３"}
            }},
            { "Blends (Drool)", new Dictionary<string, string> {
                {"Drool Only", "追加よだれ"},
                {"C0 T0 Drool", "頬０涙０よだれ"}, {"C0 T1 Drool", "頬０涙１よだれ"}, {"C0 T2 Drool", "頬０涙２よだれ"}, {"C0 T3 Drool", "頬０涙３よだれ"},
                {"C1 T0 Drool", "頬１涙０よだれ"}, {"C1 T1 Drool", "頬１涙１よだれ"}, {"C1 T2 Drool", "頬１涙２よだれ"}, {"C1 T3 Drool", "頬１涙３よだれ"},
                {"C2 T0 Drool", "頬２涙０よだれ"}, {"C2 T1 Drool", "頬２涙１よだれ"}, {"C2 T2 Drool", "頬２涙２よだれ"}, {"C2 T3 Drool", "頬２涙３よだれ"},
                {"C3 T0 Drool", "頬３涙０よだれ"}, {"C3 T1 Drool", "頬３涙１よだれ"}, {"C3 T2 Drool", "頬３涙２よだれ"}, {"C3 T3 Drool", "頬３涙３よだれ"}
            }}
        };

        private Vector2 scrollPos;
        private int selectedMaidIndex = 0;
        private bool[] categoryFoldouts;
        private string searchText = "";

        // Locking State
        private Dictionary<int, string> lockedFaces = new Dictionary<int, string>();

        public FaceManager()
        {
            categoryFoldouts = new bool[categories.Count];
            for (int i = 0; i < categoryFoldouts.Length; i++) categoryFoldouts[i] = true;
        }

        public void Update()
        {
            if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;

            foreach (var kvp in lockedFaces)
            {
                Maid m = GameMain.Instance.CharacterMgr.GetMaid(kvp.Key);
                if (m != null && m.Visible)
                {
                    string faceName = kvp.Value;
                    if (faceName.Contains("頬") || faceName.Contains("涙") || faceName.Contains("よだれ"))
                    {
                        if (m.FaceName3 != faceName)
                        {
                            m.FaceBlend(faceName);
                        }
                    }
                    else
                    {
                        if (m.ActiveFace != faceName)
                        {
                            m.FaceAnime(faceName, 1f, 0);
                        }
                    }
                }
            }
        }

        public void DrawUI()
        {
            if (GameMain.Instance == null || GameMain.Instance.CharacterMgr == null) return;

            int maidCount = GameMain.Instance.CharacterMgr.GetMaidCount();
            if (maidCount == 0)
            {
                GUILayout.Label("No Maids.");
                return;
            }

            GUILayout.BeginHorizontal();
            for (int i = 0; i < maidCount; i++)
            {
                Maid m = GameMain.Instance.CharacterMgr.GetMaid(i);
                if (m != null && m.Visible)
                {
                    if (GUILayout.Toggle(selectedMaidIndex == i, "Maid " + i, "button")) selectedMaidIndex = i;
                }
            }
            GUILayout.EndHorizontal();

            // Revert/Unlock Button
            if (GUILayout.Button("Revert / Unlock Face (Default Game State)"))
            {
                if (lockedFaces.ContainsKey(selectedMaidIndex)) lockedFaces.Remove(selectedMaidIndex);
                // Reset to default
                Maid m = GameMain.Instance.CharacterMgr.GetMaid(selectedMaidIndex);
                if (m != null) m.FaceAnime("通常", 1f, 0);
            }

            if (lockedFaces.ContainsKey(selectedMaidIndex))
            {
                GUILayout.Label($"[LOCKED]: {lockedFaces[selectedMaidIndex]}");
            }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Search:", GUILayout.Width(50));
                searchText = GUILayout.TextField(searchText);
                if (GUILayout.Button("X", GUILayout.Width(25))) searchText = "";
                GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();

            int catIndex = 0;
                bool isSearching = !string.IsNullOrEmpty(searchText);

            foreach (var cat in categories)
            {
                    bool hasMatch = true;
                    Dictionary<string, string> filteredItems = cat.Value;

                    if (isSearching)
                    {
                        filteredItems = new Dictionary<string, string>();
                        foreach (var kvp in cat.Value)
                        {
                            if (kvp.Key.ToLower().Contains(searchText.ToLower()) || kvp.Value.Contains(searchText))
                            {
                                filteredItems[kvp.Key] = kvp.Value;
                            }
                        }
                        hasMatch = filteredItems.Count > 0;
                    }

                    if (!hasMatch)
                    {
                        catIndex++;
                        continue;
                    }

                // Category Header
                    if (!isSearching)
                {
                        if (GUILayout.Button(cat.Key, "box"))
                        {
                            categoryFoldouts[catIndex] = !categoryFoldouts[catIndex];
                        }
                    }
                    else
                    {
                        GUILayout.Label(cat.Key); // Just label when searching
                }

                    if (isSearching || categoryFoldouts[catIndex])
                {
                    int columns = 3;
                    int current = 0;
                    GUILayout.BeginHorizontal();
                        foreach (var kvp in filteredItems)
                    {
                        if (current >= columns)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            current = 0;
                        }
                        if (GUILayout.Button(kvp.Key))
                        {
                            ApplyFace(kvp.Value);
                        }
                        current++;
                    }
                    GUILayout.EndHorizontal();
                }
                catIndex++;
                GUILayout.Space(5);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void ApplyFace(string faceName)
        {
            Maid m = GameMain.Instance.CharacterMgr.GetMaid(selectedMaidIndex);
            if (m != null)
            {
                // Lock it
                lockedFaces[selectedMaidIndex] = faceName;

                if (faceName.Contains("頬") || faceName.Contains("涙") || faceName.Contains("よだれ"))
                {
                    m.FaceBlend(faceName);
                }
                else
                {
                    m.FaceAnime(faceName, 1f, 0);
                }
            }
        }
    }
}
