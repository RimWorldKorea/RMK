using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;

//using Multiplayer.Client;
using RimWorld;
using Verse;

namespace TranslatedNames;

[StaticConstructorOnStartup]
public static class StaticConstructor
{
    [HarmonyPatch(typeof(PawnBioAndNameGenerator), "GiveAppropriateBioAndNameTo", MethodType.Normal)]
    private class PatchNameGiver
    {
        public static void Postfix(Pawn pawn)
		{
			if (!checkMultifaction()) // 하모니 패치가 동작하는 방식을 정확히는 모르는데, 만약 if(){ return; } 처럼 했을 때 해당 메서드에 붙는 다른 하모니 postfix 같은게 실행되지 않을 가능성이 있나?
			{
				if (pawn.Name is NameTriple nameTriple)
				{
					string translation = TranslationInfo.GetTranslation(nameTriple.First);
					string translation2 = TranslationInfo.GetTranslation(nameTriple.Nick);
					string translation3 = TranslationInfo.GetTranslation(nameTriple.Last);
					pawn.Name = new NameTriple(translation, translation2, translation3);
				}
				else if (!(pawn.Name is NameSingle nameSingle))
				{
					Log.Warning("Not both");
				}
				else
				{
					pawn.Name = new NameSingle(TranslationInfo.GetTranslation(nameSingle.Name), nameSingle.Numerical);
					Log.Warning("Trying to translate not a NameTriple!");
				}
			}
		}
	}

	[HarmonyPatch(typeof(Pawn), "ExposeData", MethodType.Normal)]
	private class PatchPawn_ExposeData
	{
		private static void Postfix(Pawn __instance)
		{
            if (__instance.def.race.intelligence == Intelligence.Humanlike && !checkMultifaction())
			{
				PatchNameGiver.Postfix(__instance);
			}
		}
	}


	public static bool checkMultifaction()
	{
		bool multifactionEnabled = false;

        if (multiplayerActive)
		{
			bool IsInMultiplayer = (bool)prop_IsInMultiplayer.GetValue(null);
			
            if (IsInMultiplayer)
			{

                object obj_settings = prop_settings.GetValue(null);

                PropertyInfo prop_PreferredLocalServerSettings = obj_settings.GetType().GetProperty("PreferredLocalServerSettings");

                object obj_PreferredLocalServerSettings = prop_PreferredLocalServerSettings.GetValue(obj_settings);

                FieldInfo field_multifaction = obj_PreferredLocalServerSettings.GetType().GetField("multifaction");

                multifactionEnabled = (bool)field_multifaction.GetValue(obj_PreferredLocalServerSettings);
			}
        }
        return multifactionEnabled;
    }


	private const string firstMale = "First_Male";

	private const string firstFemale = "First_Female";

	private const string nickMale = "Nick_Male";

	private const string nickFemale = "Nick_Female";

	private const string nickUnisex = "Nick_Unisex";

	private const string last = "Last";

	public static readonly string rootPath;

	public static readonly string translationPath;

	private static readonly Type class_MP;

	private static readonly Type class_Multiplayer;

	private static PropertyInfo prop_IsInMultiplayer; // 변동 가능 프로퍼티

	private static readonly FieldInfo prop_settings;

    public static bool multiplayerActive = false; // 그냥 필요할 때 마다 ModLister에서 읽어도 될 것 같긴 한데 혹시 성능상 불리한게 있을까봐 이렇게 해둠. 어차피 게임 중에 바뀔 일은 없는 값이기 때문에

