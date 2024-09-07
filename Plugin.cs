using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using Gunfiguration;
using HarmonyLib;
using UnityEngine;

namespace ConfigurableRatChests
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "spapi.etg.configurableratchests";
        public const string NAME = "Configurable Rat Chests";
        public const string VERSION = "1.0.2";

        public static Gunfig gunfig;

        public static float RatChest_D_Weight;
        public static float RatChest_C_Weight;
        public static float RatChest_B_Weight;
        public static float RatChest_A_Weight;
        public static float RatChest_S_Weight;

        public const string USE_RAT_ITEMS = "Use rat items?";

        public const string POOL_MODE = "Loot pool mode";
        public const string POOL_MODE_RANDOM = "Random";
        public const string POOL_MODE_ALLGUNS = "Always guns";
        public const string POOL_MODE_ALLITEMS = "Always items";

        public const string D_WEIGHT = "D tier item weight";
        public const string C_WEIGHT = "C tier item weight";
        public const string B_WEIGHT = "B tier item weight";
        public const string A_WEIGHT = "A tier item weight";
        public const string S_WEIGHT = "S tier item weight";

        public void Awake()
        {
            gunfig = Gunfig.Get(NAME);

            var weights = new List<string>()
            {
                "0",
                "0.1",
                "0.2",
                "0.3",
                "0.4",
                "0.5",
                "0.6",
                "0.7",
                "0.8",
                "0.9",
                "1"
            };

            var aWeights = new List<string>()
            {
                "1",
                "0",
                "0.1",
                "0.2",
                "0.3",
                "0.4",
                "0.5",
                "0.6",
                "0.7",
                "0.8",
                "0.9",
            };

            gunfig.AddToggle(USE_RAT_ITEMS, true, USE_RAT_ITEMS.Yellow());

            gunfig.AddScrollBox(POOL_MODE, [POOL_MODE_RANDOM, POOL_MODE_ALLGUNS, POOL_MODE_ALLITEMS], POOL_MODE.Cyan());

            gunfig.AddScrollBox(D_WEIGHT, weights,  D_WEIGHT.WithColor(new(1f, 0.5f, 0f)),  WeightSetCallback);
            gunfig.AddScrollBox(C_WEIGHT, weights,  C_WEIGHT.Blue(),                        WeightSetCallback);
            gunfig.AddScrollBox(B_WEIGHT, weights,  B_WEIGHT.Green(),                       WeightSetCallback);
            gunfig.AddScrollBox(A_WEIGHT, aWeights, A_WEIGHT.Red(),                         WeightSetCallback);
            gunfig.AddScrollBox(S_WEIGHT, weights, S_WEIGHT,                                WeightSetCallback);

            SetWeightsAndUpdateRatChestChances();

            new Harmony(GUID).PatchAll();
        }

        public static void WeightSetCallback(string key, string value)
        {
            if (!float.TryParse(value, out var weight))
                return;

            switch (key)
            {
                case D_WEIGHT:
                    RatChest_D_Weight = weight;
                    break;

                case C_WEIGHT:
                    RatChest_C_Weight = weight;
                    break;

                case B_WEIGHT:
                    RatChest_B_Weight = weight;
                    break;

                case A_WEIGHT:
                    RatChest_A_Weight = weight;
                    break;

                case S_WEIGHT:
                    RatChest_S_Weight = weight;
                    break;
            }

            UpdateRatChestChances();
        }

        public static void SetWeightsAndUpdateRatChestChances(Chest ratChest = null)
        {
            if (float.TryParse(gunfig.Value(D_WEIGHT), out var d))
                RatChest_D_Weight = d;

            if (float.TryParse(gunfig.Value(C_WEIGHT), out var c))
                RatChest_C_Weight = c;

            if(float.TryParse(gunfig.Value(B_WEIGHT), out var b))
                RatChest_B_Weight = b;

            if(float.TryParse(gunfig.Value(A_WEIGHT), out var a))
                RatChest_A_Weight = a;

            if(float.TryParse(gunfig.Value(S_WEIGHT), out var s))
                RatChest_S_Weight = s;

            UpdateRatChestChances(ratChest);
        }

        public static void UpdateRatChestChances(Chest ratChest = null)
        {
            if(ratChest == null)
                ratChest = LoadHelper.LoadAssetFromAnywhere<GameObject>("chest_rat").GetComponent<Chest>();

            if (ratChest == null || ratChest.lootTable == null)
                return;

            var loot = ratChest.lootTable;

            var sum =
                RatChest_D_Weight +
                RatChest_C_Weight +
                RatChest_B_Weight +
                RatChest_A_Weight +
                RatChest_S_Weight;

            loot.Common_Chance = 0f;

            if(sum <= 0f)
            {
                loot.D_Chance = 0f;
                loot.C_Chance = 0f;
                loot.B_Chance = 0f;
                loot.A_Chance = 0f;
                loot.S_Chance = 0f;

                return;
            }

            loot.D_Chance = RatChest_D_Weight / sum;
            loot.C_Chance = RatChest_C_Weight / sum;
            loot.B_Chance = RatChest_B_Weight / sum;
            loot.A_Chance = RatChest_A_Weight / sum;
            loot.S_Chance = RatChest_S_Weight / sum;
        }

        [HarmonyPatch(typeof(ResourcefulRatRewardRoomController), nameof(ResourcefulRatRewardRoomController.Start))]
        [HarmonyPostfix]
        public static void DisableForcedDrops(ResourcefulRatRewardRoomController __instance)
        {
            if (__instance == null || __instance.m_ratChests == null)
                return;

            foreach (var ch in __instance.m_ratChests)
            {
                if (ch == null)
                    continue;

                SetWeightsAndUpdateRatChestChances(ch);

                var poolmode = gunfig.Value(POOL_MODE);

                if (poolmode == POOL_MODE_ALLGUNS)
                    ch.lootTable.lootTable = GameManager.Instance.RewardManager.GunsLootTable;

                if (poolmode == POOL_MODE_ALLITEMS)
                    ch.lootTable.lootTable = GameManager.Instance.RewardManager.ItemsLootTable;

                else
                    ch.lootTable.lootTable = BraveUtility.RandomBool() ?
                        GameManager.Instance.RewardManager.GunsLootTable :
                        GameManager.Instance.RewardManager.ItemsLootTable;

                if (gunfig.Enabled(USE_RAT_ITEMS))
                    continue;

                ch.forceContentIds ??= [];
                ch.forceContentIds.Clear();
            }
        }
    }
}
