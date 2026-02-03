using System.Collections.Generic;

namespace COM3D2.YotogiCamControl.Plugin.Data
{
    public static class VoiceRepository
    {
        // Personal Names
        public static readonly string[] personalNames = new string[] { "Pride", "Cool", "Pure", "Yandere", "Anesan", "Genki", "Sadist", "Muku", "Majime", "Rindere", "Silent", "Devilish", "Ladylike", "Secretary", "Sister", "Curtness", "Missy", "Childhood", "Masochist", "Crafty" };

        // Loop Voice 40 Vibe
        public static readonly string[] sLoopVoice40PrideVibe = new string[] { "S0_01967.ogg", "S0_01967.ogg", "S0_01968.ogg", "S0_01969.ogg", "S0_01969.ogg" };
        public static readonly string[] sLoopVoice40CoolVibe = new string[] { "S1_03264.ogg", "S1_03264.ogg", "S1_03265.ogg", "S1_03266.ogg", "S1_03266.ogg" };
        public static readonly string[] sLoopVoice40PureVibe = new string[] { "s2_01491.ogg", "s2_01491.ogg", "s2_01492.ogg", "s2_01493.ogg", "s2_01493.ogg" };
        public static readonly string[] sLoopVoice40YandereVibe = new string[] { "S3_02964.ogg", "S3_02964.ogg", "S3_02965.ogg", "S3_02966.ogg", "S3_02966.ogg" };
        public static readonly string[] sLoopVoice40AnesanVibe = new string[] { "s4_08424.ogg", "s4_08426.ogg", "s4_08427.ogg", "s4_08428.ogg", "s4_08428.ogg" };
        public static readonly string[] sLoopVoice40GenkiVibe = new string[] { "s5_04127.ogg", "s5_04129.ogg", "s5_04131.ogg", "s5_04134.ogg", "s5_04134.ogg" };
        public static readonly string[] sLoopVoice40SadistVibe = new string[] { "s6_02477.ogg", "s6_02478.ogg", "s6_02479.ogg", "s6_02481.ogg", "s6_02480.ogg" };
        public static readonly string[] sLoopVoice40MukuVibe = new string[] { "H0_00134.ogg", "H0_00136.ogg", "H0_09239.ogg", "H0_09240.ogg", "H0_00142.ogg" };
        public static readonly string[] sLoopVoice40MajimeVibe = new string[] { "H1_00305.ogg", "H1_08979.ogg", "H1_08980.ogg", "H1_08982.ogg", "H1_00313.ogg" };
        public static readonly string[] sLoopVoice40RindereVibe = new string[] { "H2_00108.ogg", "H2_00110.ogg", "H2_00111.ogg", "H2_00113.ogg", "H2_00118.ogg" };
        public static readonly string[] sLoopVoice40SilentVibe = new string[] { "H3_00622.ogg", "H3_00625.ogg", "H3_00627.ogg", "H3_00628.ogg", "H3_00641.ogg" };
        public static readonly string[] sLoopVoice40DevilishVibe = new string[] { "H4_00959.ogg", "H4_00962.ogg", "H4_00963.ogg", "H4_00977.ogg", "H4_00980.ogg" };
        public static readonly string[] sLoopVoice40LadylikeVibe = new string[] { "H5_00922.ogg", "H5_00923.ogg", "H5_00916.ogg", "H5_00917.ogg", "H5_00920.ogg" };
        public static readonly string[] sLoopVoice40SecretaryVibe = new string[] { "H6_00263.ogg", "H6_00264.ogg", "H6_00267.ogg", "H6_00268.ogg", "H6_00284.ogg" };
        public static readonly string[] sLoopVoice40SisterVibe = new string[] { "H7_03086.ogg", "H7_03087.ogg", "H7_03102.ogg", "H7_03103.ogg", "H7_02889.ogg" };
        public static readonly string[] sLoopVoice40CurtnessVibe = new string[] { "H8_01455.ogg", "H8_01456.ogg", "H8_01457.ogg", "H8_01472.ogg", "H8_01459.ogg" };
        public static readonly string[] sLoopVoice40MissyVibe = new string[] { "H9_00894.ogg", "H9_00909.ogg", "H9_00895.ogg", "H9_00910.ogg", "H9_04413.ogg" };
        public static readonly string[] sLoopVoice40ChildhoodVibe = new string[] { "H10_04171.ogg", "H10_04165.ogg", "H10_04166.ogg", "H10_04168.ogg", "H10_04170.ogg" };
        public static readonly string[] sLoopVoice40MasochistVibe = new string[] { "H11_00957.ogg", "H11_00969.ogg", "H11_00958.ogg", "H11_00970.ogg", "H11_02817.ogg" };
        public static readonly string[] sLoopVoice40CraftyVibe = new string[] { "H12_01529.ogg", "H12_01544.ogg", "H12_01530.ogg", "H12_01545.ogg", "H12_01534.ogg" };

