"""Refine the existing Techno Puma in Blender; run through Blender MCP.

Reference: references/techno_puma.png. Preserve the existing character and rig.
Only the fitted skirt, rose hair treatment, and tapered glasses are changed.
"""
import math
import bpy
from mathutils import Vector
from mathutils.geometry import convex_hull_2d
from mathutils.bvhtree import BVHTree


def principled(material):
    return next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')


def recolor_hair():
    for obj in bpy.data.objects:
        if obj.type != 'MESH' or not obj.name.startswith('SM_TP_Golden_'):
            continue
        attr = obj.data.color_attributes.get('GoldenStrandTint')
        if not attr:
            continue
        original = obj.data.color_attributes.get('OriginalGoldenStrandTint')
        if original is None:
            original = obj.data.color_attributes.new(name='OriginalGoldenStrandTint', type='FLOAT_COLOR', domain='POINT')
            for src, dst in zip(attr.data, original.data):
                dst.color = src.color
        for src, dst in zip(original.data, attr.data):
            value = src.color[0]
            dst.color = (value * .60, value * .30, value * .34, src.color[3])
    mat = bpy.data.materials['MAT_TP_GoldenChampagne_Groom']
    shader = principled(mat)
    shader.inputs['Metallic'].default_value = 0
    shader.inputs['Roughness'].default_value = .36
    shader.inputs['Anisotropic'].default_value = .58
    shader.inputs['Sheen Weight'].default_value = .24
    mat.diffuse_color = (.55, .30, .33, 1)
    crown = bpy.data.materials['MAT_TP_GoldenCrown_Champagne']
    principled(crown).inputs['Base Color'].default_value = (.40, .22, .24, 1)
    crown.diffuse_color = (.40, .22, .24, 1)
    scalp = bpy.data.materials['MAT_TP_OriginalFibers_PinkBlondeTint']
    mix = next(n for n in scalp.node_tree.nodes if n.type == 'MIX_RGB')
    mix.blend_type = 'MULTIPLY'
    mix.inputs[0].default_value = 1
    mix.inputs[2].default_value = (.95, .57, .65, 1)


