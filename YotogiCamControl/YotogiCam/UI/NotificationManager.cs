using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.UI
{
    public static class NotificationManager
    {
        private class Notification
        {
            public string message;
            public float timeRemaining;
            public Color color;
        }

        private static List<Notification> activeNotifications = new List<Notification>();
        private static GUIStyle notificationStyle;

        public static void Show(string message, float duration = 2f, Color? color = null)
        {
            activeNotifications.Add(new Notification
            {
                message = message,
                timeRemaining = duration,
                color = color ?? Color.white
            });
        }

        public static void Update()
        {
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                activeNotifications[i].timeRemaining -= Time.deltaTime;
                if (activeNotifications[i].timeRemaining <= 0)
                {
                    activeNotifications.RemoveAt(i);
                }
            }
        }

        public static void Draw()
        {
            if (activeNotifications.Count == 0) return;

            if (notificationStyle == null)
            {
                notificationStyle = new GUIStyle(GUI.skin.label);
                notificationStyle.fontSize = 20;
                notificationStyle.fontStyle = FontStyle.Bold;
                notificationStyle.alignment = TextAnchor.LowerRight;
            }

            float y = Screen.height - 50;
            foreach (var note in activeNotifications)
            {
                GUI.color = note.color;
                GUI.Label(new Rect(Screen.width - 320, y, 300, 30), note.message, notificationStyle);
                GUI.color = Color.white;
                y -= 35;
            }
        }
    }
}
