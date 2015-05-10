#!BPY

"""
Name: 'DirectX (.x) [DP]...'
Blender: 242
Group: 'Export'
Tip: 'Export to DirectX text file format.'
"""

__author__ = "Heikki Salo"
__url__ = ("elysiun", "Home page, http://directpython.sourceforge.net/exportx.html")
__version__ = "1.0 2006/12/20"
__bpydoc__ = """Exports meshes and animations to Direct3D .x format."""

# --------------------------------------------------------------------------
# x_export.py 
# Copyright (C) Heikki Salo
# --------------------------------------------------------------------------

import os

import Blender
from Blender import Draw, BGL, Mathutils

##############################################################
#
#   Constants etc.
#
##############################################################

HEADERTEXT = "xof 0303txt 0032\n\n"
HEADERZIP = "xof 0303tzip0032"

#Templates are not included in the file. If you encounter
#a reader that can't live without them, send me a message.

MATERIAL = """\
Material %s {
    %f; %f; %f; %f;;
    %f;
    %f; %f; %f;;
    %f; %f; %f;;
    TextureFilename { "%s"; }
}
"""

FRAME = """\
FrameTransformMatrix {
    %f, %f, %f, %f,
    %f, %f, %f, %f,
    %f, %f, %f, %f,
    %f, %f, %f, %f;;
}
"""

##############################################################
#
#   Code start.
#
##############################################################

#Global flags set by the user interface.
ExportType = 0 #all=0, selected=1, animated=2
Normals = True
Colors = False
TexCoords = False
Materials = True
SwapYZ = True
UseWTrans = True
Compressed = False #Always false, not yet supported.

#Output file.
out = None
  
class XFile:
    """Simple file-like class. Writes contents
       into a file only when closed. """
    def __init__(self, filename):
        name = filename.strip().lower()
        if name[-2:] != ".x":
            name += ".x"
    
        self.fname = name
        self.contents = []
        print "\nExporting to '%s'..." % self.fname
        
    def write(self, string):
        self.contents.append(string)
  
    def close(self):
        self.f = file(self.fname, "w")
    
        if Compressed:
            assert 0
        else:
            self.f.write(HEADERTEXT)
            self.f.write("".join(self.contents))
        self.f.close()
  
def GetVertexData(data, mesh):
    """Returns formatted vertex data."""
    result = []
    for i, vertex in enumerate(data.verts):
        if UseWTrans:
            x, y, z = mesh.matrixWorld[3][0], mesh.matrixWorld[3][1], mesh.matrixWorld[3][2]
        else:
            x, y, z = 0.0, 0.0, 0.0
            
        if SwapYZ and ExportType != 2:
            result.append("%f; %f; %f;" % (vertex.co.x + x, vertex.co.z + z, vertex.co.y + y))
        else:
            result.append("%f; %f; %f;" % (vertex.co.x + x, vertex.co.y + y, vertex.co.z + z))
            
        if i == len(data.verts) - 1:
            result.append(";\n")
        else:
            result.append(",\n")
    return "".join(result)
    
def GetFaceData(data):
    """Returns formatted face data."""
    result = []
    result.append("%i;\n" % len(data.faces))
    for i, face in enumerate(data.faces):
        result.append("%i; " % len(face.v))
        result.append(", ".join([str(v.index) for v in face.v]))  
        if i == len(data.faces) - 1:
            result.append(";;")
        else:
            result.append(";,")
        result.append("\n")
    return "".join(result)
    
def GetNormalData(data):
    """Returns formatted normal data."""
    result = []
    for i, vertex in enumerate(data.verts):
        if SwapYZ and ExportType != 2:
            result.append("%f; %f; %f;" % (vertex.no.x, vertex.no.z, vertex.no.y))  
        else:
            result.append("%f; %f; %f;" % (vertex.no.x, vertex.no.y, vertex.no.z))  
            
        if i == len(data.verts) - 1:
            result.append(";\n")
        else:
            result.append(",\n")
    return "".join(result)
    