def fitted_crown():
    """Replace floating crown strips with a scalp-following center-part groom."""
    head = bpy.data.objects['Object_8'].evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = head.to_mesh()
    vertices = [head.matrix_world @ v.co for v in mesh.vertices]
    tree = BVHTree.FromPolygons(vertices, [tuple(p.vertices) for p in mesh.polygons])
    head.to_mesh_clear()
    center = Vector((0, .04, 1.58))
    def surface(point, offset=.005):
        direction = (Vector(point)-center).normalized()
        hit, normal, _, _ = tree.ray_cast(center, direction, .4)
        if hit is None:
            return Vector(point)
        return hit + direction*offset
    for obj in bpy.data.objects:
        if obj.name.startswith('SM_TP_GoldenCrown_') or obj.name == 'SM_TP_Hair_Scan_Scalp':
            obj.hide_render = True
            obj.hide_set(True)
        if obj.type != 'MESH' or not obj.name.startswith(('SM_TP_Golden_SilkWig_', 'SM_TP_Golden_LayeredLocks_')):
            continue
        original = obj.data.attributes.get('reference_fit_original_position')
        if original is None:
            original = obj.data.attributes.new('reference_fit_original_position', 'FLOAT_VECTOR', 'POINT')
            for v, item in zip(obj.data.vertices, original.data):
                item.vector = v.co
        for v, item in zip(obj.data.vertices, original.data):
            p = item.vector.copy()
            weight = max(0, min(1, (p.z-1.635)/.065))
            v.co = p.lerp(surface(p, .007), weight)
    prefix = 'SM_TP_Reference_Crown_'
    for obj in list(bpy.data.objects):
        if obj.name.startswith(prefix):
            bpy.data.objects.remove(obj, do_unlink=True)
    shades = [(.27,.12,.14,1),(.36,.17,.19,1),(.46,.23,.26,1),(.54,.29,.32,1)]
    mats = []
    for i, color in enumerate(shades):
        name = 'MAT_TP_RoseSilk_Crown_'+str(i)
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mat.use_nodes = True
        shader = principled(mat)
        shader.inputs['Base Color'].default_value = color
        shader.inputs['Roughness'].default_value = .40
        shader.inputs['Anisotropic'].default_value = .55
        shader.inputs['Sheen Weight'].default_value = .20
        mat.diffuse_color = color
        mats.append(mat)
    verts, faces = [], []
    rows, cols = 32, 128
    for j in range(rows+1):
        for i in range(cols):
            phi = i*math.tau/cols
            limit = 1.40 - .43*math.cos(phi)
            theta = .012 + (limit-.012)*j/rows
            p = center+Vector((.10*math.sin(theta)*math.sin(phi), -.12*math.sin(theta)*math.cos(phi), .145*math.cos(theta)))
            verts.append(tuple(surface(p,.005)))
            if j:
                faces.append(((j-1)*cols+i,(j-1)*cols+(i+1)%cols,j*cols+(i+1)%cols,j*cols+i))
    data = bpy.data.meshes.new(prefix+'Base')
    data.from_pydata(verts,[],faces);data.update()
    for mat in mats:data.materials.append(mat)
    for face in data.polygons:
        face.use_smooth = True
        face.material_index = 1
    obj = bpy.data.objects.new(prefix+'Base',data)
    bpy.context.collection.objects.link(obj)
    # Fine parallel fibers emerge from a narrow part and sweep over the scalp.
    for color_index, mat in enumerate(mats):
        data = bpy.data.curves.new(prefix+'Fibers_'+str(color_index),'CURVE')
        data.dimensions = '3D';data.bevel_depth = .00019;data.bevel_resolution = 2
        data.materials.append(mat)
        for side in [-1,1]:
            for strand in range(color_index, 384, 4):
                y0 = -.145 + strand/383*.43
                spline = data.splines.new('POLY');spline.points.add(63)
                for j, point in enumerate(spline.points):
                    t = j/63
                    p = (side*(.0012+.118*t), y0+.020*t*t, 1.735-.135*t*t)
                    xyz = surface(p,.0061+.0003*math.sin(strand*1.7))
                    point.co=(*xyz,1)
                    point.radius = .7+.3*math.sin(math.pi*t)
        obj=bpy.data.objects.new(data.name,data);bpy.context.collection.objects.link(obj)
    # A fine fiber shader keeps the underlayer from reading as a smooth cap.
    mat=mats[1];nodes=mat.node_tree.nodes;links=mat.node_tree.links
    for n in list(nodes):
        if n.label == 'Reference crown fiber detail':
            nodes.remove(n)
    coord=nodes.new('ShaderNodeTexCoord');coord.label='Reference crown fiber detail'
    stretch=nodes.new('ShaderNodeVectorMath');stretch.operation='MULTIPLY';stretch.label='Reference crown fiber detail'
    stretch.inputs[1].default_value=(130,5,5)
    noise=nodes.new('ShaderNodeTexNoise');noise.label='Reference crown fiber detail';noise.inputs['Scale'].default_value=3
    ramp=nodes.new('ShaderNodeValToRGB');ramp.label='Reference crown fiber detail'
    ramp.color_ramp.elements[0].color=(.13,.045,.055,1)
    ramp.color_ramp.elements[1].color=(.46,.23,.26,1)
    bump=nodes.new('ShaderNodeBump');bump.label='Reference crown fiber detail'
    bump.inputs['Strength'].default_value=.25;bump.inputs['Distance'].default_value=.0004
    links.new(coord.outputs['Generated'],stretch.inputs[0]);links.new(stretch.outputs['Vector'],noise.inputs['Vector'])
    links.new(noise.outputs['Fac'],ramp.inputs['Fac']);links.new(ramp.outputs['Color'],principled(mat).inputs['Base Color'])
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],principled(mat).inputs['Normal'])
    print('CROWN: fitted scalp cap and 768 fine center-part strands')


