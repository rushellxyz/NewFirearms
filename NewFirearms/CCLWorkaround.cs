using System.Collections.Generic;
using HarmonyLib;

namespace NewFirearms
{
    // CCL can't properly override some vanilla item behavior, so this shim exists.
    // The project intentionally keeps the compatibility layer small and explicit.
    [HarmonyPatch(typeof(Item), "SetupItems")]
    public static class CCLWorkaround
    {
        public static Dictionary<string, (string, string)> itemsToDesc;

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
            Item.GlobalItems["pistol"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshGun>().RemoveMag(body);
            };
            Item.GlobalItems["shotgun"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshGun>().RemoveMag(body);
            };
            Item.GlobalItems["rifle"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshGun>().RemoveMag(body);
            };
            Item.GlobalItems["makeshiftrifle"].useAction = delegate(Body body, Item item)
            {
                item.GetComponent<RshGun>().RemoveMag(body);
            };

            foreach (KeyValuePair<string, (string, string)> kvp in itemsToDesc)
            {
                Item.GlobalItems[kvp.Key].description = kvp.Value.Item1;
                Item.GlobalItems[kvp.Key].fullName = kvp.Value.Item2;
            }
        }

        static void Prepare()
        {
            itemsToDesc = new Dictionary<string, (string, string)>();
        }
    }
}
