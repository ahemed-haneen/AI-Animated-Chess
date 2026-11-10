using System.Collections.Generic;
using UnityEngine;

namespace BattleChess
{
    /// <summary>The 3D board: surface texture, frame, coordinates and move highlights.
    /// World layout: one square = 1 unit, white's back rank at z = -3.5, black's at z = +3.5.</summary>
    public class BoardView : MonoBehaviour
    {
        public enum Mark { LastMove, Selected, Target, Capture, Check }

        readonly List<GameObject> active = new List<GameObject>();
        readonly Dictionary<Mark, Material> markMats = new Dictionary<Mark, Material>();
        Material checkMat;

        public static Vector3 SquareToWorld(int sq) { return new Vector3((sq & 7) - 3.5f, 0f, 3.5f - (sq >> 3)); }

        public static int WorldToSquare(Vector3 p)
        {
            int c = Mathf.FloorToInt(p.x + 4f), r = Mathf.FloorToInt(4f - p.z);
            if (c < 0 || c > 7 || r < 0 || r > 7) return -1;
            return r * 8 + c;
        }

        public void Build(ThemeDefinition th)
        {
            var top = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(top.GetComponent<Collider>());
            top.name = "BoardSurface";
            top.transform.SetParent(transform, false);
            top.transform.localEulerAngles = new Vector3(90, 0, 0);
            top.transform.localScale = new Vector3(9, 9, 1);
            var m = new Material(Mats.Lit(Color.white, 0, 0.25f));
            m.color = Color.white;
            m.mainTexture = BoardTexture(th);
            top.GetComponent<MeshRenderer>().sharedMaterial = m;

            Mats.Prim(PrimitiveType.Cube, transform, Mats.Lit(th.frame * 0.8f, 0, 0.2f), new Vector3(0, -0.226f, 0), new Vector3(9f, 0.45f, 9f));
            var ground = Mats.Prim(PrimitiveType.Plane, transform, Mats.Lit(th.ground, 0, 0.05f), new Vector3(0, -0.45f, 0), new Vector3(8, 1, 8));
            ground.name = "Ground";

            var font = Hud.LoadFont();
            for (int i = 0; i < 8; i++)
            {
                Label(((char)('a' + i)).ToString(), new Vector3(i - 3.5f, 0.01f, -4.25f), font);
                Label((i + 1).ToString(), new Vector3(-4.25f, 0.01f, i - 3.5f), font);
            }

            var dot = Mats.RadialTexture(64, 0.0f, 0.32f, false);
            var ring = Mats.RadialTexture(128, 0.78f, 0.96f, true);
            var glow = Mats.RadialTexture(128, 0.0f, 1f, false);
            markMats[Mark.LastMove] = Mats.Sprite(new Color(1f, 0.83f, 0.3f, 0.35f));
            markMats[Mark.Selected] = Mats.Sprite(new Color(0.5f, 1f, 0.6f, 0.45f));
            markMats[Mark.Target] = Mats.Sprite(new Color(1f, 0.97f, 0.8f, 0.85f), dot);
            markMats[Mark.Capture] = Mats.Sprite(new Color(1f, 0.3f, 0.16f, 0.95f), ring);
            checkMat = Mats.Sprite(new Color(1f, 0.15f, 0.08f, 0.85f), glow);
            markMats[Mark.Check] = checkMat;
        }

        void Label(string text, Vector3 pos, Font font)
        {
            var go = new GameObject("Coord_" + text);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(90, 0, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.font = font; tm.fontSize = 48; tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(0.95f, 0.85f, 0.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        static Texture2D BoardTexture(ThemeDefinition th)
        {
            const int S = 1152, U = 128, O = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true);
            var px = new Color32[S * S];
            var rnd = new System.Random(3);
            Color32 frame = th.frame, gold = new Color(0.84f, 0.66f, 0.23f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    Color c;
                    int bx = x - O, by = y - O;
                    if (bx >= 0 && by >= 0 && bx < U * 8 && by < U * 8)
                    {
                        int col = bx / U, rowFromBottom = by / U;
                        bool dark = (col + rowFromBottom) % 2 == 0;     // a1 is dark
                        c = dark ? th.darkSquare : th.lightSquare;
                        float n = (float)rnd.NextDouble() * 0.06f - 0.03f;
                        c = new Color(c.r + n, c.g + n, c.b + n);
                    }
                    else
                    {
                        float grain = Mathf.Sin(y * 0.09f + Mathf.Sin(x * 0.01f) * 3f) * 0.04f;
                        c = new Color(frame.r / 255f + grain, frame.g / 255f + grain, frame.b / 255f + grain);
                        if ((bx >= -6 && bx < 0 || bx >= U * 8 && bx < U * 8 + 6) && by >= -6 && by < U * 8 + 6) c = gold;
                        if ((by >= -6 && by < 0 || by >= U * 8 && by < U * 8 + 6) && bx >= -6 && bx < U * 8 + 6) c = gold;
                    }
                    px[y * S + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            tex.anisoLevel = 4;
            return tex;
        }

        public void ClearMarks()
        {
            foreach (var g in active) Destroy(g);
            active.Clear();
        }

        public void MarkSquare(int sq, Mark kind)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Mark_" + kind;
            q.transform.SetParent(transform, false);
            float y = kind == Mark.LastMove ? 0.004f : kind == Mark.Selected ? 0.006f : 0.009f;
            q.transform.localPosition = SquareToWorld(sq) + Vector3.up * y;
            q.transform.localEulerAngles = new Vector3(90, 0, 0);
            q.transform.localScale = Vector3.one * (kind == Mark.Capture ? 1.0f : 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = markMats[kind];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            active.Add(q);
        }

        void Update()
        {
            if (checkMat != null) checkMat.color = new Color(1f, 0.15f, 0.08f, 0.55f + Mathf.Sin(Time.time * 6f) * 0.3f);
        }
    }
}
