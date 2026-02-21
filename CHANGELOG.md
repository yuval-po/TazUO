# Changelog
All notable changes to TazUO will be recorded here.

---

## v4.18.0

## Breaking Changes

As part of the *C#* scripting integration, the following changes were made, with the intent
to standardize the API better state the purposes of different classes.

* Python API classes (`Py___` were renamed to `Api___` or `ApiUi___` depending on their intended purposes

* All `IronPython` types/classes were removed from LegionAPI and replaced with standard C# constructs.
  * PythonList -> IList<T>
  * PythonTuple -> ApiPoint3D

* Return type for `LegionAPI.LastTargetPos` changed from `Vector3Int` to `ApiPoint3D`

* `API.Events` signatures changes

* `PyOnItemCreated` renamed to `OnItemCreated` and its signature changed from `EventArgs<uint>` to `EventArgs<ApiItem>`

* Signature for `OnItemUpdated` changed from effectivley void to `EventArgs<ApiItem>`

* `PyOnBuffAdded` renamed to `OnBuffAdded`

* `PyOnBuffRemoved` renamed to `OnBuffRemoved`

* `Buff` renamed to `ApiBuff` (Affects `OnBuffAdded` & `OnBuffRemoved`)


### Features

* [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369) - Added support for *C#* scripting (detected via a `.cs` file extension check)
* [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369) - Added an `Open Location` context menu to the `ScriptManagerWindow`
* [P.R 366](https://github.com/PlayTazUO/TazUO/pull/366) - Added built-in IRC (`Internet Relay Chat`) support and channel
* [P.R 363](https://github.com/PlayTazUO/TazUO/pull/363) - Added Auto-Loot priotrity tiers (High/Normal/Low)
* [P.R 362](https://github.com/PlayTazUO/TazUO/pull/362) - Added *Sound* APIs to for `Legion Scripting`
* [P.R 359](https://github.com/PlayTazUO/TazUO/pull/359) - Added a *Skill Management* tab to the *Legion Assistant*

### Fixes
* [P.R 371](https://github.com/PlayTazUO/TazUO/pull/371) - Fixed an issue in which automatic corpse opening would continue to try opening corpses
  after the player had moved away.
* [P.R 365](https://github.com/PlayTazUO/TazUO/pull/365) - Fixed a reliability issues with `LegionAPI.OnHotKey`

* Added missing API documentation types:
  * `EventSinkApiDeclaration`
  * `ApiPoint3D`
  * `ApiSoundEntry`
  * `ApiJournalEntry`
  * `ApiEntity`
  * `ApiStatic`
  * `ApiItemData`
  * `ApiUiMenuItem`
  * `ApiMulti`
  * `ApiBuff`
  * `PersistentVar`
  * `LegionApiConfig`
  * `ApiUiTiledGumpPic`


### Misc
* `ApiUiNineSliceGump` (previously `PyNineSliceGump`) now uses a `75ms` `OnResize` debounce 


---

## In Development
`Dev channel / branch`

### Misc
- This changelog
- Added auto-loot priority tiers (High/Normal/Low) - Coryigon
- Removed integrated Discord features
- Added ToggleAutoLoot macro to quickly enable/disable autolooting

### Legion
- Added sound API endpoints to LegionScripts - fpw
- Added `API.ScriptName` and `API.ScriptPath`
- Updated PSL browser UI and backend
- Added `.IsHidden` to PyMobile in API
- Added `API.PickUpToCursor`, `API.DropFromCursor` and `API.GetHeldItem`
- Added `IsGargoyle`, `IsMounted`, `IsDrivingBoat`, and `IsRunning` to PyMobile

### Assistant
- Added skills tab to Legion Assistant - Coryigon
- Organizer tab now shows graphic when hovering over the graphic art
- Added Mobile outline option - Highlighting mobiles by notoriety
- Added TazUO chat(Top menu -> More -> TazUO Chat)
- ItemDatabase search now defaults to not only "this character"

### Other
- Move automatic py doc gen to tool usage
- Added ibm-plex font to embedded fonts
- Clean up a bunch of compile-time warnings

### Bugs
- Fixed healthbar collector occasionally becoming unresponsive to targeting/clicks
- Fix rare crash when removing messages from system chat
- Various other bugs fixed