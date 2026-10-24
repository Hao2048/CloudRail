# CloudRail

**Watch a little train travel through the clouds as the railway unfolds ahead.**

CloudRail is a scenic desktop Unity project centered on watching a miniature
floating world: cream-stone castles, cyan rooftops, arched viaducts, sculpted
clouds, waterfalls, and birds circling alongside the train.

[Download for macOS](https://github.com/Hao2048/CloudRail/releases/latest) ·
[Watch the latest showcase](docs/media/showcase.mp4) · [中文](README.md)

[![Watch the latest CloudRail showcase](docs/media/cover.jpg)](docs/media/showcase.mp4)

![Birds accompanying the train, captured in Unity](docs/media/birds.gif)

The ~31-second showcase is actual Unity scene footage. The close tracking shot
with birds is at **15–23 seconds**. A 1080p copy is included with the release.

## Features

- Floating castles, terraced gardens, windmills, waterfalls and scenic landmarks.
- Arched supports, sleepers and rails unfold in staggered motion ahead of the train.
- A 3D route with curves, straights, climbs and descents; moving wheels, rods and steam.
- Flocks appear along selected sections, flapping, gliding and circling near the train.
- Opaque sculpted model clouds and continuous rock detail.
- Scenic cameras plus optional on-foot exploration, star collection and station travel.
- Original synthesized piano/bell music and ambient effects.

## Play on Mac

Download the Universal DMG from [Releases](https://github.com/Hao2048/CloudRail/releases/latest),
open it, and drag **CloudRail.app** into **Applications**. A ZIP and SHA-256
checksums are also available. Requires **macOS 12+**, Apple Silicon or Intel.
No account is required to play.

This build is not Developer ID signed or notarized. If macOS blocks the first
launch, see [Apple's instructions](https://support.apple.com/en-us/102445) and
only proceed after confirming that you trust the source.

| Action | Input |
| --- | --- |
| Switch scenic camera | `C` |
| Orbit | Hold right mouse button and drag |
| Zoom | Scroll wheel, or vertical middle-button drag |
| Pause scenic mode | `Space` |
| Toggle scenic / exploration modes | `Tab` |
| Walk in exploration mode | `W A S D` |
| Interact / board / leave the train | `E` |
| Choose destination while boarded and stopped | `F` |
| Exploration help / pause menu | `H` / `Esc` |

## Open the source

```sh
git clone https://github.com/Hao2048/CloudRail.git
```

Open the project using **Unity 6.6 / 6000.6.0f1**. Let Unity restore its UPM
packages and import the assets, then open **Assets/Scenes/CloudRail_Visual.unity**
and press Play. The project uses URP 17.6.0, Input System 1.20.0, and uGUI/TextMeshPro.
Generated meshes, materials, fonts, and audio are committed, so a fresh clone does
not require running a scene builder. Other Editor versions have not been verified.

To build, install macOS build support for this Editor and choose
**Cloud Rail → Build macOS Release (Universal)**. The output is
`Builds/macOS/CloudRail.app`; the entry point is
`CloudRail.Editor.CloudRailPlayerBuilder.BuildMac`.

On macOS, run `python3 AgentScripts/package_macos.py` to package a DMG and ZIP,
include font licenses, apply ad-hoc signing to the distribution copy, and write
SHA-256 checksums under `Releases/`.

Runtime code lives in `Assets/CloudRail/Scripts`, procedural builders in
`Assets/CloudRail/Editor`, shaders in `Assets/CloudRail/Shaders`, and synthesized
audio source scripts in `AgentScripts`. See the [Chinese README](README.md) for
the full directory guide and design documents. Earlier milestone scenes remain
as development references; the current scene and build use **CloudRail_Visual**.
Running an early full-scene generator can replace later scene content.

This is a visual prototype in active development. This release supplies a macOS
player; Windows and Linux binaries are not included. Capture helpers compile
only inside the Editor and do not run in ordinary gameplay.

## License

Original code, procedural scene content, and synthesized audio are available under
[MIT](LICENSE). Fonts, imported TextMeshPro content, and Unity components retain
their own licenses; see [third-party notices](THIRD_PARTY_NOTICES.md).
