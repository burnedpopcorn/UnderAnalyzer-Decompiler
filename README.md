## UnderAnalyzer Decompiler

This Fork of UnderTaleModTool allows for Complete Decompilation of a given GameMaker Game
<br>
And I mean TRUE Decompiling (and NOT just patching a game)
<br>
As in you can literally make a GameMaker Studio 2 or 1 Project from a Compiled Game

> [!NOTE]
> This is also supposed to be somewhat of a continuation of UTMTCE
> as this build has features specifically for Pizza Tower, but could also potentially be useful for other games as well

## Downloads

Only the GUI versions will be Included
<br>
As only GUI builds can use these new Decompiling Scripts

[Version Hash of UndertaleModTool Used: c583e53dbe323ea499bd9c5824b51b01596072dc](https://github.com/UnderminersTeam/UndertaleModTool/commit/c583e53dbe323ea499bd9c5824b51b01596072dc)

[Version Hash of UnderAnalyzer Used: 2b0ceee4cd6b9c4a4fdd2ae2d27dc680621004a9](https://github.com/UnderminersTeam/Underanalyzer/commit/2b0ceee4cd6b9c4a4fdd2ae2d27dc680621004a9)

## Main Features

Features Include:
- Frequent Updates to the Latest UnderAnalyzer and UTMT Version
- Included Decompiling Scripts, so you can turn any GMS1 or 2 Game back into a GameMaker Project File (YYP for GMS2 or GMX for GMS1), or a GameMaker Importable Package (YYMPS)
- New Tab solely for Decompiling Scripts
- Improved both Dark and Light mode
- Added Variable Definition Maker, so you can Define what variables use what asset types to avoid code using an asset's ID to call assets
- The "Create Enum Declarations" Setting is now Disabled by default, and Dark Mode is Enabled by default
- Smaller file and project size, since unused libraries have been removed

### Changes for Pizza Tower
- Added Support for finding and decompiling Pizza Tower's Enums (can also be adjusted to suit Mods if they change the location of the states)
- The Variable Definition Maker can automatically define known variables for Pizza Tower and its Mods

## Included Scripts

All Scripts from UTMT are also included, but the new Scripts are:

- GameMaker Studio 2 Decompiler Script
- GameMaker Studio 1 Decompiler Script

Miscellanous Scripts:
- BetterExportSpritesAsGIF.csx (Can Export Sprites to GIF in bulk, and can adjust the Speed of the GIF, without an external DLL)
- ExportSpriteStrip.csx (Exports Sprites as a Sprite Strip, which places all frames of a Sprite within the same file)

## Credits

UnderTaleModTool Contributors
- All previous and current UnderTaleModTool Contributors
- colinatior27, for basically carrying the UTMT project
- Dobby233Liu, for adding some 2024.11 Fixes
- CST1229, for adding the new Room Editor from UTMTCE and other changes

Decompiling Script Creators
- crystallizedsparkle, for the original GMS2 Decompiler Script
- cubeww, for the original GMS1 Decompiler Script
- CST1229, for Improving cubeww's Script a bit

Other Contributions
- Pizza Tower Variable Definitions by avievie and CST1229
- luizzeroxis, for the initial work on the Better Dark/Light mode
- zivmaor, for the "Hide Child Code Entries" Setting
- Pizza Tower Enum Resolver originally by CST1229 (completely rewritten by me in UTMT 0.7.0.0 update)

My Contributions
- Updated the Better Dark/Light Mode by luizzeroxis to also work with new windows introduced in UTMT 0.7.0.0 
- Made the Variable Definition Maker Window and all of its functionality
- Made the Pizza Tower Enum Finder Window and most of its functionality
- Improved the Decompiling Scripts in various ways (see script code comments to see list of changes)
- Made BetterExportSpritesAsGIF.csx and ExportSpriteStrip.csx
- And more to come!
