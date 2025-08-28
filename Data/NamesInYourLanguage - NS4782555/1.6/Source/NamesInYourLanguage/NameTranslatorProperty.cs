using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace NamesInYourLanguage
{
    public static class NameTranslatorProperty
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
        public static readonly FieldInfo bankNamesAccessor = AccessTools.Field(typeof(NameBank), "names");
    }
}