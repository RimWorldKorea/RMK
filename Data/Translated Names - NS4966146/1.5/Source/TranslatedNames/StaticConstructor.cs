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
			/* Core 메서드
            List<BackstoryCategoryFilter> backstoryCategoryFiltersFor = GetBackstoryCategoryFiltersFor(pawn, factionType);
            bool flag = pawn.DevelopmentalStage.Baby();
            if (!request.ForceNoBackstory && !request.OnlyUseForcedBackstories && !flag && (Rand.Value < 0.25f || pawn.kindDef.factionLeader) && TryGiveSolidBioTo(pawn, request.FixedLastName, backstoryCategoryFiltersFor))
            {
                return;
            }
            GiveShuffledBioTo(pawn, factionType, request.FixedLastName, backstoryCategoryFiltersFor, request.ForceNoBackstory, forceNoNick: false, xenotype, request.OnlyUseForcedBackstories);
            if (flag && pawn.Name is NameTriple nameTriple)
            {
                if (pawn.Faction.IsPlayerSafe())
                {
                    pawn.Name = new NameTriple("Baby".Translate().CapitalizeFirst(), null, nameTriple.Last);
                }
                else
                {
                    pawn.Name = new NameTriple(nameTriple.First, null, nameTriple.Last);
                }
            }
			*/

            if (pawn.Name is NameTriple nameTriple)
			{
				string translation = TranslationInfo.GetTranslation(nameTriple.First);
				string translation2 = TranslationInfo.GetTranslation(nameTriple.Nick);
				string translation3 = TranslationInfo.GetTranslation(nameTriple.Last);
				Name name2 = (pawn.Name = new NameTriple(translation, translation2, translation3));
				NameTriple nameTriple2 = (NameTriple)name2;
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

	[HarmonyPatch(typeof(Pawn), "ExposeData", MethodType.Normal)]
	private class PatchPawn_ExposeData
	{
		private static void Postfix(Pawn __instance)
		{
			if (__instance.def.race.intelligence == Intelligence.Humanlike)
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
