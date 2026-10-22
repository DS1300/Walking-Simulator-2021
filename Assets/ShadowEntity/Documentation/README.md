# Shadow Entity: Walking Simulator

An original, rigged supernatural humanoid made of darkness and living smoke, with
presentation animations only (no AI, no movement, no triggers, no camera logic).

## Requirements

- Unity **2019.4** LTS (made for 2019.4.4f1; built and tested in 2019.4.41f2, see "Testing" below)
- Universal Render Pipeline **7.3.1** (brings Shader Graph 7.3.1 and Core RP 7.3.1)
- No VFX Graph, no HDRP, no other packages. Smoke uses the built-in Particle System.
- Works in Linear and Gamma color space (see "Color space"). The URP Depth Texture is not needed.

## Import

1. Open your project (URP 7.3.1 already set up).
2. Assets > Import Package > Custom Package > `WalkingSimulator_ShadowEntity_URP731.unitypackage`.
3. Import everything. All files land in `Assets/ShadowEntity/`.

## Add it to a scene

Drag `ShadowEntity/Prefabs/ShadowEntity.prefab` into the scene.

- **Pivot**: on the ground, under the creature. It floats on its own; the mist trail ends just above the ground.
- **Front**: the prefab's **+Z (blue arrow)**. Its face, LookUp stare and FinalLunge all go toward +Z.
- **Height**: about 2.4 m. Scale the prefab root uniformly to change it; smoke scales with it.
- **Animation never moves the root.** Only children under `VisualRoot` move, so you can place
  and duplicate the prefab anywhere.

Hierarchy:

```
ShadowEntity            Animator + ShadowEntityController (optional helper)
  VisualRoot
    Model               rigged FBX (ShadowEntityBody mesh + ShadowEntityRig bones)
  SmokeEffects          BodySmoke, Wisps, Fragments, MistTrail
  GlitchEffects         GlitchShards
  AppearanceEffects     Burst (used by Appear / Disappear / FinalLunge)
```

## States and how to play them

Everything lives in the Animator controller `Animations/ShadowEntity.controller`.
Use the parameters directly from your own scripts or Timeline:

| What | Animator parameter | Optional helper method |
|---|---|---|
| Idle: slow floating, smoke | default state | |
| LookUp: stares straight up (tower) | `LookUp` bool = true | `SetLookUp(true)` |
| Glitch: supernatural distortion burst | `Glitch` trigger | `PlayGlitch()` / `PlayGlitch(intensity, seconds)` |
| Aggressive glitch | `GlitchAggressive` trigger | `PlayAggressiveGlitch()` |
| HeadTilt: slow unnatural head tilt | `HeadTilt` trigger | `PlayHeadTilt()` |
| Disappear: dissolves into smoke, stays hidden | `Disappear` trigger | `PlayDisappear()` |
| Appear: materializes from smoke (only while hidden) | `Appear` trigger | `PlayAppear()` |
| FinalLunge: arms up, reaches and lunges ~1.3 m toward +Z while glitching violently, then holds | `FinalLunge` trigger | `PlayFinalLunge()` |
| Back to idle (e.g. after FinalLunge) | | `ResetToIdle()` |

Layers: **Base** (Idle, LookUp, Disappear, Hidden, Appear, FinalLunge), **Glitch** (additive, so it
works on top of any pose), **HeadTilt** (additive, works on top of Idle or LookUp).

- Glitch intensity = the Glitch layer weight (0..1). Glitch duration = the `GlitchSpeed` float
  (1 = 1.5 s; the helper sets it for you).
- To start invisible: tick `Start Hidden` on ShadowEntityController, or call `animator.Play("Hidden")`.
- `ShadowEntityController` is optional and presentation-only. Delete it if you drive the Animator yourself.

Animation clips (`Animations/`, 30 fps): ShadowEntity_Idle (8 s loop), _LookUp (10 s loop),
_HeadTilt (3.5 s), _Glitch (1.5 s), _GlitchAggressive (2.5 s), _Appear (2 s), _Disappear (1.2 s),
_Hidden, _FinalLunge (2.6 s). The same actions are inside the FBX as takes.

## Rig

Generic rig, 56 bones: Root, Hips, Spine, Chest, Neck, Head, Jaw; per side Shoulder, UpperArm,
ForeArm, Hand and 3 joints for each of 5 fingers (Thumb, Index, Middle, Ring, Pinky); three mist
tendrils (TailL1-4, TailR1-4, TailC1-3). Mesh: ~24k triangles, one skinned mesh (Unity uses the
4 strongest bone weights per vertex).

