# Sander

<div align="center">

Sander Mod for [Space Station 14](https://github.com/space-wizards/space-station-14)

A client-side modification for **Space Station 14** providing enhanced gameplay features including visual overlays, compass navigation, sound subtitles for accessibility, footsteps tracking, and aimbot assistance.

<img width="941" height="138" alt="image" src="https://github.com/user-attachments/assets/04b4b643-2c08-4b9f-8d6b-abfb420295e2" />

### Community
Join my **[Matrix chat](https://matrix.to/#/#The-Robuster's-Workshop:matrix.org)** if you're looking for a community dedicated to **modding & cheats for Space Station 14**.

🇬🇧 English / 🇷🇺 Русский — both welcome.  
🇷🇴 Romanian & 🇪🇸 Spanish speakers also feel at home.

Let's build, inject, and break things together.

</div>

# WARNING!

For some people, the MOD menu covers the game interface, so make the interface slightly less than 100%!

Also if `Sander.dll` is not working for you, try a fallback version!!! 

Also, if you see in [releases](https://github.com/MelvinMod/Sander/releases) something like "1.2**b**", it means that this is a beta version (often not marked as the latest release)!!!

## Features

### 🔱 Compass Navigation
Real-time compass showing everything with color-coded markers!

### 💡 Visual Overlays

- **Fullbright**: Disable all lighting for maximum visibility
- **Shadows**: Toggle shadow rendering on/off
- **FOV**: Adjust field of view for better situational awareness

### ♿ Sound Subtitles (Accessibility)

Sound description system for players with hearing impairments or those playing with low volume:

- **Range**: Up to 128 meters
- **Position**: Comfort position (bottom-right corner)
- **Animations**: Smooth fade in/out, vertical stacking
- **Direction**: Arrow icons (→ ← ↑ ↓ ↗ ↖ ↘ ↙) showing sound source direction

**Supported Sounds:**
- Environmental: Door opens, Window breaks, Vent, Lock
- Combat: Gunshot, Impact, Explosion, Alarm
- Electronic: Computer, Radio, Electrical, Robot
- Biological: Footsteps, Breathing, Heartbeat, Pain, Death, Scream
- Other: Ghost, Mob sounds

### 👣 Footsteps Tracer

Visual trail showing movement paths of players and NPCs:

- Fade-out animation (3 seconds)
- Color-coded by entity type
- Cyan for friends, Red for syndicate, Yellow for regular

### 🎯 Aimbot System

Automated targeting assistance for combat:

| Type | Color | Trigger |
|------|-------|---------|
| GunBot | White | Ranged weapons |
| MeleeBot | Orange | Melee weapons |

> [!NOTE]
> Requires being in combat mode (triggered by left-click) to activate.

### 🧬 Implant Detection

Display implant information on entities within range:

- Shows implant type and count
- Toggle visibility independently
- Works with right-click interaction

### 📍 Coordinate System

- Set target coordinates with `!coords` command
- Shows direction and distance on compass
- Toggle coordinate display independently

## Installation

### Requirements
- Space Station 14 game client
- MarseyLoader or compatible mod loader

### Steps

1. Place the DLL in your mod loader's mod folder

2. Launch the game - the mod loads automatically

## Usage

### Opening the Menu
- Use the verb menu to access mod controls

### Feature Toggles
All features can be enabled/disabled through the in-game UI:

```
[Search] [Implants] [Coords] [Syndicate] [Pirate] [Fullbright] [Shadows] [FOV] [Sounds] [Footsteps] [GunBot] [MeleeBot]
```

### Commands
- `!coords <x> <y> <z>` - Set target coordinates
- Right-click entities - Toggle implant visibility


## Technical Details

### Performance Optimizations
- Entity caching with configurable update intervals
- Distance-based update triggers (only updates when player moves 3+ units)
- Frame-limited rendering to prevent lag

### Update Intervals
| System | Update Interval | Trigger |
|--------|-----------------|---------|
| Compass | 25 frames | Player movement |
| Sound Subtitles | 5 frames | Continuous |
| Footsteps | 3 frames | Continuous |
| Implants | 20 frames | Player movement |
| Aimbot | Every frame | Combat mode |

## Compatibility

- ✅ Linux (tested)
- ✅ Windows (tested)
- ⚠️ MacOS (not tested, may work)

## License

This mod is provided as-is for personal use. See the main Space Station 14 repository for game licensing information.

## Acknowledgments

- [Space Station 14](https://github.com/space-wizards/space-station-14) - Base game
- [Robust Toolbox](https://github.com/space-wizards/RobustToolbox) - Game engine
- Reference implementations: ArabicaCliento, CerberusWareV3

---

<div align="center">

**Sander Mod** • Created for SS14 community

</div>
