# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).


## [1.7.0] - 2026-09-023

### ⚠️ Warning: Not compatible with previous versions ⚠️

### Added
- Added Intensity modes
  - Radial Wave
  - Sparkle
  - Angular Wave
  - Block Pulse
- Added the Layout Editor window
  - Opens by double-clicking a Layout asset and follows the current selection
  - Edit blocks and rows, with copy & paste and height adjustment
  - The Block Overlay now supports editing multiple blocks at once
- Added a Motion Generator that builds Drum, Wiper and Sasage motions from parameters
- Added a preset feature to Timeline clips
- Added distance-based LOD
- Added BPM tick marks to Tempo clips
- Added Transition Scatter to Intent, staggering motion changes across the audience
- Added "Use Suggested Cycle" to Motion clips

### Changed
- Updated the penlight model and the component's icon
- Changed the default penlight color to white
- Culling now only runs during playback
- Removed Speed Multiplier from Timeline clips (only Blending, Clip In and Extrapolation are allowed)
- Moved the creation menu to `GameObject/Light/Prism Fanlight` and the component to `Rendering/Prism Fanlight`
- Tempo now starts counting beats from the beginning of each clip
- Motion clips now blend smoothly even when their motions have different cycles
- Changed how motion is sampled
- Re-tuned the Drum, Wiper and Sasage default motions
- Reorganized the Inspector's sections
- Parameters not included by a track's fields are now shown as disabled in the Inspector
- Unified the names of newly created assets
- Renamed Intensity modes (Random Sparkle → Sparkle, Block Alternating Pulse → Block Pulse)

### Fixed
- Fixed the preview being cleared while editing
- Fixed incorrect direction blending for Color and Intensity
- Fixed an error when no audience material is assigned
- Fixed an error when reusing a Time Manager
- Fixed the Tempo not being rebuilt when a clip was changed
- Fixed an error when selecting "Everything" in the field settings
- Fixed incorrect rotation axes in the Layout Editor
- Fixed Frame Selected in the Layout Editor

### Removed
- Removed material editor from Inspector
- Removed default settings from the Tempo track
- Removed fallback and intensity from Direction
- Removed Realism and Reach from Intent
- Removed Motion Amount, Height Bias, Side Scale, Forward Scale, Wrist Delay Ratio and Variation from Motion
- Removed the deprecated audience shader


## [1.6.0] - 2026-08-05

### ✨ Official release ✨
### ⚠️ Warning: Not compatible with previous versions ⚠️

### Added
- Added Color modes
  - Color Palette
  - Linear Gradient
  - Block Palette
- Added Intensity modes
  - Pulse
  - Traveling Wave
- Added Motion presets
  - Drum
  - Wiper
  - Sasage
- Tempo can now be controlled from the Timeline
- Layout settings can now be edited from the scene overlay

### Changed
- Greatly improved Inspector usability
- Unified time control through a Provider
- Reorganized all parameters and unified their appearance with clips
- Improved behavior for the block selected in the Scene View
- Reworked camera handling, greatly improving performance
- Migrated the audience shader to Shader Graph

### Fixed
- Fixed a warning caused by an unused variable

### Removed
- Providers are no longer managed via components
- Removed the Visibility feature from Timeline
- Removed VisibilityUpdate
- Deprecated the legacy audience shader


## [1.5.0] - 2026-07-22

### Added
- Audience motion can now be baked for finer control

### Changed
- Timeline clips are now evaluated by Priority
- Updated default values
- Improved the Inspector's appearance

### Fixed
- Fixed an error when placing a clip on a newly created track
- Fixed the preview not updating correctly when Timeline is stopped


## [1.4.0] - 2026-07-20

### ⚠️ Warning ⚠️ Not compatible with previous versions

### Added
- Rewrote all code to strengthen the foundation
- Added support for multiple penlight shapes
- Fully guarantees reproducibility
- Unified the time evaluation path
- ...and many more improvements ✨

### Changed
- <b>Updated the license (credit is now optional)</b>
- Overhauled the appearance, including the Inspector

### Fixed
- Not listed, as the code changed too extensively to itemize


## [1.3.0] - 2026-07-13

### Added
- Highlighted parameters overridden by Timeline
- Improved the appearance of Timeline clips
- Added a dedicated Gradient Timeline track

### Changed
- Refactored all code

### Fixed
- Fixed a flickering issue


## [1.2.0] - 2026-07-12

### <b>Full Timeline support!!</b>

### Added
- Added Timeline support for all parameters
- Penlights can now be created from the menu

### Changed
- Updated the license
- Unified the code style
- Unified namespaces

### Removed
- Removed the standard preview


## [1.1.0] - 2026-07-11

### Added
- Added Timeline support
- Implemented LOD feature
- Positions can now be changed per block

### Changed
- Refactored code
- Introduced a Seed feature to improve reproducibility

### Removed
- Removed unnecessary parameters


## [1.0.0] - 2026-06-03

- Initial release