def GetMaterialData(data):
    """Returns formatted material data."""
    result = []
    #Save material definitions
    materials = []
    
    texname = "None"
    for mat in data.materials:
        #Go through all faces and look for images. Pretty slow with big meshes...
        for face in data.faces:
            if data.materials[face.materialIndex].name == mat.name:
                #Face material is our current material.
                if face.image:
                    #Try to make the filename sensible. 
                    texname = face.image.filename.split(os.sep)[-1].strip().replace("//", "")
                break
    
        #Write a material block.
        matname = ValidateName(mat.name)
        material = MATERIAL % (matname,
            mat.R, mat.G, mat.B, mat.alpha,  #Face color
            mat.spec, #1.0, #Specular power
            mat.specR, mat.specG, mat.specB, #Specular
            0.0, 0.0, 0.0, #Emissive
            texname
        )
        materials.append(material)
    
    #Save Face->Material indices.
    result.append("%i;\n%i;\n" % (len(materials), len(data.faces)))
    for i, face in enumerate(data.faces):
        if i == len(data.faces) - 1:
            result.append("%i;;\n" % face.materialIndex)
        else:
            result.append("%i,\n" % face.materialIndex)
            
    return "".join(result + materials)
    
def GetVertexColorData(data):
    """Returns formatted vertex color data."""
    result = []
    written = set()
    for face in data.faces:
        for i, vertex in enumerate(face.v):
            vindex = vertex.index
            if vindex not in written:
                #Only write vertices once, faces can
                #share same vertices.
                color = face.col[i]
                r, g, b, a = color.r / 255.0, color.g / 255.0, color.b / 255.0, color.a / 255.0
                result.append("%i; %f; %f; %f; %f;;\n" % (vindex, r, g, b, a))
                #Add vertex index.
                written.add(vindex)
            
    return "".join(result)
    
def GetTexCoordData(data):
    """Returns formatted texture coordinate data."""
    result = ["%s;\n" % len(data.verts)] #vertexcount == coord2count
    for i, face in enumerate(data.faces):
        #Loop through all faces.
        for x in xrange(len(face.v)):
            result.append("%f; %f;" % (data.faces[i].uv[x][0], data.faces[i].uv[x][1]))
            if i == len(data.faces) - 1 and x == len(face.v) - 1:
                #Last one.
                result.append(";\n") 
            else:
                result.append(",\n")
      
    return "".join(result)
    
def GetWeightData(data, bone):
    """Returns formatted skin data for SkinWeight template."""
    try:
        groupdata = data.getVertsFromGroup(bone.name, 1)
    except:
        #Placeholder.
        return "0;\n0.0;\n" + "1.0,0.0,0.0,0.0, 0.0,1.0,0.0,0.0, 0.0,0.0,1.0,0.0, 0.0,0.0,0.0,1.0;;\n"
    
    indices = []
    weights = []
    for i, (index, weight) in enumerate(groupdata):
        if i == len(groupdata) - 1:
            indices.append("%i;\n" % index)
            weights.append("%f;\n" % weight)
        else:
            indices.append("%i,\n" % index)  
            weights.append("%f,\n" % weight)
    
    m = Mathutils.Matrix(bone.matrix["ARMATURESPACE"])
    m = m.invert()
    weights.append((15 * "%f," + "%f;;\n") % (
        m[0][0], m[0][1], m[0][2], m[0][3],
        m[1][0], m[1][1], m[1][2], m[1][3],
        m[2][0], m[2][1], m[2][2], m[2][3],
        m[3][0], m[3][1], m[3][2], m[3][3],
    ))
    count = ["%i;\n" % (len(weights)-1)]
    return "".join(count + indices + weights)
    
def GetSkinMeshHeader(data, armature, bones):
    """Calculates and returns the XSkinMeshHeader template."""
    maxinfluence = 0
    for bone in bones:
        name = bone.name
        try:
            indices = data.getVertsFromGroup(name)
        except:
            #No group for this bone.
            continue
        for index in indices:
            #Go through all vertex indices and check
            #if the target vertex has more influences
            #than the old value.
            influences = data.getVertexInfluences(index)
            if len(influences) > maxinfluence:
                maxinfluence = len(influences)

    return "\nXSkinMeshHeader {\n%i;\n%i;\n%i;\n}\n" % (maxinfluence, 
        maxinfluence * 3, len(bones))
    
