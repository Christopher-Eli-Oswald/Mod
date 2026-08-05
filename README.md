# Deep Mine Tool for Captain of Industry

This repository contains an experimental mod that is being developed to create buried solid-resource deposits without enabling sandbox mode.

## Goal

The completed tool will let the player choose a dumpable terrain material such as iron ore, copper ore, coal, limestone, quartz, sulfur, rock, dirt, or sand, then choose the top depth, thickness, radius, and replacement mode for a buried deposit. The existing surface should remain unchanged so the resource can later be reached through a normal deep open-pit mine.

## Current milestone

Version `0.1.0` is a read-only compatibility probe. Captain of Industry terrain editing is implemented through internal services whose names and signatures can change between game versions. This build discovers the terrain, heightmap, designation, dumping, mining, and sandbox-related services from the installed game and records matching method signatures in the normal game log.

It does **not** modify terrain yet. This protects maps and saves while the exact API binding is confirmed.

## Local build

1. Install Visual Studio 2022 with the .NET desktop development workload.
2. Set the `COI_ROOT` environment variable to the Captain of Industry installation folder.
3. Build `src/DeepMineMod/DeepMineMod.csproj` in Release mode.
4. Copy the generated `DeepMineMod` package folder into `%APPDATA%/Captain of Industry/Mods`.
5. Start the game once and exit.
6. Search the game log for `DeepMineMod API` and attach those lines to the repository issue or pull request.

Game DLLs are deliberately not committed to this public repository.

## Safety

Back up the save before testing any future terrain-writing build. The current probe build is read-only.
