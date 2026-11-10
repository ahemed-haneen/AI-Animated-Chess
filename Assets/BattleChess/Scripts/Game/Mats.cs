using System.Collections.Generic;
using UnityEngine;

namespace BattleChess
{
    /// <summary>Material + primitive mesh cache. Templates are assigned by the scene's Bootstrap so URP shaders are included in builds.</summary>
    public static class Mats
    {
        public static Material LitTemplate;     // URP/Lit (or Standard)
        public static Material SpriteTemplate;  // Sprites/Default — unlit, vertex-colour, transparent
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();

        static Material Template()
        {
            if (LitTemplate == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                LitTemplate = new Material(sh);
            }
            return LitTemplate;
        }

        public static Material Lit(Color c, float metallic = 0f, float smooth = 0.35f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + metallic.ToString("0.00") + smooth.ToString("0.00");
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Template());
            m.color = c;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            cache[key] = m;
            return m;
        }

        public static Material Gold { get { return Lit(new Color(0.84f, 0.66f, 0.23f), 0.9f, 0.7f); } }
        public static Material Steel { get { return Lit(new Color(0.75f, 0.78f, 0.8f), 0.9f, 0.75f); } }
        public static Material Iron { get { return Lit(new Color(0.2f, 0.2f, 0.22f), 0.7f, 0.45f); } }
        public static Material Wood { get { return Lit(new Color(0.42f, 0.27f, 0.15f), 0f, 0.2f); } }

        public static Material Sprite(Color c, Texture tex = null)
        {
            Material m;
            if (SpriteTemplate != null) m = new Material(SpriteTemplate);
            else m = new Material(Shader.Find("Sprites/Default"));
            m.color = c;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        public static Mesh Mesh(PrimitiveType t)
        {
            Mesh m;
            if (meshes.TryGetValue(t, out m) && m != null) return m;
            var go = GameObject.CreatePrimitive(t);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            meshes[t] = m;
            return m;
        }

        /// <summary>Creates a primitive with no collider.</summary>
        public static Transform Prim(PrimitiveType t, Transform parent, Material mat, Vector3 pos, Vector3 scale, Vector3 euler = default(Vector3))
        {
            var go = new GameObject(t.ToString());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = Mesh(t);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return go.transform;
        }

        public static Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        public static Texture2D RadialTexture(int size, float inner, float outer, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            float h = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - h + .5f) * (x - h + .5f) + (y - h + .5f) * (y - h + .5f)) / h;
                    float a;
                    if (ring) a = Mathf.Clamp01((d - inner) * 12f) * Mathf.Clamp01((outer - d) * 12f);
                    else a = Mathf.Clamp01((outer - d) / Mathf.Max(0.001f, outer - inner));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
