using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TombstoneDeathMod
{
    public class TombstonePlayer : ModPlayer
    {
        public Dictionary<Vector2, PlayerDeathInventory> playerDeathInventoryMap;
        int loadedValue;

        public override void Initialize()
        {
            playerDeathInventoryMap = new Dictionary<Vector2, PlayerDeathInventory>();
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            int x = (int)((player.position.X) / 16f);
            int y = (int)((player.position.Y) / 16f) + 2;

            bool isClearForTombstone = WorldGen.TileEmpty(x, y) && WorldGen.TileEmpty(x, y + 1) && WorldGen.TileEmpty(x, y - 1) && WorldGen.TileEmpty(x + 1, y - 1) && WorldGen.TileEmpty(x + 1, y - 1) && WorldGen.TileEmpty(x + 1, y - 1);

            // Check left 10 squares, then right 10 squares, then up one and repeat 20 times
            int movesY = 0;
            while (!isClearForTombstone && movesY++ < 20) {

                int movesX = 0;
                while (!isClearForTombstone && movesX++ < 10)
                {
                    x -= 1 * movesX;

                    isClearForTombstone = WorldGen.TileEmpty(x, y) && WorldGen.TileEmpty(x, y + 1) && WorldGen.TileEmpty(x, y - 1) && WorldGen.TileEmpty(x + 1, y - 1) && WorldGen.TileEmpty(x + 1, y - 1) && WorldGen.TileEmpty(x + 1, y - 1);

                    if (!isClearForTombstone)
                    {
                        x += 2 * movesX;

                        isClearForTombstone = WorldGen.TileEmpty(x, y) && WorldGen.TileEmpty(x, y + 1) && WorldGen.TileEmpty(x, y - 1) && WorldGen.TileEmpty(x + 1, y - 1) && WorldGen.TileEmpty(x + 1, y - 1) && WorldGen.TileEmpty(x + 1, y - 1);

                        if (!isClearForTombstone)
                        { // Restore x for next loop
                            x -= 1 * movesX;
                        }
                    }
                }

                if (!isClearForTombstone)
                {
                    y--;
                }
            }

            if (!isClearForTombstone)
            {
                // Revert to normal death
                Main.NewText("Unable to place tombstone. Reverting to normal death.", 255, 100, 100);
                return true;
            }

            Main.tile[x, y + 1].active(true);
            Main.tile[x + 1, y + 1].active(true);
            
            if (!WorldGen.PlaceTile(x, y, TileID.Tombstones, false, true, 1, 7))
            {
                // Revert to normal death
                Main.NewText("Unable to place tombstone. Reverting to normal death.", 255, 100, 100);
                return true;
            }

            int sign = Sign.ReadSign(x, y, true);
            if (sign >= 0)
            {
                Sign.TextSign(sign, player.name + "'s Stuff");
            }

            Vector2 tombStonePosition = new Vector2(x, y);

            PlayerDeathInventory previousInventory = null;

            if (playerDeathInventoryMap.TryGetValue(tombStonePosition, out previousInventory))
            {
                int oldItemValue = previousInventory.getValue();

                int newItemValue = 0;

                for (int i = 0; i < player.inventory.Length; i++)
                {
                    newItemValue += player.inventory[i].value;
                }

                for (int i = 0; i < player.armor.Length; i++)
                {
                    newItemValue += player.armor[i].value;
                }

                for (int i = 0; i < player.miscEquips.Length; i++)
                {
                    newItemValue += player.miscEquips[i].value;
                }

                // Remove previous inventory only if new death was more valuable
                if (newItemValue < oldItemValue)
                {
                    Main.NewText("Previous more valuable death at same position, not overwriting. Reverting to normal death.", 255, 100, 100);
                    return true;
                }

                Main.NewText("Previous less valuable death at same position, overwriting.", 255, 100, 100);

                playerDeathInventoryMap.Remove(tombStonePosition);
            }

            Item[] deathInventory = new Item[player.inventory.Length];
            Item[] deathArmor = new Item[player.armor.Length];
            Item[] deathDye = new Item[player.dye.Length];
            Item[] deathMiscEquips = new Item[player.miscEquips.Length];
            Item[] deathMiscDyes = new Item[player.miscDyes.Length];

            //INVENTORY
            for (int i = 0; i < player.inventory.Length; i++)
            {
                //put inventory into separate list
                deathInventory[i] = player.inventory[i];
                player.inventory[i] = new Item();
            }

            //ARMOR - SOCIAL
            for (int i = 0; i < player.armor.Length; i++)
            {
                //put armor into separate list
                deathArmor[i] = player.armor[i];
                player.armor[i] = new Item();
            }

            //DYES
            for (int i = 0; i < player.dye.Length; i++)
            {
                //put dye into separate list
                deathDye[i] = player.dye[i];
                player.dye[i] = new Item();
            }

            //EQUIPMENT
            for (int i = 0; i < player.miscEquips.Length; i++)
            {
                //put equipment into separate list
                deathMiscEquips[i] = player.miscEquips[i];
                player.miscEquips[i] = new Item();
            }

            //EQUIPMENT - DYE
            for (int i = 0; i < player.miscDyes.Length; i++)
            {
                //put equipment dye into separate list
                deathMiscDyes[i] = player.miscDyes[i];
                player.miscDyes[i] = new Item();
            }

            PlayerDeathInventory playerDeathInventory = new PlayerDeathInventory(deathInventory, deathArmor, deathDye, deathMiscEquips, deathMiscDyes);

            playerDeathInventoryMap.Add(tombStonePosition, playerDeathInventory);

            Main.NewText("Tombstone inventory saved at X " + x + ", Y " + y);

            return true;
        }

        public override TagCompound Save()
        {
            int maxValue = 0;
            Vector2 position = new Vector2();
            PlayerDeathInventory mostValuableDeath = null;

            foreach (KeyValuePair<Vector2, PlayerDeathInventory> entry in playerDeathInventoryMap)
            {
                int value = entry.Value.getValue();
                if (value > maxValue)
                {
                    maxValue = value;
                    position.X = entry.Key.X;
                    position.Y = entry.Key.Y;
                    mostValuableDeath = entry.Value;
                }
            }

            if (mostValuableDeath == null)
            {
                return null;
            }

            mod.Logger.Warn("Saving tombstone inventory at " + position.X + ", " + position.Y + ", valued at " + maxValue);

            List<Item> deathInventory = new List<Item>(mostValuableDeath.deathInventory);
            List<Item> deathArmor = new List<Item>(mostValuableDeath.deathArmor);
            List<Item> deathDye = new List<Item>(mostValuableDeath.deathDye);
            List<Item> deathMiscEquips = new List<Item>(mostValuableDeath.deathMiscEquips);
            List<Item> deathMiscDyes = new List<Item>(mostValuableDeath.deathMiscDyes);

            nullEmptyItems(deathInventory);
            nullEmptyItems(deathArmor);
            nullEmptyItems(deathDye);
            nullEmptyItems(deathMiscEquips);
            nullEmptyItems(deathMiscDyes);

            TagCompound tag = new TagCompound();
            tag.Add("x", position.X);
            tag.Add("y", position.Y);
            tag.Add("value", maxValue);
            tag.Add("deathInventory", deathInventory);
            tag.Add("deathArmor", deathArmor);
            tag.Add("deathDye", deathDye);
            tag.Add("deathMiscEquips", deathMiscEquips);
            tag.Add("deathMiscDyes", deathMiscDyes);

            return tag;
        }

        private void nullEmptyItems(List<Item> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                //mod.Logger.Warn("item type " + items[i].type + " name " + items[i].Name);
                   
                if (items[i].type == 0)
                {
                    //items[i] = null;
                }
            }
        }


        public override void Load(TagCompound tag)
        {
            int value = tag.GetInt("value");

            if (value > 0) {

                loadedValue = value;

                Vector2 position = new Vector2(tag.GetFloat("x"), tag.GetFloat("y"));

                Item[] deathInventory = new Item[player.inventory.Length];
                Item[] deathArmor = new Item[player.armor.Length];
                Item[] deathDye = new Item[player.dye.Length];
                Item[] deathMiscEquips = new Item[player.miscEquips.Length];
                Item[] deathMiscDyes = new Item[player.miscDyes.Length];

                loadItemList(tag.Get<List<Item>>("deathInventory"), deathInventory);
                loadItemList(tag.Get<List<Item>>("deathArmor"), deathArmor);
                loadItemList(tag.Get<List<Item>>("deathDye"), deathDye);
                loadItemList(tag.Get<List<Item>>("deathMiscEquips"), deathMiscEquips);
                loadItemList(tag.Get<List<Item>>("deathMiscDyes"), deathMiscDyes);

                PlayerDeathInventory inventory = new PlayerDeathInventory(deathInventory, deathArmor, deathDye, deathMiscEquips, deathMiscDyes);

                playerDeathInventoryMap.Add(position, inventory);
             }
        }

        public override void OnEnterWorld(Player player)
        {
            if (playerDeathInventoryMap.Count > 0)
            {
                foreach(KeyValuePair<Vector2, PlayerDeathInventory> entry in playerDeathInventoryMap)
                {
                    PlayerDeathInventory inventory = entry.Value;
                    Main.NewText("Loaded tombstone inventory " + entry.Key + ", valued " + loadedValue, 155, 155, 255);
                }
                
            }
        }

        private void loadItemList(List<Item> items, Item[] inventory)
        {
            for (int i = 0; i < inventory.Length && i < items.Count; i++)
            {
                //mod.Logger.Warn("item type " + items[i].type + " name " + items[i].Name);

                if ("Unloaded Item".Equals(items[i].Name))
                {
                    inventory[i] = new Item();
                } else
                {
                    inventory[i] = items[i];
                }
            }
        }

    }
}
