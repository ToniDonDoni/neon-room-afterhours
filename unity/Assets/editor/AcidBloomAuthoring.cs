using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonRoom.Editor
{
    // Reproducible, original character geometry. Only outfit index 2 uses this prefab.
    // Coordinates are metres, Y up, +Z facing the wardrobe camera.
    public static class AcidBloomAuthoring
    {
        private const string Folder = "Assets/resources/avatars";
        private const int Skin = 0, Leather = 1, Hair = 2, HairLight = 3,
            Silver = 4, Lens = 5, Lip = 6, Shadow = 7, Ivory = 8;
        private static readonly List<Vector3> Vertices = new List<Vector3>();
        private static readonly List<Vector2> Uvs = new List<Vector2>();
        private static readonly List<int>[] Faces = new List<int>[9];

        [MenuItem("NEON ROOM/Art/Rebuild Acid Bloom")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding artwork.");
            Vertices.Clear(); Uvs.Clear();
            for (var i = 0; i < Faces.Length; i++) Faces[i] = new List<int>();
            BuildBody(); BuildFace(); BuildHair(); BuildOutfit(); BuildJewelry();
            var mesh = new Mesh { name = "acid-bloom-mesh", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(Vertices); mesh.SetUVs(0, Uvs); mesh.subMeshCount = Faces.Length;
            for (var i = 0; i < Faces.Length; i++) mesh.SetTriangles(Faces[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            var savedMesh = SaveAsset(mesh, Folder + "/acid-bloom-mesh.asset");
            var materials = new[]
            {
                Material("skin", new Color(.79f,.55f,.44f), 0f, .39f),
                Material("patent-leather", new Color(.025f,.018f,.033f), .25f, .88f),
                Material("rose-blonde", new Color(.78f,.57f,.61f), .12f, .53f),
                Material("pearl-strands", new Color(.95f,.78f,.76f), .16f, .62f),
                Material("silver", new Color(.72f,.77f,.83f), .82f, .85f),
                Material("smoked-lenses", new Color(.016f,.013f,.032f), .55f, .96f),
                Material("rose-lipstick", new Color(.39f,.055f,.085f), .08f, .61f),
                Material("lash-and-seams", new Color(.025f,.010f,.018f), 0f, .28f),
                Material("ivory", new Color(.8f,.73f,.65f), 0f, .48f)
            };
            var root = new GameObject("acid-bloom");
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = savedMesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/acid-bloom.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Acid Bloom artwork rebuilt: " + Vertices.Count + " vertices, nine shared materials.");
        }

        private static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
            EditorUtility.CopySerialized(asset, existing);
            UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Material Material(string name, Color color, float metal, float smooth)
        {
            var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/resources/neon-room-lit.mat");
            var mat = new Material(template) { name = "acid-bloom-" + name, color = color, enableInstancing = true };
            mat.SetFloat("_Metallic", metal); mat.SetFloat("_Smoothness", smooth);
            mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black);
            return SaveAsset(mat, Folder + "/acid-bloom-" + name + ".mat");
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x,y,z);
        private static float Ease(float t) => t*t*(3-2*t);
        private static float Bell(float x, float center, float width) => Mathf.Exp(-Mathf.Pow((x-center)/width,2));

        // Smooth tensor patches, tubes and lofts form actual surfaces, not scaled primitives.
        private static void Surface(int mat, int rows, int columns, Func<float,float,Vector3> point, bool reverse = false)
        {
            var start = Vertices.Count;
            for (var r = 0; r <= rows; r++)
            for (var c = 0; c <= columns; c++)
            {
                var u = (float)c/columns; var v = (float)r/rows;
                Vertices.Add(point(u,v)); Uvs.Add(new Vector2(u,v));
            }
            for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
            {
                var a = start+r*(columns+1)+c; var b = a+1; var d = a+columns+1; var e = d+1;
                if (!reverse) Faces[mat].AddRange(new[] {a,b,d,b,e,d});
                else Faces[mat].AddRange(new[] {a,d,b,b,d,e});
            }
        }

        private static void Ellipsoid(int mat, Vector3 center, Vector3 radius, int rows = 18, int columns = 32)
        {
            Surface(mat, rows, columns, (u,v) => {
                var a = u*Mathf.PI*2; var b = v*Mathf.PI;
                return center+Vector3.Scale(radius,V(Mathf.Sin(a)*Mathf.Sin(b),-Mathf.Cos(b),Mathf.Cos(a)*Mathf.Sin(b)));
            });
        }

        private static void Loft(int mat, Vector4[] rings, float x = 0, float z = 0, int rows = 48)
        {
            // Ring = height, horizontal radius, depth radius, depth offset.
            Surface(mat, rows, 48, (u,v) => {
                var r = Profile(rings,v);
                var a = u*Mathf.PI*2;
                return V(x+Mathf.Sin(a)*r.y,r.x,z+r.w+Mathf.Cos(a)*r.z);
            });
        }

        private static Vector4 Profile(Vector4[] points, float t)
        {
            var f=Mathf.Clamp01(t)*(points.Length-1);var i=Mathf.Min((int)f,points.Length-2);var s=f-i;
            var a=points[Mathf.Max(0,i-1)];var b=points[i];var c=points[i+1];var d=points[Mathf.Min(points.Length-1,i+2)];
            return .5f*((2*b)+(-a+c)*s+(2*a-5*b+4*c-d)*s*s+(-a+3*b-3*c+d)*s*s*s);
        }

        private static Vector3 Curve(Vector3[] points, float t)
        {
            var f = Mathf.Clamp01(t)*(points.Length-1); var i = Mathf.Min((int)f,points.Length-2); var s = f-i;
            var p0=points[Mathf.Max(0,i-1)];var p1=points[i];var p2=points[i+1];var p3=points[Mathf.Min(points.Length-1,i+2)];
            return .5f*((2*p1)+(-p0+p2)*s+(2*p0-5*p1+4*p2-p3)*s*s+(-p0+3*p1-3*p2+p3)*s*s*s);
        }

        private static void Tube(int mat, Vector3[] path, float[] radii, int rows = 24, int sides = 12, float flatten = 1)
        {
            Surface(mat, rows, sides, (u,v) => {
                var p = Curve(path,v);
                var direction = (Curve(path,Mathf.Min(1,v+.002f))-Curve(path,Mathf.Max(0,v-.002f))).normalized;
                var reference = Mathf.Abs(Vector3.Dot(direction,Vector3.forward))>.94f ? Vector3.up : Vector3.forward;
                var right=Vector3.Cross(direction,reference).normalized; var back=Vector3.Cross(right,direction).normalized;
                var f=v*(radii.Length-1);var k=Mathf.Min((int)f,radii.Length-2);var radius=Mathf.Lerp(radii[k],radii[k+1],Ease(f-k));
                var a=u*Mathf.PI*2;
                return p+radius*(right*Mathf.Sin(a)+back*Mathf.Cos(a)*flatten);
            });
        }

        private static void Line(int mat, float width, params Vector3[] points) => Tube(mat,points,new[]{width,width},Mathf.Max(12,points.Length*6),8);
        private static void Ring(int mat, Vector3 center, Vector3 rx, Vector3 ry, float width)
        {
            var points=new Vector3[33];
            for(var i=0;i<points.Length;i++) {var a=i*Mathf.PI*2/32;points[i]=center+rx*Mathf.Cos(a)+ry*Mathf.Sin(a);}
            Tube(mat,points,new[]{width,width},32,6);
        }

        private static void BuildBody()
        {
            Loft(Skin,new[]{new Vector4(.92f,.12f,.09f,0),new Vector4(1.02f,.158f,.105f,0),new Vector4(1.10f,.125f,.085f,0),
                new Vector4(1.19f,.103f,.075f,0),new Vector4(1.30f,.135f,.09f,0),new Vector4(1.41f,.17f,.105f,0),
                new Vector4(1.48f,.181f,.08f,-.006f),new Vector4(1.515f,.10f,.064f,0),new Vector4(1.54f,.048f,.042f,0)});
            Loft(Skin,new[]{new Vector4(1.50f,.05f,.044f,0),new Vector4(1.56f,.042f,.041f,0),new Vector4(1.64f,.048f,.048f,0)});
            // Continuous tapered legs, with calf and knee forms.
            for(var s=-1;s<=1;s+=2)
            {
                Tube(Skin,new[]{V(s*.09f,.99f,0),V(s*.10f,.83f,.005f),V(s*.11f,.70f,.013f),V(s*.112f,.55f,.018f),V(s*.112f,.32f,-.014f),V(s*.112f,.13f,0)},
                    new[]{.083f,.078f,.053f,.037f,.040f,.025f},52,24,.91f);
            }
            Tube(Skin,new[]{V(-.18f,1.475f,0),V(-.255f,1.38f,0),V(-.30f,1.26f,.025f),V(-.29f,1.37f,.10f),V(-.23f,1.53f,.13f)},new[]{.055f,.052f,.039f,.035f,.025f},50,24);
            Tube(Skin,new[]{V(.18f,1.475f,0),V(.25f,1.37f,0),V(.30f,1.22f,.015f),V(.265f,1.12f,.09f),V(.21f,1.055f,.105f)},new[]{.055f,.052f,.036f,.031f,.024f},44,24);
            Hand(V(-.23f,1.53f,.13f),Quaternion.Euler(0,0,-20),true);
            Hand(V(.21f,1.055f,.105f),Quaternion.Euler(0,0,160),false);
            // Anatomical collarbone and navel details are intentionally understated.
            Ellipsoid(Shadow,V(0,1.165f,.074f),V(.006f,.009f,.002f),10,16);
        }

        private static void Hand(Vector3 wrist, Quaternion rotation, bool raised)
        {
            Func<Vector3,Vector3> p = local => wrist+rotation*local;
            Tube(Skin,new[]{p(V(0,0,0)),p(V(0,.028f,0)),p(V(0,.060f,0))},new[]{.024f,.031f,.025f},18,20,.43f);
            for(var f=0;f<4;f++)
            {
                var x=(f-1.5f)*.014f;var length=new[]{.057f,.065f,.06f,.046f}[f];
                var curl=raised && f>0 ? .028f : .013f;
                var start=p(V(x,.055f,0));var middle=p(V(x*1.15f,.055f+length*.55f,.003f));var end=p(V(x*1.08f,.055f+length,-curl));
                Tube(Skin,new[]{start,middle,end},new[]{.008f,.0065f,.0028f},16,10);
                Ellipsoid(Leather,end+rotation*V(0,-.004f,.0045f),V(.005f,.007f,.0025f),8,10);
                if(f==1) Ring(Silver,p(V(x,.075f,0)),rotation*V(.008f,0,0),rotation*V(0,0,.007f),.0017f);
            }
            Tube(Skin,new[]{p(V(.022f,.018f,0)),p(V(.041f,.035f,.007f)),p(V(.039f,.060f,-.008f))},new[]{.011f,.009f,.003f},18,12);
            Ring(Silver,p(V(0,.003f,0)),rotation*V(.026f,0,0),rotation*V(0,0,.016f),.004f);
            Ring(Leather,p(V(0,-.011f,0)),rotation*V(.026f,0,0),rotation*V(0,0,.017f),.004f);
        }

        private static void BuildFace()
        {
            // Sculpted jaw, cheekbones, temples and forehead. Front depth is locally shaped.
            var profile=new[]{new Vector4(1.60f,.018f,.030f,.018f),new Vector4(1.62f,.049f,.050f,.018f),
                new Vector4(1.65f,.076f,.065f,.012f),new Vector4(1.69f,.097f,.078f,.003f),new Vector4(1.74f,.111f,.088f,0),
                new Vector4(1.79f,.108f,.091f,-.006f),new Vector4(1.84f,.099f,.086f,-.009f),new Vector4(1.88f,.072f,.065f,-.010f),new Vector4(1.905f,.004f,.004f,-.01f)};
            Surface(Skin,64,80,(u,v)=>{
                var q=Profile(profile,v);
                var a=u*2*Mathf.PI;var x=Mathf.Sin(a)*q.y;var front=Mathf.Max(0,Mathf.Cos(a));
                var z=Mathf.Cos(a)*q.z+q.w;
                z+=Mathf.Pow(front,8)*(.015f*Bell(q.x,1.67f,.033f)+.012f*Bell(q.x,1.73f,.025f));
                z-=.009f*(Bell(x,.048f,.025f)+Bell(x,-.048f,.025f))*Bell(q.x,1.77f,.018f)*front;
                return V(x,q.x,z);
            });
            // A joined bridge and softly pointed nose, subtle nostril insets.
            Tube(Skin,new[]{V(0,1.785f,.085f),V(0,1.75f,.103f),V(0,1.714f,.132f),V(0,1.705f,.115f)},new[]{.010f,.012f,.016f,.009f},28,24,.8f);
            for(var s=-1;s<=1;s+=2)
            {
                Ellipsoid(Skin,V(s*.012f,1.710f,.113f),V(.011f,.008f,.012f));
                Ellipsoid(Shadow,V(s*.010f,1.703f,.119f),V(.004f,.002f,.003f),8,12);
                Ellipsoid(Skin,V(s*.109f,1.723f,-.005f),V(.015f,.032f,.021f));
                Ellipsoid(Lip,V(s*.117f,1.725f,.009f),V(.004f,.019f,.006f),12,16);
                Line(Shadow,.004f,V(s*.025f,1.805f,.081f),V(s*.051f,1.81f,.084f),V(s*.08f,1.80f,.067f));
            }
            // Slight smile; lips are continuous ribbons with a cupid's bow.
            Surface(Shadow,6,32,(u,v)=>{
                var x=(u-.5f)*.082f;var w=Mathf.Sin(u*Mathf.PI);
                return V(x,1.674f+(1-w)*.010f+(v-.5f)*.014f*w,.099f+w*.008f);
            });
            Surface(Ivory,4,28,(u,v)=>{
                var w=Mathf.Sin(u*Mathf.PI);return V((u-.5f)*.066f,1.677f+(1-w)*.006f-v*.005f*w,.108f);
            },true);
            for(var upper=0;upper<2;upper++)
            {
                var top=upper==1;
                Surface(Lip,8,40,(u,v)=>{
                    var w=Mathf.Sin(u*Mathf.PI);var x=(u-.5f)*.086f;
                    var edge=1.674f+(1-w)*.010f+(top?.004f:-.006f)*w;
                    var bow=top ? .004f*Bell(Mathf.Abs(x),.012f,.008f) : 0;
                    return V(x,edge+(top?1:-1)*Mathf.Sin(v*Mathf.PI*.5f)*.007f*w+bow,.102f+.010f*w+Mathf.Sin(v*Mathf.PI)*.004f);
                },!top);
            }
            // Oversized bevelled cat-eye frames, smoked lenses and curved temple arms.
            for(var s=-1;s<=1;s+=2)
            {
                Func<float,Vector3> outline=a=>{
                    var c=Mathf.Cos(a);var t=Mathf.Sin(a);
                    return V(s*(.060f+.055f*Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.55f)),1.768f+.034f*Mathf.Sign(t)*Mathf.Pow(Mathf.Abs(t),.65f)+.009f*c,.107f-.015f*Mathf.Max(0,c));
                };
                Surface(Lens,10,48,(u,v)=>Vector3.Lerp(V(s*.060f,1.768f,.113f),outline(u*2*Mathf.PI),v),s>0);
                var rim=new Vector3[49];for(var i=0;i<rim.Length;i++)rim[i]=outline(i*Mathf.PI*2/48);
                Tube(Leather,rim,new[]{.006f,.006f},80,10);
                Line(Leather,.005f,V(s*.111f,1.787f,.091f),V(s*.12f,1.784f,.025f),V(s*.113f,1.754f,-.025f));
                Ellipsoid(Silver,V(s*.108f,1.79f,.101f),V(.005f,.002f,.002f),8,12);
                Line(Silver,.0012f,V(s*.035f,1.787f,.113f),V(s*.055f,1.79f,.113f),V(s*.071f,1.788f,.110f));
            }
            Line(Leather,.004f,V(-.012f,1.776f,.114f),V(0,1.781f,.122f),V(.012f,1.776f,.114f));
        }

        private static void BuildHair()
        {
            // Centre part and a fitted scalp cap; the front hairline stays above the frames.
            Surface(Hair,30,64,(u,v)=>{
                var a=u*Mathf.PI*2;var front=Mathf.Max(0,Mathf.Cos(a));
                var end=Mathf.Lerp(2.3f,1.12f,front*front);var b=.005f+v*end;
                return V(.119f*Mathf.Sin(b)*Mathf.Sin(a),1.766f+.151f*Mathf.Cos(b),-.012f+.108f*Mathf.Sin(b)*Mathf.Cos(a));
            },true);
            // Swept, overlapping locks with fine lighter fibres, tapered to individual points.
            for(var s=-1;s<=1;s+=2)
            for(var i=0;i<25;i++)
            {
                var a=.27f+i/24f*2.80f;
                var endY=1.27f+.08f*Mathf.Sin(i*.78f)+.10f*(i>16?1:0);
                var x=s*Mathf.Sin(a);var z=Mathf.Cos(a);
                var sideX=s*Mathf.Max(.91f,Mathf.Sin(a));
                if(z<0)sideX=x;
                var path=new[]{V(s*.006f,1.914f-i*.0012f,-.014f-i*.0018f),
                    V(x*.092f,1.882f,z*.071f-.014f),V(sideX*.126f,1.765f,z*.090f-.017f),
                    V(sideX*.135f,1.58f,z*.090f-.011f),V(sideX*.144f,endY+.10f,z*.102f),V(sideX*.158f,endY,z*.110f+.014f)};
                Tube(i%4==0?HairLight:Hair,path,new[]{.002f,.020f,.018f,.014f,.010f,.0005f},40,10,.35f);
                if(i%2==0)
                {
                    var strand=new Vector3[path.Length];
                    for(var k=0;k<path.Length;k++)strand[k]=path[k]+V(s*.002f,.0008f,.006f);
                    Tube(HairLight,strand,new[]{.0008f,.0013f,.0012f,.0010f,.0008f,.0001f},32,6);
                }
            }
        }

        private static void BuildOutfit()
        {
            // Cropped glossy bodice with a sweetheart neckline.
            Surface(Leather,26,64,(u,v)=>{
                var a=u*2*Mathf.PI;var front=Mathf.Max(0,Mathf.Cos(a));var x=Mathf.Sin(a);
                var top=1.413f+.028f*Mathf.Sin(Mathf.Abs(x)*Mathf.PI)*front-.035f*Bell(x,0,.22f)*front;
                var y=Mathf.Lerp(1.255f,top,v);var w=Mathf.Lerp(.124f,.163f,v);var depth=Mathf.Lerp(.087f,.104f,v);
                return V(x*w,y,Mathf.Cos(a)*depth+.012f*front*Mathf.Sin(v*Mathf.PI));
            });
            for(var s=-1;s<=1;s+=2)
            {
                Line(Leather,.008f,V(s*.117f,1.40f,.074f),V(s*.128f,1.495f,.028f),V(s*.121f,1.415f,-.078f));
                Line(Silver,.0017f,V(s*.09f,1.27f,.075f),V(s*.101f,1.33f,.09f),V(s*.10f,1.40f,.088f));
            }
            // Open short biker jacket: back and side panels leave the fitted top visible.
            Surface(Leather,32,56,(u,v)=>{
                var a=Mathf.Lerp(.80f,Mathf.PI*2-.80f,u);
                var w=Mathf.Lerp(.132f,.193f,v);var d=Mathf.Lerp(.10f,.093f,v);
                return V(Mathf.Sin(a)*w,Mathf.Lerp(1.23f,1.50f,v),Mathf.Cos(a)*d-.008f);
            });
            for(var s=-1;s<=1;s+=2)
            {
                Surface(Leather,16,12,(u,v)=>{
                    var outer=Vector3.Lerp(V(s*.146f,1.245f,.075f),V(s*.168f,1.49f,.048f),v);
                    var inner=Vector3.Lerp(V(s*.093f,1.245f,.104f),V(s*.075f,1.49f,.074f),v);
                    return Vector3.Lerp(inner,outer,u)+V(0,0,.012f*Mathf.Sin(u*Mathf.PI));
                },s<0);
                Line(Silver,.002f,V(s*.094f,1.246f,.107f),V(s*.098f,1.36f,.115f),V(s*.075f,1.49f,.077f));
                Ellipsoid(Silver,V(s*.113f,1.451f,.096f),V(.005f,.005f,.002f),8,12);
                var path=s<0?new[]{V(-.175f,1.48f,0),V(-.251f,1.39f,0),V(-.285f,1.30f,.018f)}:
                    new[]{V(.175f,1.48f,0),V(.25f,1.38f,0),V(.28f,1.29f,.007f)};
                Ellipsoid(Leather,path[0],V(.064f,.055f,.061f),20,28);
                Tube(Leather,path,new[]{.064f,.059f,.046f},26,24);
                Line(Silver,.0018f,path[0]+V(0,0,.062f),path[1]+V(0,0,.057f),path[2]+V(0,0,.048f));
            }
            // High-waisted fitted shorts, shaped around each leg opening.
            Loft(Leather,new[]{new Vector4(.95f,.162f,.111f,0),new Vector4(1.015f,.163f,.11f,0),new Vector4(1.065f,.135f,.092f,0),new Vector4(1.09f,.124f,.085f,0)});
            for(var s=-1;s<=1;s+=2)
            {
                Loft(Leather,new[]{new Vector4(.895f,.078f,.086f,0),new Vector4(.95f,.083f,.099f,0),new Vector4(1.00f,.080f,.097f,0)},s*.081f);
                Ring(Leather,V(s*.101f,.807f,.006f),V(.076f,0,0),V(0,0,.069f),.010f);
                Line(Leather,.008f,V(s*.16f,.965f,.035f),V(s*.174f,.87f,.023f),V(s*.175f,.809f,.021f));
                Ring(Silver,V(s*.174f,.82f,.047f),V(.011f,0,0),V(0,.015f,0),.0025f);
                // Sculpted tall boots with a fitted shaft, platform and block heel.
                Loft(Leather,new[]{new Vector4(.10f,.043f,.047f,0),new Vector4(.18f,.037f,.043f,0),new Vector4(.32f,.057f,.051f,-.014f),
                    new Vector4(.43f,.054f,.051f,0),new Vector4(.55f,.048f,.05f,.018f),new Vector4(.66f,.060f,.058f,.015f),new Vector4(.735f,.069f,.066f,.008f)},s*.112f);
                Ellipsoid(Leather,V(s*.112f,.078f,.073f),V(.048f,.050f,.120f),20,32);
                Ellipsoid(Shadow,V(s*.112f,.038f,.087f),V(.050f,.026f,.110f),16,32);
                Loft(Leather,new[]{new Vector4(.005f,.027f,.028f,0),new Vector4(.03f,.027f,.028f,0),new Vector4(.10f,.037f,.036f,0)},s*.112f,-.027f,18);
                Ring(Silver,V(s*.112f,.733f,.008f),V(.069f,0,0),V(0,0,.067f),.002f);
                Line(Silver,.0013f,V(s*.15f,.18f,.023f),V(s*.163f,.36f,.018f),V(s*.16f,.54f,.048f),V(s*.177f,.73f,.035f));
            }
            Ring(Leather,V(0,1.076f,0),V(.131f,0,0),V(0,0,.092f),.013f);
            Ring(Silver,V(0,1.077f,.097f),V(.024f,0,0),V(0,.018f,0),.004f);
            for(var i=0;i<7;i++) Ellipsoid(Silver,V(.041f+i*.010f,1.077f,.097f-i*.0035f),V(.002f,.002f,.002f),6,8);
        }

        private static void BuildJewelry()
        {
            Ring(Leather,V(0,1.563f,0),V(.046f,0,0),V(0,0,.044f),.006f);
            Ring(Silver,V(0,1.560f,.047f),V(.010f,0,0),V(0,.011f,0),.002f);
            Line(Silver,.002f,V(-.042f,1.555f,.017f),V(-.043f,1.507f,.068f),V(0,1.476f,.085f),V(.043f,1.507f,.068f),V(.042f,1.555f,.017f));
            Ellipsoid(Silver,V(0,1.47f,.086f),V(.006f,.011f,.003f),12,12);
            for(var s=-1;s<=1;s+=2)
                Ring(Silver,V(s*.119f,1.681f,.009f),V(.017f,0,0),V(0,.026f,0),.0028f);
            // Individually modelled links on the asymmetric waist chain.
            for(var i=0;i<21;i++)
            {
                var t=i/20f;var x=.025f+.13f*t;var y=1.06f-.10f*Mathf.Sin(t*Mathf.PI);var z=.11f-.037f*t;
                Ring(Silver,V(x,y,z),V(.005f,0,i%2==0?.002f:0),V(0,.008f,0),.0015f);
            }
        }
    }
}
