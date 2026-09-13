# HexQuickStackStorage

Inspired by Goldenrevolver's **Quick Stack Store Sort Trash** mod.

This mod adds Quick Stack, inventory sorting, chest sorting, trash management, and item favorites.

The mod has been designed with multiplayer compatibility in mind, but it has not been tested in multiplayer yet.

ServerSync is currently not supported, so multiplayer behavior may still be inconsistent.

Any multiplayer feedback is welcome. I don't currently have testers for multiplayer scenarios.

## Quick Stack Access Modes and Limitations

Quick Stack has two configurable container access modes:

- **CharacterOwned** - Only uses containers created by the current character (default).
- **Accessible** - Uses public containers the current character can access. Select this if you want to Quick Stack into any public container.

Ward-protected containers are only used when the character has access to the protected area.

Quick Stack also has a few limitations:

- Containers that are currently in use are skipped during nearby Quick Stack.
- Items are only moved to containers that already contain that item type.
- Empty containers are not used.
- Favorited, equipped, and hotbar items are ignored.
- Only items inside the normal player inventory are considered.
- A container must have enough stack space or an empty slot for at least part of the item.
- The configured access mode applies to nearby Quick Stack searches. Manually opened containers can still use the mod's stack and sort actions.

## Instructions

### Quick Stack

Click the **Q** button in your inventory or use the configured Quick Stack keyboard shortcut (default `P`).

Quick Stack searches nearby eligible containers and moves matching inventory items into them.

If a container already contains an item type, Quick Stack will fill existing stacks first and then use empty slots in that container if needed.

### Sort Inventory

Click the **S** button in your inventory to sort your normal player inventory.

Sorting will:

- Leave hotbar items in their current slots
- Leave equipped items in their current slots
- Leave favorited items in their current slots
- Consolidate compatible item stacks
- Sort the remaining inventory items

### Sort Chests

When a chest is open, click the **Sort** button beneath the chest inventory to sort it.

Automatic chest sorting can also be enabled in the configuration. When enabled, a chest is sorted when successfully opened.

### Favorite Items

Hold **Left Control** and **Right Click** an item to favorite or unfavorite that item type.

Favorited items have a gold border and:

- Cannot be Quick Stacked
- Will not be moved or consolidated when sorting your inventory
- Cannot be deleted

Favorites apply to the entire item type. For example, favoriting Wood Arrows causes all Wood Arrow stacks to be treated as favorites.

The Favorite modifier key can be changed in the configuration.

### Delete Items

There are two ways to delete items.

#### Drag and Delete

Drag an item from your inventory, then click the trash icon.

Equipped, hotbar, and favorited items cannot be deleted.

#### Mark Items as Trash

Hold **Left Shift** and **Right Click** an item to mark or unmark that item type as trash.

Items marked as trash have a red border.

Click the trash icon to delete all currently marked trash items from your inventory.

Trash applies to the entire item type. Newly picked up items of the same type will also be treated as trash.

Equipped, hotbar, and favorited items cannot be marked as trash.

Marking an item as trash removes its favorite state.

The Trash modifier key can be changed in the configuration.

## Favorite and Trash Storage

Favorite and trash selections are stored as text files in the BepInEx config directory:

- `HexQuickStackStorage.favorites.txt`
- `HexQuickStackStorage.junk.txt`

Each file stores one internal item name per line and is updated automatically when item states change.

Deleting either file clears the corresponding saved selections the next time the mod starts.

## Multiplayer

- Client-side mod
- Does not currently support ServerSync
- Has not been tested in multiplayer
- Multiplayer behavior may be inconsistent

## Configuration

Configuration options include:

- Quick Stack search radius
- Quick Stack keyboard shortcut
- Container access mode
- Trash modifier key
- Favorite modifier key
- Automatic chest sorting

The Trash and Favorite modifier keys cannot use the same key.

## Support

Discord support:

https://discord.gg/wU2FXD94v4

## Source

GitHub:

https://github.com/guillenjgg/valheim-hex-quick-stack-store