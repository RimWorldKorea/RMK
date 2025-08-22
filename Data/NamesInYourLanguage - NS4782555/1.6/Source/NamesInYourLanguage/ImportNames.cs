using System;
using System.Collections.Generic;
using Verse;

using static NamesInYourLanguage.NameTranslator;
using static NamesInYourLanguage.Constants;

namespace NamesInYourLanguage
{
    /** [2.0.0.0]
     * 원래 Harmony로 LanguageDatabase.InitAllMetadata 실행 시점 직후에 실행되도록 제어되고 있었는데
     * 림월드 초기화 순서를 검토해보니 딱히 그럴 필요가 없는 것 같아서 NameTranslator의 정적 생성자 시점으로 옮김
     * 왜인진 몰라도 실행시간이 5배 빨라졌어
     **/
    public static class ImportNames
    {
        public static void Execute()
        {
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
                    Log.Error(logSignature + "NIYL.Import.InvalidLine".Translate(textLine));
                    continue;
                }

                if (result.ContainsKey(key))
                {
                    Log.Error(logSignature + "NIYL.Import.DuplicatedKey".Translate(key));
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
                Log.Error(logSignature + "NIYL.Import.InvalidName".Translate(fullNameText));
                triple = default;
                return false;
            }
            
            triple = new NameTripleReduced(first, last, nick);
            return true;
        }
    }
}