using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleChess.Core;

namespace BattleChess
{
    /// <summary>
    /// Choreographs each move: walking, castling, and the cinematic "set piece" for captures
    /// (camera close-up, approach, strike or shot, slow-motion impact, death).
    /// This is where most of the TV-worthy spectacle lives — extend it per theme.
    /// </summary>
    public class BattleDirector : MonoBehaviour
    {
        public GameController game;
        public bool cinematic = true;
        [Range(0.1f, 1f)] public float slowMoScale = 0.35f;
        public float slowMoSeconds = 0.35f;

        public IEnumerator Play(GameState prev, Move m, Dictionary<int, PieceActor> actors)
        {
            var p = prev.Board[m.From];
            PieceActor a;
            if (!actors.TryGetValue(p.Id, out a) || a == null) yield break;
            Vector3 to = BoardView.SquareToWorld(m.To);

            if (m.IsCastle)
            {
                PieceActor rook;
                actors.TryGetValue(prev.Board[m.RookFrom].Id, out rook);
                var kingWalk = StartCoroutine(a.MoveTo(to));
                if (rook != null) { yield return new WaitForSeconds(0.15f); yield return rook.MoveTo(BoardView.SquareToWorld(m.RookTo)); }
                yield return kingWalk;
                if (rook != null) StartCoroutine(rook.FaceHome());
                yield return a.FaceHome();
                yield break;
            }

            int vsq = m.EpCapture >= 0 ? m.EpCapture : (prev.Board[m.To].Empty ? -1 : m.To);
            float jump = p.T == PType.N ? (a.Def.locomotion == Locomotion.Gallop ? 0.55f : 0.75f) : 0f;

            if (vsq < 0)
            {
                yield return a.MoveTo(to, jump);
            }
            else
            {
                PieceActor v;
                actors.TryGetValue(prev.Board[vsq].Id, out v);
                if (v == null) { yield return a.MoveTo(to, jump); }
                else
                {
                    var vDef = v.Def;
                    game.Announce(a.Def.displayName + " attacks " + vDef.displayName + "!");
                    Vector3 vp = v.transform.position;
                    if (cinematic) CameraRig.I.BeginCloseUp(a.transform.position, vp);

                    if (a.Def.IsRanged)
                    {
                        if (cinematic) yield return new WaitForSeconds(0.35f);
                        yield return a.TurnToward(vp);
                        yield return a.Attack(v, () => Impact(a, v));
                        yield return new WaitForSeconds(0.8f);
                    }
                    else
                    {
                        Vector3 start = a.transform.position;
                        Vector3 dir = vp - start; dir.y = 0;
                        float dist = dir.magnitude; dir.Normalize();
                        float reach = 0.7f;
                        if (dist > reach) yield return a.MoveTo(vp - dir * reach, jump * 0.8f);
                        yield return a.TurnToward(vp);
                        yield return a.Attack(v, () => Impact(a, v));
                        yield return new WaitForSeconds(0.6f);
                    }
                    if (cinematic) CameraRig.I.EndCloseUp();
                    yield return a.MoveTo(to);
                }
            }

            if (m.Promo != PType.None) yield return game.PromoteRoutine(p.Id, m.Promo, p.White);
            PieceActor final;
            if (actors.TryGetValue(p.Id, out final) && final != null) yield return final.FaceHome();
        }

        void Impact(PieceActor a, PieceActor v)
        {
            Vector3 hit = v.transform.position + Vector3.up * 0.55f;
            if (a.Def.impactVfx != null) Fx.I.Spawn(a.Def.impactVfx, hit, Quaternion.identity);
            else Fx.I.Sparks(hit, new Color(1f, 0.85f, 0.5f), 12);
            v.PlayClip(a.Def.impactSound, "stab", 0.8f);
            v.StartCoroutine(v.Die(a.transform.position));
            if (cinematic) StartCoroutine(SlowMo());
        }

        IEnumerator SlowMo()
        {
            Time.timeScale = slowMoScale;
            yield return new WaitForSecondsRealtime(slowMoSeconds);
            float t = 0;
            while (t < 0.25f) { t += Time.unscaledDeltaTime; Time.timeScale = Mathf.Lerp(slowMoScale, 1f, t / 0.25f); yield return null; }
            Time.timeScale = 1f;
        }
    }
}
