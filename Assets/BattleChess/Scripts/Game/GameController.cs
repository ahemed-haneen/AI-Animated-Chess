using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleChess.Core;

namespace BattleChess
{
    [Serializable]
    public class GameSettings
    {
        public bool vsComputer = true;
        public int level = 2;          // 1 easy, 2 medium, 3 hard
        public bool humanWhite = true;
        public GameSettings Copy() { return (GameSettings)MemberwiseClone(); }
    }

    /// <summary>Game flow: turns, selection, AI, undo, promotion, game over and saving.</summary>
    public class GameController : MonoBehaviour
    {
        public ThemeDefinition Theme;
        public BoardView Board;
        public CameraRig Cam;
        public Hud Hud;
        public BattleDirector Director;
        public GameSettings Settings = new GameSettings();

        readonly List<GameState> states = new List<GameState>();
        readonly List<Move> moves = new List<Move>();
        readonly List<string> keys = new List<string>();
        readonly Dictionary<int, PieceActor> actors = new Dictionary<int, PieceActor>();
        List<Move> legal = new List<Move>();
        Transform piecesRoot;
        int selected = -1, aiToken;
        bool busy, thinking, over;

        const string SaveKey = "battlechess_save_v1";

        GameState Cur { get { return states[states.Count - 1]; } }
        bool HumanTurn { get { return !over && !busy && !thinking && (!Settings.vsComputer || Cur.WhiteToMove == Settings.humanWhite); } }

        [Serializable]
        class SaveData
        {
            public bool vsComputer = true; public int level = 2; public bool humanWhite = true;
            public bool cinematic = true; public bool muted;
            public List<string> moves = new List<string>();
        }

        public void Init()
        {
            piecesRoot = new GameObject("Pieces").transform;
            Hud.Bind(this, Theme);
            Cam.OnTap += OnTap;
            Cam.IsOverUI = Hud.IsBlocking;
            if (!Load()) NewGame(Settings);
        }

        // ---------------- public commands (HUD) ----------------
        public void NewGame(GameSettings s)
        {
            aiToken++; StopAllCoroutines(); Director.StopAllCoroutines(); Time.timeScale = 1f;
            Settings = s.Copy();
            busy = thinking = over = false; selected = -1;
            states.Clear(); moves.Clear(); keys.Clear();
            states.Add(Chess.Initial()); keys.Add(Chess.Key(Cur));
            ClearActors();
            SyncActors(true);
            Cam.SetSide(!Settings.vsComputer || Settings.humanWhite);
            legal = Chess.Legal(Cur);
            Cam.EndCloseUp();
            Refresh();
            Save();
            MaybeAI(1.4f);
        }

        public void Undo()
        {
            if (busy || moves.Count == 0) return;
            aiToken++; thinking = false; over = false; selected = -1;
            Hud.HideEnd();
            Pop();
            if (Settings.vsComputer) while (moves.Count > 0 && Cur.WhiteToMove != Settings.humanWhite) Pop();
            foreach (var a in actors.Values) if (a != null) a.ResetState();
            SyncActors(false);
            legal = Chess.Legal(Cur);
            SoundBank.I.Play("select");
            Refresh(); Save();
            MaybeAI(0.6f);
        }

        public bool CanUndo { get { return !busy && moves.Count > 0 && (!Settings.vsComputer || states.Take(states.Count - 1).Any(s => s.WhiteToMove == Settings.humanWhite)); } }
        public bool Busy { get { return busy; } }
        public void Rotate() { Cam.Flip(); }
        public bool ToggleCinematic() { Director.cinematic = !Director.cinematic; Save(); return Director.cinematic; }
        public bool ToggleSound() { SoundBank.I.muted = !SoundBank.I.muted; Save(); return !SoundBank.I.muted; }
        public void Announce(string text) { Hud.SetStatus(text, false); }

        void Pop() { states.RemoveAt(states.Count - 1); moves.RemoveAt(moves.Count - 1); keys.RemoveAt(keys.Count - 1); }

        // ---------------- actors ----------------
        void ClearActors()
        {
            foreach (var a in actors.Values) if (a != null) Destroy(a.gameObject);
            actors.Clear();
        }