    static StaticConstructor()
	{
		Assembly thisAssembly = typeof(StaticConstructor).Assembly;

        string dllPath = Directory.GetParent(thisAssembly.Location).ToString(); // 이 어셈블리가 위치한 폴더 경로입니다.

		string rootPath = Directory.GetParent(dllPath).ToString(); // 현재 로드된 Translated Names 모듈의 루트 폴더 경로입니다.

		string translationsPath = Path.Combine(rootPath, "Translations"); // Translations 폴더 경로입니다.

		string translationLanguage = GetTranslationLanguage(Path.Combine(translationsPath, "Config.xml"));

		if (!string.IsNullOrEmpty(translationLanguage))
		{
			translationPath = Path.Combine(translationsPath, translationLanguage); // 설정된 언어의 Translation 폴더 경로입니다.
			new Harmony("RMK.TranslatedNames").PatchAll(thisAssembly);
		}

		ModMetaData modMultiplayer = ModLister.GetActiveModWithIdentifier("rwmt.Multiplayer", true); // Multiplayer 모드가 활성화 상태인지 확인하고 기록합니다.
        if (modMultiplayer != null)
        {
            StaticConstructor.multiplayerActive = modMultiplayer.Active; // 이거 앞에 생성자 이름 명시적으로 붙여야 되드라고요. VS는 지워도 된다고 할거임.
        }
        
		if (multiplayerActive)
		{
			Log.Message("[Translated Names] detected Multiplayer is active on mod list.");

            List<Type> typesInMultiplayer = new List<Type>();
            Assembly[] currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			try
			{
                Assembly assembly_MultiplayerAPI = currentAssemblies.FirstOrDefault(t => t.GetName().Name == "0MultiplayerAPI");
                Assembly assembly_Multiplayer = currentAssemblies.FirstOrDefault(t => t.GetName().Name == "Multiplayer");

                StaticConstructor.class_MP = assembly_MultiplayerAPI.GetType("Multiplayer.API.MP");
                StaticConstructor.class_Multiplayer = assembly_Multiplayer.GetType("Multiplayer.Client.Multiplayer");

				StaticConstructor.prop_IsInMultiplayer = class_MP.GetProperty("IsInMultiplayer", BindingFlags.Static | BindingFlags.Public);
                StaticConstructor.prop_settings = class_Multiplayer.GetField("settings", BindingFlags.Static | BindingFlags.Public);
            }
			catch
			{
				Log.Error("[Translated Names] couldn't find contents of Multiplayer mod.");
				StaticConstructor.multiplayerActive = false;
			}
        }
    }

    private static void GetFilesWithNames()
	{
		string[] array = new string[6] { "First_Male", "First_Female", "Nick_Male", "Nick_Female", "Nick_Unisex", "Last" };
		string text = Path.Combine(rootPath, "Debug");
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		string[] array2 = array;
		string[] array3 = array2;
		foreach (string text2 in array3)
		{
			dictionary.Add(text2, GenFile.LinesFromFile(Path.Combine("Names", text2)).ToList());
		}
		GenderPossibility[] array4 = new GenderPossibility[3]
		{
			GenderPossibility.Male,
			GenderPossibility.Female,
			GenderPossibility.Either
		};
		GenderPossibility[] array5 = array4;
		foreach (GenderPossibility genderPossibility in array5)
		{
			foreach (NameTriple item in PawnNameDatabaseSolid.GetListForGender(genderPossibility))
			{
				FillByGender(genderPossibility, item, dictionary);
			}
		}
		foreach (PawnBio allBio in SolidBioDatabase.allBios)
		{
			FillByGender(allBio.gender, allBio.name, dictionary);
		}
		string[] array6 = array;
		string[] array7 = array6;
		foreach (string text3 in array7)
		{
			using StreamWriter streamWriter = File.CreateText(Path.ChangeExtension(Path.Combine(Directory.CreateDirectory(text).FullName, text3), ".txt"));
			foreach (string item2 in dictionary[text3])
			{
				streamWriter.WriteLine(item2);
			}
		}
		Log.Message("Files with names have been created in: " + text);
	}

	private static void FillByGender(GenderPossibility gender, NameTriple name, Dictionary<string, List<string>> dict)
	{
		string first = name.First;
		string nick = name.Nick;
		AddName(dict["Last"], name.Last);
		switch (gender)
		{
		case GenderPossibility.Male:
			AddName(dict["First_Male"], first);
			if (!string.IsNullOrEmpty(nick) && !dict["Nick_Unisex"].Contains(nick))
			{
				AddName(dict["Nick_Male"], nick);
			}
			break;
		case GenderPossibility.Female:
			AddName(dict["First_Female"], first);
			if (!string.IsNullOrEmpty(nick) && !dict["Nick_Unisex"].Contains(nick))
			{
				AddName(dict["Nick_Female"], nick);
			}
			break;
		case GenderPossibility.Either:
			AddName(dict["First_Male"], first);
			AddName(dict["First_Female"], first);
			AddName(dict["Nick_Unisex"], nick);
			break;
		default:
			Log.Error("There is an error in the gender switch for name " + name.ToString());
			break;
		}
	}

	private static void AddName(List<string> collection, string s)
	{
		if (!string.IsNullOrEmpty(s) && !collection.Contains(s))
		{
			collection.Add(s);
		}
	}

	private static string GetTranslationLanguage(string configPath)
	{
		XDocument xDocument = XDocument.Load(configPath);
		string friendlyNameEnglish = LanguageDatabase.activeLanguage.FriendlyNameEnglish;
		string friendlyNameNative = LanguageDatabase.activeLanguage.FriendlyNameNative;
		foreach (XElement item in xDocument.Root.Elements())
		{
			foreach (XElement item2 in item.Element("supported_languages").Elements())
			{
				if (friendlyNameEnglish.Contains(item2.Value.Trim()) || friendlyNameNative.Contains(item2.Value.Trim()))
				{
					return item.Attribute("folderName").Value;
				}
			}
		}
		return null;
	}
}
