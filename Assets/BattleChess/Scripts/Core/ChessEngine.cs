// Battle Chess — rules engine + AI (pure C#, no Unity dependencies).
// Ported from the web prototype. Verified with standard perft positions (see ChessSelfTest).
using System;
using System.Collections.Generic;
using System.Text;

namespace BattleChess.Core
{
    public enum PType : byte { None = 0, P, N, B, R, Q, K }

    [Serializable]
    public struct Piece
    {
        public PType T;
        public bool White;
        public int Id;   // stable identity, used to keep the same 3D character across moves
        public Piece(PType t, bool white, int id) { T = t; White = white; Id = id; }
        public bool Empty => T == PType.None;
    }

    public sealed class Move
    {
        public int From, To;
        public PType Promo;          // promotion piece, None if not a promotion
        public PType Captured;       // captured piece type, None if quiet
        public int EpCapture = -1;   // square of pawn removed by en passant
        public int RookFrom = -1, RookTo = -1; // castling rook squares
        public bool DoublePush;
        internal int Order;
        public bool IsCapture => Captured != PType.None;
        public bool IsCastle => RookFrom >= 0;
        public override string ToString() =>
            Chess.SquareName(From) + Chess.SquareName(To) + (Promo != PType.None ? char.ToLower(Chess.Letter(Promo)).ToString() : "");
    }

    public sealed class GameState
    {
        public readonly Piece[] Board = new Piece[64]; // index = row*8+col, row 0 = rank 8
        public bool WhiteToMove = true;
        public int Castling = 15;   // bits: 1=K 2=Q 4=k 8=q
        public int EnPassant = -1;  // target square or -1
        public int HalfMove;

        public GameState Clone()
        {
            var s = new GameState();
            Array.Copy(Board, s.Board, 64);
            s.WhiteToMove = WhiteToMove; s.Castling = Castling; s.EnPassant = EnPassant; s.HalfMove = HalfMove;
            return s;
        }
    }

    public static class Chess
    {
        public const int CK = 1, CQ = 2, Ck = 4, Cq = 8;
        static readonly int[][] Knight = { new[]{-2,-1}, new[]{-2,1}, new[]{-1,-2}, new[]{-1,2}, new[]{1,-2}, new[]{1,2}, new[]{2,-1}, new[]{2,1} };
        static readonly int[][] Diag = { new[]{-1,-1}, new[]{-1,1}, new[]{1,-1}, new[]{1,1} };
        static readonly int[][] Orth = { new[]{-1,0}, new[]{1,0}, new[]{0,-1}, new[]{0,1} };
        static readonly int[][] All = { new[]{-1,-1}, new[]{-1,1}, new[]{1,-1}, new[]{1,1}, new[]{-1,0}, new[]{1,0}, new[]{0,-1}, new[]{0,1} };
        static readonly PType[] PromoOrder = { PType.Q, PType.R, PType.B, PType.N };

        public static GameState Initial()
        {
            var s = new GameState();
            PType[] back = { PType.R, PType.N, PType.B, PType.Q, PType.K, PType.B, PType.N, PType.R };
            int id = 0;
            for (int c = 0; c < 8; c++)
            {
                s.Board[c] = new Piece(back[c], false, id++);
                s.Board[8 + c] = new Piece(PType.P, false, id++);
                s.Board[48 + c] = new Piece(PType.P, true, id++);
                s.Board[56 + c] = new Piece(back[c], true, id++);
            }
            return s;
        }