        PieceActor Spawn(Piece p)
        {
            var army = Theme.Army(p.White);
            var a = PieceActor.Create(p, army.Get(p.T), army.color, piecesRoot);
            actors[p.Id] = a;
            return a;
        }

        void SyncActors(bool pop)
        {
            var present = new HashSet<int>();
            int i = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                var p = Cur.Board[sq];
                if (p.Empty) continue;
                present.Add(p.Id);
                PieceActor a;
                actors.TryGetValue(p.Id, out a);
                if (a == null || a.IsDead || a.Type != p.T)
                {
                    if (a != null && !a.IsDead) Destroy(a.gameObject);
                    a = Spawn(p);
                    if (pop) StartCoroutine(PopIn(a, i++ * 0.03f));
                }
                a.transform.position = BoardView.SquareToWorld(sq);
                a.transform.rotation = Quaternion.Euler(0, a.HomeYaw, 0);
            }
            foreach (var id in actors.Keys.ToList())
            {
                if (present.Contains(id)) continue;
                var a = actors[id];
                if (a != null && !a.IsDead) Destroy(a.gameObject);
                actors.Remove(id);
            }
        }

        IEnumerator PopIn(PieceActor a, float delay)
        {
            a.transform.localScale = Vector3.one * 0.001f;
            yield return new WaitForSeconds(delay);
            float t = 0;
            while (t < 0.45f && a != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.45f);
                float back = 1 + 2.9f * Mathf.Pow(k - 1, 3) + 1.9f * Mathf.Pow(k - 1, 2);
                a.transform.localScale = Vector3.one * Mathf.Max(0.001f, back);
                yield return null;
            }
            if (a != null) a.transform.localScale = Vector3.one;
        }

        public IEnumerator PromoteRoutine(int id, PType t, bool white)
        {
            PieceActor old;
            if (!actors.TryGetValue(id, out old) || old == null) yield break;
            SoundBank.I.Play("promote");
            Vector3 pos = old.transform.position;
            Fx.I.Burst(pos + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.4f), 30, 2.5f, 0.08f, 1f, 0.6f, 0.3f);
            float s = 0;
            while (s < 0.45f) { s += Time.deltaTime; float k = s / 0.45f; old.transform.Rotate(0, 900 * Time.deltaTime, 0); old.transform.localScale = Vector3.one * Mathf.Max(0.001f, 1 - k); yield return null; }
            Destroy(old.gameObject);
            var a = Spawn(new Piece(t, white, id));
            a.transform.position = pos;
            a.transform.rotation = Quaternion.Euler(0, a.HomeYaw, 0);
            yield return PopIn(a, 0f);
        }

        // ---------------- input ----------------
        void OnTap(Vector2 screen)
        {
            if (!HumanTurn) return;
            int sq = Pick(screen);
            if (sq < 0) { selected = -1; Refresh(); return; }
            var p = Cur.Board[sq];
            if (selected >= 0)
            {
                var ms = legal.FindAll(m => m.From == selected && m.To == sq);
                if (ms.Count > 0)
                {
                    if (ms[0].Promo != PType.None)
                        Hud.AskPromotion(Cur.WhiteToMove, t => StartCoroutine(DoMove(ms.Find(m => m.Promo == t))));
                    else StartCoroutine(DoMove(ms[0]));
                    return;
                }
            }
            if (!p.Empty && p.White == Cur.WhiteToMove)
            {
                selected = selected == sq ? -1 : sq;
                if (selected >= 0)
                {
                    SoundBank.I.Play("select");
                    var def = Theme.Get(p.T, p.White);
                    Hud.SetStatus(def.displayName + " · " + RoleName(p.T) + (legal.Exists(m => m.From == sq) ? "" : " — no moves"), false);
                }
            }
            else selected = -1;
            Refresh(false);
        }

        int Pick(Vector2 screen)
        {
            var ray = Cam.Cam.ScreenPointToRay(screen);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f))
            {
                var a = hit.collider.GetComponentInParent<PieceActor>();
                if (a != null && !a.IsDead)
                    for (int sq = 0; sq < 64; sq++)
                        if (!Cur.Board[sq].Empty && Cur.Board[sq].Id == a.PieceId) return sq;
            }
            var plane = new Plane(Vector3.up, Vector3.zero);
            float d;
            if (plane.Raycast(ray, out d)) return BoardView.WorldToSquare(ray.GetPoint(d));
            return -1;
        }

        public static string RoleName(PType t)
        {
            switch (t) { case PType.K: return "King"; case PType.Q: return "Queen"; case PType.R: return "Rook"; case PType.B: return "Bishop"; case PType.N: return "Knight"; default: return "Pawn"; }
        }

        // ---------------- moves ----------------
        IEnumerator DoMove(Move m)
        {
            if (m == null) yield break;
            busy = true; selected = -1;
            var prev = Cur;
            var next = Chess.Apply(prev, m);
            states.Add(next); moves.Add(m); keys.Add(Chess.Key(next));
            Refresh(false);
            var mover = Theme.Get(prev.Board[m.From].T, prev.WhiteToMove);
            if (!m.IsCapture) Hud.SetStatus(mover.displayName + (m.IsCastle ? " takes cover (castling)" : " advances"), false);
            yield return Director.Play(prev, m, actors);
            Time.timeScale = 1f;
            SyncActors(false);
            busy = false;
            legal = Chess.Legal(next);
            CheckOver(true);
            Refresh();
            Save();
            if (!over && Chess.InCheck(next)) SoundBank.I.Play("check");
            MaybeAI(0.25f);
        }

        void MaybeAI(float delay)
        {
            if (over || !Settings.vsComputer || Cur.WhiteToMove == Settings.humanWhite) return;
            StartCoroutine(AiRoutine(delay));
        }

        IEnumerator AiRoutine(float delay)
        {
            int tok = ++aiToken;
            thinking = true; Refresh();
            yield return new WaitForSeconds(delay);
            if (tok != aiToken) yield break;
            var s = Cur.Clone();
            int depth = Settings.level == 1 ? 1 : Settings.level == 2 ? 2 : 3;
            int noise = Settings.level == 1 ? 90 : Settings.level == 2 ? 12 : 0;
            var recent = new HashSet<string>(keys.Skip(Math.Max(0, keys.Count - 12)));
            Move result = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            result = ChessAI.BestMove(s, depth, noise, recent);
#else
            var task = System.Threading.Tasks.Task.Run(() => ChessAI.BestMove(s, depth, noise, recent));
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null) { Debug.LogException(task.Exception); thinking = false; yield break; }
            result = task.Result;
