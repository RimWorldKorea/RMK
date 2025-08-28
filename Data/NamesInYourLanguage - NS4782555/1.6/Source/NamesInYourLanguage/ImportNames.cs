using System;
using System.Collections.Generic;
using Verse;

using static NamesInYourLanguage.NameTranslatorProperty;
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
                List<(string, string)> paredText = importedRaw_SolidNames.PareText(); // (키-값, 주석)
                
                foreach ((string, string) paredLine in paredText)
                {
                    (string, string, string) brokenArrow = paredLine.Item1.BreakArrow(out _);
                    string comment = paredLine.Item2;
                    
                    // 번역 이름의 형식까지 유효하면 번역 코드에서 호출될 목록에 저장. 키의 유효성은 번역시 확인.
                    if (brokenArrow.Item3.SplitIntoTriple(out NameTripleReduced triple))
                        solidNames_TranslationRequest.Add(brokenArrow.Item1, triple);
                    else
                        comment += "[Added by System: Translation Name is Invalid]";
                
                    solidNames_TranslationRequestRaw.Add((brokenArrow.Item1, brokenArrow.Item2, brokenArrow.Item3, comment));
                }
            }
            
            // SolidBioNames.txt를 불러와 사용할 수 있는 형태로 변환합니다.
            if (Translator.TryGetTranslatedStringsForFile("Names/" + fileName_SolidBioNames, out List<string> importedRaw_SolidBioNames))
            {
                List<(string, string)> paredText = importedRaw_SolidBioNames.PareText(); // (키-값, 주석)
                
                foreach ((string, string) paredLine in paredText)
                {
                    (string, string, string) brokenArrow = paredLine.Item1.BreakArrow(out _);
                    string comment = paredLine.Item2;
                    
                    // 번역 이름의 형식까지 유효하면 번역 코드에서 호출될 목록에 저장. 키의 유효성은 번역시 확인.
                    if (brokenArrow.Item3.SplitIntoTriple(out NameTripleReduced triple))
                        solidBioNames_TranslationRequest.Add(brokenArrow.Item1, triple);
                    else
                        comment += "[Added by System: Translation Name is Invalid]";
                
                    solidBioNames_TranslationRequestRaw.Add((brokenArrow.Item1, brokenArrow.Item2, brokenArrow.Item3, comment));
                }
            }
            
            // ShuffledNames.txt를 불러와 사용할 수 있는 형태로 변환합니다.
            if (Translator.TryGetTranslatedStringsForFile("Names/" + fileName_ShuffledNames, out List<string> importedRaw_shuffledNames))
            {
                List<(string, string)> paredText = importedRaw_shuffledNames.PareText();

                foreach ((string, string) paredLine in paredText)
                {
                    (string, string, string) brokenArrow = paredLine.Item1.BreakArrow(out _);
                    string comment = paredLine.Item2;
                    
                    if (!brokenArrow.Item3.NullOrEmpty())
                        shuffledNames_TranslationRequest.Add(brokenArrow.Item1, brokenArrow.Item3);
                    else
                        comment += "[Added by System: Translation Name is Invalid]";
                    
                    shuffledNames_TranslationRequestRaw.Add((brokenArrow.Item1, brokenArrow.Item2, brokenArrow.Item3, comment));
                }
            }
        }

        /** 텍스트 전체에서 빈 줄을 날리고 주석을 보존합니다.
         *  '//'의 우측부는 Translator.TryGetTranslatedStringsForFile의 호출 과정에서 LoadFromFile_Strings에 의해 제거됩니다.
         *  NIYL에선 '/*'의 우측부를 보존합니다. '//*는' 보존되지 않습니다. 
         */
        public static List<(string, string)> PareText(this List<string> entireText)
        {
            List<(string, string)> result = new List<(string, string)>();
            foreach (string textLine in entireText)
            {
                if (textLine.Trim().NullOrEmpty()) continue;

                string beforeSlash = string.Empty;
                string afterSlash = string.Empty;
                
                int slashIndex = textLine.IndexOf("/*");
                
                // Substring의 유효성을 보장하기 위해 '/*' 이후에 빈 칸을 포함한 뭔가가 있는 경우만 진행
                if (slashIndex < 0)
                    beforeSlash = textLine;
                else
                {
                    beforeSlash = textLine.Substring(0, slashIndex).Trim();
                    if (slashIndex + 2 < textLine.Length)
                        afterSlash = textLine.Substring(slashIndex + 2);
                }

                result.Add((beforeSlash, afterSlash));
            }

            return result;
        }

        // (...)-> 를 부숩니다. 분절된 문자열의 번역 요청문으로써 유효성은 별도로 확인하세요.
        public static (string, string, string) BreakArrow(this string textLine, out string errorText)
        {
            string key = String.Empty;
            string originalRef = String.Empty;
            string value = String.Empty;
            
            if (textLine.Trim().NullOrEmpty())
            {
                errorText = null;
                return (key, originalRef, value);
            }

            int arrowIndex = textLine.IndexOf("->");
            try
            {
                string lhs = textLine.Substring(0, arrowIndex).Trim();
                value = textLine.Substring(arrowIndex + 2).Trim();

                int leftBraceIndex = lhs.IndexOf("(");
                int rightBraceIndex = lhs.LastIndexOf(")");
                
                key = lhs.Substring(0, leftBraceIndex < 0 ? lhs.Length : leftBraceIndex).Trim();
                
                if(leftBraceIndex >= 0 && leftBraceIndex < rightBraceIndex)
                    originalRef = textLine.Substring(leftBraceIndex + 1, rightBraceIndex - leftBraceIndex - 1);

                errorText = null;
            }
            catch
            {
                errorText = textLine;
                Log.Warning($"{logSignature} + {errorText} is invalid");
            }

            return (key, originalRef, value);
        }

        public static bool SplitIntoTriple(this string fullNameText, out NameTripleReduced triple)
        {
            // 빈 텍스트는 먼저 뱉어내기
            if (fullNameText.Trim().NullOrEmpty())
            {
                triple = default;
                return false;
            }
            
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
                Log.Warning(logSignature + "NIYL.Import.InvalidName".Translate(fullNameText));
                triple = default;
                return false;
            }
            
            triple = new NameTripleReduced(first, last, nick);
            return true;
        }
    }
}