using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NamesInYourLanguage
{
    [StaticConstructorOnStartup]
    public static class StaticConstructor
    {
        public static readonly DictionaryWithMetaValue<string, string, NameTriple> NameTranslationDict = new DictionaryWithMetaValue<string, string, NameTriple>();
        public static readonly DictionaryWithMetaValue<string, string, NameTriple> NotTranslated = new DictionaryWithMetaValue<string, string, NameTriple>();

        private static readonly Dictionary<string, NameTriple> PawnNameDatabaseSolidAllNames = new Dictionary<string, NameTriple>(); // 바닐라 원문에서 추출해올 데이터입니다.
        private static readonly Dictionary<string, NameTriple> SolidBioDatabaseAllBiosName = new Dictionary<string, NameTriple>(); // 바닐라 원문에서 추출해올 데이터입니다.

        public static readonly bool loadedEnableSetting = LoadedModManager.GetMod<NIYL_Mod>().GetSettings<NIYL_Settings>().Enable; // '현재 게임에 적용된' 설정을 기록해둡니다.

        public static long TotalWorkTime = 0; // 전체 동작 시간 체크용

        public static void Prepare()
        {
            Stopwatch stopwatch_Prepare = Stopwatch.StartNew();

            if (Translator.TryGetTranslatedStringsForFile("Names/Translations", out List<string> lst))
            {
                NameTranslationDict.Clear();
                foreach (string item in lst)
                {
                    if (item.StartsWith("//"))
                        continue;

                    string pattern1 = @"^(?:<([^>]*)>)?([^>]+)->(.+)$"; // <Group1>Group2->Group3
                    Match match1 = Regex.Match(item, pattern1);

                    string lhs = match1.Groups[2].Value; // Group2 값 저장
                    string rhs = match1.Groups[3].Value; // Group3 값 저장

                    if (lhs == string.Empty || rhs == string.Empty)
                        continue;

                    string meta = match1.Groups[1].Value; // Group1 값 저장
                    string pattern2 = @"^(.*?)(?:::(.*?))?(?:::(.*?))?$"; // match1의 Group1 -> Group1::Group2::Group3
                    Match match2 = Regex.Match(meta, pattern2);

                    string first = match2.Groups[1].Value;
                    string nick = match2.Groups[2].Value;
                    string last = match2.Groups[3].Value;

                    NameTriple triple = null;
                    if (first + nick + last != string.Empty) // 일단 Translations.txt에 NameTriple 메타 데이터가 기재되어 있을 경우 그걸 같이 저장해둡니다.
                    {
                        triple = new NameTriple(first, nick, last);
                    }

                    Log.ResetMessageCount();
                    NameTranslationDict.Add(lhs, rhs, triple);
                }
            }
            else
            {
                Log.Warning("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.NoTranslationsFile".Translate());
            }

            stopwatch_Prepare.Stop();
            TotalWorkTime += stopwatch_Prepare.ElapsedMilliseconds;
        }

        static StaticConstructor()
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                Stopwatch stopwatch_main = Stopwatch.StartNew();

                // 빠른 색인을 위해 바닐라의 비번역 NameTriple을 부분별로 쪼개서 정리해둡니다.
                foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                {
                    PawnNameDatabaseSolidAllNames.TryAddOnDictionary(nameTriple.First, nameTriple);
                    PawnNameDatabaseSolidAllNames.TryAddOnDictionary(nameTriple.Nick, nameTriple);
                    PawnNameDatabaseSolidAllNames.TryAddOnDictionary(nameTriple.Last, nameTriple);
                }

                foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                {
                    SolidBioDatabaseAllBiosName.TryAddOnDictionary(pawnBio.name.First, pawnBio.name);
                    SolidBioDatabaseAllBiosName.TryAddOnDictionary(pawnBio.name.Nick, pawnBio.name);
                    SolidBioDatabaseAllBiosName.TryAddOnDictionary(pawnBio.name.Last, pawnBio.name);
                }
                //___________________________________________________________________________________________________________

                // Translation.txt 파일을 통해 생성한 NameTranslationDict의 비어있는 NameTriple 정보를 바닐라 데이터에서 찾아봅니다.
                Dictionary<string, NameTriple> tempTripleDict = new Dictionary<string, NameTriple>();

                foreach (var (key, tuple) in NameTranslationDict)
                {
                    Log.ResetMessageCount();

                    NameTriple triple = tuple.Item2;

                    // Translations.txt 파일에서 NameTriple 정보가 기록되지 않은 경우
                    if (triple == null || triple.First + triple.Nick + triple.Last == "")
                    {
                        // 바닐라 데이터에서 검색을 시도합니다.
                        if (TryFindNameTripleFromSolid(key, out NameTriple searchedTriple))
                        {
                            tempTripleDict.Add(key, searchedTriple);
                        }
                    }
                }
                //___________________________________________________________________________________________________________

                // 위 단계에서 tempTripleDict에 저장된 NameTriple을 찾은 이름들을 NameTranslationDict에서 다시 찾아 Triple 정보를 채워줍니다.
                foreach (var (key, triple) in tempTripleDict)
                {
                    NameTranslationDict.TrySetMetaValue(key, triple);
                }
                //___________________________________________________________________________________________________________

                // 모듈 설정이 활성화 돼있을 경우 번역을 시작합니다.
                if (loadedEnableSetting)
                {
                    // PawnNameDatabaseShuffled의 이름을 번역합니다.
                    var banks = (Dictionary<PawnNameCategory, NameBank>)AccessTools.Field(typeof(PawnNameDatabaseShuffled), "banks").GetValue(null);
                    foreach (var nameBank in banks.Values)
                    {
                        var names = (List<string>[,])AccessTools.Field(typeof(NameBank), "names").GetValue(nameBank);
                        foreach (var name in names)
                        {
                            for (int k = 0; k < name.Count; k++)
                            {
                                if (NameTranslationDict.TryGetValue(name[k], out var translation))
                                    name[k] = translation;
                                else
                                {
                                    AddIfNotTranslated(name[k]);
                                }
                            }
                        }
                    }

                    // PawnNameDatabaseSolid의 NameTriple 형식의 이름을 번역합니다.
                    foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                    {
                        TranslateNameTriple(nameTriple);
                    }

                    // SolidBioDatabase.allBios의 NameTriple 형식의 이름을 번역합니다.
                    foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                    {
                        TranslateNameTriple(pawnBio.name);
                    }

                    stopwatch_main.Stop();
                    TotalWorkTime += stopwatch_main.ElapsedMilliseconds;
                    Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.OverallReport".Translate(NameTranslationDict.Count(), NotTranslated.Count(), TotalWorkTime));
                }
                else
                {
                    stopwatch_main.Stop();
                    TotalWorkTime += stopwatch_main.ElapsedMilliseconds;
                    Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.ModuleDisabled".Translate());
                }
                //___________________________________________________________________________________________________________
            }
            , "RMK.NIYL.StartUp".Translate(), false, null);
        }

        private static readonly FieldInfo FieldInfoNameFirst = AccessTools.Field(typeof(NameTriple), "firstInt");
        private static readonly FieldInfo FieldInfoNameLast = AccessTools.Field(typeof(NameTriple), "lastInt");
        private static readonly FieldInfo FieldInfoNameNick = AccessTools.Field(typeof(NameTriple), "nickInt");

        private static void TranslateNameTriple(NameTriple nameTriple)
        {
            if (nameTriple.First != null && NameTranslationDict.TryGetValue(nameTriple.First, out var translation))
                FieldInfoNameFirst.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.First, nameTriple);

            if (nameTriple.Last != null && NameTranslationDict.TryGetValue(nameTriple.Last, out translation))
                FieldInfoNameLast.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.Last, nameTriple);

            if (nameTriple.Nick != null && NameTranslationDict.TryGetValue(nameTriple.Nick, out translation))
                FieldInfoNameNick.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.Nick, nameTriple);
        }

        private static void AddIfNotTranslated(string name, NameTriple triple = null)
        {
            if (Regex.IsMatch(name, "[A-Za-z]+") && !Regex.IsMatch(name, "[가-힣]+"))
            {
                if (!NotTranslated.ContainsKey(name))
                {
                    NotTranslated.Add(name, name, triple);
                }
            }
        }

        private static bool TryFindNameTripleFromSolid(string name, out NameTriple result)
        {
            result = null;
            bool found = false;

            if (!found)
                if (PawnNameDatabaseSolidAllNames.TryGetValue(name, out result))
                {
                    found = true;
                }

            if (!found)
                if (SolidBioDatabaseAllBiosName.TryGetValue(name, out result))
                {
                    found = true;
                }

            return found;
        }
    }

    // Dictionary 기본 Add 메서드는 TKey 중복 상황에서 예외를 뱉는데, 그러지 말라고 만듦.
    public static class DictionaryExtension
    {
        public static bool TryAddOnDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
