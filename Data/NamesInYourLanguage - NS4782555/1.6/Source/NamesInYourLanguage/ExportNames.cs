using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Verse;

using static NamesInYourLanguage.Constants;

namespace NamesInYourLanguage
{
    public static class ExportNames
    {
        public static void Execute()
        {
            // 내보낼 solidName 데이터를 준비합니다
            List<string> solidNamesExport_Translated = new List<string>();
            List<string> solidNamesExport_NickNotTranslated = new List<string>();
            List<string> solidNamesExport_NotFullyTranslated = new List<string>();
            foreach (var nameEntry in NameTranslator.solidNamesOriginal)
            {
                NameTriple activeTriple = NameTranslator.solidNames[nameEntry.Key];

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
            foreach (var nameEntry in NameTranslator.solidBioNamesOriginal)
            {
                NameTriple activeTriple = NameTranslator.solidBioNames[nameEntry.Key];

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
            foreach (var nameEntry in NameTranslator.shuffledNamesOriginal)
            {
                string originalName = nameEntry.Value;

                // 해당 이름이 속한 리스트를 찾습니다
                string keyForList = nameEntry.Key.Substring(0, nameEntry.Key.LastIndexOf('.'));
                List<string> activeList = NameTranslator.shuffledNameLists[keyForList];

                // 원문 정보로부터 인덱스를 얻어 키와 연관된 현재 적용된 이름을 찾습니다
                string activeName = activeList[NameTranslator.shuffledNamesOriginalIndex[nameEntry.Key]];

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
}