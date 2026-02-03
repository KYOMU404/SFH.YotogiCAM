using System;
using System.Collections.Generic;

namespace COM3D2.YotogiCamControl.Plugin.Data
{
    [Serializable]
    public class ProfileContainer
    {
        public List<MaidProfileData> profiles;
    }

    [Serializable]
    public class MaidProfileData
    {
        public bool faceEnabled, chestEnabled, pelvisEnabled;
        public float faceSize, chestSize, pelvisSize;
        public CamSetting faceSet, chestSet, pelvisSet;
        public bool eyesOnly;
    }
}
