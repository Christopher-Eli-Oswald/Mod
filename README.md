# Deep Mine Tool for Captain of Industry

This repository contains an experimental mod for creating buried solid-resource deposits without enabling sandbox mode.

## Current test build

The first functional brush is now implemented for Captain of Industry `0.8.6c`.
It uses the game's confirmed `TryAddMaterialToUndergroundTopFourLayer_NoHeightChange` terrain API so the surface height remains unchanged.

### Controls

- `F10`: activate the Deep Mine brush
- `Alt + mouse wheel`: cycle terrain material
- `Shift + mouse wheel`: change circular brush radius
- `Ctrl + mouse wheel`: change deposit thickness
- `Left click`: paint the underground deposit
- `Right click`: exit the tool

The selected material and current settings are written to the normal game log whenever they change.

## Local build

1. Install Visual Studio 2022 with the .NET desktop development workload.
2. Set the `COI_ROOT` environment variable to the Captain of Industry installation folder.
3. Check out the `agent/deep-mine-mod` branch.
4. Build `src/DeepMineMod/DeepMineMod.csproj` in Release mode.
5. Copy `src/DeepMineMod/bin/Release/package/DeepMineMod` into `%APPDATA%/Captain of Industry/Mods`, replacing the previous test build.
6. Back up the save before testing.
7. Start the game, load a save, press `F10`, choose a material and paint a small test area.
8. Upload the newest game log after the test.

Game DLLs are deliberately not committed to this public repository.

## Safety and limitations

This is the first terrain-writing build. Test it on a backed-up save and start with a small radius.
The current build inserts material into the game's supported underground top-four-layer structure. A later version will add a proper window, exact depth selection, rectangular selection, preview, and optional replacement rules.
