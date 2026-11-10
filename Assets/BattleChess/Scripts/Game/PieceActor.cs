using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleChess.Core;

namespace BattleChess
{
    /// <summary>
    /// One chess piece on the board. Drives either a rigged character (Animator) or the built-in placeholder.
    /// Animator contract (created by 'Battle Chess > Make Character From Selected Model'):
    ///   float Speed, bool Selected, triggers Attack / Hit / Die / Victory.
    ///   Attack state tagged "Attack". Optional animation event "Impact" on the attack clip.
    /// </summary>
    public class PieceActor : MonoBehaviour
    {
        public int PieceId;
        public PType Type;
        public bool White;
        public PieceDefinition Def;
        public Animator Anim;
        public PlaceholderRig Rig;
        public bool IsDead;
        public bool Moving;

        Transform visual;
        readonly HashSet<string> animParams = new HashSet<string>();
        static Texture2D ringTex;

        float phase, walkAmt, moveSpeed, hopY, lunge, rear, recoil, sinkRoll, sinkY;
        bool selected, cheering, alarm, airborne, impactFlag;
        float poseArmR, poseGripR, poseArmL, poseLean;
        float restArmR, restGripR, restArmL;
        int poseTok;

        public float HomeYaw { get { return White ? 0f : 180f; } }

        public static PieceActor Create(Piece p, PieceDefinition def, Color team, Transform parent)
        {
            var go = new GameObject((p.White ? "W_" : "B_") + p.T + "_" + p.Id);
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<PieceActor>();
            a.PieceId = p.Id; a.Type = p.T; a.White = p.White; a.Def = def;
            a.Build(team);
            return a;
        }

        void Build(Color team)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            if (Def.prefab != null)
            {
                var model = Instantiate(Def.prefab, visual);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * Def.prefabScale;
                Anim = model.GetComponentInChildren<Animator>();
                if (Anim != null)
                {
                    Anim.applyRootMotion = false;
                    foreach (var pr in Anim.parameters) animParams.Add(pr.name);
                    var relay = Anim.GetComponent<AnimationEventRelay>();
                    if (relay == null) relay = Anim.gameObject.AddComponent<AnimationEventRelay>();
                    relay.actor = this;
                }
                foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
            }
            else
            {
                Rig = PlaceholderFactory.Build(Def.placeholder, visual);
            }

