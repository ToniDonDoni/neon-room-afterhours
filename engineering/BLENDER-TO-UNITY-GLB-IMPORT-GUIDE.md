# Blender to Unity GLB Import Guide

## Purpose

Transfer a Blender character into Unity while preserving its mesh and PBR textures. Glass Heart uses this workflow.

## Is Blender required?

- For a `.blend` source, use Blender to edit the model and export it as GLB.
- For an existing `.glb`, import it directly into Unity with glTFast. Blender is optional unless the model needs changes.
- Unity does not need Blender installed to use the exported GLB. Keep the `.blend` as the editable source.

## Export from Blender

1. Open and save the intended source, such as `blender/techno_puma_ai.blend`.
2. Select only the character and its required objects. Exclude the default cube, cameras and lights.
3. Use **File → Export → glTF 2.0**, choose **glTF Binary (.glb)** and enable **Selected Objects**.
4. Export materials and textures. Keep the mesh detail; check that base color, normal and metallic/roughness maps are included. Bake unsupported procedural materials to textures before export.

GLB packages the mesh, material definitions and image textures into one file. The Glass Heart export preserved all three texture maps.

## Import into Unity

1. Open the `unity/` project. In Package Manager, use **Install package by name** to install `com.unity.cloud.gltfast`, version `6.20.0`. This dependency is recorded in `unity/Packages/manifest.json`.
2. If installing its Burst dependency requests an Editor restart, restart Unity before continuing.
3. Copy the GLB to `unity/Assets/resources/avatars/glass-heart-ai.glb`. Let Unity finish importing and compiling.
4. Keep the generated `.meta` beside the asset. Commit both files when committing the model.

## Connect and inspect the character

`unity/Assets/NeonRoomGame.cs` loads `Resources.Load<GameObject>("avatars/glass-heart-ai")` for Glass Heart, outfit index `8`. It instantiates the imported model, preserves its materials, centers it, places its feet at ground level, scales it to `2.08` units tall and assigns the character's render layer.

Enter Play mode, open the wardrobe and select **Glass Heart**. Rotate the preview and check proportions, orientation, textures and Console errors. Editor appearance does not replace a WebGL build check.

For agent automation, Unity MCP provides `manage_packages`, `refresh_unity`, `execute_code`, `read_console` and `manage_camera`. Blender MCP can export the selected objects through `bpy.ops.export_scene.gltf`.
