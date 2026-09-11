using System.Collections.Generic;
using HarmonyLib;

namespace NewFirearms
{
    // CCL Can't preoperly overrite vanilla items
    // Боже мой, такой колхоз, хотя ccl supposed to make modding easier
    // Ааааа, почему CCL такой убогий, сидели бы мы на rshlib и не было бы проблем
    // да ИИ слоп было бы сложнее генерить без CCL но может это и к лучшему
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

        static bool Prepare()
        {
            if (Plugin.useCuCore)
            {
                itemsToDesc = new Dictionary<string, (string, string)>();
                return true;
            }
       else     return false;
        }
    }
}
