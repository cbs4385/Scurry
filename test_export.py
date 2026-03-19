import bpy
import os

bpy.ops.wm.open_mainfile(filepath=r"C:\Users\chris\source\repos\Scurry\Scurry\Assets\Resources\Blender Card Models\001.blend")

# First, make sure all images are packed
for img in bpy.data.images:
    print(f"Image: {img.name}, packed={img.packed_file is not None}, filepath='{img.filepath}', size={img.size[0]}x{img.size[1]}")
    if img.packed_file is None and img.filepath:
        try:
            img.pack()
            print(f"  Packed successfully")
        except Exception as e:
            print(f"  Failed to pack: {e}")

dst = r"C:\Users\chris\source\repos\Scurry\Scurry\Assets\Resources\Card Models\001.fbx"

# Export with COPY mode and embed
bpy.ops.export_scene.fbx(
    filepath=dst,
    use_selection=False,
    path_mode='COPY',
    embed_textures=True,
    mesh_smooth_type='FACE',
    use_mesh_modifiers=True,
    bake_anim=False,
    apply_scale_options='FBX_SCALE_ALL'
)

# Check file size to see if textures were embedded
size = os.path.getsize(dst)
print(f"\nExported: {dst}")
print(f"File size: {size} bytes ({size/1024:.1f} KB)")
print("If textures are embedded, size should be > 100KB for a 512x512 model")
