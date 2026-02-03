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

            // Drive Selection
            if (GUILayout.Button("Drives", GUILayout.Width(60)))
            {
                currentDirectory = ""; // Special state to show drives
                currentDirectories = null;
                currentFiles = null;
                try
                {
                    string[] drives = Directory.GetLogicalDrives();
                    currentDirectories = drives;
                }
                catch {}
            }

            if (GUILayout.Button("Root", GUILayout.Width(50)))
            {
                try {
                    currentDirectory = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
                    Refresh();
                } catch {}
            }
            if (GUILayout.Button("Up", GUILayout.Width(50)))
            {
                if (!string.IsNullOrEmpty(currentDirectory))
                {
                    DirectoryInfo parent = Directory.GetParent(currentDirectory);
                    if (parent != null)
                    {
                        currentDirectory = parent.FullName;
                        Refresh();
                    }
                    else
                    {
                        // Maybe at root, go to drives
                        currentDirectory = "";
                        try {
                            currentDirectories = Directory.GetLogicalDrives();
                        } catch {}
                        currentFiles = null;
                    }
                }
            }
            GUILayout.Label(string.IsNullOrEmpty(currentDirectory) ? "Select Drive" : currentDirectory, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.Width(25))) onCancel();
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUI.backgroundColor = Color.cyan;
            if (string.IsNullOrEmpty(currentDirectory) && currentDirectories != null)
            {
                foreach (var drive in currentDirectories)
                {
                    if (GUILayout.Button("[" + drive + "]"))
                    {
                        currentDirectory = drive;
                        Refresh();
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = Color.yellow;
            if (!string.IsNullOrEmpty(currentDirectory) && currentDirectories != null)
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
