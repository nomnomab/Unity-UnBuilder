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
[ ] Load packages for unity version for a lookup
    [ ] Cache results per unity version next to exe
    [ ] Keep a self-made list of associations and have version overrides if needed
[ ] Match all available packages to the ones in the game via DLLs
    [ ] Prompt when there are some that don't match
[ ] Connect the package files to the output files
[ ] Verify Unity project
