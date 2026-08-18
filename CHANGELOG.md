# Changelog

All notable changes to md2loop are documented here.

This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases before 1.2.1 are documented on the
[GitHub releases page](https://github.com/trsdn/md2loop-windows/releases).

## [1.2.1] - 2026-08-18

### Fixed

- The main window came up far too small on high-DPI displays and clipped the
  page content. `AppWindow.Resize` takes physical pixels rather than DIPs, so
  the hard-coded size produced a window of only 206x160 DIPs at 175% scaling.
  The window is now sized from the current rasterization scale, so it is
  360x300 DIPs on every display scale.
- The window is given a DPI-scaled minimum size, so it can no longer be
  resized small enough to clip its own content, and it is re-checked when the
  window moves to a monitor with a different scale factor.
- The window is now centered when it first appears.
- `installer/build.ps1` did not pass `/DMyAppArm64` to Inno Setup, so local
  ARM64 builds produced an installer labelled `win-x64` that refused to install
  on ARM64. It also did not pass `-p:Version`, so locally built binaries were
  always stamped 1.0.0.

### Changed

- The window is 300 DIPs tall instead of 280, so the post-conversion feedback
  row fits without clipping.

[1.2.1]: https://github.com/trsdn/md2loop-windows/releases/tag/v1.2.1
