# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**"Where You Are" — Mobile AR Companion App.** The Ingress/Pokémon Go layer for the desktop game at `~/Projects/game`.

Players use this app in the real world to:
- See an AR overlay of faction territory, Displacement Zones, and Warden Historical Markers at their GPS location
- Place items (dead drops, supply caches) at real-world GPS coordinates
- Pick up items left by other players at physical locations (first-come-first-served)
- Receive Snatch warnings when enemies are near a cache they own
- View and interact with the Warden Mark system

**This is NOT a standalone game.** It is a companion app. The desktop UE5 game (`~/Projects/game`) is the primary experience.

### Platform Split
| Platform | Repo | Role |
|----------|------|------|
| Desktop (UE5) | `~/Projects/game` | 3rd-person shooter, main game |
| Mobile (AR) | `~/Projects/game-mobile` (this repo) | AR overlay, GPS item drops, faction territory |

### Cross-Platform Item State (Critical)
Items placed at GPS coordinates via this app are claimable by desktop players at the equivalent world-space position. Desktop players can also place items retrievable by mobile players. **Both platforms share a single authoritative item state backend.** This is core gameplay — not a nice-to-have.

- GPS → UE5 world space conversion: `FWYAGeoMath::GeoToWorld` (see desktop repo)
- All item state reads/writes go through the shared backend API (to be designed)

## Tech Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| Framework | Unity 2022.3 LTS + AR Foundation 5.1 | Decided 2026-03-17 |
| AR | ARCore (Android) / ARKit (iOS) | Camera + GPS overlay |
| Maps | Google Maps SDK or Mapbox | Digital twin base layer |
| GPS | Native device GPS | High accuracy required |
| Backend | Shared with desktop game | Item state, faction territory, player positions |

## AR Features

- **Dead Drops** — items placed at GPS coords, visible in AR as floating objects
- **Supply Caches** — larger item containers, faction-locked or open
- **Warden Historical Markers** — lore points of interest at real-world landmarks
- **Faction Territory** — AR overlay showing which faction controls an area
- **Displacement Zones** — areas of heightened activity / danger
- **Snatch Warnings** — push notification when a player approaches your cached items
- **The Warden Mark** — special interaction points tied to the main story

## Narrative Context

Full lore in `~/Projects/game/docs/narrative/`. Key docs for this app:
- `ar-mobile-layer.md` — AR feature specifications
- `factions-wardens.md` — Warden Marker system
- `lore-bible.md` — world context

## Multi-Agent Workflow

Same structure as the desktop repo. Each agent reads this file first.

| Agent | Context file | Output dir |
|-------|-------------|------------|
| **Core** (this session) | `CLAUDE.md` | `src/` |
| **Narrative** | `docs/narrative/AGENT.md` | `docs/narrative/` |
| **Art Direction** | `docs/art-direction/AGENT.md` | `docs/art-direction/` |
| **AR Features** | `docs/ar-features/AGENT.md` | `docs/ar-features/` |
| **Economy** | `docs/economy/AGENT.md` | `docs/economy/` |

- Major decisions → `decisions-log.md`
- Weekly summaries → `sync-meetings/YYYY-MM-DD-<agent>.md`

## Current Status

- [x] Repo initialized
- [x] Tech stack decision → Unity 2022.3 LTS + AR Foundation 5.1
- [ ] Shared backend API design
- [ ] AR prototype (item placement at GPS coord)
- [ ] Cross-platform item state sync with desktop game
