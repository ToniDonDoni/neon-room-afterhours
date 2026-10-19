using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonRoom.Editor
{
    // Reproducible, original character geometry. Only outfit index 3 uses this prefab.
    // Coordinates are metres, Y up, +Z facing the wardrobe camera.
    public static class VelvetStaticAuthoring
    {
        private const string Folder = "Assets/resources/avatars";
        private const int Skin = 0, Leather = 1, Hair = 2, HairLight = 3,
            Silver = 4, Lens = 5, Lip = 6, Shadow = 7, Ivory = 8;
        private static readonly List<Vector3> Vertices = new List<Vector3>();
        private static readonly List<Vector2> Uvs = new List<Vector2>();
        private static readonly List<int>[] Faces = new List<int>[9];

        [MenuItem("NEON ROOM/Art/Rebuild Velvet Static")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding artwork.");
            Vertices.Clear(); Uvs.Clear();
            for (var i = 0; i < Faces.Length; i++) Faces[i] = new List<int>();
            BuildBody(); BuildFace(); BuildHair(); BuildOutfit(); BuildJewelry();
            var mesh = new Mesh { name = "velvet-static-mesh", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(Vertices); mesh.SetUVs(0, Uvs); mesh.subMeshCount = Faces.Length;
            for (var i = 0; i < Faces.Length; i++) mesh.SetTriangles(Faces[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            var savedMesh = SaveAsset(mesh, Folder + "/velvet-static-mesh.asset");
            var materials = new[]
            {
                Material("skin", new Color(.83f,.61f,.53f), 0f, .39f),
                Material("patent-leather", new Color(.020f,.014f,.026f), .38f, .82f),
                Material("rose-blonde", new Color(.77f,.55f,.58f), 0f, .13f),
                Material("pearl-strands", new Color(.85f,.65f,.67f), 0f, .16f),
                Material("silver", new Color(.72f,.77f,.83f), .82f, .85f),
                Material("smoked-lenses", new Color(.006f,.006f,.014f), .30f, .94f),
                Material("rose-lipstick", new Color(.39f,.055f,.085f), .08f, .61f),
                Material("lash-and-seams", new Color(.025f,.010f,.018f), 0f, .28f),
                Material("ivory", new Color(.8f,.73f,.65f), 0f, .48f)
            };
            var root = new GameObject("velvet-static");
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = savedMesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/velvet-static.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Velvet Static artwork rebuilt: " + Vertices.Count + " vertices, nine shared materials.");
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
            var mat = new Material(template) { name = "velvet-static-" + name, color = color, enableInstancing = true };
            mat.SetFloat("_Metallic", metal); mat.SetFloat("_Smoothness", smooth);
            if (name == "smoked-lenses" || name == "rose-blonde" || name == "pearl-strands") mat.SetInt("_Cull", 0);
            
mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black);
            return SaveAsset(mat, Folder + "/velvet-static-" + name + ".mat");
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
            Tube(Skin,new[]{V(-.18f,1.475f,0),V(-.245f,1.39f,0),V(-.282f,1.23f,.012f),V(-.275f,1.115f,.016f),V(-.283f,1.045f,.036f)},new[]{.052f,.049f,.038f,.028f,.021f},50,24);
            Tube(Skin,new[]{V(.18f,1.475f,0),V(.245f,1.39f,0),V(.282f,1.23f,.012f),V(.275f,1.115f,.016f),V(.283f,1.045f,.036f)},new[]{.052f,.049f,.038f,.028f,.021f},50,24);
            Hand(V(-.283f,1.045f,.036f),Quaternion.Euler(0f,0f,180f),false);
            Hand(V(.283f,1.045f,.036f),Quaternion.Euler(0f,0f,180f),false);
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
            var profile=new[]{new Vector4(1.60f,.020f,.029f,.018f),new Vector4(1.62f,.043f,.045f,.018f),
                new Vector4(1.65f,.068f,.060f,.012f),new Vector4(1.69f,.091f,.076f,.003f),new Vector4(1.74f,.103f,.085f,0),
                new Vector4(1.79f,.102f,.087f,-.006f),new Vector4(1.84f,.099f,.086f,-.009f),new Vector4(1.88f,.076f,.068f,-.010f),new Vector4(1.905f,.004f,.004f,-.01f)};
            Surface(Skin,64,80,(u,v)=>{
                var q=Profile(profile,v);
                var a=u*2*Mathf.PI;var x=Mathf.Sin(a)*q.y;var front=Mathf.Max(0,Mathf.Cos(a));
                var z=Mathf.Cos(a)*q.z+q.w;
                z+=Mathf.Pow(front,8)*(.009f*Bell(q.x,1.67f,.033f)+.008f*Bell(q.x,1.73f,.025f));
                z-=.009f*(Bell(x,.048f,.025f)+Bell(x,-.048f,.025f))*Bell(q.x,1.77f,.018f)*front;
                return V(x,q.x,z);
            });
            // A joined bridge and softly pointed nose, subtle nostril insets.
            Tube(Skin,new[]{V(0,1.785f,.085f),V(0,1.75f,.103f),V(0,1.714f,.118f),V(0,1.705f,.115f)},new[]{.009f,.010f,.012f,.008f},28,24,.8f);
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
            // A continuous cap and overlapping flowing panels replace the exposed strand lattice.
            Surface(Hair,48,72,(u,v)=>{
                var a=u*Mathf.PI*2;
                var b=.003f+v*1.53f;
                return V(.117f*Mathf.Sin(b)*Mathf.Sin(a),1.766f+.151f*Mathf.Cos(b),
                    -.014f+.113f*Mathf.Sin(b)*Mathf.Cos(a));
            });
            // Rear curtain: broad overlapping hair cards with small depth variation.
            for(int i=0;i<40;i++)
            {
                var t=(i+.5f)/40f;var x=(t-.5f)*.34f;
                var wave=.0035f*Mathf.Sin(i*.37f);
                var path=new[]{V(x*.18f,1.912f,-.022f),V(x*.76f,1.85f,-.084f),
                    V(x*.98f,1.68f,-.125f),V(x+wave,1.48f,-.144f),
                    V(x*1.02f+wave,1.32f,-.145f),V(x+wave,1.20f+.038f*Mathf.Sin(i*1.73f),-.139f)};
                Surface(i%8==0?HairLight:Hair,42,6,(u,v)=>{
                    var p=Curve(path,v);
                    var w=.0095f*Mathf.Pow(Mathf.Max(0f,Mathf.Sin(Mathf.PI*v)),.17f);
                    return p+V((u-.5f)*2*w,0,.0013f*Mathf.Sin(u*Mathf.PI));
                },i%2==0);
            }
            // Face-framing curtains sit outside the cheek and taper below the bust.
            for(int side=-1;side<=1;side+=2)
            for(int i=0;i<22;i++)
            {
                var t=(i+.5f)/22f;var x=side*(.065f+t*.125f);
                var path=new[]{V(side*(.013f+t*.015f),1.912f,-.008f),
                    V(side*(.052f+t*.058f),1.867f,.032f),
                    V(x*.97f,1.74f,.082f),
                    V(x,1.57f,.111f),
                    V(x*1.05f,1.39f,.120f),
                    V(x*1.04f,1.25f+.065f*t,.111f)};
                Surface(i%7==0?HairLight:Hair,40,6,(u,v)=>{
                    var p=Curve(path,v);
                    var w=.0092f*Mathf.Pow(Mathf.Max(0f,Mathf.Sin(Mathf.PI*v)),.20f);
                    return p+V((u-.5f)*2*w,0,.0014f*Mathf.Sin(u*Mathf.PI));
                },side<0);
            }
        }

        private static void BuildOutfit()
        {

            // Black patent bralette: two contoured cups, center ring and slim shoulder straps.
            for(var s=-1;s<=1;s+=2)
            {
                Surface(Leather,22,44,(u,v)=>{
                    var angle=(u-.5f)*Mathf.PI;
                    var radial=Mathf.Sin(v*Mathf.PI*.5f);
                    var x=s*(.085f+.065f*Mathf.Sin(angle)*radial);
                    var y=1.305f+.097f*v+.016f*Mathf.Cos(angle)*radial;
                    var z=.111f+.041f*Mathf.Sin(v*Mathf.PI)*Mathf.Cos(angle);
                    return V(x,y,z);
                },s>0);
                Line(Leather,.009f,V(s*.018f,1.326f,.122f),V(s*.079f,1.303f,.133f),V(s*.14f,1.348f,.093f));
                Line(Leather,.008f,V(s*.130f,1.39f,.081f),V(s*.137f,1.475f,.044f),V(s*.12f,1.50f,-.043f));
                Line(Silver,.0018f,V(s*.145f,1.385f,.079f),V(s*.126f,1.335f,.11f),V(s*.051f,1.302f,.129f));
            }
            Ring(Silver,V(0,1.314f,.137f),V(.013f,0,0),V(0,.014f,0),.004f);
            // Cut-out straps under the bust and at the exposed midriff.
            for(var s=-1;s<=1;s+=2)
            {
                Line(Leather,.008f,V(s*.137f,1.318f,.060f),V(s*.122f,1.264f,.095f),V(s*.067f,1.254f,.12f));
                Line(Leather,.007f,V(s*.122f,1.263f,.094f),V(s*.133f,1.19f,.077f),V(s*.148f,1.115f,.043f));
            }
            // Cropped lacquer bolero jacket, open at the front with fitted 3/4 sleeves.
            Surface(Leather,30,56,(u,v)=>{
                var a=Mathf.Lerp(.84f,Mathf.PI*2-.84f,u);
                var y=Mathf.Lerp(1.31f,1.52f,v);
                return V(Mathf.Sin(a)*Mathf.Lerp(.156f,.191f,v),y,Mathf.Cos(a)*Mathf.Lerp(.111f,.090f,v)-.018f);
            });
            for(var s=-1;s<=1;s+=2)
            {
                Surface(Leather,15,15,(u,v)=>{
                    var outer=Vector3.Lerp(V(s*.151f,1.315f,.063f),V(s*.181f,1.497f,.053f),v);
                    var inner=Vector3.Lerp(V(s*.137f,1.315f,.10f),V(s*.082f,1.497f,.077f),v);
                    return Vector3.Lerp(inner,outer,u)+V(0,0,.004f*Mathf.Sin(u*Mathf.PI));
                },s<0);
                var arm=new[]{V(s*.17f,1.47f,.0f),V(s*.243f,1.389f,.006f),
                    V(s*.275f,1.28f,.02f),V(s*.272f,1.17f,.021f)};
                Ellipsoid(Leather,arm[0],V(.063f,.049f,.064f),20,24);
                Tube(Leather,arm,new[]{.063f,.057f,.050f,.044f},30,22);
                Ring(Leather,arm[3],V(.045f,0,0),V(0,0,.046f),.010f);
                Line(Silver,.0015f,arm[0]+V(0,.015f,.059f),arm[1]+V(0,0,.054f),arm[2]+V(0,0,.045f));
            }
            // Tight lacquer mini skirt with fitted waistband; bare thighs remain visible.
            Loft(Leather,new[]{
                new Vector4(.882f,.159f,.115f,0),
                new Vector4(.914f,.174f,.123f,-.005f),
                new Vector4(.957f,.175f,.126f,0),
                new Vector4(1.005f,.160f,.112f,0),
                new Vector4(1.055f,.135f,.103f,0),
                new Vector4(1.086f,.132f,.100f,0)
            });
            Ring(Leather,V(0,1.069f,0),V(.137f,0,0),V(0,0,.102f),.018f);
            Ring(Silver,V(0,1.069f,.111f),V(.021f,0,0),V(0,.019f,0),.0035f);
            Line(Silver,.0018f,V(.014f,1.063f,.11f),V(.020f,1.006f,.124f),V(.022f,.92f,.140f));
            for(var s=-1;s<=1;s+=2)
            {
                // Belt loops, hip seams and hanging silver chains.
                Line(Leather,.009f,V(s*.118f,1.092f,.081f),V(s*.129f,1.036f,.091f));
                Line(Silver,.0017f,V(s*.155f,1.011f,.067f),V(s*.170f,.946f,.045f),V(s*.164f,.899f,.049f));
                for(var i=0;i<19;i++)
                {
                    var t=i/18f;
                    var x=s*(.130f+.046f*Mathf.Sin(t*Mathf.PI));
                    var y=1.050f-.135f*t;
                    Ring(Silver,V(x,y,.091f-.046f*t),V(.004f,0,0),V(0,.006f,0),.0012f);
                }
            }
            // High platform ankle boots with five silver buckles on the outer edges.
            for(var s=-1;s<=1;s+=2)
            {
                Loft(Leather,new[]{
                    new Vector4(.090f,.052f,.064f,0),new Vector4(.14f,.052f,.058f,0),
                    new Vector4(.225f,.049f,.052f,0),new Vector4(.310f,.058f,.061f,0),
                    new Vector4(.35f,.061f,.065f,0)
                },s*.112f);
                Ellipsoid(Leather,V(s*.112f,.085f,.073f),V(.055f,.045f,.13f),18,28);
                Ellipsoid(Shadow,V(s*.112f,.035f,.080f),V(.057f,.028f,.128f),16,28);
                // Actual thick heel and sole silhouette.
                Loft(Leather,new[]{
                    new Vector4(.004f,.039f,.047f,-.029f),
                    new Vector4(.031f,.043f,.047f,-.029f),
                    new Vector4(.109f,.043f,.044f,-.027f)
                },s*.112f);
                for(var i=0;i<5;i++)
                {
                    var y=.14f+i*.045f;
                    Ring(Leather,V(s*.112f,y,0),V(.053f,0,0),V(0,0,.057f),.009f);
                    Ring(Silver,V(s*.161f,y,.028f),V(.010f,0,0),V(0,.011f,0),.0023f);
                }
                Line(Silver,.0017f,V(s*.164f,.120f,.06f),V(s*.170f,.235f,.054f),V(s*.169f,.335f,.057f));
            }
            // Garters, miniature side handbag and wrist bands.
            Ring(Leather,V(.12f,.73f,0f),V(.085f,0,0),V(0,0,.077f),.013f);
            Line(Leather,.008f,V(.175f,.92f,.045f),V(.20f,.80f,.015f),V(.175f,.734f,.05f));
            Ring(Silver,V(.176f,.754f,.054f),V(.014f,0,0),V(0,.017f,0),.003f);
            for(var s=-1;s<=1;s+=2)
            {
                Ring(Silver,V(s*.28f,1.083f,.04f),V(.026f,0,0),V(0,0,.025f),.006f);
                Ring(Leather,V(s*.28f,1.064f,.04f),V(.028f,0,0),V(0,0,.027f),.011f);
            }
            Ellipsoid(Leather,V(.245f,.735f,.042f),V(.050f,.066f,.022f),14,22);
            Ring(Silver,V(.245f,.750f,.067f),V(.020f,0,0),V(0,.016f,0),.002f);
            for(var i=0;i<13;i++)
            {
                var t=i/12f;
                Ring(Silver,V(.176f+.069f*t, .99f-.215f*t,.067f+.012f*t),V(.003f,0,0),V(0,.005f,0),.0012f);
            }
        }

        private static void BuildJewelry()
        {

            // Fine layered necklace and heart pendant, earrings and metallic chains.
            for(var s=-1;s<=1;s+=2)
            {
                Line(Silver,.0021f,V(s*.044f,1.570f,.030f),V(s*.037f,1.530f,.065f),V(0,1.480f,.097f));
                Ring(Silver,V(s*.112f,1.722f,.011f),V(.015f,0,0),V(0,.024f,0),.003f);
            }
            Ellipsoid(Silver,V(0,1.478f,.099f),V(.012f,.013f,.004f),14,18);
            Line(Silver,.0013f,V(-.048f,1.566f,.018f),V(0,1.415f,.12f),V(.048f,1.566f,.018f));
            Ellipsoid(Silver,V(0,1.412f,.126f),V(.004f,.008f,.003f),10,14);
            for(var s=-1;s<=1;s+=2)
            {
                for(var i=0;i<13;i++)
                {
                    var t=i/12f;
                    Ring(Silver,V(s*(.09f+.08f*t),1.053f-.09f*Mathf.Sin(t*Mathf.PI),.104f-.043f*t),
                        V(.005f,0,0),V(0,.007f,0),.0016f);
                }
            }
        }
    }
}
