import bpy

bpy.ops.wm.open_mainfile(filepath=r"C:\Users\chris\source\repos\Scurry\Scurry\Assets\Resources\Blender Card Models\001.blend")

for obj in bpy.data.objects:
    if obj.type == 'MESH':
        print(f"Object: {obj.name}")
        for slot in obj.material_slots:
            mat = slot.material
            if mat is None:
                print("  Material: None")
                continue
            print(f"  Material: {mat.name}, use_nodes={mat.use_nodes}")
            if mat.use_nodes:
                for node in mat.node_tree.nodes:
                    print(f"    Node: {node.type} ({node.name})")
                    if node.type == 'TEX_IMAGE':
                        img = node.image
                        if img:
                            print(f"      Image: {img.name}, packed={img.packed_file is not None}, filepath={img.filepath}, size={img.size[0]}x{img.size[1]}")
                        else:
                            print(f"      Image: None")
            else:
                print("    No shader nodes")
