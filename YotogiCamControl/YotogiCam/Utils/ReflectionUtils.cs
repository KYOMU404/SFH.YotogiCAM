using System;
using System.Reflection;

namespace COM3D2.YotogiCamControl.Plugin.Utils
{
    public static class ReflectionUtils
    {
        public static FieldInfo GetField(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
    }
}