def body_sections():
    depsgraph = bpy.context.evaluated_depsgraph_get()
    meshes = []
    for name in ['Object_9', 'Object_11']:
        obj = bpy.data.objects[name].evaluated_get(depsgraph)
        mesh = obj.to_mesh()
        verts = [obj.matrix_world @ v.co for v in mesh.vertices]
        edges = [(e.vertices[0], e.vertices[1]) for e in mesh.edges]
        meshes.append((verts, edges))
        obj.to_mesh_clear()
    return meshes


def ring_at(z, meshes, count=96):
    points = []
    for vertices, edges in meshes:
        for ia, ib in edges:
            a, b = vertices[ia], vertices[ib]
            if (a.z <= z < b.z) or (b.z <= z < a.z):
                p = a.lerp(b, (z-a.z)/(b.z-a.z))
                points.append(Vector((p.x, p.y)))
    hull = [points[i] for i in convex_hull_2d(points)]
    center = sum(hull, Vector((0, 0))) / len(hull)
    def cross(a, b):
        return a.x*b.y-a.y*b.x
    radii = []
    for i in range(count):
        direction = Vector((math.cos(i*math.tau/count), math.sin(i*math.tau/count)))
        hits = []
        for j, a in enumerate(hull):
            edge = hull[(j+1)%len(hull)]-a
            denom = cross(direction, edge)
            if abs(denom) > 1e-9:
                t, u = cross(a-center, edge)/denom, cross(a-center, direction)/denom
                if t > 0 and -.0001 <= u <= 1.0001:
                    hits.append(t)
        radii.append(max(hits))
    # Smooth the scan's small facets while keeping the actual hip profile.
    for _ in range(3):
        radii = [(radii[(i-1)%count]+2*radii[i]+radii[(i+1)%count])/4 for i in range(count)]
    result = []
    for i, r in enumerate(radii):
        theta = i*math.tau/count
        wrinkle = .00065 * math.sin(theta*14 + z*85) * math.sin(math.pi*(z-.80)/.233)**2
        r += .006 + wrinkle
        result.append((center.x+r*math.cos(theta), center.y+r*math.sin(theta), z))
    return result


def replace_curve(name, points, bevel=None, closed=False):
    obj = bpy.data.objects[name]
    obj.data.splines.clear()
    spline = obj.data.splines.new('POLY')
    spline.points.add(len(points)-1)
    for point, xyz in zip(spline.points, points):
        point.co = (*xyz, 1)
    spline.use_cyclic_u = closed
    if bevel is not None:
        obj.data.bevel_depth = bevel
    return obj


