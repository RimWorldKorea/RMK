using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using Verse;

using static NamesInYourLanguage.NameTranslator;
using static NamesInYourLanguage.Constants;

namespace NamesInYourLanguage
{
    [HarmonyPatch]
    public static class Patches
    {
        // LanguageDatabase.InitAllMetadata 실행 중 Languages 폴더가 등록되므로 번역 파일은 이 시점 이후에 로딩되어야 함
        // 그런데 정적 생성자 호출이 한참 이후에 진행되므로 굳이 필요하진 않은 것 같은데?..
        [HarmonyPatch(typeof(LanguageDatabase)), HarmonyPatch(nameof(LanguageDatabase.InitAllMetadata)), HarmonyPostfix]
        public static void Postfix_InitAllMetadata()
        {
            Log.Message("[NIYL.Debug] ImportNames.Execute()");
            ImportNames.Execute();
        }
    }

    public static class ImportNames
    {
        public static void Execute()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // SolidNames.txt를 불러와 사용할 수 있는 형태로 변환합니다.
            if (Translator.TryGetTranslatedStringsForFile("Names/" + fileName_SolidNames, out List<string> importedRaw_SolidNames))
            {
                Dictionary<string, string> crashPod = importedRaw_SolidNames.PareText().BreakArrow();
                foreach (var precursor in crashPod)
                {
                    if (precursor.Value.SplitIntoTriple(out NameTripleReduced triple))
                    {
                        solidNamesTranslationRequest.Add(precursor.Key, triple);
                    }
                }
            }
            
            // SolidBioNames.txt를 불러와 사용할 수 있는 형태로 변환합니다.
            if (Translator.TryGetTranslatedStringsForFile("Names/" + fileName_SolidBioNames, out List<string> importedRaw_SolidBioNames))
            {
                Dictionary<string, string> crashPod = importedRaw_SolidBioNames.PareText().BreakArrow();
                foreach (var precursor in crashPod)
                {
                    if (precursor.Value.SplitIntoTriple(out NameTripleReduced triple))
                    {
                        solidBioNamesTranslationRequest.Add(precursor.Key, triple);
                    }
                }
            }
            
            // ShuffledNames.txt를 불러와 사용할 수 있는 형태로 변환합니다.
            if (Translator.TryGetTranslatedStringsForFile("Names/" + fileName_ShuffledNames, out List<string> importedRaw_shuffledNames))
            {
                Dictionary<string, string> crashPod = importedRaw_shuffledNames.PareText().BreakArrow();
                foreach (var precursor in crashPod)
                    shuffledNamesTranslationRequest.Add(precursor.Key, precursor.Value);
            }
            
            stopwatch.Stop();
            TotalWorkTime += stopwatch.ElapsedMilliseconds;
        }

        // 주석과 빈 줄을 날립니다.
        public static List<string> PareText(this List<string> entireText)
        {
            List<string> result = new List<string>();
            foreach (string textLine in entireText)
            {
                string text = textLine;

                int leftEndIndex = text.IndexOf("//");
                if (leftEndIndex > 0) text = text.Substring(0, leftEndIndex);

                if (text.Trim().NullOrEmpty()) continue;

                result.Add(text);
            }

            return result;
        }

        // (...)-> 를 부숩니다. 분절된 문자열의 유효성은 보장됩니다.
        public static Dictionary<string, string> BreakArrow(this List<string> entireText)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string textLine in entireText)
            {
                string key, value;
                try
                {
                    int arrowIndex = textLine.IndexOf("->");

                    key = textLine.Substring(0, arrowIndex).Trim();
                    value = textLine.Substring(arrowIndex + 2).Trim();

                    int braceIndex = key.IndexOf("(");
                    key = key.Substring(0, braceIndex);

                    if (key.NullOrEmpty() || value.NullOrEmpty())
                        throw new Exception();
                }
                catch
                {
                    Log.Error("[RMK.NamesInYourLanguage] Invalid name translation request: " + textLine);
                    continue;
                }

                if (result.ContainsKey(key))
                {
                    Log.Error($"[RMK.NamesInYourLanguage] Translation key '{key}' is duplicated:");
                    continue;
                }
                result.Add(key, value);
            }
            
            return result;
        }

        public static bool SplitIntoTriple(this string fullNameText, out NameTripleReduced triple)
        {
            string first = null, last = null, nick = null;
            try
            {
                int leftAposIndex = fullNameText.IndexOf('\'');
                int rightAposIndex = fullNameText.LastIndexOf('\'');
                
                first = fullNameText.Substring(0, leftAposIndex).Trim();
                last = fullNameText.Substring(rightAposIndex + 1).Trim();
                nick = fullNameText.Substring(leftAposIndex + 1, rightAposIndex - leftAposIndex - 1).Trim();
                
                if (first.NullOrEmpty() || last.NullOrEmpty() || nick.NullOrEmpty())
                    throw new Exception();
            }
            catch
            {
                Log.Error("[RMK.NamesInYourLanguage] Invalid name for translation: " + fullNameText);
                triple = default;
                return false;
            }
            
            triple = new NameTripleReduced(first, last, nick);
            return true;
        }
    }
}