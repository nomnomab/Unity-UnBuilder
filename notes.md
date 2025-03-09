The main goal of this project is to make a better version of my prior build->project conversion.

## Goals:
- Determine Unity version automatically.
- Extract usable scripts.
- Extract usable assets.
- Fix guids across the project so prefabs and other references function normally.
- Make most of the process automatic and in this CLI tool itself.

## Steps

- [x] Grab game information
- [x] Extract assets from AssetRipper into an output folder
- [x] Load packages for unity version for a lookup
    - [ ] Cache results per unity version next to exe
    - [x] Keep a self-made list of associations and have version overrides if needed
- [x] Match all available packages to the ones in the game via DLLs
    - [ ] Prompt when there are some that don't match
    - [x] Match DLLs from SRC/GameAssemblies with the ones in TEMP/Library/ScriptAssemblies
        Matches can say which packages are installed, which would make some mapping easier?
    - [x] Also map packages in [UnityHubPath]\Editor\Data\Resources\PackageManager\BuiltInPackages
        if there isn't the package in the project :/
- [x] Connect the package files to the output files
    - [x] Builds a lookup for the TEMP project of all of its scripts, as the
        original project won't have them attached to the normal packages properly.
    - [x] Need to first find which guids map from the TEMP project to the SRC project.
        Such as scripts from packages mapping to the decompiled versions in SRC/Scripts/*
        These would be used to fix the guids located in the normal assets, like SOs and
        prefabs.
- [ ] Verify Unity project


## Other

- [x] Game-specific config for overrides
    - [ ] Override AssetRipper options
    - [x] Override package versioning
    - [x] Change package manifest?
- [x] Animation clips can be duplicated and break if gotten via name in code
- [x] Some legacy axis can just not be in the build I guess?
    Having to re-define multiple of them manually.
- [ ] Decompiled shaders have to be manually cleaned up as well, clean up common cases?
- [ ] Making an asmdefref for specific package folders
    Probably assign in game specific settings
- [ ] Assign LocalizationSettings if available and installed
    - [ ] Fetch localization tables
- [ ] Rename files entirely after the fact, such as txt -> csv
- [x] Fix new input system asset files
    - [x] Proper file extension
    - [x] Proper json data
    - [x] New action asset isn't properly being inserted
        Thinks it is corrupted or an old version
- [x] Map generated shaders to existing shaders in the project
    Such as Unity standard shaders
    - [x] Parse shader names for files
    - [x] Map shader guid to ones that exist in the project first
        Nuke the rest
    - ~~[x] Duplicate shaders next to each other need to be removed~~
    - [x] Properly merge same shaders together into the same file + guid
- [x] InputSystemUIInputModule fails to fix guid
    Something wrong with the package itself?
- [x] TextMeshPro needs to import the "essentials" unitypackage
- [x] Figure out what is duplicating assets :/
- [ ] Migrate my old Unity Network package fixer for games like Lethal Company
- [x] Ignore files with `1 in them
- [ ] Handle assets like ReWired being weird
- [x] 2017 can't use package SearchAll
- [x] Custom Legacy input axis
- [x] Can copy Project files into project entirely
    - [x] Folder to install unity packages from
    - [ ] Pre-guid and post-guid folder?
- [x] Move files as a post-fix to fix things like AnimationClip names expecting
      something that is no longer there
- [ ] Copy over Assembly-CSharp and any scripts that have a replacement, like what
      shaders do
- [x] First extract shader stubs, then do another extract just for shaders.
      Then keep the ones that have meta files.
- [ ] Windows paths longer than 260 cause problems

## Games Tested With
Stable:
- Lunacid
- Toree3D

Unstable:
- n/a

Can't compile yet:
- Risk of Rain 2
    - ReWired
- SuperKiwi64
    - AssetRipper fails to decompile shaders
    - Bad package version now
- Enter the Gungeon
    - 2017
    - ReWired
    - Uses UnityScript :/
- Rain World
    - ReWired
- Magicite
    - Unity 5
    - Shaders aren't matching
    - Uses UnityScript :/

Untested on this version:
- Lethal Company
    - Works on old version
- Content Warning
