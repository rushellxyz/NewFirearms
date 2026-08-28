using HarmonyLib;

namespace NewFirearms
{
    // Worka around, around, around what?
    // Cuz my best guess that CCL overrides eveythinbg except onUseAction?! Very weird.
    // Anyway without this patch you cant unload smallmagazine boxof12gauge and riflemagazine when using CUCoreLib
    [HarmonyPatch(typeof(Item), "SetupItems")]
    static class CCLWorkaround
    {
        static void Postfix()
        {
            Item.GlobalItems["boxof12gauge"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshMag>().RemoveRound(body);
            };
            Item.GlobalItems["smallmagazine"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshMag>().RemoveRound(body);
            };
            Item.GlobalItems["riflemagazine"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshMag>().RemoveRound(body);
            };
        }

        static bool Prepare()
         => Plugin.useCuCore;
    }
}