def WriteMesh(mesh, armature=None):
    """Writes a complete mesh in .x form."""
    data = mesh.getData()
    meshname = ValidateName(mesh.getName())
    print "Writing mesh '%s'..." % meshname

    #Mesh header.
    out.write("\nMesh %s {\n%i;\n" % (meshname, len(data.verts)))

    #Vertexdata
    out.write(GetVertexData(data, mesh))

    #Facedata
    facedata = GetFaceData(data)
    out.write(facedata)
    
    if Materials:
        #Materialdata
        out.write("\nMeshMaterialList {\n")
        out.write(GetMaterialData(data))
        out.write("}\n")
    
    if Colors:# and data.hasVertexColours():
        #Color data.
        out.write("\nMeshVertexColors {\n%i;\n" % len(data.verts))
        out.write(GetVertexColorData(data))
        out.write("}\n")
            
    if Normals:
        #Normaldata
        out.write("\nMeshNormals {\n%i;\n" % len(data.verts))
        out.write(GetNormalData(data))
        out.write(facedata)
        out.write("}\n")
    
    if TexCoords:# and data.hasFaceUV():
        #Texture coordinates
        out.write("\nMeshTextureCoords {\n")
        out.write(GetTexCoordData(data))
        out.write("}\n")
        
    if ExportType == 2:
        #Animated, write skin/weight data.
        root = GetRootBone(armature)
        bones = [root] + root.getAllChildren()
        
        #XSkinMesh header.
        out.write(GetSkinMeshHeader(data, armature, bones))

        #Skin weights.
        for bone in bones:
            out.write("\nSkinWeights {\n\"%s\";\n" % ValidateName(bone.name))
            out.write(GetWeightData(data, bone))
            out.write("}\n")
        
    #Mesh header end.
    out.write("}\n")
    

##############################################################
#
#   Animation export.
#
##############################################################
    
def WriteFrames(bone, mesh, armature):
    #Writes the base frame hierarchy.
    out.write("Frame %s {\n" % ValidateName(bone.name))

    m = CombineAnimationMatrices(armature, bone) 
    result = FRAME % (
        m[0][0], m[0][1], m[0][2], m[0][3],
        m[1][0], m[1][1], m[1][2], m[1][3],
        m[2][0], m[2][1], m[2][2], m[2][3],
        m[3][0], m[3][1], m[3][2], m[3][3],
    )  
    out.write(result)
    
    if bone.hasChildren():
        for child in bone.children:
            WriteFrames(child, mesh, armature)
    
    out.write("}\n")
    
def WriteAnimations(armature, action):
    #Write animation keys.
    result = []
    action.setActive(armature)
    data = armature.getData()
    ipos = action.getAllChannelIpos()

    for bone in data.bones.values():
        try:
            channel = ipos[bone.name]   
        except:
            #Skip, no animation.
            continue

        if channel is not None:
            #print "Exporting animation '%s'..." % channel.name
            ipobone = Blender.Ipo.Get(channel.name)
            frames = action.getFrameNumbers()
            if frames and ipobone:
                #Bone has animation.
                result.append("\nAnimation {\n") 
                result.append("  AnimationKey {\n    %i;\n    %i;\n" % (4, len(frames))) 
         
                for i, frame in enumerate(frames):
                    Blender.Set('curframe', frame)

                    info = "    %i;16;" + (15 * "%f,") + "%f;;"
                    m = CombineAnimationMatrices(armature, bone) 

                    result.append(info % (frame,
                        m[0][0], m[0][1], m[0][2], m[0][3],
                        m[1][0], m[1][1], m[1][2], m[1][3],
                        m[2][0], m[2][1], m[2][2], m[2][3],
                        m[3][0], m[3][1], m[3][2], m[3][3],        
                    ))    
                    if i == len(frames) - 1:
                        result.append(";\n")
                    else:
                        result.append(",\n") 
                     
                result.append("  }\n  { %s }\n}\n" %  ValidateName(bone.name))

    Blender.Set('curframe', 0)     
    out.write("".join(result))
    
