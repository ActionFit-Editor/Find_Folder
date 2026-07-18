# AI Guide - Find Folder

This file is shipped inside the UPM package so an AI assistant in a consuming Unity project can understand the package without access to the source project's `Docs/AI` folder.

## Package Identity

- Package ID: `com.actionfit.findfolder`
- Display name: Find Folder
- Repository: `https://github.com/ActionFit-Editor/Find_Folder.git`
- Current package version at generation time: `1.0.10`
- Unity version: `6000.2`

## Purpose

Find Folder manages saved folder/file focus shortcuts. Use `README.md`, `package.json`, package source files, and `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` together to understand the user-facing workflow and catalog metadata.

## Agent Skills

- `Skills~/manifest.json` registers schema v2 `find-folder-help` for Codex and Claude with the unique `find-folder` prefix.
- The help skill is read-only and reads the generated `PACKAGE_SKILLS.md` inventory before explaining package workflows.
- No skill changes saved targets, Shared/Local JSON, GUIDs, settings, or Project window selection.
- Custom Package Manager owns project-local installation and safe refresh of this package's skill.

## Project Router Registration

This package should be listed in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md`.

Requested router entry:

- `Packages/com.actionfit.findfolder/AI_GUIDE.md` - Find Folder manages saved folder/file focus shortcuts. Read when changing GUID/path tracking, shortcut storage, migration, or Project window focusing.

If the router file is not already included in the AI assistant's default reading sequence, the router file is responsible for asking the user to link it from `Docs/AI/PROJECT.md` when available, or otherwise from `AGENTS.md`, `CLAUDE.md`, or another primary AI markdown entry point.

Read this file when:

- changing files under `Packages/com.actionfit.findfolder/`
- diagnosing `Find Folder` behavior in a consuming project
- preparing a release for `com.actionfit.findfolder`
- editing package metadata, README, AI guide, package version, or release notes

## Required Reading For AI

- Read this `AI_GUIDE.md` before changing, diagnosing, or explaining this package.
- Read `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md` when deciding which installed ActionFit package `AI_GUIDE.md` applies to a task.
- Read `README.md` for human-facing setup and usage.
- Read `package.json` for package ID, version, Unity version, and dependencies.
- Read `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` for catalog metadata, repository name, owner, status, description, release note, and dependency override.

## Editing Rules

- Keep changes scoped to this package unless the user explicitly asks for cross-package edits.
- Do not change package IDs, repository names, public menu paths, serialized field names, or package assembly names casually; these can affect installed projects.
- Preserve Unity `.meta` files when adding, moving, or renaming files inside the package.
- When behavior changes, update this `AI_GUIDE.md` in the same package before publishing so consuming projects receive the latest AI context.
- Keep `README.md` focused on human usage. Keep this file focused on AI-facing architecture, constraints, migration notes, and package-specific editing rules.

## Menu And Storage Notes

- Main menu: `Tools/Package/Find Folder/Open Window`.
- Shared group settings are stored under `Assets/_Data/FindFolder/Shared/{rootGroupId}.json`.
- Local group settings are stored under `UserSettings/FindFolder/Local/{rootGroupId}.json`.
- Folder and file focus entries should prefer GUID-based identity so moved assets can still be found.
- Clicking a folder entry should open that folder's contents in the Unity Project Browser; file entries should keep the existing select-and-ping behavior.
- When migrating older entries, use the stored path only to discover and backfill the GUID.

## Package Tools Menu

- Unity menu root: `Tools/Package/Find Folder/`.
- Keep package commands under this package root.
- Lower separated entries:
- `Setting SO`: focuses this package's settings ScriptableObject.
- `README`: opens this package README.
- Do not add README or Setting SO access back to Custom Package Manager package rows or Project Files.

## Release Note Rules

- `ActionFitPackageInfo_SO.ReleaseNote` must contain only the single version being prepared.
- Do not copy older changelog entries into the newest release note.
- Version history and update-range summaries are composed by Custom Package Manager from separate catalog version rows.
- Do not add headings such as `## 1.0.0` inside ReleaseNote unless a specific package UI requires it; the catalog row already carries the version.
## Publish Notes

- Publishing is manual through Custom Package Manager.
- Before reusing a version, check the remote Git tags. Published tags are immutable.
- If this package is modified after a version was tagged, bump to the next unused patch version before publishing.
- The package repository should include this `AI_GUIDE.md` so other projects can load the AI package context after installing the package.
