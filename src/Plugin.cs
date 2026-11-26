using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BothThighSlots
{
    [BepInPlugin(Guid, "Both Thigh Slots", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "humanresistor.cu.boththighslots";
        public const string Version = "1.5.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> CfgEnabled;
        internal static ConfigEntry<bool> CfgVerbose;
        internal static ConfigEntry<bool> CfgMirrorSorting;
        internal static ConfigEntry<string> CfgGroupSlots;
        internal static ConfigEntry<bool> CfgShowLimb;

        internal static HashSet<string> GroupSlots = new HashSet<string> { "thigh", "thighback" };

        private void Awake()
        {
            Log = Logger;
            CfgEnabled = Config.Bind("General", "Enabled", true,
                "允许腿部挂包（材料包/腿包）在两条腿之间自由分配，每条腿最多一件。");
            CfgVerbose = Config.Bind("General", "VerboseLog", true, "换腿时在日志里打印一行。");
            CfgMirrorSorting = Config.Bind("General", "MirrorSortingOffset", true,
                "换到另一条腿时翻转贴图排序偏移（材料包原本画在腿后，换到前腿后改为画在腿前）。");
            CfgGroupSlots = Config.Bind("General", "GroupSlots", "thigh,thighback",
                "视为同一组“腿部挂包”的穿戴槽位，组内每条腿最多一件，逗号分隔。");

            CfgShowLimb = Config.Bind("General", "ShowLimbInTooltip", true,
                "在物品描述顶部标出它挂在哪条腿上（不用按 Shift 展开也能看到）。");

            ReloadGroup();
            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(WearWearablePatch));
            harmony.PatchAll(typeof(HoverDescriptionPatch));
            harmony.PatchAll(typeof(GetWearablePatch));
            Log.LogInfo("Both Thigh Slots v" + Version + " 已加载。组槽位: " + string.Join(",", GroupSlots));
        }

        internal static void ReloadGroup()
        {
            var set = new HashSet<string>();
            foreach (string part in (CfgGroupSlots.Value ?? "").Split(','))
            {
                string s = part.Trim().ToLowerInvariant();
                if (s.Length > 0) set.Add(s);
            }
            if (set.Count == 0)
            {
                set.Add("thigh");
                set.Add("thighback");
            }
            GroupSlots = set;
        }

        /// <summary>把肢体名的 F/B 后缀互换，得到配对的另一侧（ThighB -> ThighF）；名字不以 F/B 结尾返回 null。</summary>
        internal static string MirrorLimb(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 2) return null;
            char last = name[name.Length - 1];
            char repl;
            if (last == 'F') repl = 'B';
            else if (last == 'B') repl = 'F';
            else return null;
            return name.Substring(0, name.Length - 1) + repl;
        }
    }

    [HarmonyPatch(typeof(Body), nameof(Body.WearWearable))]
    internal static class WearWearablePatch
    {
        /// <summary>取该肢体上属于“腿部挂包组”的物品（没有则 null）。</summary>
        private static Item GroupItemOn(Limb limb)
        {
            if (limb == null) return null;
            foreach (Transform child in limb.transform)
            {
                Item it = child.GetComponent<Item>();
                if (it == null || it.Stats == null || !it.Stats.wearable) continue;
                string slot = it.Stats.wearSlotId;
                if (slot != null && Plugin.GroupSlots.Contains(slot.ToLowerInvariant())) return it;
            }
            return null;
        }

        [HarmonyPrefix]
        private static bool Prefix(Body __instance, Item item)
        {
            if (!Plugin.CfgEnabled.Value || item == null) return true;

            ItemInfo stats = item.Stats;
            if (stats == null || !stats.wearable) return true;
            string slot = stats.wearSlotId;
            if (slot == null || !Plugin.GroupSlots.Contains(slot.ToLowerInvariant())) return true;

            string want = stats.desiredWearLimb;
            if (string.IsNullOrEmpty(want)) return true;

            Limb primary = __instance.LimbByName(want);
            if (primary == null) return true;

            string mirrorName = Plugin.MirrorLimb(want);
            Limb mirror = string.IsNullOrEmpty(mirrorName) ? null : __instance.LimbByName(mirrorName);

            // 按「原定腿 -> 对侧腿」找第一条没断、且组内一件都没有的腿
            Limb chosen = null;
            if (!primary.dismembered && GroupItemOn(primary) == null) chosen = primary;
            else if (mirror != null && !mirror.dismembered && GroupItemOn(mirror) == null) chosen = mirror;

            bool mirrored = chosen != null && chosen != primary;
            bool vanillaAllows = !primary.dismembered && __instance.GetWearableBySlotID(slot) == null;

            if (chosen == null)
            {
                // 两条腿都挂满了：如果原版本来会放行（不同槽位不互斥），这里要拦住
                if (vanillaAllows)
                {
                    Item blocker = GroupItemOn(primary) ?? GroupItemOn(mirror);
                    string msg = Locale.GetOther("alertalreadywearing")
                        .Replace("<1>", stats.rec.recognizable ? item.fullName : Locale.GetOther("unknownobject"))
                        .Replace("<2>", blocker != null && blocker.Stats.rec.recognizable ? blocker.fullName : Locale.GetOther("unknownobject"));
                    if (PlayerCamera.main != null) PlayerCamera.main.DoAlert(msg);
                    return false;
                }
                return true;   // 交给原版（肢体缺失 / 已经穿着）
            }

            // 原版本来就会放行且就是用原定腿 -> 交给原版，行为完全一致
            if (!mirrored && vanillaAllows) return true;

            if (!__instance.DoPickupCheck(item)) return false;

            WearOn(__instance, item, chosen, mirrored);

            if (Plugin.CfgVerbose.Value)
                Plugin.Log.LogInfo("腿部挂包 " + item.id + "（槽位 " + slot + "，原定 " + want + "）挂到 "
                    + chosen.name + (mirrored ? "（换侧，已翻转排序偏移）" : ""));
            return false;
        }

        private static void WearOn(Body body, Item item, Limb limb, bool mirrored)
        {
            ItemInfo stats = item.Stats;
            body.CreateCloudMini(item.transform.position);

            Container cont;
            if (item.TryGetParentContainer(out cont))
            {
                cont.UnloadItem(item);
                if (PlayerCamera.main != null) PlayerCamera.main.PlayBackpackSound();
            }

            item.rb.simulated = false;
            SpriteRenderer itemSprite = item.GetComponent<SpriteRenderer>();
            SpriteRenderer limbSprite = limb.GetComponent<SpriteRenderer>();

            int visualOffset = stats.wearableVisualOffset;
            if (mirrored && Plugin.CfgMirrorSorting.Value) visualOffset = -visualOffset;
            itemSprite.sortingOrder = limbSprite.sortingOrder + visualOffset;

            item.transform.SetParent(limb.transform);
            item.transform.localScale = Vector3.one;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localPosition = Vector3.zero;

            WearableSpriteSwap swap = null;
            if (item.TryGetComponent<WearableSpriteSwap>(out swap))
            {
                int ct = body.charType;
                if (swap.sprites != null && swap.sprites.Count > ct && swap.sprites[ct] != null)
                    itemSprite.sprite = swap.sprites[ct];
                if (swap.customScale != null && swap.customScale.Count > ct && swap.customScale[ct] != 0f)
                    item.transform.localScale = Vector3.one * swap.customScale[ct];
            }

            Wearable wearable = null;
            if (item.TryGetComponent<Wearable>(out wearable))
                wearable.CreateSprites(body, swap);
        }
    }
}

