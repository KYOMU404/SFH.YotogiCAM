using System;
using UnityEngine;

namespace COM3D2.YotogiCamControl.Plugin.Data
{
    [Serializable]
    public class CamSetting
    {
        public float fov = 45f;
        public float distance = 0.5f;
        public Vector3 offset = Vector3.zero;
        public float rotationX = 0f; // Yaw
        public float rotationY = 0f; // Pitch
        public bool invert = false;
    }
}
