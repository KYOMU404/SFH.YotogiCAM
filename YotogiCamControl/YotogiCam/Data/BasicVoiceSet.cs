namespace COM3D2.YotogiCamControl.Plugin.Data
{
    public class BasicVoiceSet
    {
        public string[][] sLoopVoice20Vibe;
        public string[][] sLoopVoice20Fera;
        public string[][] sLoopVoice30Vibe;
        public string[][] sLoopVoice30Fera;
        public string[][] sOrgasmVoice30Vibe;
        public string[][] sOrgasmVoice30Fera;
        public string[] sLoopVoice40Vibe;

        public BasicVoiceSet(string[][] v1, string[][] v2, string[][] v3, string[][] v4, string[][] v5, string[][] v6, string[] v7)
        {
            sLoopVoice20Vibe = v1; sLoopVoice20Fera = v2;
            sLoopVoice30Vibe = v3; sLoopVoice30Fera = v4;
            sOrgasmVoice30Vibe = v5; sOrgasmVoice30Fera = v6;
            sLoopVoice40Vibe = v7;
        }
    }
}
