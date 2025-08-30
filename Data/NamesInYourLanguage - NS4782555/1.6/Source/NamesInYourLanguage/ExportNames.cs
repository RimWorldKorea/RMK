using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Verse;

using static NamesInYourLanguage.Constants;
using static NamesInYourLanguage.NameTranslatorProperty;

namespace NamesInYourLanguage
{
    public static class ExportNames
    {
        public static void Execute()
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            List<string> solidNamesExport_Translated = new List<string>();
            List<string> solidNamesExport_NickNotTranslated = new List<string>();
            List<string> solidNamesExport_NotFullyTranslated = new List<string>();
            
            // TranslationRequestRaw를 복사 (오리지널 데이터가 게임 중 변경될 수 있으므로 이 때 동작이 달라지는걸 고려)
            List<(string, string, string, string)> solidNamesExport_precursorList = new List<(string, string, string, string)>();
            foreach (var entry in solidNames_TranslationRequestRaw)
                solidNamesExport_precursorList.Add((entry.Item1, entry.Item2, entry.Item3, entry.Item4));
            
            // solidNamesExport_precursorList(아직 TranslationRequestRaw와 완전히 같은)를 첫번째 요소(키)를 기준으로 인덱스를 저장
            Dictionary<string, int> solidNames_TranslationRequestRawIndex = new Dictionary<string, int>();
            for (int i = 0; i < solidNamesExport_precursorList.Count; i++)
            {
                string key = solidNamesExport_precursorList[i].Item1;

                if (!solidNames_TranslationRequestRawIndex.ContainsKey(key))
                    solidNames_TranslationRequestRawIndex.Add(key, i);
                else
                {
                    string newComment = solidNamesExport_precursorList[i].Item4
                                     + " [Added by NIYL: The key of this line is duplicated]";
                    solidNamesExport_precursorList[i] = (
                        key,
                        solidNamesExport_precursorList[i].Item2,
                        solidNamesExport_precursorList[i].Item4,
                        newComment);
                }
            }
            
