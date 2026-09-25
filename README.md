<h1 align="center">
  Prism Fanlight
</h1>

<p align="center">
  <a href="https://github.com/NullClone/PrismFanlight/releases/latest">
    <img src="https://img.shields.io/github/v/release/NullClone/PrismFanlight" alt="Latest Release"></a>
  <a href="https://github.com/NullClone/PrismFanlight/blob/main/LICENSE.md">
    <img src="https://img.shields.io/badge/License-MIT-brightgreen.svg" alt="License MIT"></a>
</p>

<p align="center">
  <a href="#about">About</a> •
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#requirements">Requirements</a> •
  <a href="#license">License</a>
</p>

<p align="center">
  English | <a href="README_ja.md">日本語</a>
</p>

## About

Prism Fanlight is a system for creating large-audience penlight shows in virtual live concerts.

It can render crowds of tens of thousands with low overhead, while Timeline gives you detailed control over their color, brightness, and other effects.

## Features

- **Timeline control**
  - Control motion, color, brightness, and BPM with dedicated tracks
  - Blend clips for smooth transitions
  - Seek, reverse, scrub, and loop the show
- **Lighting and color effects**
  - Control color across the venue with palettes and gradients
  - Create waves, pulses, random flickers, and other light patterns that move through the crowd
  - Synchronize effects to the beat of the music
- **Audience motion**
  - Includes basic penlight motion presets
  - Blend between motions for natural transitions
- **Venue layout editing**
  - Design the venue with blocks, rows, and seat counts
  - Use grid generation and mirroring to arrange the audience
  - Bake the layout into data used at runtime
- **GPU rendering and optimization**
  - Process audience poses, light emission, and rendering on the GPU for crowds of tens of thousands
  - Use frustum culling and distance-based audience LOD

## Installation

1. Open the Package Manager: `Window > Package Manager`
2. Click the `+` button in the top-left corner and select `Add package from git URL...`.

<p align="center">
  <img width="50%" src="https://github.com/user-attachments/assets/ed1fc738-0412-40e8-aa84-b32b643c31cb">
</p>

3. Enter the following URL.
   ```bash
   https://github.com/NullClone/PrismFanlight.git
   ```

> [!NOTE]
> You can also install it by dragging the `.unitypackage` from [the latest release](https://github.com/NullClone/PrismFanlight/releases/latest) onto Unity.

## Getting Started

1. Create it in your scene via `GameObject > Light > Prism Fanlight`.
2. Set the default show in the Inspector. It is used when no Timeline clip is active.
3. Open the Layout Editor by double-clicking the Layout asset, then edit the venue and bake it.
4. Add Prism Fanlight tracks to your Timeline and place clips.

## Requirements

- Unity 6.3 – 6.5
- URP or HDRP
- A platform that supports Compute Shaders

## License

This tool is released under the **MIT License** (see [LICENSE.md](LICENSE.md) for details).

You're free to use it for both commercial and non-commercial projects.
It's not required, but if you find this tool useful, I'd appreciate your consideration on the following two points.

### 1. Credit

If you could mention the author's name and the repository URL in your project's staff roll or accompanying documentation, it would mean a lot.

`Tools developed by NullClone (github.com/NullClone/PrismFanlight)`

### 2. Use by companies or large teams

If you're using this tool at a company or on a large-scale project, I'd be delighted to hear about it via email or social media.
With your permission, I'd also love to feature the project in my portfolio.
