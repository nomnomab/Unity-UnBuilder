The main goal of this project is to make a better version of my prior build->project conversion.

## Goals:
- Determine Unity version automatically.
- Extract usable scripts.
- Extract usable assets.
- Fix guids across the project so prefabs and other references function normally.
- Make most of the process automatic and in this CLI tool itself.

## Steps

[x] Grab game information
[x] Extract assets from AssetRipper into an output folder
[x] Load packages for unity version for a lookup
    [ ] Cache results per unity version next to exe
    [x] Keep a self-made list of associations and have version overrides if needed
[x] Match all available packages to the ones in the game via DLLs
    [ ] Prompt when there are some that don't match
    [x] Match DLLs from SRC/GameAssemblies with the ones in TEMP/Library/ScriptAssemblies
        Matches can say which packages are installed, which would make some mapping easier?
    [x] Also map packages in [UnityHubPath]\Editor\Data\Resources\PackageManager\BuiltInPackages
        if there isn't the package in the project :/
[x] Connect the package files to the output files
    [x] Builds a lookup for the TEMP project of all of its scripts, as the
        original project won't have them attached to the normal packages properly.
    [x] Need to first find which guids map from the TEMP project to the SRC project.
        Such as scripts from packages mapping to the decompiled versions in SRC/Scripts/*
        These would be used to fix the guids located in the normal assets, like SOs and
        prefabs.
[ ] Verify Unity project


## Other

[x] Game-specific config for overrides
    [ ] Override AssetRipper options
    [x] Override package versioning
    [ ] Change package manifest?
[ ] Animation clips can be duplicated and break if gotten via name in code
[ ] Some legacy axis can just not be in the build I guess?
    Having to re-define multiple of them manually.
[ ] Decompiled shaders have to be manually cleaned up as well, clean up common cases?
[ ] Making an asmdefref for specific package folders
    Probably assign in game specific settings
[ ] Assign LocalizationSettings if available and installed
    [ ] Fetch localization tables
[ ] Rename files entirely after the fact, such as txt -> csv
[x] Fix new input system asset files
    [x] Proper file extension
    [x] Proper json data
    [x] New action asset isn't properly being inserted
        Thinks it is corrupted or an old version
[ ] Map generated shaders to existing shaders in the project
    Such as Unity standard shaders
[x] InputSystemUIInputModule fails to fix guid
    Something wrong with the package itself?
[x] TextMeshPro needs to import the "essentials" unitypackage
