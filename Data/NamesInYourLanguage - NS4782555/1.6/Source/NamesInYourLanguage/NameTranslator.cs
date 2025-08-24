using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System;
using HarmonyLib;
using RimWorld;
using Verse;

using static NamesInYourLanguage.Constants;

namespace NamesInYourLanguage
{
    [StaticConstructorOnStartup]
    public static class NameTranslator
    {
        // 게임에 현재 적용되어있는 설정입니다
        public static readonly bool loadedEnableSetting = LoadedModManager.GetMod<NIYL>().GetSettings<NIYL_Settings>().Enable;
        // 전체 동작 시간 체크용
        public static long TotalWorkTime = 0;
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        // 번역 요청 데이터를 저장
        public static readonly Dictionary<string, NameTripleReduced> solidNames_TranslationRequest = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, NameTripleReduced> solidBioNames_TranslationRequest = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, string> shuffledNames_TranslationRequest = new Dictionary<string, string>();
        
        
        // 번역 파일 데이터를 유효성 관계 없이 형식만 맞춰서 저장 (파일의 원래 모습을 기록하기 위해)
        // (키, 원문, 번역값, 주석) 각 요소는 모두 실제 유효한 값이 아닐 수 있음에 주의
        public static readonly List<(string, string, string, string)> solidNames_TranslationRequestRaw = new List<(string, string, string, string)>();
        public static readonly List<(string, string, string, string)> solidBioNames_TranslationRequestRaw = new List<(string, string, string, string)>();
        public static readonly List<(string, string, string, string)> shuffledNames_TranslationRequestRaw = new List<(string, string, string, string)>();
        
        // 번역 요청 데이터 중 번역이 거부된 것을 저장
        public static readonly List<string> solidNames_TranslationRequestRefuesed = new List<string>();
        public static readonly List<string> solidBioNames_TranslationRequestRefuesed = new List<string>();
        public static readonly List<string> shuffledNames_TranslationRequestRefuesed = new List<string>();
        
        // 원본 이름이 값으로 저장 (게임 초기화 시점, 개체별 구분 없이 이름값만 같으면 같게 취급)
        public static readonly Dictionary<string, NameTripleReduced> solidNames_Original = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, NameTripleReduced> solidBioNames_Original = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, string> shuffledNames_Original = new Dictionary<string, string>();
        public static readonly Dictionary<string, int> shuffledNames_OriginalIndex = new Dictionary<string, int>(); // 현 시점 key에 해당하는 인덱스 번호를 보장함
        
        // 현재 이름이 참조로 저장
        public static readonly Dictionary<string, NameTriple> solidNames = new Dictionary<string,NameTriple>();
        public static readonly Dictionary<string, NameTriple> solidBioNames = new Dictionary<string,NameTriple>();
        public static readonly Dictionary<string, List<string>> shuffledNameLists = new Dictionary<string, List<string>>();
        
        // 번역 중 현재 이름을 검색하는 과정에서 이름이 중복되는 경우 최초 개체를 제외하고 모두 별도로 저장.
        // 바닐라는 중복이 없도록 정리가 돼있기 때문에 필요없지만 모드로 인해 필요할 수 있습니다.
        // ShuffledName은 아마 필요 없을걸로 생각돼서 확인은 안해봤는데, 어차피 개체 구분 없는 리스트라 중복 처리가 돼있겠지?
        public static readonly Dictionary<string, List<NameTriple>> solidNames_Duplicated = new Dictionary<string, List<NameTriple>>();
        public static readonly Dictionary<string, List<NameTriple>> solidBioNames_Duplicated = new Dictionary<string, List<NameTriple>>();
        
