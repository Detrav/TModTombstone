# TModTombstone
A mod for Terraria to drop a tombstone with all your items when you die in medium core mode, instead of scattering it everywhere.

Attempts to spawn the tombstone in the vicinity of where the player died. May destroy small plants to ease placement but never terrain, trees or buildings. Message with coordinates is printed in chat to help finding tombstone during high speed death. If no ground position is found it tries to place 2 dirt blocks in the air and the tombstone on top of that.

If unable to place a tombstone the player dies as normal and items are dropped on the ground.
If you die twice in the same spot it only overwrites the old tombstone if the new death inventory had more valuable items (sum of armor, accessories and inventory value). Otherwise current stuff is dropped on the ground.

Save support: Your most valuable tombstone is saved with the character and restored when loading.
A message is printed on world join if a saved tomb stone was loaded with its position so you can find it if it was a while ago or the tombstone is missing (just place a new one close by where it says). Supports dying and restoring on different worlds.


Know issues:
Creates gravestone when player has item that prevents dying. Can not fix. Workaround: Quickly click on the tombstone when death was prevented.

Changelog

- 2020-04-06 fixed prevent death event