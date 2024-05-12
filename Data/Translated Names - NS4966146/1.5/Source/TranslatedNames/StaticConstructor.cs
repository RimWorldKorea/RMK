using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
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
			if (!MultiplayerCompat.checkMultifaction(multiplayerActive)) // 하모니 패치가 동작하는 방식을 정확히는 모르는데, 만약 if(){ return; } 처럼 했을 때 해당 메서드에 붙는 다른 하모니 postfix 같은게 실행되지 않을 가능성이 있나?
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
            if (__instance.def.race.intelligence == Intelligence.Humanlike && !MultiplayerCompat.checkMultifaction(multiplayerActive))
			{
				PatchNameGiver.Postfix(__instance);
			}
		}
	}

	private const string firstMale = "First_Male";

	private const string firstFemale = "First_Female";

	private const string nickMale = "Nick_Male";

	private const string nickFemale = "Nick_Female";

	private const string nickUnisex = "Nick_Unisex";

	private const string last = "Last";

	public static readonly string rootPath;

	public static readonly string translationPath;

    public static bool multiplayerActive = false;
    static StaticConstructor()
    {
        Assembly thisAssembly = typeof(StaticConstructor).Assembly; // 원래 rootPath를 판단하기 위해 Assembly.Location을 사용했는데, Prepatcher 모드와 호환되지 않아서 직접적으로 사용 불가.

		try
		{
            ModMetaData rmkModData = ModLister.GetActiveModWithIdentifier("RMK.translation");

			string currentRimworldVersion = VersionControl.CurrentVersionStringWithoutBuild; // 현재 실행된 림월드 버전

            List<LoadFolder> listLoadFolders = rmkModData.loadFolders.FoldersForVersion(currentRimworldVersion).Where(t => t.ShouldLoad == true).ToList(); // 현재 실행중인 림월드 버전에 따른 RMK의 LoadFolders 정보를 획득

            string folderNameInRMK = "Translated Names - NS4966146";

            string loadedPartialPath = listLoadFolders.Where(t => t.folderName.Contains(folderNameInRMK)).FirstOrDefault().folderName;
            // listLoadFolders에서 이미 버전 분기 경로를 LoadFolders의 정보로 판단했기 때문에, loadedPartialPath를 현재 로드된 정확한 폴더라고 간주할 수 있다.

            rootPath = Path.Combine(rmkModData.RootDir.FullName, loadedPartialPath);
        }
		catch
		{
			// 나중에 여기에 Translated Names 모듈이 RMK에 있지 않은 채 실행된 상태를 가정하고
			// rootPath를 설정하는 기능을 구현
			// 아님 아예 프리패처를 뚫고 원래 어셈블리 위치를 불러오는 방법을 알아오든지 (Mono.Cecil 등?)
		}

		string translationsPath = Path.Combine(rootPath, "Translations");// Translations 폴더 경로입니다.

		string translationLanguage = GetTranslationLanguage(Path.Combine(translationsPath, "Config.xml"));

		if (!string.IsNullOrEmpty(translationLanguage))
		{
			translationPath = Path.Combine(translationsPath, translationLanguage); // 설정된 언어의 Translation 폴더 경로입니다.
			new Harmony("RMK.TranslatedNames").PatchAll(thisAssembly);
		}

		ModMetaData modMultiplayer = ModLister.GetActiveModWithIdentifier("rwmt.Multiplayer", true); // Multiplayer 모드가 활성화 상태인지 확인하고 기록합니다.
        if (modMultiplayer != null)
        {
            StaticConstructor.multiplayerActive = modMultiplayer.Active; // 이거 앞에 생성자 이름 IDE에서 안 붙여도 된다고 할텐데 붙여야 됨.
        }
        
		if (multiplayerActive)
		{
			MultiplayerCompat.loadMultiplayerTypes();
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