def WriteAnimatedMesh(mesh, armature):
    print "Exporting mesh '%s' with armature '%s'..." % (mesh.getName(), armature.getName())
    root = GetRootBone(armature)
    
    if SwapYZ:
        #Write a root frame that transforms
        #all child frames. Non-animated
        #meshes do this by simply swapping
        #vertices y and z values.
        identity = Mathutils.Matrix([1, 0, 0, 0], [0, 1, 0, 0], [0, 0, -1, 0], [0, 0, 0, 1])
        rotation = Mathutils.RotationMatrix(-90, 4, 'x')
        m = rotation * identity
        frame = FRAME % (
            m[0][0], m[0][1], m[0][2], m[0][3],
            m[1][0], m[1][1], m[1][2], m[1][3],
            m[2][0], m[2][1], m[2][2], m[2][3],
            m[3][0], m[3][1], m[3][2], m[3][3],
        )
        out.write("Frame SwapYZ {\n")
        out.write(frame)
        
    #All frames.
    WriteFrames(root, mesh, armature)
    
    #Write a container (identity) frame for the mesh.
    m = Mathutils.Matrix([1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1])
    
    out.write("Frame Container_%s {\n" % ValidateName(mesh.name))
    out.write(FRAME % (
        m[0][0], m[0][1], m[0][2], m[0][3],
        m[1][0], m[1][1], m[1][2], m[1][3],
        m[2][0], m[2][1], m[2][2], m[2][3],
        m[3][0], m[3][1], m[3][2], m[3][3],
    ))
    WriteMesh(mesh, armature)
    out.write("}\n")
    
    if SwapYZ:
        out.write("}\n")
    
    #Animation speed.
    out.write("\nAnimTicksPerSecond {\n  %i;\n}\n" % Ticks)
    
    actions = Blender.Armature.NLA.GetActions()
    for key, action in actions.iteritems():
        out.write("AnimationSet %s {\n" % ValidateName(key))
        WriteAnimations(armature, action)
        out.write("}\n")
     
def CombineAnimationMatrices(armature, bone):
    pose = armature.getPose()
    posebone = pose.bones[bone.name]
    matrixbone = posebone.poseMatrix
    if bone.hasParent():
        parent = bone.parent
        matrix = pose.bones[parent.name].poseMatrix
    else:
        matrix = Mathutils.Matrix([1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1])

    result = Mathutils.Matrix(matrix)
    result.invert()
 
    return matrixbone * result
      
def GetRootBone(armature):
    #Returns the root bone of an armature object.
    data = armature.getData()
    for bone in data.bones.values():
        if not bone.hasParent():
            return bone
    
##############################################################
#
#    Startup.
#
##############################################################
    
def StartExport(filename):
    global out
    out = XFile(filename)
    
    scene = Blender.Scene.GetCurrent()
    
    if ExportType == 0:
        #Export all.
        for obj in scene.getChildren():
            if obj.getType() == "Mesh":
                WriteMesh(obj)
    elif ExportType == 1:
        #Export selected.
        for obj in scene.getChildren():
            if obj.sel:
                WriteMesh(obj)
    elif ExportType == 2:
        #Export animated.
        mesh = None
        armature = None
        count = 0
        for obj in scene.getChildren():
            if obj.sel and obj.getType() == "Armature":
                armature = obj
                count += 1
            elif obj.sel and obj.getType() == "Mesh":
                mesh = obj
                count += 1

        if count != 2:
            PrintError("Invalid amount of objects selected")
                
        WriteAnimatedMesh(mesh, armature)
    else:
        assert 0
      
    out.close()
    print "Finished!\n" 
    
def ValidateName(name):
    """Returns a valid version of the given
       identifier. Currently only replaces dots."""
    return name.replace(".", "_")
    
def PrintError(msg):
    #Draw.PupBlock("Error", ["See console"])
    raise RuntimeError(msg)
    
##############################################################
#
#    GUI stuff.
#
##############################################################
    
def DrawBox(x, y, w, h):
    BGL.glBegin(BGL.GL_LINE_LOOP)
    BGL.glVertex2i(x,y)
    BGL.glVertex2i(x+w,y)
    BGL.glVertex2i(x+w,y-h)
    BGL.glVertex2i(x,y-h)
    BGL.glEnd()
    