def fitted_skirt():
    meshes = body_sections()
    zs = [.800, .802] + [.806+i*(.223/32) for i in range(33)] + [1.031, 1.033]
    rings = [ring_at(z, meshes) for z in zs]
    vertices = [p for ring in rings for p in ring]
    count = len(rings[0])
    faces = [(j*count+i, j*count+(i+1)%count, (j+1)*count+(i+1)%count, (j+1)*count+i)
             for j in range(len(rings)-1) for i in range(count)]
    obj = bpy.data.objects['SM_TP_Vinyl_Micro_Mini_Skirt']
    mats = list(obj.data.materials)
    mesh = bpy.data.meshes.new('TechnoPuma_FittedSkirt')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for mat in mats:
        mesh.materials.append(mat)
    for face in mesh.polygons:
        face.use_smooth = True
    obj.data = mesh
    for mod in obj.modifiers:
        if mod.type == 'SOLIDIFY':
            mod.thickness = .002
            mod.offset = 0
        elif mod.type == 'BEVEL':
            mod.width = .001
    uv = mesh.uv_layers.new(name='UVMap')
    for face in mesh.polygons:
        for loop_index in face.loop_indices:
            vi = mesh.loops[loop_index].vertex_index
            u = (vi % count)/count
            if face.index % count == count-1 and u == 0:
                u = 1
            uv.data[loop_index].uv = (u, (vertices[vi][2]-.800)/.233)
    replace_curve('SM_TP_Skirt_Refitted_Lower_Hem', rings[0], .0015, True)
    replace_curve('SM_TP_Skirt_Refitted_Waist_Hem', rings[-1], .0025, True)
    for side, index in [(-1, count//2), (1, 0)]:
        replace_curve('SM_TP_Skirt_Side_Seam_'+str(side), [ring[index] for ring in rings], .0010)
    # Bring existing belt hardware to the fitted waistband.
    front = min(p[1] for p in rings[-1])
    bpy.data.objects['SM_TP_Waist_Chrome_Buckle'].location.y = front-.006
    bpy.data.objects['SM_TP_Waist_Buckle_Inner'].location.y = front-.012
    # Keep the old decorative chains attached after removing the skirt flare.
    for name in ['SM_TP_Hip_Chain_-1','SM_TP_Hip_Chain_1','SM_TP_Hip_Chrome_Loop_-1','SM_TP_Hip_Chrome_Loop_1']:
        accessory=bpy.data.objects[name]
        if accessory.get('reference_fit_attached'):
            continue
        points=[p for s in accessory.data.splines for p in (list(s.bezier_points) if s.type=='BEZIER' else list(s.points))]
        for p in points:
            if len(p.co)==3:
                # Bezier handles must move with the control points.
                new=Vector((p.co.x*.87,p.co.y*.73,p.co.z))
                delta=new-p.co;p.co=new;p.handle_left+=delta;p.handle_right+=delta
            else:
                p.co=(p.co.x*.87,p.co.y*.73,p.co.z,1)
        accessory['reference_fit_attached']=True
    shader = principled(bpy.data.materials['MAT_TP_Black_Liquid_Latex'])
    shader.inputs['Roughness'].default_value = .23
    shader.inputs['Coat Weight'].default_value = .35
    print('FITTED_SKIRT', len(vertices), 'vertices', 'hip width', round(max(v[0] for v in vertices)-min(v[0] for v in vertices), 3))


def tapered_glasses():
    for obj in bpy.data.objects:
        if not obj.name.startswith('SM_TP_Wayfarer_'):
            continue
        if obj.type == 'MESH':
            points = list(obj.data.vertices)
        elif obj.type == 'CURVE':
            points = [p for s in obj.data.splines for p in s.points]
        else:
            continue
        key = 'reference_fit_original_coordinates'
        if key not in obj:
            obj[key] = [float(v) for p in points for v in p.co[:3]]
        original = list(obj[key])
        is_lens_or_rim = 'Lens_' in obj.name or 'BlackRim_' in obj.name
        for i, p in enumerate(points):
            x, y, z = original[i*3:i*3+3]
            if is_lens_or_rim:
                center = -.051 if x < 0 else .051
                vertical = max(0, min(1, (z-1.561)/.053))
                width = .69 + .31*vertical
                x = center+(x-center)*width
                if obj.type == 'CURVE':
                    p.radius = .72+.30*vertical
            # Move the entire pair closer to the face, retaining the temple shape.
            y += .017
            if obj.type == 'MESH':
                p.co = (x, y, z)
            else:
                p.co = (x, y, z, 1)
    print('GLASSES: lower rim narrowed by 31 percent; upper width preserved')
    frame=principled(bpy.data.materials['MAT_TP_RayBan_Gloss_Black_Acetate'])
    frame.inputs['Metallic'].default_value=0
    frame.inputs['Coat Weight'].default_value=.25
    frame.inputs['Roughness'].default_value=.26
    frame.inputs['Specular IOR Level'].default_value=.28
    lenses=principled(bpy.data.materials['MAT_TP_RayBan_Dark_Smoke'])
    lenses.inputs['Specular IOR Level'].default_value=.20
    lenses.inputs['Coat Weight'].default_value=.08


recolor_hair()
fitted_crown()
fitted_skirt()
tapered_glasses()
bpy.data.objects['LGT_TP_Pink_Hair_Rim'].data.energy = 125
bpy.context.view_layer.update()
print('Techno Puma reference fit applied; inspect renders before saving.')