        public static GameState FromFen(string fen)
        {
            var parts = fen.Trim().Split(' ');
            var s = new GameState();
            int sq = 0, id = 0;
            foreach (char ch in parts[0])
            {
                if (ch == '/') continue;
                if (char.IsDigit(ch)) { sq += ch - '0'; continue; }
                s.Board[sq++] = new Piece(FromLetter(ch), char.IsUpper(ch), id++);
            }
            s.WhiteToMove = parts.Length < 2 || parts[1] == "w";
            s.Castling = 0;
            if (parts.Length > 2)
            {
                if (parts[2].Contains("K")) s.Castling |= CK;
                if (parts[2].Contains("Q")) s.Castling |= CQ;
                if (parts[2].Contains("k")) s.Castling |= Ck;
                if (parts[2].Contains("q")) s.Castling |= Cq;
            }
            s.EnPassant = -1;
            if (parts.Length > 3 && parts[3] != "-") s.EnPassant = (8 - (parts[3][1] - '0')) * 8 + (parts[3][0] - 'a');
            return s;
        }

        public static PType FromLetter(char ch)
        {
            switch (char.ToLower(ch)) { case 'p': return PType.P; case 'n': return PType.N; case 'b': return PType.B; case 'r': return PType.R; case 'q': return PType.Q; case 'k': return PType.K; }
            return PType.None;
        }
        public static char Letter(PType t) => " PNBRQK"[(int)t];
        public static string SquareName(int sq) => ((char)('a' + (sq & 7))).ToString() + (8 - (sq >> 3));
        public static int Value(PType t)
        {
            switch (t) { case PType.P: return 100; case PType.N: return 320; case PType.B: return 330; case PType.R: return 500; case PType.Q: return 900; }
            return 0;
        }

        static bool On(int r, int c) => r >= 0 && r < 8 && c >= 0 && c < 8;

        public static bool IsAttacked(Piece[] b, int sq, bool byWhite)
        {
            int r = sq >> 3, c = sq & 7;
            int pr = byWhite ? r + 1 : r - 1;
            if (pr >= 0 && pr < 8)
                for (int dc = -1; dc <= 1; dc += 2)
                {
                    int cc = c + dc;
                    if (cc < 0 || cc > 7) continue;
                    var p = b[pr * 8 + cc];
                    if (p.T == PType.P && p.White == byWhite) return true;
                }
            foreach (var d in Knight)
            {
                int rr = r + d[0], cc = c + d[1];
                if (!On(rr, cc)) continue;
                var p = b[rr * 8 + cc];
                if (p.T == PType.N && p.White == byWhite) return true;
            }
            foreach (var d in All)
            {
                int rr = r + d[0], cc = c + d[1];
                if (!On(rr, cc)) continue;
                var p = b[rr * 8 + cc];
                if (p.T == PType.K && p.White == byWhite) return true;
            }
            foreach (var d in Diag)
            {
                int rr = r + d[0], cc = c + d[1];
                while (On(rr, cc))
                {
                    var p = b[rr * 8 + cc];
                    if (!p.Empty) { if (p.White == byWhite && (p.T == PType.B || p.T == PType.Q)) return true; break; }
                    rr += d[0]; cc += d[1];
                }
            }
            foreach (var d in Orth)
            {
                int rr = r + d[0], cc = c + d[1];
                while (On(rr, cc))
                {
                    var p = b[rr * 8 + cc];
                    if (!p.Empty) { if (p.White == byWhite && (p.T == PType.R || p.T == PType.Q)) return true; break; }
                    rr += d[0]; cc += d[1];
                }
            }
            return false;
        }

        public static int KingSquare(Piece[] b, bool white)
        {
            for (int i = 0; i < 64; i++) if (b[i].T == PType.K && b[i].White == white) return i;
            return -1;
        }