        // private 멤버 접근을 위한 Harmony 접근자
        private static readonly FieldInfo bankNamesAccessor = AccessTools.Field(typeof(NameBank), "names");
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        static NameTranslator()
        {
            // 원문 데이터를 저장해두고 번역을 시도합니다.
            LongEventHandler.QueueLongEvent(() =>
            {
                Stopwatch stopwatch_NameTranslator = Stopwatch.StartNew();
                
                // 번역 파일로부터 번역할 이름을 불러옵니다.
                ImportNames.Execute();
                
                // PawnNameDatabaseSolid의 모든 이름의 참조를 solidNameDict에 저장합니다
                foreach (NameTriple currentTriple in PawnNameDatabaseSolid.AllNames())
                {
                    string key = currentTriple.GetHashCode().ToString("x8"); // NameTriple 색인용

                    try
                    {
                        solidNames.Add(key, currentTriple);
                        solidNames_Original.Add(key, new NameTripleReduced(currentTriple));
                    }
                    catch // key 중복(이름이 같은 별도 개체)인 경우 별도로 저장합니다
                    {
                        if (!solidNames_Duplicated.ContainsKey(key))
                            solidNames_Duplicated.Add(key, new List<NameTriple>());
                        
                        solidNames_Duplicated[key].Add(currentTriple);
                    }
                }
                
                // SolidBioDatabase의 모든 이름의 참조를 solidBioNameDict에 저장합니다
                foreach (PawnBio currentBio in SolidBioDatabase.allBios)
                {
                    string key = currentBio.name.GetHashCode().ToString("x8");

                    try
                    {
                        solidBioNames.Add(key, currentBio.name); // NameTriple 색인용
                        solidBioNames_Original.Add(key, new NameTripleReduced(currentBio.name));
                    }
                    catch // key 중복(이름이 같은 별도 개체)인 경우 별도로 저장합니다
                    {
                        if (!solidBioNames_Duplicated.ContainsKey(key))
                            solidBioNames_Duplicated.Add(key, new List<NameTriple>());
                        
                        solidBioNames_Duplicated[key].Add(currentBio.name);
                    }
                }
                
                // PawnNameDatabaseShuffled의 각 NameBank에 저장된 각각의 이름 리스트의 참조를 shuffledNameListDict에 저장합니다
                // PawnNameDatabaseSolid나 SolidBioDatabase와 달리 이름이 리스트에 저장된 개별 string으로 존재하므로 취급에 주의
                int numGenders = Enum.GetValues(typeof(Gender)).Length;
                int numSlots = Enum.GetValues(typeof(PawnNameSlot)).Length;
                
                foreach (PawnNameCategory nameCategory in Enum.GetValues(typeof(PawnNameCategory)))
                {
                    if (nameCategory == PawnNameCategory.NoName) continue;
                    
                    NameBank bank = PawnNameDatabaseShuffled.BankOf(nameCategory);
                    
                    List<string>[,] safe = (List<string>[,])bankNamesAccessor.GetValue(bank);

                    for (int i = 0; i < numGenders; i++)
                    {
                        for (int j = 0; j < numSlots; j++)
                        {
                            List<string> list = safe[i, j];
                            string listKey = nameCategory
                                             + "." + Enum.GetNames(typeof(Gender))[i]
                                             + "." + Enum.GetNames(typeof(PawnNameSlot))[j];
                            shuffledNameLists.Add(listKey, list);

                            // 원본 이름을 shuffledNamesOriginal에 저장합니다
                            for (int k = 0; k < safe[i, j].Count(); k++)
                            {
                                string nameKey = listKey + "." + list[k];
                                shuffledNames_Original.Add(nameKey, list[k]);
                                shuffledNames_OriginalIndex.Add(nameKey, k);
                            }
                        }
                    }
                }
                
                /////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////
                
                // 설정이 활성화돼있을 경우 번역을 시작합니다.
                if (loadedEnableSetting)
                {
                    // 번역 결과를 사용자에게 보고하기 위한 보조 정보
                    int solidNamesTranslated = 0,
                        solidBioNamesTranslated = 0,
                        shuffledNamesTranslated = 0,
                        solidNamesTranslatedButEqual = 0,
                        solidBioNamesTranslatedButEqual = 0,
                        shuffledNamesTranslatedButEqual = 0;
                    
                    // SolidName을 번역합니다.
                    foreach (var request in solidNames_TranslationRequest)
                    {
                        if (!solidNames.ContainsKey(request.Key)) // 번역하려는 키가 현재 게임에 존재해?
                        {
                            // Log.Error(logSignature + "NIYL.Log.InvalidKey".Translate(request.Key));
                            continue;
                        }

                        if (request.Value.Equals(solidNames[request.Key])) // 동등 검사
                            solidNamesTranslatedButEqual++;
                        else
                        {
                            request.Value.ReplaceNameOf(solidNames[request.Key]);

                            if (solidNames_Duplicated.ContainsKey(request.Key))
                            {
                                foreach (NameTriple duplicatedName in solidNames_Duplicated[request.Key])
                                    request.Value.ReplaceNameOf(duplicatedName);
                                Log.Message(logSignature + "NIYL.Log.TranslatedDuplicatedName".Translate(
                                                solidNames_Original[request.Key].ToStringFullPossibly(),
                                                solidNames_Duplicated[request.Key].Count + 1));
                            }
                            
                            solidNamesTranslated++;
                        }
                    }
                    
                    // SolidBioName을 번역합니다.
                    foreach (var request in solidBioNames_TranslationRequest)
                    {
                        if (!solidBioNames.ContainsKey(request.Key))
                        {
                            // Log.Error(logSignature + "NIYL.Log.InvalidKey".Translate(request.Key));
                            continue;
                        }

                        if (request.Value.Equals(solidBioNames[request.Key]))
                            solidBioNamesTranslatedButEqual++;
                        else
                        {
                            request.Value.ReplaceNameOf(solidBioNames[request.Key]);

                            if (solidBioNames_Duplicated.ContainsKey(request.Key))
                            {
                                foreach (NameTriple duplicatedName in solidBioNames_Duplicated[request.Key])
                                    request.Value.ReplaceNameOf(duplicatedName);
                                Log.Message(logSignature + "NIYL.Log.TranslatedDuplicatedName".Translate(
                                                solidNames_Original[request.Key].ToStringFullPossibly(),
                                                solidNames_Duplicated[request.Key].Count + 1));
                            }
                            
                            solidBioNamesTranslated++;
                        }
                    }
                    
                    // ShuffledName을 번역합니다.
                    foreach (var request in shuffledNames_TranslationRequest)
                    {

                        if(!shuffledNames_Original.ContainsKey(request.Key)){}
                            // Log.Error(logSignature + "NIYL.Log.InvalidKey".Translate(request.Key));
                        else
                        {
                            string listKey = request.Key.Substring(0, request.Key.LastIndexOf('.'));
                            int targetIndex = shuffledNames_OriginalIndex[request.Key];

                            if (request.Value.Equals(shuffledNameLists[listKey][targetIndex]))
                                shuffledNamesTranslatedButEqual++;
                            else
                            {
                                shuffledNameLists[listKey][targetIndex] = request.Value;
                                shuffledNamesTranslated++;
                            }
                        }
                    }
                    
                    ////////////////////////////////////////////////////////////////////////////////////////////////////
                    ////////////////////////////////////////////////////////////////////////////////////////////////////

                    int namesTranslated =   solidNamesTranslated
                                          + solidBioNamesTranslated
                                          + shuffledNamesTranslated;
                    int namesTranslatedButEqual =   solidNamesTranslatedButEqual
                                                  + solidBioNamesTranslatedButEqual
                                                  + shuffledNamesTranslatedButEqual;
                    int namesTotal =   solidNames_Original.Count
                                     + solidBioNames_Original.Count
                                     + shuffledNames_Original.Count;
                    
                    stopwatch_NameTranslator.Stop();
                    TotalWorkTime += stopwatch_NameTranslator.ElapsedMilliseconds;
                    
                    Log.Message(logSignature + "NIYL.Log.OverallReport".Translate(
                        namesTranslated,
                        namesTranslatedButEqual,
                        namesTotal - (namesTranslated + namesTranslatedButEqual),
                        TotalWorkTime));
                }
                else
                {
                    stopwatch_NameTranslator.Stop();
                    TotalWorkTime += stopwatch_NameTranslator.ElapsedMilliseconds;
                    Log.Message(logSignature + "NIYL.Log.ModuleDisabled".Translate());
                }
            }
            , "NIYL.StartUp".Translate(), false, null);
        }
    }
    