        public static BasicVoiceSet[] GetVoiceSets()
        {
            BasicVoiceSet[] bvs = new BasicVoiceSet[20];

            // Pride
            bvs[0] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20PrideVibe, VoiceArrays.sLoopVoice20PrideFera,
                VoiceArrays.sLoopVoice30PrideVibe, VoiceArrays.sLoopVoice30PrideFera,
                VoiceArrays.sOrgasmVoice30PrideVibe, VoiceArrays.sOrgasmVoice30PrideFera,
                sLoopVoice40PrideVibe);

            // Cool
            bvs[1] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20CoolVibe, VoiceArrays.sLoopVoice20CoolFera,
                VoiceArrays.sLoopVoice30CoolVibe, VoiceArrays.sLoopVoice30CoolFera,
                VoiceArrays.sOrgasmVoice30CoolVibe, VoiceArrays.sOrgasmVoice30CoolFera,
                sLoopVoice40CoolVibe);

            // Pure
            bvs[2] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20PureVibe, VoiceArrays.sLoopVoice20PureFera,
                VoiceArrays.sLoopVoice30PureVibe, VoiceArrays.sLoopVoice30PureFera,
                VoiceArrays.sOrgasmVoice30PureVibe, VoiceArrays.sOrgasmVoice30PureFera,
                sLoopVoice40PureVibe);

            // Yandere
            bvs[3] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20YandereVibe, VoiceArrays.sLoopVoice20YandereFera,
                VoiceArrays.sLoopVoice30YandereVibe, VoiceArrays.sLoopVoice30YandereFera,
                VoiceArrays.sOrgasmVoice30YandereVibe, VoiceArrays.sOrgasmVoice30YandereFera,
                sLoopVoice40YandereVibe);

            // Anesan
            bvs[4] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20AnesanVibe, VoiceArrays.sLoopVoice20AnesanFera,
                VoiceArrays.sLoopVoice30AnesanVibe, VoiceArrays.sLoopVoice30AnesanFera,
                VoiceArrays.sOrgasmVoice30AnesanVibe, VoiceArrays.sOrgasmVoice30AnesanFera,
                sLoopVoice40AnesanVibe);

            // Genki
            bvs[5] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20GenkiVibe, VoiceArrays.sLoopVoice20GenkiFera,
                VoiceArrays.sLoopVoice30GenkiVibe, VoiceArrays.sLoopVoice30GenkiFera,
                VoiceArrays.sOrgasmVoice30GenkiVibe, VoiceArrays.sOrgasmVoice30GenkiFera,
                sLoopVoice40GenkiVibe);

            // Sadist
            bvs[6] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20SadistVibe, VoiceArrays.sLoopVoice20SadistFera,
                VoiceArrays.sLoopVoice30SadistVibe, VoiceArrays.sLoopVoice30SadistFera,
                VoiceArrays.sOrgasmVoice30SadistVibe, VoiceArrays.sOrgasmVoice30SadistFera,
                sLoopVoice40SadistVibe);

            // Muku
            bvs[7] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20MukuVibe, VoiceArrays.sLoopVoice20MukuFera,
                VoiceArrays.sLoopVoice30MukuVibe, VoiceArrays.sLoopVoice30MukuFera,
                VoiceArrays.sOrgasmVoice30MukuVibe, VoiceArrays.sOrgasmVoice30MukuFera,
                sLoopVoice40MukuVibe);

            // Majime
            bvs[8] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20MajimeVibe, VoiceArrays.sLoopVoice20MajimeFera,
                VoiceArrays.sLoopVoice30MajimeVibe, VoiceArrays.sLoopVoice30MajimeFera,
                VoiceArrays.sOrgasmVoice30MajimeVibe, VoiceArrays.sOrgasmVoice30MajimeFera,
                sLoopVoice40MajimeVibe);

            // Rindere
            bvs[9] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20RindereVibe, VoiceArrays.sLoopVoice20RindereFera,
                VoiceArrays.sLoopVoice30RindereVibe, VoiceArrays.sLoopVoice30RindereFera,
                VoiceArrays.sOrgasmVoice30RindereVibe, VoiceArrays.sOrgasmVoice30RindereFera,
                sLoopVoice40RindereVibe);

            // Silent
            bvs[10] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20SilentVibe, VoiceArrays.sLoopVoice20SilentFera,
                VoiceArrays.sLoopVoice30SilentVibe, VoiceArrays.sLoopVoice30SilentFera,
                VoiceArrays.sOrgasmVoice30SilentVibe, VoiceArrays.sOrgasmVoice30SilentFera,
                sLoopVoice40SilentVibe);

            // Devilish
            bvs[11] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20DevilishVibe, VoiceArrays.sLoopVoice20DevilishFera,
                VoiceArrays.sLoopVoice30DevilishVibe, VoiceArrays.sLoopVoice30DevilishFera,
                VoiceArrays.sOrgasmVoice30DevilishVibe, VoiceArrays.sOrgasmVoice30DevilishFera,
                sLoopVoice40DevilishVibe);

            // Ladylike
            bvs[12] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20LadylikeVibe, VoiceArrays.sLoopVoice20LadylikeFera,
                VoiceArrays.sLoopVoice30LadylikeVibe, VoiceArrays.sLoopVoice30LadylikeFera,
                VoiceArrays.sOrgasmVoice30LadylikeVibe, VoiceArrays.sOrgasmVoice30LadylikeFera,
                sLoopVoice40LadylikeVibe);

            // Secretary
            bvs[13] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20SecretaryVibe, VoiceArrays.sLoopVoice20SecretaryFera,
                VoiceArrays.sLoopVoice30SecretaryVibe, VoiceArrays.sLoopVoice30SecretaryFera,
                VoiceArrays.sOrgasmVoice30SecretaryVibe, VoiceArrays.sOrgasmVoice30SecretaryFera,
                sLoopVoice40SecretaryVibe);

            // Sister
            bvs[14] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20SisterVibe, VoiceArrays.sLoopVoice20SisterFera,
                VoiceArrays.sLoopVoice30SisterVibe, VoiceArrays.sLoopVoice30SisterFera,
                VoiceArrays.sOrgasmVoice30SisterVibe, VoiceArrays.sOrgasmVoice30SisterFera,
                sLoopVoice40SisterVibe);

            // Curtness
            bvs[15] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20CurtnessVibe, VoiceArrays.sLoopVoice20CurtnessFera,
                VoiceArrays.sLoopVoice30CurtnessVibe, VoiceArrays.sLoopVoice30CurtnessFera,
                VoiceArrays.sOrgasmVoice30CurtnessVibe, VoiceArrays.sOrgasmVoice30CurtnessFera,
                sLoopVoice40CurtnessVibe);

            // Missy
            bvs[16] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20MissyVibe, VoiceArrays.sLoopVoice20MissyFera,
                VoiceArrays.sLoopVoice30MissyVibe, VoiceArrays.sLoopVoice30MissyFera,
                VoiceArrays.sOrgasmVoice30MissyVibe, VoiceArrays.sOrgasmVoice30MissyFera,
                sLoopVoice40MissyVibe);

            // Childhood
            bvs[17] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20ChildhoodVibe, VoiceArrays.sLoopVoice20ChildhoodFera,
                VoiceArrays.sLoopVoice30ChildhoodVibe, VoiceArrays.sLoopVoice30ChildhoodFera,
                VoiceArrays.sOrgasmVoice30ChildhoodVibe, VoiceArrays.sOrgasmVoice30ChildhoodFera,
                sLoopVoice40ChildhoodVibe);

            // Masochist
            bvs[18] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20MasochistVibe, VoiceArrays.sLoopVoice20MasochistFera,
                VoiceArrays.sLoopVoice30MasochistVibe, VoiceArrays.sLoopVoice30MasochistFera,
                VoiceArrays.sOrgasmVoice30MasochistVibe, VoiceArrays.sOrgasmVoice30MasochistFera,
                sLoopVoice40MasochistVibe);

            // Crafty
            bvs[19] = new BasicVoiceSet(
                VoiceArrays.sLoopVoice20CraftyVibe, VoiceArrays.sLoopVoice20CraftyFera,
                VoiceArrays.sLoopVoice30CraftyVibe, VoiceArrays.sLoopVoice30CraftyFera,
                VoiceArrays.sOrgasmVoice30CraftyVibe, VoiceArrays.sOrgasmVoice30CraftyFera,
                sLoopVoice40CraftyVibe);

            return bvs;
        }
    }
}