def OnButton(event):
    global ExportType, Normals, Colors, TexCoords, Compressed
    global Materials, SwapYZ, UseWTrans, Ticks, Tickctrl
    if event in (0, 1, 2):
        ExportType = event
        Draw.Redraw(1)
    elif event in (3, 4, 5, 6, 7, 8, 9, 10, 11):
        if event == 3:
            Normals = not Normals
        elif event == 4:
            Colors = not Colors
        elif event == 5:
            pass
        elif event == 6:
            TexCoords = not TexCoords
        elif event == 7:
            Materials = not Materials      
        elif event == 8:
            SwapYZ = not SwapYZ 
        elif event == 9:
            UseWTrans = not UseWTrans 
        elif event == 10:
            Ticks = Tickctrl.val
        elif event == 11:
            Compressed = not Compressed

        Draw.Redraw(1) 
    elif event == 101:
        Blender.Draw.Exit()  
    elif event == 100:
        #Blender.Draw.Exit() 
        name = Blender.Get('filename').strip(".blend") + ".x"
        Blender.Window.FileSelector(StartExport, "Export", name)
     
def OnEvent(event, value):
    if event == Blender.Draw.ESCKEY:
        Blender.Draw.Exit()   

TOP = 400
LEFT = 30  
EDATA = TOP - 110 
MISC = EDATA - 100
DOIT = MISC - 130
        
Ticks = 25  
Tickctrl = None
     
def OnDraw():
    global Ticks, Tickctrl
    BGL.glClear(Blender.BGL.GL_COLOR_BUFFER_BIT)
    
    BGL.glColor3f(0.2, 0.3, 0.3)
    DrawBox(LEFT-10, TOP - 10, 300, 380)
    
    BGL.glColor3f(0.1, 0.1, 0.1)
    
    BGL.glRasterPos2i(LEFT, TOP)
    Draw.Text("DirectX Exporter", "large")
    
    BGL.glRasterPos2i(LEFT, TOP - 40)
    Draw.Text("Export", "large")
    Draw.Toggle("All", 0, LEFT, TOP - 80, 80, 20, ExportType == 0, "Export all static objects in the current scene.")
    Draw.Toggle("Selected", 1, LEFT + 100, TOP - 80, 80, 20, ExportType == 1, "Export all selected objects.")
    Draw.Toggle("Animated", 2, LEFT + 200, TOP - 80, 80, 20, ExportType == 2, "Export one mesh with one armature. Make sure that only the mesh and it's armature are selected.")
     
    BGL.glRasterPos2i(LEFT, EDATA)
    Draw.Text("Exported data", "large")
    Draw.Toggle("Normals", 3, LEFT, EDATA - 40, 80, 20, Normals, "Export vertex normals.")
    Draw.Toggle("Colors", 4, LEFT + 200, EDATA - 40, 80, 20, Colors, "Export vertex diffuse colors.")
    Draw.Toggle("Texcoords", 6, LEFT, EDATA - 70, 80, 20, TexCoords, "Export vertex texture coordinates.")   
    Draw.Toggle("Materials", 7, LEFT + 100, EDATA - 40, 80, 20, Materials, "Export materials.") 
     
    BGL.glRasterPos2i(LEFT, MISC)
    Draw.Text("Misc options", "large")
    Draw.Toggle("Swap y and z", 8, LEFT, MISC - 40, 80, 20, SwapYZ, "Right-handed to left-handed system.")
    Draw.Toggle("Apply world", 9, LEFT + 100, MISC - 40, 80, 20, UseWTrans, "Apply world transformation to exported vertices.")
    Tickctrl = Draw.Number("Speed", 10, LEFT + 200, MISC - 40, 80, 20, Ticks, 1, 100, "Animation ticks per second.")
    #Draw.Toggle("Compressed", 11, LEFT, MISC - 70, 80, 20, Compressed, "Compress the file.")
    
    Draw.Button("Export...", 100, LEFT + 40, DOIT, 80, 40, "Export data.")
    Draw.Button("Exit", 101, LEFT + 140, DOIT, 80, 40, "Exit the script.")
     
    BGL.glColor3f(0.3, 0.3, 0.3)
    BGL.glRasterPos2i(LEFT, 35)
    Draw.Text("(C) 2006 Heikki Salo", "small")
    BGL.glRasterPos2i(LEFT, 20)
    Draw.Text("http://directpython.sourceforge.net/exportx.html", "small")

Blender.Draw.Register(OnDraw, OnEvent, OnButton)
