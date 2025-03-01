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
    [ ] Match DLLs from SRC/GameAssemblies with the ones in TEMP/Library/ScriptAssemblies
        Matches can say which packages are installed, which would make some mapping easier?
    [ ] Also map packages in [UnityHubPath]\Editor\Data\Resources\PackageManager\BuiltInPackages
        if there isn't the package in the project :/
[ ] Connect the package files to the output files
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
