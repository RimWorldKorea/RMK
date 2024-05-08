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
			//if (!checkMultifaction()) // 하모니 패치가 동작하는 방식을 정확히는 모르는데, 만약 if(){ return; } 처럼 했을 때 해당 메서드에 붙는 다른 하모니 postfix 같은게 실행되지 않을 가능성이 있나?
			//{
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
			//}
		}
	}

	[HarmonyPatch(typeof(Pawn), "ExposeData", MethodType.Normal)]
	private class PatchPawn_ExposeData
	{
		private static void Postfix(Pawn __instance)
		{
            if (__instance.def.race.intelligence == Intelligence.Humanlike /*&& !checkMultifaction()*/)
			{
				PatchNameGiver.Postfix(__instance);
			}
		}
	}

	/*
	public static bool checkMultifaction()
		// Multiplayer 모드에서 멀티플레이어 서버 설정 중 Multifaction이 켜져있는지 확인해주는 정적 메서드
		// 멀티플레이어 모드가 로드되지 않았는데 관련 코드를 호출하면 문제가 생기지 않을까 걱정돼서
		// multifaction을 바로 불러오지 말고 모드 활성화 여부와 멀티플레이 진행 여부를 모두 차례로 체크하고 진행하도록 해둠
		// multifaction 자체는 서버가 열렸다 닫혔다 하면서 계속 바뀔 수 있는 값 같은데 확인 필요
	{
        bool multifactionEnabled = false;

        if (multiplayerActive)
		{


            if (Multiplayer.API.MP.IsInMultiplayer)
			{
				multifactionEnabled = Multiplayer.Client.Multiplayer.settings.PreferredLocalServerSettings.multifaction;
			}
		}

        return multifactionEnabled;
    }
	*/

	private const string firstMale = "First_Male";

	private const string firstFemale = "First_Female";

	private const string nickMale = "Nick_Male";

	private const string nickFemale = "Nick_Female";

	private const string nickUnisex = "Nick_Unisex";

	private const string last = "Last";

	public static readonly string rootPath;

	public static readonly string translationPath;

	//private static readonly MethodInfo mp_MP_IsInMultiplayer;


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
			new Harmony("rimworld.rmk.translatednames.mainconstructor").PatchAll(thisAssembly);
		}

		ModMetaData modMultiplayer = ModLister.GetActiveModWithIdentifier("rwmt.Multiplayer", true); // Multiplayer 모드가 활성화 상태인지 확인하고 기록합니다.
        if (modMultiplayer != null)
        {
            StaticConstructor.multiplayerActive = modMultiplayer.Active;
        }
        
		if (multiplayerActive)
		{
			Log.Message("Translated Names detected Multiplayer is Active.");

			Type[] multiplayerTypes; 

            Assembly[] currentAassemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in currentAassemblies)
			{
                Type[] typesInNamespace = assembly.GetTypes().Where(t => t.Namespace == "Multiplayer").ToArray();
				if (typesInNamespace.Length > 0)
				{
					foreach(Type type in typesInNamespace)
					{
						multiplayerTypes.Append(type); /*커서위치*/
					}
				}	
            }

            Type mp_MP = Type.GetType("Multiplayer.API.MP");
			if (mp_MP != null)
			{
				Log.Message(string.Format("loaded {0}", mp_MP.FullName));
			}
			else
			{
				Log.Message("mp_MP is null");
			}
            /*
			MethodInfo mp_MP_IsInMultiplayer = mp_MP.GetMethod("IsInMultiplayer");
            Log.Message(string.Format("loaded {0}", mp_MP_IsInMultiplayer.Name));

            Type mp_Multiplayer = Type.GetType("Multiplayer.Client.Multiplayer");
			PropertyInfo mp_Multiplayer_settings = mp_Multiplayer.GetProperty("PreferredLocalServerSettings");
			object mp_Multiplayer_multifaction = mp_Multiplayer_settings.GetValue("multifaction");
			*/

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
