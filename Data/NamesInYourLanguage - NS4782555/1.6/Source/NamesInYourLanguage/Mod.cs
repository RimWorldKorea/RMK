using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NamesInYourLanguage
{
    public class NIYL_Mod : Mod
    {
        NIYL_Settings settings;
        public NIYL_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<NIYL_Settings>();

            Harmony harmony = new Harmony("seohyeon.namesinyourlanguage");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // 활성화 여부를 설정하는 체크박스입니다.
            listing.CheckboxLabeled("RMK.NIYL.Enable.Label".Translate(), ref settings.Enable, "RMK.NIYL.Enable.Desc".Translate());

            // 설정을 바꿀 경우 안내 문구를 띄웁니다.
            if(StaticConstructor.loadedEnableSetting != LoadedModManager.GetMod<NIYL_Mod>().GetSettings<NIYL_Settings>().Enable)
            {
                using (new TextBlock(TextAnchor.MiddleCenter))
                {
                    listing.Label("RMK.NIYL.Enable.Notify".Translate());
                }
            }
            else
            {
                listing.Gap((float)23.7); // 23.7! if:else 양쪽에서 똑같은 줄 간격이 유지되는 값입니다. 적어도 작성자의 환경에서는..
            }

            // 빈 공간 삽입
            listing.Gap(Listing_Standard.DefaultGap);

            // 이름 추출 버튼
            if (listing.ButtonText("RMK.NIYL.Export.BottonLabel".Translate(), null, (float)0.28))
            {
                List<string> allNames = new List<string>(); // 여기에 내보낼 이름 데이터를 저장합니다.

                // 이미 번역된 이름도 정리해서 담아둡니다.
                foreach (var (key, tuple) in StaticConstructor.NameTranslationDict)
                {
                    NameTriple triple = tuple.Item2;
                    string tripleStrip = string.Empty;
                    if (triple != null)
                        tripleStrip = $"<{triple.First}::{triple.Nick}::{triple.Last}>";

                    allNames.Add($"{tripleStrip}{key}->{tuple.Item1}");
                }

                allNames = allNames.Distinct().ToList();
                allNames.Sort();

                // 번역되지 않은 이름을 정리해서 담아둡니다.
                foreach (var (key, tuple) in StaticConstructor.NotTranslated)
                {
                    NameTriple triple = tuple.Item2;
                    string tripleStrip = string.Empty;
                    if (triple != null)
                        tripleStrip = $"<{triple.First}::{triple.Nick}::{triple.Last}>";

                    allNames.Add($"{tripleStrip}{key}->{tuple.Item1}");
                }

                // 내보낼 파일 내용 앞 부분에 덧붙일 안내문을 준비합니다.
                string[] Export_Instruction = "RMK.NIYL.Export.Instruction".Translate().ToString().Split('\n');
                for (int i = 0; i < Export_Instruction.Length; i++)
                {
                    Export_Instruction[i] = "// " + Export_Instruction[i];
                }

                // 여기에다 내보낼 데이터를 모두 담을겁니다.
                List<string> linesToExport = new List<string>(Export_Instruction.ToList());
                linesToExport.AddRange(allNames);

                // 정리된 이름 데이터를 바탕화면에 내보냅니다.
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Translations.txt");
                try
                {
                    File.WriteAllLines(path, linesToExport);

                    MessageTypeDef RMK_ExportComplete = new MessageTypeDef();
                    Messages.Message("RMK.NIYL.Export.Success".Translate(path), RMK_ExportComplete, false);
                }
                catch
                {
                    Log.Error("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Export.Failed".Translate());
                }
            }

            listing.End();
        }

        // 모드 설정 창에서 보여지는 이름입니다.
        public override string SettingsCategory()
        {
            return "RMK.NIYL.ModTitle".Translate();
        }
    }
}