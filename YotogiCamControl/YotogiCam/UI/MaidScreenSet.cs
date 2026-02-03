using UnityEngine;
using COM3D2.YotogiCamControl.Plugin.Data;

namespace COM3D2.YotogiCamControl.Plugin.UI
{
    public class MaidScreenSet
    {
        public bool IsInitialized = false;
        public bool layoutInitialized = false;
        public bool IsActive = false;
        public SubCamera faceCam;
        public SubCamera chestCam;
        public SubCamera pelvisCam;

        public bool faceEnabled = true;
        public bool chestEnabled = true;
        public bool pelvisEnabled = true;

        public CamSetting faceSet = new CamSetting { distance = 0.35f, fov = 30f };
        public CamSetting chestSet = new CamSetting { distance = 0.4f, fov = 45f };
        public CamSetting pelvisSet = new CamSetting { distance = 0.4f, fov = 45f, offset = new Vector3(0, 0.05f, 0) };

        public float faceSize = 200f;
        public float chestSize = 200f;
        public float pelvisSize = 200f;

        public Rect faceRect;
        public Rect chestRect;
        public Rect pelvisRect;

        public LookAtType lookAtType = LookAtType.None;
        public int lookAtTargetIndex = -1;

        public bool eyesOnly = false; // Added Eyes Only

        private int maidIndex;

        public void Initialize(int index)
        {
            maidIndex = index;
            faceCam = new SubCamera("FaceCam_" + index);
            chestCam = new SubCamera("ChestCam_" + index);
            pelvisCam = new SubCamera("PelvisCam_" + index);

            faceRect = new Rect(0, 0, faceSize, faceSize * 0.75f + 25);
            chestRect = new Rect(0, 0, chestSize, chestSize * 0.75f + 25);
            pelvisRect = new Rect(0, 0, pelvisSize, pelvisSize * 0.75f + 25);

            IsInitialized = true;
            layoutInitialized = false;
        }

        public void Update(Maid maid)
        {
            IsActive = true;

            Transform faceT = maid.body0.trsHead;
            Transform chestT = maid.body0.Spine1a;
            Transform pelvisT = maid.body0.GetBone("_IK_vagina");
            if (pelvisT == null) pelvisT = maid.body0.Pelvis;

            if (faceEnabled) faceCam.Update(faceT, faceSet); else faceCam.SetActive(false);
            if (chestEnabled) chestCam.Update(chestT, chestSet); else chestCam.SetActive(false);
            if (pelvisEnabled) pelvisCam.Update(pelvisT, pelvisSet); else pelvisCam.SetActive(false);
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            if (!active)
            {
                faceCam?.SetActive(false);
                chestCam?.SetActive(false);
                pelvisCam?.SetActive(false);
            }
        }

        public MaidProfileData ToProfileData()
        {
            return new MaidProfileData
            {
                faceEnabled = faceEnabled,
                chestEnabled = chestEnabled,
                pelvisEnabled = pelvisEnabled,
                faceSize = faceSize,
                chestSize = chestSize,
                pelvisSize = pelvisSize,
                faceSet = faceSet,
                chestSet = chestSet,
                pelvisSet = pelvisSet,
                eyesOnly = eyesOnly
            };
        }

        public void ApplyProfileData(MaidProfileData data)
        {
            faceEnabled = data.faceEnabled; chestEnabled = data.chestEnabled; pelvisEnabled = data.pelvisEnabled;
            faceSize = data.faceSize; chestSize = data.chestSize; pelvisSize = data.pelvisSize;
            faceSet = data.faceSet; chestSet = data.chestSet; pelvisSet = data.pelvisSet;
            eyesOnly = data.eyesOnly;
            layoutInitialized = false;
        }

        public void Destroy()
        {
            faceCam?.Destroy();
            chestCam?.Destroy();
            pelvisCam?.Destroy();
        }
    }
}