    // NameTriple 클래스에서 이름 속성만을 복사하여 별도로 다루기 위한 간소화된 형식입니다
    public readonly struct NameTripleReduced : IEquatable<NameTripleReduced>, IEquatable<NameTriple>
    {
        private readonly string firstInt;
        private readonly string lastInt;
        private readonly string nickInt;

        public string First => firstInt;
        public string Last => lastInt;
        public string Nick => nickInt;

        public NameTripleReduced(NameTriple nameTriple)
        {
            firstInt = nameTriple.First;
            lastInt = nameTriple.Last;
            nickInt = nameTriple.Nick;
        }

        public NameTripleReduced(string first, string last, string nick)
        {
            firstInt = first;
            lastInt = last;
            nickInt = nick;
        }

        // NameTriple의 ToStringFull처럼 전체 이름을 반환합니다. 그러나 형식이 완전하지 않을 수 있습니다.
        public string ToStringFullPossibly()
        {
            string result = First;
            result += Nick.NullOrEmpty() ? "" : $" '{Nick}'";
            result += Last.NullOrEmpty() ? "" : $" {Last}";
            return result.Trim();
        }

        public override int GetHashCode()
        {
            // Verse.NameTriple.HashCombine과 같음
            return Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(0, First), Last), Nick);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is NameTripleReduced)
                return Equals((NameTripleReduced)obj);
            if (obj != null && obj is NameTriple) 
                return Equals((NameTriple)obj);
            return false;
        }
        
        public bool Equals(NameTripleReduced other)
        {
            return this.First == other.First && this.Last == other.Last &&  this.Nick == other.Nick;
        }

        public bool Equals(NameTriple other)
        {
            return this.First == other.First && this.Last == other.Last &&  this.Nick == other.Nick;
        }
        
        // private 멤버 접근을 위한 Harmony 접근자
        private static readonly FieldInfo firstIntAccessor = AccessTools.Field(typeof(NameTriple), "firstInt");
        private static readonly FieldInfo lastIntAccessor = AccessTools.Field(typeof(NameTriple), "lastInt");
        private static readonly FieldInfo nickIntAccessor = AccessTools.Field(typeof(NameTriple), "nickInt");
        public void ReplaceNameOf(NameTriple obj)
        {
            firstIntAccessor.SetValue(obj, this.First);
            lastIntAccessor.SetValue(obj, this.Last);
            nickIntAccessor.SetValue(obj, this.Nick);
        }
    }
}