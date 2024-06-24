using HarmonyLib;
using Verse;

namespace NamesInYourLanguage
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(LanguageDatabase)), HarmonyPatch(nameof(LanguageDatabase.InitAllMetadata)), HarmonyPostfix]
        public static void Postfix_InitAllMetadata()
        {
            StaticConstructor.Prepare();
        }
    }
}