        public static List<Move> Generate(GameState s, bool capturesOnly = false)
        {
            var b = s.Board; bool me = s.WhiteToMove;
            var list = new List<Move>(48);
            for (int sq = 0; sq < 64; sq++)
            {
                var p = b[sq];
                if (p.Empty || p.White != me) continue;
                int r = sq >> 3, c = sq & 7;
                switch (p.T)
                {
                    case PType.P:
                    {
                        int d = me ? -1 : 1, start = me ? 6 : 1, last = me ? 0 : 7, r1 = r + d;
                        if (r1 < 0 || r1 > 7) break;
                        int t = r1 * 8 + c;
                        if (b[t].Empty && (!capturesOnly || r1 == last))
                        {
                            AddPawn(list, sq, t, PType.None, r1 == last);
                            if (!capturesOnly && r == start && b[(r + 2 * d) * 8 + c].Empty)
                                list.Add(new Move { From = sq, To = (r + 2 * d) * 8 + c, DoublePush = true });
                        }
                        for (int dc = -1; dc <= 1; dc += 2)
                        {
                            int cc = c + dc; if (cc < 0 || cc > 7) continue;
                            int t2 = r1 * 8 + cc;
                            if (!b[t2].Empty && b[t2].White != me) AddPawn(list, sq, t2, b[t2].T, r1 == last);
                            else if (t2 == s.EnPassant) list.Add(new Move { From = sq, To = t2, EpCapture = r * 8 + cc, Captured = PType.P });
                        }
                        break;
                    }
                    case PType.N:
                    case PType.K:
                    {
                        foreach (var d in (p.T == PType.N ? Knight : All))
                        {
                            int rr = r + d[0], cc = c + d[1];
                            if (!On(rr, cc)) continue;
                            int t = rr * 8 + cc; var q = b[t];
                            if (q.Empty) { if (!capturesOnly) list.Add(new Move { From = sq, To = t }); }
                            else if (q.White != me) list.Add(new Move { From = sq, To = t, Captured = q.T });
                        }
                        if (p.T == PType.K && !capturesOnly)
                        {
                            int home = me ? 60 : 4;
                            if (sq == home && !IsAttacked(b, sq, !me))
                            {
                                bool kR = (s.Castling & (me ? CK : Ck)) != 0, qR = (s.Castling & (me ? CQ : Cq)) != 0;
                                var rk = b[sq + 3]; var rq = b[sq - 4];
                                if (kR && b[sq + 1].Empty && b[sq + 2].Empty && rk.T == PType.R && rk.White == me && !IsAttacked(b, sq + 1, !me) && !IsAttacked(b, sq + 2, !me))
                                    list.Add(new Move { From = sq, To = sq + 2, RookFrom = sq + 3, RookTo = sq + 1 });
                                if (qR && b[sq - 1].Empty && b[sq - 2].Empty && b[sq - 3].Empty && rq.T == PType.R && rq.White == me && !IsAttacked(b, sq - 1, !me) && !IsAttacked(b, sq - 2, !me))
                                    list.Add(new Move { From = sq, To = sq - 2, RookFrom = sq - 4, RookTo = sq - 1 });
                            }
                        }
                        break;
                    }
                    default:
                    {
                        var dirs = p.T == PType.B ? Diag : p.T == PType.R ? Orth : All;
                        foreach (var d in dirs)
                        {
                            int rr = r + d[0], cc = c + d[1];
                            while (On(rr, cc))
                            {
                                int t = rr * 8 + cc; var q = b[t];
                                if (q.Empty) { if (!capturesOnly) list.Add(new Move { From = sq, To = t }); }
                                else { if (q.White != me) list.Add(new Move { From = sq, To = t, Captured = q.T }); break; }
                                rr += d[0]; cc += d[1];
                            }
                        }
                        break;
                    }
                }
            }
            return list;
        }

        static void AddPawn(List<Move> list, int from, int to, PType cap, bool promo)
        {
            if (promo) foreach (var pr in PromoOrder) list.Add(new Move { From = from, To = to, Captured = cap, Promo = pr });
            else list.Add(new Move { From = from, To = to, Captured = cap });
        }

