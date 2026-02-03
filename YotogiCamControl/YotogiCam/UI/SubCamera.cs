using UnityEngine;
using COM3D2.YotogiCamControl.Plugin.Data;

namespace COM3D2.YotogiCamControl.Plugin.UI
{
    public class SubCamera
    {
        public GameObject obj;
        public Camera cam;
        public RenderTexture renderTexture;
        private string name;

        public SubCamera(string name)
        {
            this.name = name;
        }

        public void Update(Transform target, CamSetting setting)
        {
            if (target == null) return;

            if (obj == null)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture = null;
                }

                obj = new GameObject(name);
                cam = obj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = Color.black;

                if (GameMain.Instance.MainCamera != null)
                {
                    Camera mainCam = GameMain.Instance.MainCamera.GetComponent<Camera>();
                    if (mainCam != null)
                    {
                        cam.CopyFrom(mainCam);
                        cam.rect = new Rect(0, 0, 1, 1);
                    }
                }

                cam.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
                cam.cullingMask &= ~(1 << LayerMask.NameToLayer("NGUI"));

                renderTexture = new RenderTexture(512, 512, 24);
                cam.targetTexture = renderTexture;
            }

            if (!obj.activeSelf) obj.SetActive(true);

            Quaternion targetRot = target.rotation;
            Quaternion orbitRot = Quaternion.Euler(setting.rotationY, setting.rotationX, 0);
            Vector3 offsetDir = orbitRot * Vector3.forward;

            Vector3 pos = target.position + (targetRot * offsetDir * setting.distance) + setting.offset;

            obj.transform.position = pos;

            obj.transform.LookAt(target, target.up);

            if (setting.invert)
            {
                obj.transform.Rotate(0, 0, 180, Space.Self);
            }

            cam.fieldOfView = setting.fov;
        }

        public void SetActive(bool active)
        {
            if (obj != null) obj.SetActive(active);
        }

        public void Destroy()
        {
            if (obj != null) UnityEngine.Object.Destroy(obj);
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
        }
    }
}
