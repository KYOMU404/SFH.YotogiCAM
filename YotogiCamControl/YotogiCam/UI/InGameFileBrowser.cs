using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.UI
{
    public class InGameFileBrowser
    {
        private string currentDirectory;
        private string[] currentDirectories;
        private string[] currentFiles;
        private Vector2 scrollPos;

        private string[] filters = { ".mp4", ".avi", ".mkv", ".mov", ".webm", ".ine" };

        public void Initialize()
        {
            currentDirectory = Directory.GetCurrentDirectory();
            Refresh();
        }

        private void Refresh()
        {
            try
            {
                currentDirectories = Directory.GetDirectories(currentDirectory);
                currentFiles = Directory.GetFiles(currentDirectory)
                    .Where(f => filters.Contains(Path.GetExtension(f).ToLower()))
                    .ToArray();
            }
            catch (Exception)
            {
                currentDirectory = Directory.GetDirectoryRoot(currentDirectory);
                currentDirectories = new string[0];
                currentFiles = new string[0];
                try
                {
                    currentDirectories = Directory.GetDirectories(currentDirectory);
                    currentFiles = Directory.GetFiles(currentDirectory)
                        .Where(f => filters.Contains(Path.GetExtension(f).ToLower()))
                        .ToArray();
                }
                catch { }
            }
        }

        public void Draw(Rect rect, Action<string> onFileSelected, Action onCancel)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Root", GUILayout.Width(50)))
            {
                currentDirectory = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
                Refresh();
            }
            if (GUILayout.Button("Up", GUILayout.Width(50)))
            {
                DirectoryInfo parent = Directory.GetParent(currentDirectory);
                if (parent != null)
                {
                    currentDirectory = parent.FullName;
                    Refresh();
                }
            }
            GUILayout.Label(currentDirectory, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.Width(25))) onCancel();
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUI.backgroundColor = Color.yellow;
            if (currentDirectories != null)
            {
                foreach (var dir in currentDirectories)
                {
                    if (GUILayout.Button("[" + Path.GetFileName(dir) + "]"))
                    {
                        currentDirectory = dir;
                        Refresh();
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            if (currentFiles != null)
            {
                foreach (var file in currentFiles)
                {
                    if (GUILayout.Button(Path.GetFileName(file)))
                    {
                        onFileSelected(file);
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