        public static GameState Apply(GameState s, Move m)
        {
            var n = s.Clone(); var b = n.Board;
            var p = b[m.From]; var captured = b[m.To];
            b[m.To] = m.Promo != PType.None ? new Piece(m.Promo, p.White, p.Id) : p;
            b[m.From] = default(Piece);
            if (m.EpCapture >= 0) { captured = b[m.EpCapture]; b[m.EpCapture] = default(Piece); }
            if (m.RookFrom >= 0) { b[m.RookTo] = b[m.RookFrom]; b[m.RookFrom] = default(Piece); }
            if (p.T == PType.K) n.Castling &= p.White ? ~(CK | CQ) : ~(Ck | Cq);
            foreach (int x in new[] { m.From, m.To })
            {
                if (x == 63) n.Castling &= ~CK; else if (x == 56) n.Castling &= ~CQ;
                else if (x == 7) n.Castling &= ~Ck; else if (x == 0) n.Castling &= ~Cq;
            }
            n.EnPassant = m.DoublePush ? (m.From + m.To) / 2 : -1;
            n.HalfMove = (p.T == PType.P || !captured.Empty) ? 0 : s.HalfMove + 1;
            n.WhiteToMove = !s.WhiteToMove;
            return n;
        }

        public static List<Move> Legal(GameState s, bool capturesOnly = false)
        {
            var all = Generate(s, capturesOnly);
            var res = new List<Move>(all.Count);
            bool me = s.WhiteToMove;
            foreach (var m in all)
            {
                var n = Apply(s, m);
                if (!IsAttacked(n.Board, KingSquare(n.Board, me), !me)) res.Add(m);
            }
            return res;
        }

        public static bool InCheck(GameState s) => IsAttacked(s.Board, KingSquare(s.Board, s.WhiteToMove), !s.WhiteToMove);

        public static long Perft(GameState s, int depth)
        {
            if (depth == 0) return 1;
            long n = 0;
            foreach (var m in Legal(s)) n += Perft(Apply(s, m), depth - 1);
            return n;
        }

        public static string Key(GameState s)
        {
            var sb = new StringBuilder(72);
            foreach (var p in s.Board) sb.Append(p.Empty ? '.' : (p.White ? Letter(p.T) : char.ToLower(Letter(p.T))));
            sb.Append(s.WhiteToMove ? 'w' : 'b').Append(s.Castling).Append(':').Append(s.EnPassant);
            return sb.ToString();
        }

        public static bool InsufficientMaterial(GameState s)
        {
            int count = 0; PType only = PType.None;
            foreach (var p in s.Board) if (!p.Empty && p.T != PType.K) { count++; only = p.T; }
            return count == 0 || (count == 1 && (only == PType.N || only == PType.B));
        }
    }

    public static class ChessAI
    {
        const int Mate = 100000;
        static readonly int[][] Pst = {
            null,
            new[]{0,0,0,0,0,0,0,0,50,50,50,50,50,50,50,50,10,10,20,30,30,20,10,10,5,5,10,25,25,10,5,5,0,0,0,20,20,0,0,0,5,-5,-10,0,0,-10,-5,5,5,10,10,-20,-20,10,10,5,0,0,0,0,0,0,0,0},
            new[]{-50,-40,-30,-30,-30,-30,-40,-50,-40,-20,0,0,0,0,-20,-40,-30,0,10,15,15,10,0,-30,-30,5,15,20,20,15,5,-30,-30,0,15,20,20,15,0,-30,-30,5,10,15,15,10,5,-30,-40,-20,0,5,5,0,-20,-40,-50,-40,-30,-30,-30,-30,-40,-50},
            new[]{-20,-10,-10,-10,-10,-10,-10,-20,-10,0,0,0,0,0,0,-10,-10,0,5,10,10,5,0,-10,-10,5,5,10,10,5,5,-10,-10,0,10,10,10,10,0,-10,-10,10,10,10,10,10,10,-10,-10,5,0,0,0,0,5,-10,-20,-10,-10,-10,-10,-10,-10,-20},
            new[]{0,0,0,0,0,0,0,0,5,10,10,10,10,10,10,5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,0,0,0,5,5,0,0,0},
            new[]{-20,-10,-10,-5,-5,-10,-10,-20,-10,0,0,0,0,0,0,-10,-10,0,5,5,5,5,0,-10,-5,0,5,5,5,5,0,-5,0,0,5,5,5,5,0,-5,-10,5,5,5,5,5,0,-10,-10,0,5,0,0,0,0,-10,-20,-10,-10,-5,-5,-10,-10,-20},
            new[]{-30,-40,-40,-50,-50,-40,-40,-30,-30,-40,-40,-50,-50,-40,-40,-30,-30,-40,-40,-50,-50,-40,-40,-30,-30,-40,-40,-50,-50,-40,-40,-30,-20,-30,-30,-40,-40,-30,-30,-20,-10,-20,-20,-20,-20,-20,-20,-10,20,20,0,0,0,0,20,20,20,30,10,0,0,10,30,20}
        };

