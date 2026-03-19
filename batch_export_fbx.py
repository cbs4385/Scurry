import bpy
import os
import glob

src_dir = r"C:\Users\chris\source\repos\Scurry\Scurry\Assets\Resources\Blender Card Models"
dst_dir = r"C:\Users\chris\source\repos\Scurry\Scurry\Assets\Resources\Card Models"
tex_dir = os.path.join(dst_dir, "Textures")
os.makedirs(tex_dir, exist_ok=True)

blend_files = sorted(glob.glob(os.path.join(src_dir, "*.blend")))
print(f"Found {len(blend_files)} .blend files to process")

results = []
for blend_path in blend_files:
    basename = os.path.splitext(os.path.basename(blend_path))[0]
    fbx_path = os.path.join(dst_dir, f"{basename}.fbx")

    try:
        bpy.ops.wm.open_mainfile(filepath=blend_path)

        # Save packed textures as separate PNGs so Unity can find them
        for img in bpy.data.images:
            if img.name == 'Render Result' or img.size[0] == 0:
                continue
            png_name = f"{basename}_{img.name}.png"
            png_path = os.path.join(tex_dir, png_name)
            # Unpack to save, then repack
            try:
                img.filepath_raw = png_path
                img.file_format = 'PNG'
                img.save()
                print(f"  Saved texture: {png_name} ({img.size[0]}x{img.size[1]})")
            except Exception as e:
                print(f"  Failed to save texture {img.name}: {e}")

        # Export FBX with embedded textures
        bpy.ops.export_scene.fbx(
            filepath=fbx_path,
            use_selection=False,
            path_mode='COPY',
            embed_textures=True,
            mesh_smooth_type='FACE',
            use_mesh_modifiers=True,
            bake_anim=False,
            apply_scale_options='FBX_SCALE_ALL'
        )

        results.append(f"OK: {basename}")
        print(f"  Exported: {basename}.fbx")
    except Exception as e:
        results.append(f"FAIL: {basename} - {str(e)}")
        print(f"  FAILED: {basename} - {e}")

fail_count = sum(1 for r in results if r.startswith("FAIL"))
print(f"\nDone: {len(results)} processed, {len(results) - fail_count} OK, {fail_count} failures")
