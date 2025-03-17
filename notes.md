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
    - [?] Override AssetRipper options
    - [x] Override package versioning
    - [x] Change package manifest?
- [x] Animation clips can be duplicated and break if gotten via name in code
- [x] Some legacy axis can just not be in the build I guess?
    Having to re-define multiple of them manually.
- [x] Decompiled shaders have to be manually cleaned up as well, clean up common cases?
- [ ] Making an asmdefref for specific package folders
    Probably assign in game specific settings
- [x] Rename files entirely after the fact, such as txt -> csv
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
- [x] Migrate my old Unity Network package fixer for games like Lethal Company
- [x] Ignore files with `1 in them
- [ ] Handle assets like ReWired being weird
- [x] 2017 can't use package SearchAll
- [x] Custom Legacy input axis
- [x] Can copy Project files into project entirely
    - [x] Folder to install unity packages from
    - [ ] Pre-guid and post-guid folder?
- [x] Move files as a post-fix to fix things like AnimationClip names expecting
      something that is no longer there
- [?] Copy over Assembly-CSharp and any scripts that have a replacement, like what
      shaders do
- [x] First extract shader stubs, then do another extract just for shaders.
      Then keep the ones that have meta files.
- [ ] Windows paths longer than 260 cause problems
- [x] Match SO m_Name to its file name
- [x] Scripts from DLLs aren't discovered
- [ ] Wwise
    - [x] Files with `using AK.Wwise;` and `using UnityEngine;` need to replace all `Event` with `AK.Wwise.Event`
    - [ ] Wwise missing DLLs, maybe need to install wwise through normal methods first
- [ ] InputSystem still sometimes doesn't get enabled properly and gives the prompt
- [x] Lone meta files can break existing guids
- [ ] Baked meshes just show black
- [x] Add GameSettings for importing additional .unitypackage
- [ ] Assign LocalizationSettings if available and installed
    - [ ] Fetch localization tables
- [ ] Fix Localization tables
    - [ ] Set localization settings asset in project settings
    - [ ] Attach to Locale SOs in Localization folder
    - [ ] Remove duplicate locales in Project Settings
    - [ ] Make new collection for lang tables
    - [ ] Swap locales with new ones
- [?] package `ExcludePrefixesFromProject` from GameSettings
- [?] package `ExcludeNamesFromProject` from GameSettings
- [ ] FishNet de-generator
- [ ] Some SOs are generating empty
- [?] Find ways to lower RAM usage
- [x] Remove lines like `global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(spiralEffectInsensity);`
- [ ] Valheim
    - In `Scripts\assembly_valheim\Terminal.cs`
        - [x] Replace `count(character.m_name, character.GetLevel());` with `count(counts, character.m_name, character.GetLevel());`
        - [x] Replace `void count(string key, int level, int increment = 1)` with `void count(Dictionary<string, Dictionary<int, int>> counts, string key, int level, int increment = 1)`
        - [x] Replace `P_3.` with nothing
    - In `Scripts\Assembly-CSharp\SoftReferencePrefabSpawner.cs`
        - [x] Replace `Utils.Instantiate(m_prefab, base.transform.parent).name = m_prefab.Name;` with `SoftReferenceableAssets.Utils.Instantiate(m_prefab, base.transform.parent).name = m_prefab.Name;`
    - [ ] `Unable to parse file Assets/Systems/_GameMain.prefab: [Parser Failure at line 268994: Expect ':' between key and value within mapping]`
    - [?] A way to disable post processing components, or components in general
    - [x] Copy steam_appid.txt
- [x] GameSettings way to do file content-string replacements
- [ ] Manually replace shader by shader path as well
- [ ] Addressables
    - [x] BepInEx plugin to rip id <-> name
    - [x] Create Addressables Settings to reimport bundles
    - [x] Convert Legacy Bundles
- [ ] Auto detect certain plugins that are shared across various games
    - [ ] `$PLUGINS$/x86_64/steam_api64.dll`
    - [ ] `$PLUGINS$/x86_64/discord_game_sdk.dll`
    - [ ] `$MANAGED$/Facepunch.Steamworks.Win64.dll`
    - [?] Just copy over the plugins folder by default?

## Games Tested With
Stable:
- Lunacid
- Toree3D
- SuperKiwi64
    - Baked meshes show black
- Lethal Company
- Valheim
    - Needs a way to disable the post processing components
- How Fish is Made
    - Needs a better replacement shader for psx

In Progress:
- FlipWitch
    - Wwise missing DLLs
- Gun Frog
    - FlatKit asset for shaders (paid)
    - StylizedWater asset for shaders (paid)
    - CartoonGrass asset for shaders (paid)
    - Localization tables are broken
- Outpath
    - AStar Free
    - FishNet
        - Needs de-generator
    - UI Particle Image (paid)
- Risk of Rain 2
    - ReWired (paid)
- Rain World
    - ReWired (paid)
- Idea Fix
    - Localization tables are broken
    - Some SOs are generating empty
- ULTRAKILL
    - Addressables guids don't map yet
    - Move root assets into categorized folders

Unstable:
- n/a

Can't compile yet:
- Enter the Gungeon
    - 2017
    - ReWired (paid)
    - Uses UnityScript :/
- Magicite
    - Unity 5
    - Shaders aren't matching
    - Uses UnityScript :/

Untested on this version:
- Content Warning
- Golden Light
- BetonBrutal
