using Verse;
using UnityEngine;

namespace TranslatedNames
{
    public class TN_ModSettings : ModSettings
    {
        public bool activate = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref activate, "TranslatedNames_Activation");
            base.ExposeData();
        }
    }

    public class TN_Mod : Mod
    {
        TN_ModSettings settings;

        public TN_Mod(ModContentPack content) : base(content) 
        {
            this.settings = GetSettings<TN_ModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("RMK.TranslatedNamesActiveLabel".Translate(), ref settings.activate, "RMK.TranslatedNamesActiveDesc".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RMK.ModuleName".Translate();
        }
    }
}