            List<(string, string, string, string)> notOnSolidNamesTXT = new List<(string, string, string, string)>();
            foreach (var entry in solidNames_Original)
            {
                string key = entry.Key;
                string currentName = $"{solidNames[key].First} '{solidNames[key].Nick}' {solidNames[key].Last}";
                
                // 오리지널 데이터 중 SolidNames.txt에 있는 것을 찾아서 전구체 리스트에 추가
                if (solidNames_TranslationRequestRawIndex.ContainsKey(key))
                {
                    int targetIndex = solidNames_TranslationRequestRawIndex[key];
                    
                    (string, string, string, string) entryInjection = (
                        key,
                        solidNames_Original[key].ToStringFullPossibly(),
                        currentName,
                        solidNamesExport_precursorList[targetIndex].Item4); // comment
                    
                    solidNamesExport_precursorList[targetIndex] = entryInjection;;
                }
                else // 없으면 따로 모아둠
                {
                    notOnSolidNamesTXT.Add((key, entry.Value.ToStringFullPossibly(), currentName, String.Empty));
                }
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            List<string> solidBioNamesExport_Translated = new List<string>();
            List<string> solidBioNamesExport_NickNotTranslated = new List<string>();
            List<string> solidBioNamesExport_NotFullyTranslated = new List<string>();
            
            // TranslationRequestRaw를 복사 (오리지널 데이터가 게임 중 변경될 수 있으므로 이 때 동작이 달라지는걸 고려)
            List<(string, string, string, string)> solidBioNamesExport_precursorList = new List<(string, string, string, string)>();
            foreach (var entry in solidBioNames_TranslationRequestRaw)
                solidBioNamesExport_precursorList.Add((entry.Item1, entry.Item2, entry.Item3, entry.Item4));
            
            // solidBioNamesExport_precursorList(아직 TranslationRequestRaw와 완전히 같은)를 첫번째 요소(키)를 기준으로 인덱스를 저장
            Dictionary<string, int> solidBioNames_TranslationRequestRawIndex = new Dictionary<string, int>();
            for (int i = 0; i < solidBioNamesExport_precursorList.Count; i++)
            {
                string key = solidBioNamesExport_precursorList[i].Item1;

                if (!solidBioNames_TranslationRequestRawIndex.ContainsKey(key))
                    solidBioNames_TranslationRequestRawIndex.Add(key, i);
                else
                {
                    string newComment = solidBioNamesExport_precursorList[i].Item4
                                        + " [Added by NIYL: The key of this line is duplicated]";
                    solidNamesExport_precursorList[i] = (
                        key,
                        solidBioNamesExport_precursorList[i].Item2,
                        solidBioNamesExport_precursorList[i].Item4,
                        newComment);
                }
            }
            
            List<(string, string, string, string)> notOnSolidBioNamesTXT = new List<(string, string, string, string)>();
            foreach (var entry in solidBioNames_Original)
            {
                string key = entry.Key;
                string currentName = $"{solidBioNames[key].First} '{solidBioNames[key].Nick}' {solidBioNames[key].Last}";
                
                // 오리지널 데이터 중 SolidBioNames.txt에 있는 것을 찾아서 전구체 리스트에 추가
                if (solidBioNames_TranslationRequestRawIndex.ContainsKey(key))
                {
                    int targetIndex = solidBioNames_TranslationRequestRawIndex[key];
                    
                    (string, string, string, string) entryInjection = (
                        key,
                        solidBioNames_Original[key].ToStringFullPossibly(),
                        currentName,
                        solidBioNamesExport_precursorList[targetIndex].Item4); // comment
                    
                    solidBioNamesExport_precursorList[targetIndex] = entryInjection;;
                }
                else // 없으면 따로 모아둠
                {
                    notOnSolidBioNamesTXT.Add((key, entry.Value.ToStringFullPossibly(), currentName, String.Empty));
                }
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            List<string> shuffledNamesExport_Translated = new List<string>();
            List<string> shuffledNamesExport_NotFullyTranslated = new List<string>();
            
            // TranslationRequestRaw를 복사 (오리지널 데이터가 게임 중 변경될 수 있으므로 이 때 동작이 달라지는걸 고려)
            List<(string, string, string, string)> shuffledNamesExport_precursorList = new List<(string, string, string, string)>();
            foreach (var entry in shuffledNames_TranslationRequestRaw)
                shuffledNamesExport_precursorList.Add((entry.Item1, entry.Item2, entry.Item3, entry.Item4));
            
            // shuffledNamesExport_precursorList(아직 TranslationRequestRaw와 완전히 같은)를 첫번째 요소(키)를 기준으로 인덱스를 저장
            Dictionary<string, int> shuffledNames_TranslationRequestRawIndex = new Dictionary<string, int>();
            for (int i = 0; i < shuffledNamesExport_precursorList.Count; i++)
            {
                string key = shuffledNamesExport_precursorList[i].Item1;

                if (!shuffledNames_TranslationRequestRawIndex.ContainsKey(key))
                    shuffledNames_TranslationRequestRawIndex.Add(key, i);
                else
                {
                    string newComment = shuffledNamesExport_precursorList[i].Item4
                                        + " [Added by NIYL: The key of this line is duplicated]";
                    solidNamesExport_precursorList[i] = (
                        key,
                        shuffledNamesExport_precursorList[i].Item2,
                        shuffledNamesExport_precursorList[i].Item4,
                        newComment);
                }
            }
            
            List<(string, string, string, string)> notOnShuffledNamesTXT = new List<(string, string, string, string)>();
            foreach (var entry in shuffledNames_Original)
            {
                string key = entry.Key;
                string listKey = key.Substring(0, key.LastIndexOf('.'));
                List<string> currentList = shuffledNameLists[listKey];
                string currentName = currentList[shuffledNames_OriginalIndex[key]];
                
                // 오리지널 데이터 중 ShuffledNames.txt에 있는 것을 찾아서 전구체 리스트에 추가
                if (shuffledNames_TranslationRequestRawIndex.ContainsKey(entry.Key))
                {
                    int targetIndex = shuffledNames_TranslationRequestRawIndex[key];
                    
                    (string, string, string, string) entryInjection = (
                        key,
                        entry.Value,
                        currentName,
                        shuffledNamesExport_precursorList[targetIndex].Item4); // comment
                    
                    shuffledNamesExport_precursorList[targetIndex] = entryInjection;;
                }
                else // 없으면 따로 모아둠
                {
                    notOnShuffledNamesTXT.Add((key, entry.Value, currentName, String.Empty));
                }
            }   
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            solidNamesExport_precursorList.AddRange(notOnSolidNamesTXT); // 이제 이게 실제 출력할 파일의 데이터
            TranslationResult isDeliveredTo = default; // 이건 주석 위치 결정용 트리거. 쓰고 나면 반드시 초기화!
            
            foreach (var entry in solidNamesExport_precursorList)
            {
                // text와 comment 동시에 비어있는 경우는 Import 단계에서 다 걸러졌을 것
                string text = entry.Item1.NullOrEmpty() ? String.Empty : $"{entry.Item1}({entry.Item2})->{entry.Item3}";
                string comment = entry.Item4.NullOrEmpty() ? String.Empty : entry.Item4;

                // 최종 텍스트 라인 준비
                string result;
                if (text.NullOrEmpty()) // 본문이 비어있고 주석은 있다
                    result = "/*" + comment;
                else if (comment.NullOrEmpty()) // 본문은 있는데 주석이 없다
                    result = text;
                else // 본문도 주석도 있다
                    result = text + " /*" + comment;
                
                // 번역이 어떻게 돼있는지 따라 분류
                // isDeliveredTo를 통해 다음 엔트리에 이전 줄의 분류 결과 넘기기
                if (!text.NullOrEmpty()) // 이름부가 유효하다!
                {
                    entry.Item2.ConvertToTriple(out NameTripleReduced originalName);
                    entry.Item3.ConvertToTriple(out NameTripleReduced activeName);

                    if (originalName.First == activeName.First || originalName.Last == activeName.Last)
                    {
                        solidNamesExport_NotFullyTranslated.Add(result);
                        isDeliveredTo = TranslationResult.NotFullyTranslated;
                    }
                    else if (originalName.Nick == activeName.Nick)
                    {
                        solidNamesExport_NickNotTranslated.Add(result);
                        isDeliveredTo = TranslationResult.NickNotTranslated;
                    }
                    else
                    {
                        solidNamesExport_Translated.Add(result);
                        isDeliveredTo = TranslationResult.Translated;
                    }
                }
                else // 이름부가 없다! -> 주석만 있다
                {
                    switch (isDeliveredTo)
                    {
                        case TranslationResult.None:
                        case TranslationResult.NotFullyTranslated:
                            solidNamesExport_NotFullyTranslated.Add(result); break;
                        case TranslationResult.NickNotTranslated:
                            solidNamesExport_NickNotTranslated.Add(result); break;
                        case TranslationResult.Translated:
                            solidNamesExport_Translated.Add(result); break;
                    }
                }
            }
            isDeliveredTo = TranslationResult.None;
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

            solidBioNamesExport_precursorList.AddRange(notOnSolidBioNamesTXT);
            
            foreach (var entry in solidBioNamesExport_precursorList)
            {
                // text와 comment 동시에 비어있는 경우는 Import 단계에서 다 걸러졌을 것
                string text = entry.Item1.NullOrEmpty() ? String.Empty : $"{entry.Item1}({entry.Item2})->{entry.Item3}";
                string comment = entry.Item4.NullOrEmpty() ? String.Empty : entry.Item4;
                
                string result;
                if (text.NullOrEmpty()) // 본문이 비어있고 주석은 있다
                    result = "/*" + comment;
                else if (comment.NullOrEmpty()) // 본문은 있는데 주석이 없다
                    result = text;
                else // 본문도 주석도 있다
                    result = text + " /*" + comment;

                // 번역이 어떻게 돼있는지 따라 분류
                // isDeliveredTo를 통해 다음 엔트리에 이전 줄의 분류 결과 넘기기
                if (!text.NullOrEmpty()) // 이름부가 유효하다!
                {
                    entry.Item2.ConvertToTriple(out NameTripleReduced originalName);
                    entry.Item3.ConvertToTriple(out NameTripleReduced activeName);

                    if (originalName.First == activeName.First || originalName.Last == activeName.Last)
                    {
                        solidBioNamesExport_NotFullyTranslated.Add(result);
                        isDeliveredTo = TranslationResult.NotFullyTranslated;
                    }
                    else if (originalName.Nick == activeName.Nick)
                    {
                        solidBioNamesExport_NickNotTranslated.Add(result);
                        isDeliveredTo = TranslationResult.NickNotTranslated;
                    }
                    else
                    {
                        solidBioNamesExport_Translated.Add(result);
                        isDeliveredTo = TranslationResult.Translated;
                    }
                }
                else // 이름부가 없다! -> 주석만 있다
                {
                    switch (isDeliveredTo)
                    {
                        case TranslationResult.None:
                        case TranslationResult.NotFullyTranslated:
                            solidBioNamesExport_NotFullyTranslated.Add(result); break;
                        case TranslationResult.NickNotTranslated:
                            solidBioNamesExport_NickNotTranslated.Add(result); break;
                        case TranslationResult.Translated:
                            solidBioNamesExport_Translated.Add(result); break;
                    }
                }
            }
            isDeliveredTo = TranslationResult.None;
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            shuffledNamesExport_precursorList.AddRange(notOnShuffledNamesTXT);
            
            foreach (var entry in shuffledNamesExport_precursorList)
            {
                // text와 comment 동시에 비어있는 경우는 Import 단계에서 다 걸러졌을 것
                string text = entry.Item1.NullOrEmpty() ? String.Empty : $"{entry.Item1}({entry.Item2})->{entry.Item3}";
                string comment = entry.Item4.NullOrEmpty() ? String.Empty : entry.Item4;

                string result;
                if (text.NullOrEmpty()) // 본문이 비어있고 주석은 있다
                    result = "/*" + comment;
                else if (comment.NullOrEmpty()) // 본문은 있는데 주석이 없다
                    result = text;
                else // 본문도 주석도 있다
                    result = text + " /*" + comment;

                if (!text.NullOrEmpty())
                {
                    string originalName = entry.Item2;
                    string activeName = entry.Item3;

                    if (originalName == activeName)
                    {
                        shuffledNamesExport_NotFullyTranslated.Add(result);
                        isDeliveredTo = TranslationResult.NotFullyTranslated;
                    }
                    else
                    {
                        shuffledNamesExport_Translated.Add(result);
                        isDeliveredTo = TranslationResult.Translated;
                    }
                }
                else
                {
                    switch (isDeliveredTo)
                    {
                        case TranslationResult.None:
                        case TranslationResult.NotFullyTranslated:
                            shuffledNamesExport_NotFullyTranslated.Add(result); break;
                        case TranslationResult.Translated:
                            shuffledNamesExport_Translated.Add(result); break;
                    }
                }
            }
            isDeliveredTo = TranslationResult.None;
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            // 내보낼 텍스트 상단에 위치할 공통 안내문을 준비합니다
            List<string> intro = "NIYL.Export.Introduction".Translate().ToString().Split('\n').ToList();
            for (int i = 0; i < intro.Count; i++)
            {
                intro[i] = "// " + intro[i];
            }
            
            // 내보낼 텍스트에 있어야할 데이터 및 기타 요소를 등록합니다.
            List<string> SolidNames_txt = new List<string>(intro);

            SolidNames_txt.Add("\n// 1. [Fully Translated]");
            if (solidNamesExport_Translated.Count == 0) SolidNames_txt.Add("// - No Items -");
            else SolidNames_txt.AddRange(solidNamesExport_Translated);

            SolidNames_txt.Add("\n// 2. [Nick name is same with original name]");
            if (solidNamesExport_NickNotTranslated.Count == 0) SolidNames_txt.Add("// - No Items -");
            else SolidNames_txt.AddRange(solidNamesExport_NickNotTranslated);

            SolidNames_txt.Add("\n// 3. [First name or Last name is same with original name]");
            if (solidNamesExport_NotFullyTranslated.Count == 0) SolidNames_txt.Add("// - No Items -");
            else SolidNames_txt.AddRange(solidNamesExport_NotFullyTranslated);
            
            // 내보낼 텍스트에 있어야할 데이터 및 기타 요소를 등록합니다.
            List<string> SolidBioNames_txt = new List<string>(intro);

            SolidBioNames_txt.Add("\n// 1. [Fully Translated]");
            if (solidBioNamesExport_Translated.Count == 0) SolidBioNames_txt.Add("// - No Items -");
            else SolidBioNames_txt.AddRange(solidBioNamesExport_Translated);

            SolidBioNames_txt.Add("\n// 2. [Nick name is same with original name]");
            if (solidBioNamesExport_NickNotTranslated.Count == 0) SolidBioNames_txt.Add("// - No Items -");
            else SolidBioNames_txt.AddRange(solidBioNamesExport_NickNotTranslated);

            SolidBioNames_txt.Add("\n// 3. [First name or Last name is same with original name]");
            if (solidBioNamesExport_NotFullyTranslated.Count == 0) SolidBioNames_txt.Add("// - No Items -");
            else SolidBioNames_txt.AddRange(solidBioNamesExport_NotFullyTranslated);

            // 내보낼 텍스트에 있어야할 데이터 및 기타 요소를 등록합니다.
            List<string> shuffledNames_txt = new List<string>(intro);

            shuffledNames_txt.Add("\n// 1. [Translated]");
            if (shuffledNamesExport_Translated.Count == 0) shuffledNames_txt.Add("// - No Items -");
            else shuffledNames_txt.AddRange(shuffledNamesExport_Translated);

            shuffledNames_txt.Add("\n// 2. [May not be translated]");
            if (shuffledNamesExport_NotFullyTranslated.Count == 0) shuffledNames_txt.Add("// - No Items -");
            else shuffledNames_txt.AddRange(shuffledNamesExport_NotFullyTranslated);
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // 정리된 이름 데이터를 바탕화면에 내보냅니다.
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try
            {
                File.WriteAllLines(Path.Combine(desktopPath, fileName_SolidNames + ".txt"), SolidNames_txt);
                File.WriteAllLines(Path.Combine(desktopPath, fileName_SolidBioNames + ".txt"), SolidBioNames_txt);
                File.WriteAllLines(Path.Combine(desktopPath, fileName_ShuffledNames + ".txt"), shuffledNames_txt);

                MessageTypeDef NIYL_ExportComplete = new MessageTypeDef();
                Messages.Message("NIYL.Export.Success".Translate(desktopPath), NIYL_ExportComplete, false);
            }
            catch
            {
                Log.Error(logSignature + "NIYL.Export.Failed".Translate());
            }
            
        }
        
        public static void CleanExecute()
        {
            // 내보낼 solidName 데이터를 준비합니다
            List<string> solidNamesExport_Translated = new List<string>();
            List<string> solidNamesExport_NickNotTranslated = new List<string>();
            List<string> solidNamesExport_NotFullyTranslated = new List<string>();
            foreach (var nameEntry in solidNames_Original)
            {
                NameTriple activeTriple = solidNames[nameEntry.Key];

                string originalName = $"{nameEntry.Value.First} '{nameEntry.Value.Nick}' {nameEntry.Value.Last}";
                string activeName =
                    $"{activeTriple.First} '{activeTriple.Nick}' {activeTriple.Last}"; // NameTriple.ToStringFull은 Nick이 없을 경우 출력이 생략되므로 용도가 안맞음

                string textLine = $"{nameEntry.Key}({originalName})->{activeName}";

                if (nameEntry.Value.First == activeTriple.First || nameEntry.Value.Last == activeTriple.Last)
                    solidNamesExport_NotFullyTranslated.Add(textLine);
                else if (nameEntry.Value.Nick == activeTriple.Nick)
                    solidNamesExport_NickNotTranslated.Add(textLine);
                else
                    solidNamesExport_Translated.Add(textLine);
            }

            // 내보낼 solidBioName 데이터를 준비합니다
            List<string> solidBioNamesExport_Translated = new List<string>();
            List<string> solidBioNamesExport_NickNotTranslated = new List<string>();
            List<string> solidBioNamesExport_NotFullyTranslated = new List<string>();
            foreach (var nameEntry in solidBioNames_Original)
            {
                NameTriple activeTriple = solidBioNames[nameEntry.Key];

                string originalName = $"{nameEntry.Value.First} '{nameEntry.Value.Nick}' {nameEntry.Value.Last}";
                string activeName = $"{activeTriple.First} '{activeTriple.Nick}' {activeTriple.Last}";

                string textLine = $"{nameEntry.Key}({originalName})->{activeName}";

                if (nameEntry.Value.First == activeTriple.First || nameEntry.Value.Last == activeTriple.Last)
                    solidBioNamesExport_NotFullyTranslated.Add(textLine);
                else if (nameEntry.Value.Nick == activeTriple.Nick)
                    solidBioNamesExport_NickNotTranslated.Add(textLine);
                else
                    solidBioNamesExport_Translated.Add(textLine);
            }

            // 내보낼 shuffledName 데이터를 준비합니다
            List<string> shuffledNamesExport_Translated = new List<string>();
            List<string> shuffledNamesExport_NotFullyTranslated = new List<string>();
            foreach (var nameEntry in shuffledNames_Original)
            {
                string originalName = nameEntry.Value;

                // 해당 이름이 속한 리스트를 찾습니다
                string keyForList = nameEntry.Key.Substring(0, nameEntry.Key.LastIndexOf('.'));
                List<string> activeList = shuffledNameLists[keyForList];

                // 원문 정보로부터 인덱스를 얻어 키와 연관된 현재 적용된 이름을 찾습니다
                string activeName = activeList[shuffledNames_OriginalIndex[nameEntry.Key]];

                if (originalName == activeName)
                    shuffledNamesExport_NotFullyTranslated.Add($"{nameEntry.Key}({originalName})->{activeName}");
                else
                    shuffledNamesExport_Translated.Add($"{nameEntry.Key}({originalName})->{activeName}");
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            // 내보낼 텍스트 상단에 위치할 공통 안내문을 준비합니다
            List<string> intro = "NIYL.Export.Introduction".Translate().ToString().Split('\n').ToList();
            for (int i = 0; i < intro.Count; i++)
            {
                intro[i] = "// " + intro[i];
            }

            // 내보낼 텍스트에 있어야할 데이터 및 기타 요소를 등록합니다.
            List<string> SolidNames_txt = new List<string>(intro);

            SolidNames_txt.Add("\n// 1. [Fully Translated]");
            if (solidNamesExport_Translated.Count == 0) SolidNames_txt.Add("// - No Items -");
            else SolidNames_txt.AddRange(solidNamesExport_Translated);

            SolidNames_txt.Add("\n// 2. [Nick name is same with original name]");
            if (solidNamesExport_NickNotTranslated.Count == 0) SolidNames_txt.Add("// - No Items -");
            else SolidNames_txt.AddRange(solidNamesExport_NickNotTranslated);

            SolidNames_txt.Add("\n// 3. [First name or Last name is same with original name]");
            if (solidNamesExport_NotFullyTranslated.Count == 0) SolidNames_txt.Add("// - No Items -");
            else SolidNames_txt.AddRange(solidNamesExport_NotFullyTranslated);

            // 내보낼 텍스트에 있어야할 데이터 및 기타 요소를 등록합니다.
            List<string> SolidBioNames_txt = new List<string>(intro);

            SolidBioNames_txt.Add("\n// 1. [Fully Translated]");
            if (solidBioNamesExport_Translated.Count == 0) SolidBioNames_txt.Add("// - No Items -");
            else SolidBioNames_txt.AddRange(solidBioNamesExport_Translated);

            SolidBioNames_txt.Add("\n// 2. [Nick name is same with original name]");
            if (solidBioNamesExport_NickNotTranslated.Count == 0) SolidBioNames_txt.Add("// - No Items -");
            else SolidBioNames_txt.AddRange(solidBioNamesExport_NickNotTranslated);

            SolidBioNames_txt.Add("\n// 3. [First name or Last name is same with original name]");
            if (solidBioNamesExport_NotFullyTranslated.Count == 0) SolidBioNames_txt.Add("// - No Items -");
            else SolidBioNames_txt.AddRange(solidBioNamesExport_NotFullyTranslated);

            // 내보낼 텍스트에 있어야할 데이터 및 기타 요소를 등록합니다.
            List<string> shuffledNames_txt = new List<string>(intro);

            shuffledNames_txt.Add("\n// 1. [Translated]");
            if (shuffledNamesExport_Translated.Count == 0) shuffledNames_txt.Add("// - No Items -");
            else shuffledNames_txt.AddRange(shuffledNamesExport_Translated);

            shuffledNames_txt.Add("\n// 2. [May not be translated]");
            if (shuffledNamesExport_NotFullyTranslated.Count == 0) shuffledNames_txt.Add("// - No Items -");
            else shuffledNames_txt.AddRange(shuffledNamesExport_NotFullyTranslated);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            // 정리된 이름 데이터를 바탕화면에 내보냅니다.
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try
            {
                File.WriteAllLines(Path.Combine(desktopPath, fileName_SolidNames + ".txt"), SolidNames_txt);
                File.WriteAllLines(Path.Combine(desktopPath, fileName_SolidBioNames + ".txt"), SolidBioNames_txt);
                File.WriteAllLines(Path.Combine(desktopPath, fileName_ShuffledNames + ".txt"), shuffledNames_txt);

                MessageTypeDef NIYL_ExportComplete = new MessageTypeDef();
                Messages.Message("NIYL.Export.Success".Translate(desktopPath), NIYL_ExportComplete, false);
            }
            catch
            {
                Log.Error(logSignature + "NIYL.Export.Failed".Translate());
            }
        }
    }

    enum TranslationResult
    {
        None = 0,
        Translated,
        NotFullyTranslated,
        NickNotTranslated
    }
}