namespace BothThighSlots
{
    /// <summary>在物品悬停描述最前面加一行“位置：左大腿/右大腿”，折叠状态也可见。</summary>
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    internal static class HoverDescriptionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Item item, ref System.ValueTuple<string, string> __result)
        {
            if (!Plugin.CfgShowLimb.Value || item == null || item.Stats == null || !item.Stats.wearable) return;

            Transform parent = item.transform.parent;
            if (parent == null) return;
            Limb limb = parent.GetComponent<Limb>();
            if (limb == null) return;

            string slot = item.Stats.wearSlotId;
            if (slot == null || !Plugin.GroupSlots.Contains(slot.ToLowerInvariant())) return;

            string label = Locale.GetOther(limb.gameObject.name);
            if (string.IsNullOrEmpty(label) || label == limb.gameObject.name) label = limb.shortName;

            bool zh = (Locale.currentLangName ?? "").ToLowerInvariant().StartsWith("zh");
            string head = zh ? "位置：" : "Position: ";
            string line = "<color=#91d1ff><sprite index=7 tint=1>" + head + label + "</color>\n";

            __result.Item2 = line + __result.Item2;
        }
    }
}

namespace BothThighSlots
{
    /// <summary>
    /// 原版 Body.GetWearable(itemid) 只查该物品「原定肢体」，导致：
    ///   - 被换到对侧腿的包查不到自己 → 拖不下来
    ///   - 同 id 的第二件也依赖第一件在原定腿上才判定成功
    /// 这里在结果为 null 时兜底：扫描全部肢体，找 id 相同的穿戴物返回。
    /// </summary>
    [HarmonyPatch(typeof(Body), nameof(Body.GetWearable), new System.Type[] { typeof(string) })]
    internal static class GetWearablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance, string itemid, ref Item __result)
        {
            if (__result != null || string.IsNullOrEmpty(itemid) || __instance == null || __instance.limbs == null) return;

            foreach (Limb limb in __instance.limbs)
            {
                if (limb == null) continue;
                foreach (Transform child in limb.transform)
                {
                    Item it = child.GetComponent<Item>();
                    if (it != null && it.id == itemid && it.Stats != null && it.Stats.wearable)
                    {
                        __result = it;
                        return;
                    }
                }
            }
        }
    }
}