        public static int Evaluate(GameState s)
        {
            int sc = 0;
            for (int i = 0; i < 64; i++)
            {
                var p = s.Board[i]; if (p.Empty) continue;
                int idx = p.White ? i : (7 - (i >> 3)) * 8 + (i & 7);
                int v = Chess.Value(p.T) + Pst[(int)p.T][idx];
                sc += p.White ? v : -v;
            }
            return s.WhiteToMove ? sc : -sc;
        }

        static void Order(List<Move> ms, GameState s)
        {
            foreach (var m in ms)
                m.Order = (m.IsCapture ? 10 * Chess.Value(m.Captured) - Chess.Value(s.Board[m.From].T) + 1000 : 0) + (m.Promo == PType.Q ? 900 : 0);
            ms.Sort((a, b) => b.Order.CompareTo(a.Order));
        }

        static int Quiesce(GameState s, int alpha, int beta, int qd)
        {
            int stand = Evaluate(s);
            if (stand >= beta) return stand;
            if (stand > alpha) alpha = stand;
            if (qd >= 4) return alpha;
            var ms = Chess.Legal(s, true); Order(ms, s);
            foreach (var m in ms)
            {
                int v = -Quiesce(Chess.Apply(s, m), -beta, -alpha, qd + 1);
                if (v >= beta) return v;
                if (v > alpha) alpha = v;
            }
            return alpha;
        }

        static int Negamax(GameState s, int depth, int alpha, int beta, int ply)
        {
            if (depth <= 0) return Quiesce(s, alpha, beta, 0);
            var ms = Chess.Legal(s);
            if (ms.Count == 0) return Chess.InCheck(s) ? -Mate + ply : 0;
            if (s.HalfMove >= 100) return 0;
            Order(ms, s);
            foreach (var m in ms)
            {
                int v = -Negamax(Chess.Apply(s, m), depth - 1, -beta, -alpha, ply + 1);
                if (v >= beta) return v;
                if (v > alpha) alpha = v;
            }
            return alpha;
        }

        /// <summary>Pick a move. depth 1..4, noise in centipawns makes easy levels play loosely.</summary>
        public static Move BestMove(GameState s, int depth, int noise, ICollection<string> recentPositions = null, int seed = 0)
        {
            var rng = new Random(seed == 0 ? Environment.TickCount : seed);
            var ms = Chess.Legal(s); Order(ms, s);
            Move best = null; int bestV = int.MinValue, alpha = -Mate * 2;
            foreach (var m in ms)
            {
                var n = Chess.Apply(s, m);
                int v = -Negamax(n, depth - 1, -Mate * 2, noise > 0 ? Mate * 2 : -alpha, 1);
                if (recentPositions != null && v > -50 && recentPositions.Contains(Chess.Key(n))) v -= 60;
                if (noise > 0) v += rng.Next(noise);
                if (v > bestV) { bestV = v; best = m; }
                if (v > alpha) alpha = v;
            }
            return best;
        }
    }
}
