# Changelog

## v1.4.0

### Added
- ServerSync integration
- Configuration option to auto stack trophies

## v1.3.0

### Added
- Added a confirmation dialog before deleting items.
- Added a configurable option to enable or disable delete confirmation.
- Added a "Don't show this confirmation again" checkbox to the delete confirmation dialog.
- Added a confirmation message when items are successfully deleted.

### Changed
- The delete confirmation dialog can be dragged to reposition it.
- Delete confirmation is skipped when it has been disabled through the configuration or confirmation dialog.
- Item deletion messages are now displayed in the center of the screen.
- The "Items Deleted" message is only shown when at least one item was actually deleted.

## v1.2.0

### Added
- Added configurable container access modes for Quick Stack.
- Added `CharacterOwned` mode to only Quick Stack into containers created by the current character.
- Added `Accessible` mode to Quick Stack into any public container the character can access.

### Changed
- Quick Stack now respects ward-protected container access.
- Quick Stack now supports stacking into carts.
- The vanilla Place Stacks and hold-to-stack actions now respect favorited items.

### Fixed
- Fixed Quick Stack failing on containers due to Valheim's restricted container access methods.
- Fixed Quick Stack failing on carts due to restricted `ZNetView` access.
- Fixed Quick Stack failing when targeting an already-open container.
- Fixed Place Stacks moving favorited items into containers.

## v1.1.0

### Added
- Compiled with Valheim v1.0.12.
- Added the ability to favorite item types.
- Favorited items are protected from quick stacking, sorting, and deletion.
- Favorite selections persist between game sessions.
- Added a configurable Favorite modifier key.
- Added a Sort button to opened chests.
- Added an option to automatically sort chests when opened.

### Changed
- Auto chest sorting is now a configuration option and is disabled by default.
- Quick Stack keyboard shortcut now works while the inventory is open.
- Inventory sorting now consolidates compatible item stacks.
- Hotbar, equipped, and favorited items remain in their current slots when sorting.

### Fixed
- Fixed chest auto-sorting not respecting its configuration setting.
- Fixed Quick Stack not working while the inventory is open.
- Fixed Quick Stack not partially transferring an item stack when a chest only has room for part of the stack.

## v1.0.1

### Fixed
- You can now delete items marked as trash by clicking the trash icon.

## v1.0.0

- Initial release