The mesh stores its rest position in UV0 (x, height) and UV1 (depth, mist mask). The shaders use
these for smoke noise that stays stuck to the body. Keep them if you edit the mesh.

## Materials and exposed settings

All four shaders are real Shader Graph 7.3.1 graphs in `Shaders/` and stay editable.

**M_ShadowEntityBody** (SG_ShadowEntityBody: Lit, opaque, alpha-clip, two-sided)

| Property | Meaning |
|---|---|
| Main Shadow Color | body color (keep very dark) |
| Edge Highlight Color | cold edge light (HDR) |
| Edge Power / Edge Intensity | width / strength of the edge light |
| Distance Edge Boost | extra edge light far away (36 m+) so the silhouette reads at night |
| Distortion Intensity | smoke-like vertex deformation (meters) |
| Noise Speed / Noise Scale | speed and size of the moving shadow patterns |
| Smoke Breakup | how much the edges and lower body erode into smoke |
| Opacity | overall opacity (dithered) |
| Dissolve Amount | 0 = solid, 1 = gone (animated by Appear/Disappear) |
| Glitch (animated) | driven by the glitch clips; leave at 0 |
| Glitch Intensity | multiplier for every glitch effect |
| Smoothness | surface sheen |

**M_ShadowEntitySmokeShell** (transparent smoke hull drawn over the body): Smoke Color, Edge
Highlight Color, Smoke Shell Thickness, Smoke Shell Opacity, Noise Speed/Scale, Opacity, Dissolve.

**M_ShadowEntityGhost** (ghosted outlines; only visible while glitching): Ghost Outline Color,
Ghost Opacity, Ghost Offset, Glitch Intensity.

**M_ShadowSmokeParticles** (all smoke particles): Smoke Texture, Smoke Tint, Distortion Intensity,
Noise Speed.

The body renderer has three materials on one mesh (body, smoke shell, ghost). Unity draws the mesh
once per material. That's intentional, and Unity shows a small note about it in the Inspector.

## Particles

Six small emitters, about 200 particles alive per creature (measured). They use World space, so smoke trails
behind when you move the prefab. The particle Shape module emits from the skinned mesh, so the
FBX has Read/Write enabled.

## Performance (measured)

Demo player, 1280x720, AMD RX 6700 XT, Linear: scene alone 0.57 ms/frame, with 1 creature 0.66 ms,
with 9 creatures 1.14 ms. Animator culling is "Cull Update Transforms". Animated material values use
a MaterialPropertyBlock, so the SRP Batcher skips these renderers. That's normal and cheap at this count.

## Demo

`Demo/ShadowEntity_Demo.unity`: a neutral inspection area, a dark night test area with forest,
gravestone, tower and cabin-window stand-ins, and viewpoints (ground, 35 m, tower top, cabin
window, close-up). Press Play and use the on-screen panel (H hides it) to trigger every state,
toggle a flashlight and duplicate the creature. The demo scripts are only for this scene.

## Source files

`Documentation/Source/ShadowEntity_BlenderSource.zip` holds `ShadowEntity.blend` (needs Blender 5.2
or newer) and `build_entity.py`, the script that generates the model, rig, weights and actions. It's
zipped so Unity doesn't try to import the .blend. Re-export to FBX with: Forward -Z, Up Y,
Apply Scalings "FBX All", no leaf bones, all actions baked.

## Color space

Made in Linear. In Gamma projects a small Custom Function node in the body and ghost graphs
(`ShadowEntityGlow`) redoes the glow math in linear, so the edge light stays visible. Gamma still looks
a little dimmer; set **Edge Intensity** to about 1.5 on M_ShadowEntityBody if you want to match.

## Testing

- Built and checked in Unity 2019.4.41f2 with URP 7.3.1 / Shader Graph 7.3.1 in a Windows player
  build: all shaders compile, no pink materials, no missing scripts, every state captured on screen.
- The exported package was imported into a brand-new 2019.4.41f2 + URP 7.3.1 project (Gamma) and
  checked again: 9/9 materials OK, 0 missing scripts, Animator complete, player build and screenshots OK.
- The package holds only `Assets/ShadowEntity`. It doesn't bundle URP or Shader Graph files; it
  uses the copies already in your project.
- **Not tested in 2019.4.4f1 itself**: Unity's free Personal license can't activate editors older
  than 2019.4.41f2. Both use the same package versions (URP/Shader Graph 7.3.1) and the same 2019.4
  file formats, so it should open the same. Please give it a quick check in 2019.4.4f1.

## License

Original work made for Walking Simulator with Blender and Unity. It contains no third-party,
store-bought or ripped assets. The smoke texture is generated by code.