            if (ringTex == null) ringTex = Mats.RadialTexture(128, 0.8f, 0.97f, true);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ring.GetComponent<Collider>());
            ring.name = "TeamRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0, 0.012f, 0);
            ring.transform.localEulerAngles = new Vector3(90, 0, 0);
            ring.transform.localScale = Vector3.one * 0.9f;
            var rr = ring.GetComponent<MeshRenderer>();
            rr.sharedMaterial = Mats.Sprite(new Color(team.r, team.g, team.b, 0.95f), ringTex);
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.55f, 0);
            col.size = new Vector3(0.8f, 1.1f, 0.8f);

            switch (Def.placeholder.weapon)
            {
                case WeaponKind.Sword: case WeaponKind.Cutlass: restArmR = -10; restGripR = 80; break;
                case WeaponKind.Pistol: restArmR = 0; restGripR = 150; break;
                case WeaponKind.Blunderbuss: restArmR = -25; restGripR = 70; restArmL = -40; break;
                default: restArmR = 0; restGripR = 0; break;
            }
            ResetState();
        }

        public void ResetState()
        {
            poseTok++;
            poseArmR = restArmR; poseGripR = restGripR; poseArmL = restArmL; poseLean = 0;
            lunge = rear = recoil = sinkRoll = sinkY = 0;
            cheering = alarm = airborne = Moving = false;
            visual.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        // ---------- per frame ----------
        void Update()
        {
            float dt = Time.deltaTime;
            if (Moving) phase += dt * moveSpeed * StepRate();
            walkAmt = Mathf.MoveTowards(walkAmt, Moving ? 1f : 0f, dt * 6f);

            if (Anim != null)
            {
                if (animParams.Contains("Speed")) Anim.SetFloat("Speed", Moving ? moveSpeed : 0f, 0.1f, dt);
                if (animParams.Contains("Selected")) Anim.SetBool("Selected", selected || alarm);
            }
            if (Rig != null) AnimatePlaceholder(dt);

            float targetHop = 0;
            if (cheering) targetHop = Mathf.Abs(Mathf.Sin(Time.time * 7f + PieceId)) * 0.15f;
            else if (selected && !Moving && !IsDead) targetHop = Mathf.Abs(Mathf.Sin(Time.time * 5.5f)) * 0.08f;
            hopY = (cheering || selected) ? targetHop : Mathf.MoveTowards(hopY, 0, dt);
            visual.localPosition = new Vector3(0, hopY - sinkY, lunge);
        }

        float StepRate()
        {
            switch (Def.locomotion)
            {
                case Locomotion.Gallop: return 9f;
                case Locomotion.Roll: return 9f;
                case Locomotion.Sail: return 0f;
                default: return 12f;
            }
        }

        void AnimatePlaceholder(float dt)
        {
            float sw = Mathf.Sin(phase) * walkAmt;
            var h = Rig.human;
            if (h != null)
            {
                if (!h.seated)
                {
                    h.legL.localEulerAngles = new Vector3(sw * 35f, 0, 0);
                    h.legR.localEulerAngles = new Vector3(-sw * 35f, 0, 0);
                    h.hips.localPosition = new Vector3(0, 0.5f + Mathf.Abs(Mathf.Sin(phase)) * 0.025f * walkAmt, 0);
                }
                h.armR.localEulerAngles = new Vector3(poseArmR - sw * 10f, 0, 0);
                h.armL.localEulerAngles = new Vector3(poseArmL + sw * 25f, 0, 0);
                h.gripR.localEulerAngles = new Vector3(poseGripR, 0, 0);
                h.torso.localEulerAngles = new Vector3(poseLean, 0, 0);
                h.torso.localScale = new Vector3(1, 1 + Mathf.Sin(Time.time * 2.2f + PieceId) * 0.012f, 1);
                if (!IsDead) h.head.localEulerAngles = new Vector3(0, Mathf.Sin(Time.time * 0.6f + PieceId) * 22f * (1 - walkAmt), 0);
            }
            if (Rig.horseLegs != null)
            {
                float[] off = { 0f, 0.5f, Mathf.PI, Mathf.PI + 0.5f };
                for (int i = 0; i < 4; i++) Rig.horseLegs[i].localEulerAngles = new Vector3(Mathf.Sin(phase + off[i]) * 40f * walkAmt, 0, 0);
                Rig.horseBody.localEulerAngles = new Vector3(Mathf.Sin(phase * 2f) * 4f * walkAmt + rear, 0, 0);
            }
            if (Rig.wheels != null && Moving)
                foreach (var w in Rig.wheels) w.Rotate(Vector3.right, dt * moveSpeed / 0.18f * Mathf.Rad2Deg, Space.Self);
            if (Rig.barrel != null) Rig.barrel.localPosition = new Vector3(0, 0.32f, 0.05f - recoil);
            if (Rig.hull != null)
            {
                Rig.hull.localEulerAngles = new Vector3(0, 0, Mathf.Sin(Time.time * 1.4f + PieceId) * 3f + sinkRoll);
                Rig.hull.localPosition = new Vector3(0, Mathf.Sin(Time.time * 1.8f + PieceId) * 0.015f, 0);
                if (Moving && UnityEngine.Random.value < 0.3f) Fx.I.Burst(transform.position + Vector3.up * 0.05f, new Color(1, 1, 1, 0.7f), 1, 0.2f, 0.08f, 0.7f);
            }
        }

        // ---------- public controls ----------
        public void SetSelected(bool on)
        {
            if (selected == on) return;
            selected = on;
            if (on && Rig != null && Rig.human != null && !Moving) StartCoroutine(SalutePose());
        }

        public void SetAlarm(bool on)
        {
            if (alarm == on) return;
            alarm = on;
            if (Rig != null) StartCoroutine(on ? Pose(-150, 165, poseArmL, 0, 0.3f) : Pose(restArmR, restGripR, restArmL, 0, 0.3f));
        }

        public void Celebrate()
        {
            cheering = true;
            if (Anim != null && animParams.Contains("Victory")) Anim.SetTrigger("Victory");
            if (Rig != null) StartCoroutine(Pose(-170, poseGripR, -170, 0, 0.4f));
        }

        public void OnAnimationImpact() { impactFlag = true; }

        public IEnumerator TurnTo(float yaw, float degPerSec = 600f)
        {
            float start = transform.eulerAngles.y;
            float delta = Mathf.DeltaAngle(start, yaw);
            float dur = Mathf.Abs(delta) / degPerSec;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Euler(0, start + delta * Mathf.Clamp01(t / dur), 0);
                yield return null;
            }
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        public IEnumerator TurnToward(Vector3 p)
        {
            var d = p - transform.position; d.y = 0;
            if (d.sqrMagnitude < 1e-4f) yield break;
            yield return TurnTo(Quaternion.LookRotation(d).eulerAngles.y);
        }

        public IEnumerator FaceHome() { yield return TurnTo(HomeYaw, 400f); }

        public IEnumerator MoveTo(Vector3 target, float jump = 0f)
        {
            Vector3 start = transform.position; target.y = 0; start.y = 0;
            float dist = Vector3.Distance(start, target);
            if (dist < 0.01f) yield break;
            yield return TurnToward(target);
            float dur;
            switch (Def.locomotion)
            {
                case Locomotion.Gallop: dur = 0.35f + dist * 0.2f; break;
                case Locomotion.Roll: dur = 0.45f + dist * 0.3f; break;
                case Locomotion.Sail: dur = 0.5f + dist * 0.3f; break;
                default: dur = 0.35f + dist * 0.29f; break;
            }
            Moving = true; moveSpeed = dist / dur; selected = false; airborne = jump > 0;
            if (Def.locomotion == Locomotion.Sail) SoundBank.I.Play("splash", 0.6f);
            float t = 0; int lastStep = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = (jump > 0 || Def.locomotion == Locomotion.Gallop) ? k * k * (3 - 2 * k) : k;
                var p = Vector3.Lerp(start, target, e);
                p.y = jump > 0 ? Mathf.Sin(Mathf.PI * k) * jump : 0f;
                transform.position = p;
                int step = (int)(phase / Mathf.PI);
                if (step != lastStep) { lastStep = step; Footstep(); }
                yield return null;
            }
            transform.position = target;
            Moving = false; airborne = false;
        }

        void Footstep()
        {
            switch (Def.locomotion)
            {
                case Locomotion.Gallop: SoundBank.I.Play("clop", 0.5f); break;
                case Locomotion.Roll: SoundBank.I.Play("roll", 0.6f); break;
                case Locomotion.Walk: SoundBank.I.Play("step", 0.35f); break;
            }
        }

        /// <summary>Plays the attack; calls onImpact at the moment the blow / shot lands.</summary>
        public IEnumerator Attack(PieceActor victim, Action onImpact)
        {
            impactFlag = false;
            bool fired = false;
            Action fire = () => { if (fired) return; fired = true; if (onImpact != null) onImpact(); };

            if (Anim != null && animParams.Contains("Attack"))
            {
                Anim.SetTrigger("Attack");
                float t = 0;
                while (t < 0.4f && !Anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) { t += Time.deltaTime; yield return null; }
                t = 0;
                while (t < 3f)
                {
                    var st = Anim.GetCurrentAnimatorStateInfo(0);
                    if (impactFlag || (st.IsTag("Attack") && st.normalizedTime >= Def.impactAt) || (!st.IsTag("Attack") && t > 0.5f)) break;
                    t += Time.deltaTime;
                    yield return null;
                }
                if (Def.IsRanged) yield return RangedEffects(victim);
                else PlayClip(Def.attackSound, "clash");
                fire();
                yield return new WaitForSeconds(0.25f);
                yield break;
            }

            switch (Def.attack)
            {
                case AttackStyle.Shoot:
                    yield return Pose(-85, 175, -75, 0, 0.3f);
                    yield return new WaitForSeconds(0.12f);
                    yield return RangedEffects(victim);
                    fire();
                    StartCoroutine(RestAfter(0.35f));
                    break;
                case AttackStyle.Cannon:
                    if (Rig != null && Rig.human != null) StartCoroutine(Pose(-50, 60, -30, 10, 0.25f));
                    yield return new WaitForSeconds(0.3f);
                    yield return RangedEffects(victim);
                    fire();
                    StartCoroutine(RestAfter(0.4f));
                    break;
                case AttackStyle.Thrust:
                    yield return Pose(-30, 120, -20, -5, 0.2f);
                    StartCoroutine(Lunge(0.12f));
                    yield return Pose(-85, 175, -20, 8, 0.1f);
                    PlayClip(Def.attackSound, "stab");
                    fire();
                    StartCoroutine(RestAfter(0.2f));
                    break;
                case AttackStyle.Smash:
                    yield return Pose(-170, 170, -170, -10, 0.25f);
                    yield return Pose(-60, 150, -60, 20, 0.12f);
                    PlayClip(Def.attackSound, "thud");
                    CameraRig.I.Shake(0.25f);
                    fire();
                    StartCoroutine(RestAfter(0.2f));
                    break;
                default: // Slash, Charge
                    if (Rig != null && Rig.horseBody != null) StartCoroutine(Rear());
                    yield return Pose(-165, 175, -20, -5, 0.22f);
                    StartCoroutine(Lunge(0.1f));
                    yield return Pose(-55, 150, -20, 8, 0.1f);
                    PlayClip(Def.attackSound, "clash");
                    fire();
                    StartCoroutine(RestAfter(0.2f));
                    break;
            }
        }

        IEnumerator RangedEffects(PieceActor v)
        {
            Vector3 mz = MuzzlePosition();
            Vector3 tgt = v.transform.position + Vector3.up * 0.5f;
            bool cannon = Def.attack == AttackStyle.Cannon;
            Fx.I.Flash(mz, cannon);
            Fx.I.Smoke(mz, cannon);
            if (Def.muzzleVfx != null) Fx.I.Spawn(Def.muzzleVfx, mz, transform.rotation);
            PlayClip(Def.attackSound, cannon ? "cannon" : "shot");
            if (cannon)
            {
                CameraRig.I.Shake(0.35f);
                StartCoroutine(Recoil());
                yield return Fx.I.Projectile(mz, tgt - Vector3.up * 0.2f, 0.38f, 0.6f, true);
                Fx.I.Sparks(tgt, new Color(1f, 0.7f, 0.3f), 14);
            }
            else yield return Fx.I.Projectile(mz, tgt, 0.1f, 0f, false);
        }

        public Vector3 MuzzlePosition()
        {
            if (Rig != null && Rig.muzzle != null) return Rig.muzzle.position;
            var m = FindDeep(transform, "Muzzle");
            if (m != null) return m.position;
            if (Anim != null && Anim.isHuman)
            {
                var hand = Anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null) return hand.position + transform.forward * 0.3f;
            }
            return transform.position + Vector3.up * 0.7f + transform.forward * 0.4f;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        public void PlayClip(AudioClip clip, string fallback, float vol = 1f)
        {
            if (clip != null) SoundBank.I.Play(clip, vol); else SoundBank.I.Play(fallback, vol);
        }

        /// <summary>Death: faces the attacker, falls, then sinks away (unless keep, used for the checkmated king).</summary>
        public IEnumerator Die(Vector3 from, bool keep = false)
        {
            IsDead = !keep; selected = false; cheering = false; alarm = false;
            if (!keep) { var c = GetComponent<Collider>(); if (c != null) c.enabled = false; }
            yield return TurnToward(from);
            PlayClip(Def.deathSound, "thud");
            if (Anim != null && animParams.Contains("Die"))
            {
                Anim.SetTrigger("Die");
                yield return new WaitForSeconds(1.4f);
            }
            else
            {
                bool upright = Rig != null && Rig.kind == PlaceholderKind.Soldier;
                bool ship = Rig != null && Rig.kind == PlaceholderKind.Ship;
                if (upright) StartCoroutine(Pose(-150, poseGripR, -150, 0, 0.25f));
                Quaternion r0 = visual.localRotation;
                Quaternion r1 = upright ? Quaternion.Euler(-85, 0, 0) : ship ? Quaternion.identity : Quaternion.Euler(0, 0, 80);
                float t = 0, dur = ship ? 1.4f : 0.5f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / dur);
                    visual.localRotation = Quaternion.Slerp(r0, r1, k * k);
                    if (ship) { sinkRoll = 35f * k; sinkY = 0.6f * k * k; }
                    yield return null;
                }
            }
            Fx.I.Dust(transform.position, 1.2f);
            if (keep) yield break;
            yield return new WaitForSeconds(0.5f);
            Vector3 p0 = transform.position; float s = 0;
            while (s < 0.6f)
            {
                s += Time.deltaTime;
                float k = Mathf.Clamp01(s / 0.6f);
                transform.position = p0 + Vector3.down * 0.5f * k * k;
                transform.localScale = Vector3.one * (1 - 0.7f * k);
                yield return null;
            }
            Destroy(gameObject);
        }

        // ---------- placeholder pose helpers ----------
        IEnumerator Pose(float armR, float gripR, float armL, float lean, float dur)
        {
            int tok = ++poseTok;
            float a0 = poseArmR, g0 = poseGripR, l0 = poseArmL, n0 = poseLean, t = 0;
            while (t < dur)
            {
                if (tok != poseTok) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur); k = k * k * (3 - 2 * k);
                poseArmR = Mathf.Lerp(a0, armR, k); poseGripR = Mathf.Lerp(g0, gripR, k);
                poseArmL = Mathf.Lerp(l0, armL, k); poseLean = Mathf.Lerp(n0, lean, k);
                yield return null;
            }
        }

        IEnumerator RestAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return Pose(restArmR, restGripR, restArmL, 0, 0.35f);
        }

        IEnumerator SalutePose()
        {
            yield return Pose(restArmR - 40, restGripR, restArmL, 0, 0.15f);
            yield return Pose(restArmR, restGripR, restArmL, 0, 0.3f);
        }

        IEnumerator Lunge(float dur)
        {
            float t = 0;
            while (t < dur) { t += Time.deltaTime; lunge = 0.14f * Mathf.Clamp01(t / dur); yield return null; }
            yield return new WaitForSeconds(0.15f);
            t = 0;
            while (t < 0.3f) { t += Time.deltaTime; lunge = 0.14f * (1 - Mathf.Clamp01(t / 0.3f)); yield return null; }
            lunge = 0;
        }

        IEnumerator Rear()
        {
            float t = 0;
            while (t < 0.35f) { t += Time.deltaTime; rear = -22f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.35f)); yield return null; }
            rear = 0;
        }

        IEnumerator Recoil()
        {
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float k = t / 0.5f;
                recoil = 0.1f * (k < 0.15f ? k / 0.15f : 1 - (k - 0.15f) / 0.85f);
                lunge = -recoil;
                yield return null;
            }
            recoil = 0; lunge = 0;
        }
    }
}
