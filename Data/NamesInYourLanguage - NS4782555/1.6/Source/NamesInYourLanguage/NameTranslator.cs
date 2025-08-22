using System;
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
    public static class NameTranslator
    {
        // 게임에 현재 적용되어있는 설정입니다
        public static readonly bool loadedEnableSetting = LoadedModManager.GetMod<NIYL>().GetSettings<NIYL_Settings>().Enable;

        public static long TotalWorkTime; // 전체 동작 시간 체크용
        
        // 번역 요청 데이터를 저장
        public static readonly Dictionary<string, NameTripleReduced> solidNamesTranslationRequest = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, NameTripleReduced> solidBioNamesTranslationRequest = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, string> shuffledNamesTranslationRequest = new Dictionary<string, string>();
        
        // 원본 이름이 값으로 저장
        public static readonly Dictionary<string, NameTripleReduced> solidNamesOriginal = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, NameTripleReduced> solidBioNamesOriginal = new Dictionary<string, NameTripleReduced>();
        public static readonly Dictionary<string, string> shuffledNamesOriginal = new Dictionary<string, string>();
        public static readonly Dictionary<string, int> shuffledNamesOriginalIndex = new Dictionary<string, int>(); // 현 시점 key에 해당하는 인덱스 번호를 보장함
        
        // 현재 이름이 참조로 저장
        public static readonly Dictionary<string, NameTriple> solidNames = new Dictionary<string,NameTriple>();
        public static readonly Dictionary<string, NameTriple> solidBioNames = new Dictionary<string,NameTriple>();
        public static readonly Dictionary<string, List<string>> shuffledNameLists = new Dictionary<string, List<string>>();
        
        // private 멤버 접근을 위한 Harmony 접근자
        private static readonly FieldInfo firstIntAccessor = AccessTools.Field(typeof(NameTriple), "firstInt");
        private static readonly FieldInfo lastIntAccessor = AccessTools.Field(typeof(NameTriple), "lastInt");
        private static readonly FieldInfo nickIntAccessor = AccessTools.Field(typeof(NameTriple), "nickInt");
        private static readonly FieldInfo bankNamesAccessor = AccessTools.Field(typeof(NameBank), "names");
        
        static NameTranslator()
        {
            Log.Message("[NIYL.Debug] NameTranslator()");
            
            LongEventHandler.QueueLongEvent(() =>
            {
                Stopwatch stopwatch_NameTranslator = Stopwatch.StartNew();

                // PawnNameDatabaseSolid의 모든 이름의 참조를 solidNameDict에 저장합니다
                foreach (NameTriple currentTriple in PawnNameDatabaseSolid.AllNames())
                {
                    string key = currentTriple.GetHashCode().ToString("x8"); // NameTriple 색인용
                    
                    solidNames.Add(key, currentTriple);
                    solidNamesOriginal.Add(key, new NameTripleReduced(currentTriple));
                }
                
                
                // SolidBioDatabase의 모든 이름의 참조를 solidBioNameDict에 저장합니다
                foreach (PawnBio currentBio in SolidBioDatabase.allBios)
                {
                    string key = currentBio.name.GetHashCode().ToString("x8");
                    
                    solidBioNames.Add(key, currentBio.name); // NameTriple 색인용
                    solidBioNamesOriginal.Add(key, new NameTripleReduced(currentBio.name));
                }
                
                
                // PawnNameDatabaseShuffled의 각 NameBank에 저장된 각각의 이름 리스트의 참조를 shuffledNameListDict에 저장합니다
                // PawnNameDatabaseSolid나 SolidBioDatabase와 달리 이름이 리스트에 저장된 개별 string으로 존재하므로 취급에 주의
                int numGenders = Enum.GetValues(typeof(Gender)).Length;
                int numSlots = Enum.GetValues(typeof(PawnNameSlot)).Length;


                
                foreach (PawnNameCategory nameCategory in Enum.GetValues(typeof(PawnNameCategory)))
                {
                    if (nameCategory == PawnNameCategory.NoName) continue;
                    
                    NameBank bank = PawnNameDatabaseShuffled.BankOf(nameCategory);
                    // 여기까지 ok
                    
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
                                shuffledNamesOriginal.Add(nameKey, list[k]);
                                shuffledNamesOriginalIndex.Add(nameKey, k);
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
                    foreach (var request in solidNamesTranslationRequest)
                    {
                        if (!solidNames.ContainsKey(request.Key)) // 키 검사
                            Log.Error("[NameInYourLanguage] Key from translation request is invalid");
                        else if (request.Value.Equals(solidNames[request.Key])) // 동등 검사
                            solidNamesTranslatedButEqual++;
                        else
                        {
                            firstIntAccessor.SetValue(solidNames[request.Key], request.Value.First);
                            lastIntAccessor.SetValue(solidNames[request.Key], request.Value.Last);
                            nickIntAccessor.SetValue(solidNames[request.Key], request.Value.Nick);
                            solidNamesTranslated++;
                        }
                    }
                    
                    // SolidBioName을 번역합니다.
                    foreach (var request in solidBioNamesTranslationRequest)
                    {
                        if (!solidBioNames.ContainsKey(request.Key))
                            Log.Error("[NameInYourLanguage] Key from translation request is invalid");
                        else if (request.Value.Equals(solidBioNames[request.Key]))
                            solidBioNamesTranslatedButEqual++;
                        else
                        {
                            firstIntAccessor.SetValue(solidBioNames[request.Key], request.Value.First);
                            lastIntAccessor.SetValue(solidBioNames[request.Key], request.Value.Last);
                            nickIntAccessor.SetValue(solidBioNames[request.Key], request.Value.Nick);
                            solidBioNamesTranslated++;
                        }
                    }
                    
                    // ShuffledName을 번역합니다.
                    foreach (var request in shuffledNamesTranslationRequest)
                    {

                        if(!shuffledNamesOriginal.ContainsKey(request.Key))
                            Log.Error($"[NameInYourLanguage] Key {request.Key} from translation request is invalid");
                        else
                        {
                            string listKey = request.Key.Substring(0, request.Key.LastIndexOf('.'));
                            int targetIndex = shuffledNamesOriginalIndex[request.Key];

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
                    int namesTotal =   solidNamesOriginal.Count
                                     + solidBioNamesOriginal.Count
                                     + shuffledNamesOriginal.Count;
                    
                    stopwatch_NameTranslator.Stop();
                    TotalWorkTime += stopwatch_NameTranslator.ElapsedMilliseconds;
                    
                    Log.Message("[RMK.NamesInYourLanguage] " + "NIYL.Log.OverallReport".Translate(
                        namesTranslated,
                        namesTranslatedButEqual,
                        namesTotal - (namesTranslated + namesTranslatedButEqual),
                        TotalWorkTime));
                }
                else
                {
                    stopwatch_NameTranslator.Stop();
                    TotalWorkTime += stopwatch_NameTranslator.ElapsedMilliseconds;
                    Log.Message("[RMK.NamesInYourLanguage] " + "NIYL.Log.ModuleDisabled".Translate());
                }
            }
            , "NIYL.StartUp".Translate(), false, null);
        }
    }
    
    // NameTriple 클래스에서 이름 속성만을 복사하여 별도로 다루기 위한 간소화된 구조체입니다
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
    }
}
