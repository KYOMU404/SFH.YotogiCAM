using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.UI
{
    public static class StyleManager
    {
        private static bool initialized = false;
        private static GUIStyle _headerStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _activeButtonStyle;
        private static GUIStyle _boxStyle;
        private static GUIStyle _tabStyle;

        public static GUIStyle HeaderStyle
        {
            get
            {
                Initialize();
                return _headerStyle;
            }
        }

        public static GUIStyle TabStyle
        {
            get
            {
                Initialize();
                return _tabStyle;
            }
        }

        public static GUIStyle ButtonStyle
        {
            get
            {
                Initialize();
                return _buttonStyle;
            }
        }

        public static GUIStyle ActiveButtonStyle
        {
            get
            {
                Initialize();
                return _activeButtonStyle;
            }
        }

        public static GUIStyle BoxStyle
        {
            get
            {
                Initialize();
                return _boxStyle;
            }
        }

        private static void Initialize()
        {
            if (initialized) return;

            // Header Style
            _headerStyle = new GUIStyle(GUI.skin.button);
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            _headerStyle.normal.textColor = Color.white;
            // Create a simple colored background texture if needed, or rely on skin

            // Active Button Style (Greenish)
            _activeButtonStyle = new GUIStyle(GUI.skin.button);
            _activeButtonStyle.normal.textColor = Color.green;
            _activeButtonStyle.hover.textColor = Color.green;
            _activeButtonStyle.fontStyle = FontStyle.Bold;

            // Standard Button Style
            _buttonStyle = new GUIStyle(GUI.skin.button);

            // Box Style
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.padding = new RectOffset(10, 10, 10, 10);

            // Tab Style (Bigger)
            _tabStyle = new GUIStyle(GUI.skin.button);
            _tabStyle.fontSize = 20;
            _tabStyle.fixedHeight = 50;
            _tabStyle.margin = new RectOffset(2, 2, 2, 2);
            _tabStyle.wordWrap = true;

            initialized = true;
        }

        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
