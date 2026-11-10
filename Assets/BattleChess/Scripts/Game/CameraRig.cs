using System;
using UnityEngine;

namespace BattleChess
{
    /// <summary>
    /// Orbit camera that always fits the board to the screen (portrait phone or landscape TV),
    /// with drag-to-rotate, pinch/scroll zoom, tap detection, cinematic close-ups and shake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I;
        public Camera Cam;
        public float yaw = 0f, pitch = 55f, zoom = 1f;
        public event Action<Vector2> OnTap;
        public Func<Vector2, bool> IsOverUI;

        float yawTarget;
        Vector3 focus = new Vector3(0, 0.35f, 0);
        bool closeUp; float cuBlend; Vector3 cuPos, cuLook;
        float shake;
        bool pressed, dragging, pinching; Vector2 downPos; float yawStart, pitchStart, pinchDist, zoomStart;

        void Awake()
        {
            I = this;
            Cam = GetComponent<Camera>();
            Cam.fieldOfView = 40f;
            Cam.nearClipPlane = 0.1f; Cam.farClipPlane = 120f;
            yawTarget = yaw;
        }

        public void SetSide(bool whiteAtBottom) { yawTarget = whiteAtBottom ? 0f : 180f; }
        public void Flip() { yawTarget += 180f; }
        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        public void BeginCloseUp(Vector3 attacker, Vector3 victim)
        {
            Vector3 mid = (attacker + victim) * 0.5f;
            Vector3 line = victim - attacker; line.y = 0;
            if (line.sqrMagnitude < 1e-4f) line = Vector3.forward;
            line.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, line);
            Vector3 toCam = transform.position - mid; toCam.y = 0;
            if (Vector3.Dot(side, toCam) < 0) side = -side;
            cuPos = mid + side * 2.4f - line * 0.6f + Vector3.up * 1.25f;
            cuLook = mid + Vector3.up * 0.5f;
            closeUp = true;
        }

        public void EndCloseUp() { closeUp = false; }

        void LateUpdate()
        {
            HandleInput();
            float udt = Time.unscaledDeltaTime;
            if (!dragging) yaw = Mathf.MoveTowardsAngle(yaw, yawTarget, 260f * udt);
            cuBlend = Mathf.MoveTowards(cuBlend, closeUp ? 1f : 0f, udt * 1.6f);

            float vfov = Cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float hfov = Mathf.Atan(Mathf.Tan(vfov) * Cam.aspect);
            float el = pitch * Mathf.Deg2Rad, half = 4.3f;
            float d = Mathf.Max(half / Mathf.Tan(hfov) + half * Mathf.Cos(el) * 0.8f,
                                (half * Mathf.Sin(el) + 1f) / Mathf.Tan(vfov) + half * Mathf.Cos(el) * 0.4f) * zoom;
            Vector3 orbitPos = focus + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * d;

            float k = cuBlend * cuBlend * (3 - 2 * cuBlend);
            Vector3 pos = Vector3.Lerp(orbitPos, cuPos, k);
            Vector3 look = Vector3.Lerp(focus, cuLook, k);
            if (shake > 0)
            {
                pos += UnityEngine.Random.insideUnitSphere * shake * 0.25f;
                shake = Mathf.Max(0, shake - udt);
            }
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
        }

        void HandleInput()
        {
            Vector2 a, b;
            if (InputRouter.TwoFingers(out a, out b))
            {
                float dist = Vector2.Distance(a, b);
                if (!pinching) { pinching = true; pinchDist = Mathf.Max(20f, dist); zoomStart = zoom; }
                zoom = Mathf.Clamp(zoomStart * pinchDist / Mathf.Max(20f, dist), 0.45f, 1.35f);
                dragging = false; pressed = false;
                return;
            }
            if (pinching) { pinching = false; pressed = false; return; }

            float sc = InputRouter.Scroll();
            if (Mathf.Abs(sc) > 0.01f) zoom = Mathf.Clamp(zoom * (1f - sc * 0.08f), 0.45f, 1.35f);

            Vector2 p;
            if (InputRouter.PrimaryDown(out p))
            {
                if (IsOverUI != null && IsOverUI(p)) { pressed = false; return; }
                pressed = true; dragging = false; downPos = p; yawStart = yaw; pitchStart = pitch;
            }
            if (pressed && InputRouter.PrimaryHeld(out p))
            {
                Vector2 delta = p - downPos;
                float px = delta.magnitude / Mathf.Max(1f, Screen.dpi > 0 ? Screen.dpi / 160f : 1f);
                if (!dragging && px > 10f) dragging = true;
                if (dragging)
                {
                    float scale = 180f / Mathf.Max(400f, Screen.width);
                    yaw = yawStart + delta.x * scale;
                    yawTarget = yaw;
                    pitch = Mathf.Clamp(pitchStart - delta.y * scale * 0.7f, 20f, 85f);
                }
            }
            if (pressed && InputRouter.PrimaryUp(out p))
            {
                if (!dragging && OnTap != null) OnTap(p);
                pressed = false; dragging = false;
            }
        }
    }
}
