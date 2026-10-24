# Third-party notices

The MIT license at the repository root applies to the original CloudRail source,
procedural scene content, and original synthesized audio. It does not replace the
licenses of the components below.

## Noto Sans CJK SC

- Bundled font: `Assets/CloudRail/Fonts/NotoSansCJKsc-Regular.otf`.
- The derived TextMeshPro font asset is `Assets/CloudRail/Fonts/CloudRailChinese.asset`.
- Copyright embedded in the font: © 2014–2021 Adobe (http://www.adobe.com/).
- License: SIL Open Font License 1.1; full text is in `Assets/CloudRail/Fonts/OFL.txt`.
- Upstream: https://github.com/notofonts/noto-cjk

## Liberation Sans / TextMeshPro essentials

- Font and font assets: `Assets/TextMesh Pro/Fonts/` and
  `Assets/TextMesh Pro/Resources/Fonts & Materials/`.
- Liberation Sans uses SIL Open Font License 1.1. Copyright and full license:
  `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`.
- Imported TextMeshPro shaders and settings retain their original notices and
  Unity package licensing; they are not relicensed as original CloudRail code.

## Unity Editor, packages and player runtime

Unity, URP, Input System, uGUI/TextMeshPro and other dependencies are supplied by
Unity under their respective licenses. Package versions are declared in
`Packages/manifest.json` and `Packages/packages-lock.json`. Their source licenses
can be found in each resolved package. Unity's cached packages and Editor are not
redistributed in this source repository. The downloadable macOS player includes
Unity runtime components subject to Unity's license terms.

## Project content

The scene is procedurally modeled in the C# builder scripts. The soundtrack and
ambient effects are synthesized by `AgentScripts/compose_cloudline.py` and
`AgentScripts/compose_waterfall.py`. The showcase uses actual Unity scene footage
and those audio tracks; it does not include the reference Douyin video or its
music. System fonts used for the showcase title were rendered into the media;
their font files are not bundled.
