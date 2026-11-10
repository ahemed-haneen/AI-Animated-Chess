using System.Collections;
using UnityEngine;

namespace BattleChess
{
    /// <summary>Procedural particle bursts, muzzle flashes and projectiles. Swap for VFX prefabs per piece in the Theme asset.</summary>
    public class Fx : MonoBehaviour
    {
        public static Fx I;
        Texture2D soft;
        Light flashLight;

        void Awake()
        {
            I = this;
            soft = Mats.RadialTexture(64, 0f, 1f, false);
            var lg = new GameObject("FlashLight");
            lg.transform.SetParent(transform, false);
            flashLight = lg.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.intensity = 0;
            flashLight.range = 4;
            flashLight.color = new Color(1f, 0.8f, 0.45f);
        }

        public void Burst(Vector3 pos, Color color, int count, float speed, float size, float life, float gravity = 0f, float spread = 0.05f, Vector3 direction = default(Vector3))
        {
            var go = new GameObject("FxBurst");
            go.transform.position = pos;
            if (direction != Vector3.zero) go.transform.rotation = Quaternion.LookRotation(direction);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count + 4;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = direction != Vector3.zero ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Sphere;
            sh.angle = 25f;
            sh.radius = spread;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Sprite(Color.white, soft);
            ps.Emit(count);
            Destroy(go, life + 0.5f);
        }

        public void Dust(Vector3 pos, float scale = 1f)
        {
            Burst(new Vector3(pos.x, 0.08f, pos.z), new Color(0.75f, 0.66f, 0.5f, 0.7f), 14, 1.2f * scale, 0.25f * scale, 0.9f, 0.05f, 0.25f);
        }

        public void Smoke(Vector3 pos, bool big)
        {
            Burst(pos, new Color(0.92f, 0.9f, 0.86f, 0.75f), big ? 22 : 8, big ? 0.9f : 0.35f, big ? 0.45f : 0.22f, big ? 2f : 1.3f, -0.03f, big ? 0.15f : 0.04f);
        }

        public void Sparks(Vector3 pos, Color c, int n = 16)
        {
            Burst(pos, c, n, 3f, 0.06f, 0.45f, 1.2f, 0.05f);
        }

        public void Flash(Vector3 pos, bool big)
        {
            Burst(pos, new Color(1f, 0.85f, 0.45f, 1f), big ? 4 : 2, 0.2f, big ? 0.6f : 0.25f, 0.15f);
            StartCoroutine(FlashRoutine(pos, big));
        }

        IEnumerator FlashRoutine(Vector3 pos, bool big)
        {
            flashLight.transform.position = pos;
            flashLight.range = big ? 7 : 4;
            float t = 0, dur = big ? 0.3f : 0.15f, peak = big ? 8 : 4;
            while (t < dur) { flashLight.intensity = peak * (1 - t / dur); t += Time.deltaTime; yield return null; }
            flashLight.intensity = 0;
        }

        /// <summary>Moves a small projectile from a to b; yields until it lands.</summary>
        public IEnumerator Projectile(Vector3 a, Vector3 b, float duration, float arc, bool cannonball)
        {
            var go = new GameObject(cannonball ? "Cannonball" : "Shot");
            go.AddComponent<MeshFilter>().sharedMesh = Mats.Mesh(PrimitiveType.Sphere);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = cannonball ? Mats.Iron : Mats.Sprite(new Color(1f, 0.9f, 0.5f));
            go.transform.localScale = Vector3.one * (cannonball ? 0.11f : 0.04f);
            float t = 0;
            while (t < duration)
            {
                float k = t / duration;
                go.transform.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(Mathf.PI * k) * arc;
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(go);
        }

        public void Spawn(GameObject prefab, Vector3 pos, Quaternion rot, float life = 3f)
        {
            if (prefab == null) return;
            var go = Instantiate(prefab, pos, rot);
            Destroy(go, life);
        }
    }
}
