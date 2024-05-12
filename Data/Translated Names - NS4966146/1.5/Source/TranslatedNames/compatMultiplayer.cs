using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace TranslatedNames;
public static class MultiplayerCompat
{
    private static Type class_MP;

    private static Type class_Multiplayer;

    private static PropertyInfo prop_IsInMultiplayer; // 게임 중 변동 가능

    private static FieldInfo field_settings;

    private static PropertyInfo prop_PreferredLocalServerSettings;

    private static FieldInfo field_multifaction;

    public static void loadMultiplayerTypes() // StaticConstructor의 생성자에 정의되어 게임 시작과 함께 필요한 타입 정보를 불러옵니다.
    {
        Log.Message("[Translated Names] detected Multiplayer is active on mod list.");

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
        // Translated Namse 작동 시 함께 호출되어 multifaction 설정을 확인합니다.
        // 서버를 열거나 닫고 다시 열거나 할 때 multifaction 설정 또한 달라질 수 있기 때문에 매 번 확인해야 합니다.
        // 이로 인한 오버헤드는 평균 0.005 ms 정도로 매우 작은 것으로 확인됩니다.
    {
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

        return multifactionEnabled;
    }
}
