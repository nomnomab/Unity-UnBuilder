# Unity UnBuilder CLI Tool

> [!WARNING]  
> This is a work-in-progress research project and is not fully complete,
> and is for education/resource purposes only.
>
> Things will break and not every game will be compatible.
>
> This tool also does not condone piracy or illegal asset usage.
> Do not redistribute game assets, package assets, or anything else without
> explicit permission from the game owner(s).
>
> Use at your own risk.

This is a CLI tool to essentially turn a Unity game build back into a functional Unity Editor project.

The process of determining what each individual game needs is a matter of patience, but this tool tries
to make some aspects simpler.

## Contributions

Contributions via PRs are *very* welcome here, as I don't have a lot of free time to work on the more demanding aspects of this project.

Just make sure your PR works across multiple games before integration.

Read the [notes](notes.md) to get an idea on the current tool progress and current roadblocks that need addressing.


## Command Line Arguments
```bash
# required
# the path to the game executable
--game_path "path to exe"

# the path to a folder that the game folder will be generated into
--output_path "path to folder"

# skip AssetRipper if the game has already been extracted before
--skip_ar

# skip fetching packages if they have already been extracted
--skip_pack_fetch

# skip fetching and installing packages if they are already installed
# auto-enables --skip_pack_fetch
--skip_pack_all
```

## Compiling this Tool

```bash
# builds the tool into ./UnityUnBuilder/bin/Release/net9.0/
dotnet build -c Release
```

## Usage

### Tool Usage

```bash
UnityUnBuilder.exe --game_path "Path/To/Game.exe"
```

When supplying a game path to the tool and running it, it will generate a dotnet project for you in `/settings/[GAME NAME]` to modify with whatever your specific game requires.
The project is given a wrapper for the `GameSettings` it expects it to return, which has various properties to edit.

This project is built by this tool and the resulting dll is imported automatically for usage.

There is also a `[GAME NAME]/exclude/Resources` folder where you can put files in the same structure as a unity project, and the tool will copy them into
the final project at the same locations. This is useful if you get a finished package list and want to supply a `Resources/Packages/manifest.json`. Or if
the project needs to fix up the old `InputSystem` via `Resources/ProjectSettings/InputManager.asset`.

Most projects will need to experiment with what packages are actually used, and which versions are actually needed. The tool at the moment
attempts to determine which packages are there for the dlls present, but it isn't fantastic as a final result.

Tweaks and exclusions can be provided
to the `GameConfig.Packages` object, and once you figure out all the packages needed, you can copy the `/UnityProject/Packages/manifest.json` into `[GAME NAME]/exclude/Resources/Packages/manifest.json` to skip the entire package process for future usage.

## Sharing Game Config

Game configs, or rather the dotnet project folder for a specific game, is essentially what the tool uses to work for that specific game.

Make sure they are put into a folder with the same name as the game:
```
UnityUnBuilder.exe
/settings
    /mygame
        /exclude
            /Resources
        mygame.csproj
        # other files
```

Make sure you exclude folders such as:
- `/exclude/UnityProject`: This is the final project. This contains assets that **cannot** be re-distributed.
- `/output`: This is where the dll is built and used from.

## Third Party

- [AssetRipper](https://github.com/AssetRipper/AssetRipper)
