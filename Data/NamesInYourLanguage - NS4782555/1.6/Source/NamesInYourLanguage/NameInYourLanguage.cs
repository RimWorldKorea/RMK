using UnityEngine;
using HarmonyLib;
using Verse;

namespace NamesInYourLanguage
{
    public class NIYL_Settings : ModSettings
    {
        public bool Enable = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref Enable, "NIYL_Enable", true); // 모드 설정 파일에서 설정을 가져옵니다.
            // base.ExposeData();
        }
    }
    
    public class NIYL : Mod
    {
        public override string SettingsCategory() { return "NIYL.ModTitle".Translate(); } // 모드 설정창 표시 이름
        NIYL_Settings settings;
        
        public NIYL(ModContentPack content) : base(content)
        {
            settings = GetSettings<NIYL_Settings>();

            Harmony harmony = new Harmony("seohyeon.namesinyourlanguage");
            harmony.PatchAll();
        }
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // 활성화 여부를 설정하는 체크박스입니다.
            listing.CheckboxLabeled("NIYL.Enable.Label".Translate(), ref settings.Enable, "NIYL.Enable.Desc".Translate());

            // 설정을 바꿀 경우 안내 문구를 띄웁니다.
            if(NameTranslator.loadedEnableSetting != LoadedModManager.GetMod<NIYL>().GetSettings<NIYL_Settings>().Enable)
            {
                using (new TextBlock(TextAnchor.MiddleCenter))
                {
                    listing.Label("NIYL.Enable.Notify".Translate());
                }
            }
            else
            {
                listing.Gap((float)23.7); // 23.7 <- if:else 어느 쪽이든 시각적으로 똑같은 줄 배열이 유지되는 값입니다.
            }
            
            listing.Gap();

            // 이름 추출 버튼 (불러온 번역 파일 정보를 보존)
            if (listing.ButtonText("NIYL.Export.ButtonLabel".Translate(), null, (float)0.28))
                ExportNames.CleanExecute();
            listing.Gap();

            // 이름 클린 추출 버튼 (불러온 파일과 관계없이 현재 게임에 적용된 데이터만으로 추출)
            if (listing.ButtonText("NIYL.Export.ButtonLabel".Translate(), null, (float)0.28))
                ExportNames.CleanExecute();
            
            listing.End();
        }
    }

    public static class Constants
    {
        public const string fileName_SolidNames = "SolidNames";
        public const string fileName_SolidBioNames = "SolidBioNames";
        public const string fileName_ShuffledNames = "ShuffledNames";
        
        public const string logSignature = "[RMK.NamesInYourLanguage] ";
    }
}