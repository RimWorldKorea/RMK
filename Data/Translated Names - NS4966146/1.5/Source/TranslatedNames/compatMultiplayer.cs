using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace TranslatedNames;
public static class MultiplayerCompat
{
    private static Type class_MP;

    private static Type class_Multiplayer;


    private static PropertyInfo prop_IsInMultiplayer; // 변동 가능 프로퍼티


    private static FieldInfo field_settings;

    private static PropertyInfo prop_PreferredLocalServerSettings;

    private static FieldInfo field_multifaction;

    public static void loadMultiplayerTypes()
    {
        Log.Message("[Translated Names] detected Multiplayer is active on mod list.");

        List<Type> typesInMultiplayer = new List<Type>();
        Assembly[] currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        try
        {
            Assembly assembly_MultiplayerAPI = currentAssemblies.FirstOrDefault(t => t.GetName().Name == "0MultiplayerAPI");
            Assembly assembly_Multiplayer = currentAssemblies.FirstOrDefault(t => t.GetName().Name == "Multiplayer");

            MultiplayerCompat.class_MP = assembly_MultiplayerAPI.GetType("Multiplayer.API.MP");
            MultiplayerCompat.class_Multiplayer = assembly_Multiplayer.GetType("Multiplayer.Client.Multiplayer");

            MultiplayerCompat.prop_IsInMultiplayer = class_MP.GetProperty("IsInMultiplayer", BindingFlags.Static | BindingFlags.Public);
            MultiplayerCompat.field_settings = class_Multiplayer.GetField("settings", BindingFlags.Static | BindingFlags.Public);

            object objForInit_settings = field_settings.GetValue(null);

            MultiplayerCompat.prop_PreferredLocalServerSettings = objForInit_settings.GetType().GetProperty("PreferredLocalServerSettings");

            object objForInit_PreferredLocalServerSettings = prop_PreferredLocalServerSettings.GetValue(objForInit_settings);

            MultiplayerCompat.field_multifaction = objForInit_PreferredLocalServerSettings.GetType().GetField("multifaction");
        }
        catch
        {
            Log.Error("[Translated Names] couldn't find contents of Multiplayer mod.");
            StaticConstructor.multiplayerActive = false;
        }

    }
    public static bool checkMultifaction(bool isMultiplayerActive)
    {
        Stopwatch sw = Stopwatch.StartNew();

        bool multifactionEnabled = false;

        if (isMultiplayerActive)
        {
            bool IsInMultiplayer = (bool)prop_IsInMultiplayer.GetValue(null);

            if (IsInMultiplayer)
            {
                object obj_settings = field_settings.GetValue(null);
                object obj_PreferredLocalServerSettings = prop_PreferredLocalServerSettings.GetValue(obj_settings);

                multifactionEnabled = (bool)field_multifaction.GetValue(obj_PreferredLocalServerSettings);
            }
        }

        sw.Stop();
        Log.Message(string.Format("{0:F4} ms elapsed.", sw.Elapsed.TotalMilliseconds));

        return multifactionEnabled;
    }
}