#endif
            if (tok != aiToken) yield break;
            thinking = false;
            if (result == null) yield break;
            var m = legal.Find(x => x.From == result.From && x.To == result.To && x.Promo == result.Promo);
            if (m != null) StartCoroutine(DoMove(m));
        }

        void CheckOver(bool animate)
        {
            var s = Cur;
            string title = null, sub = null; bool mate = false; bool winnerWhite = false;
            if (legal.Count == 0)
            {
                if (Chess.InCheck(s)) { mate = true; winnerWhite = !s.WhiteToMove; title = "Checkmate!"; sub = "The " + Theme.Army(winnerWhite).name + " win the battle!"; }
                else { title = "Draw"; sub = "Stalemate"; }
            }
            else if (s.HalfMove >= 100) { title = "Draw"; sub = "50-move rule"; }
            else if (keys.Count(k => k == keys[keys.Count - 1]) >= 3) { title = "Draw"; sub = "Threefold repetition"; }
            else if (Chess.InsufficientMaterial(s)) { title = "Draw"; sub = "Insufficient material"; }
            if (title == null) return;
            over = true;
            Hud.SetStatus(mate ? "Checkmate — the " + Theme.Army(winnerWhite).name + " win!" : "Draw — " + sub, true);
            if (!animate) return;
            if (mate)
            {
                int ks = Chess.KingSquare(s.Board, s.WhiteToMove);
                PieceActor king;
                if (ks >= 0 && actors.TryGetValue(s.Board[ks].Id, out king) && king != null)
                    king.StartCoroutine(king.Die(king.transform.position + king.transform.forward, true));
                foreach (var a in actors.Values) if (a != null && a.White == winnerWhite) a.Celebrate();
                bool humanWon = !Settings.vsComputer || winnerWhite == Settings.humanWhite;
                SoundBank.I.Play(humanWon ? "win" : "lose");
            }
            StartCoroutine(ShowEndLater(title, sub, mate ? 2.6f : 1.2f));
        }

        IEnumerator ShowEndLater(string title, string sub, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (over) Hud.ShowEnd(title, sub, () => NewGame(Settings), null);
        }

        // ---------------- view refresh ----------------
        void Refresh(bool status = true)
        {
            Board.ClearMarks();
            if (moves.Count > 0) { var lm = moves[moves.Count - 1]; Board.MarkSquare(lm.From, BoardView.Mark.LastMove); Board.MarkSquare(lm.To, BoardView.Mark.LastMove); }
            foreach (var a in actors.Values) if (a != null) a.SetSelected(false);
            if (selected >= 0)
            {
                Board.MarkSquare(selected, BoardView.Mark.Selected);
                PieceActor sa;
                if (actors.TryGetValue(Cur.Board[selected].Id, out sa) && sa != null) sa.SetSelected(true);
                var seen = new HashSet<int>();
                foreach (var m in legal)
                    if (m.From == selected && seen.Add(m.To))
                        Board.MarkSquare(m.To, m.IsCapture ? BoardView.Mark.Capture : BoardView.Mark.Target);
            }
            bool check = !busy && !over && Chess.InCheck(Cur);
            int ks = Chess.KingSquare(Cur.Board, Cur.WhiteToMove);
            if (check) Board.MarkSquare(ks, BoardView.Mark.Check);
            foreach (var a in actors.Values)
                if (a != null && a.Type == PType.K && !a.IsDead) a.SetAlarm(check && a.White == Cur.WhiteToMove);

            Hud.SetTurn(Cur.WhiteToMove, over);
            Hud.SetCaptured(Cur);
            Hud.SetMode(Settings.vsComputer ? "vs Computer · " + new[] { "", "Easy", "Medium", "Hard" }[Settings.level] : "2 players");
            Hud.SetButtons(CanUndo, !busy);
            if (status && !over)
            {
                string side = Theme.Army(Cur.WhiteToMove).name;
                if (Settings.vsComputer && Cur.WhiteToMove != Settings.humanWhite) Hud.SetStatus("The " + side + " are plotting…", false);
                else if (check) Hud.SetStatus("Check! Protect your " + Theme.Get(PType.K, Cur.WhiteToMove).displayName, true);
                else Hud.SetStatus(Settings.vsComputer ? "Your command" : side + " to move", false);
            }
        }

        // ---------------- save / load ----------------
        void Save()
        {
            var d = new SaveData
            {
                vsComputer = Settings.vsComputer, level = Settings.level, humanWhite = Settings.humanWhite,
                cinematic = Director.cinematic, muted = SoundBank.I.muted,
                moves = moves.Select(m => m.ToString()).ToList()
            };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        bool Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)); }
            catch { return false; }
            if (d == null) return false;
            Director.cinematic = d.cinematic;
            SoundBank.I.muted = d.muted;
            Hud.SetToggles(d.cinematic, !d.muted);
            Settings = new GameSettings { vsComputer = d.vsComputer, level = Mathf.Clamp(d.level, 1, 3), humanWhite = d.humanWhite };
            states.Clear(); moves.Clear(); keys.Clear();
            states.Add(Chess.Initial()); keys.Add(Chess.Key(Cur));
            foreach (var u in d.moves)
            {
                var m = Chess.Legal(Cur).Find(x => x.ToString() == u);
                if (m == null) break;
                var n = Chess.Apply(Cur, m);
                states.Add(n); moves.Add(m); keys.Add(Chess.Key(n));
            }
            ClearActors();
            SyncActors(true);
            Cam.SetSide(!Settings.vsComputer || Settings.humanWhite);
            legal = Chess.Legal(Cur);
            CheckOver(false);
            Refresh();
            MaybeAI(1.2f);
            return true;
        }
    }
}
