# HexQuickStackStorage

Inspired by Goldenrevolver's **Quick Stack Store Sort Trash** mod.

This mod allows you to automatically store items from your inventory into nearby chests within a configurable radius.

You can also delete items from your inventory and mark item types as trash.

## Instructions

### Quick Stack

Click the **Q** button in your inventory or use the configured Quick Stack keyboard shortcut (defautlt 'P'.)

Items will only be moved if:

- The item is not equipped
- The item is not in your hotbar
- The item is inside the normal player inventory
- A nearby player-owned chest already contains that item type
- The chest has enough room for the item

Items will not be stored in empty chests or chests that do not already contain that item.

### Sort

Click the **S** button to sort your inventory.

If a chest is currently open, the chest will also be sorted.

The player's hotbar and equipped items will not be moved.

### How to Delete Items

There are two ways to delete items.

#### Drag and Delete

Click and drag an item from your inventory, then click the trash icon.

The dragged item will be deleted.

Items cannot be deleted while equipped or while they are in the hotbar. Move the item into the normal inventory first.

#### Mark Items as Trash

Hold **Shift** and **Right Click** an item to mark that item type as trash.

Items marked as trash will have a red border around them.

Click the trash icon to automatically delete all items currently marked as trash.

Shift + Right Click the item again to remove the trash tag.

Once an item type is marked as trash, newly picked up items of that same type will also be marked as trash.

Trash selections persist between game sessions.

## Container Ownership

Quick Stack and chest sorting only work with player-created containers.

Naturally generated/world containers are ignored.

## Multiplayer

- Client-side mod
- Does not have server sync
- Uses chest ownership validation

## Configuration

Configuration options include:

- Quick Stack search radius
- Quick Stack keyboard shortcut

## Support

Discord support:

https://discord.gg/wU2FXD94v4

## Source

GitHub:

https://github.com/guillenjgg/valheim-hex-quick-stack-store