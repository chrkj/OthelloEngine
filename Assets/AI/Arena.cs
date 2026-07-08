using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Othello.Core;

namespace Othello.AI
{
    /// <summary>A single competitor: a display name and a factory that makes a fresh engine per game.</summary>
    public class EngineEntry
    {
        public readonly string Name;
        public readonly Func<ISearchEngine> Factory;

        public EngineEntry(string name, Func<ISearchEngine> factory)
        {
            Name = name;
            Factory = factory;
        }
    }

    /// <summary>The outcome of a set of games between two entrants.</summary>
    public class MatchResult
    {
        public EngineEntry EntrantA;
        public EngineEntry EntrantB;
        public int Games;
        public int AWins;
        public int BWins;
        public int Draws;

        public double AScore => Games > 0 ? (AWins + 0.5 * Draws) / Games : 0;
    }

    /// <summary>A leaderboard row, aggregated across all of an entrant's matches.</summary>
    public class Standing
    {
        public string Name;
        public int Games;
        public int Wins;
        public int Draws;
        public int Losses;
        public double Points => Wins + 0.5 * Draws;
        public double Score => Games > 0 ? Points / Games : 0;
    }

    /// <summary>
    /// Headless self-play helpers. Pure game logic — the caller drives the pacing, which lets a
    /// coroutine step games one ply at a time (needed so GPU rollouts run on Unity's main thread).
    /// </summary>
    public static class Arena
    {
        public const int GameRunning = -1;

        public static Board CreateStartBoard()
        {
            var board = new Board();
            board.ResetBoard(Piece.BLACK);
            board.LoadStartPosition();
            return board;
        }

        /// <summary>Advances the game by one ply: a move, or a pass when the side to move has none.</summary>
        public static void PlayPly(Board board, ISearchEngine black, ISearchEngine white)
        {
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            board.GenerateLegalMoves(ref legalMoves);
            if (legalMoves.Length == 0)
            {
                board.ChangePlayer();
                return;
            }

            var engine = board.IsWhiteToMove ? white : black;
            var move = engine.StartSearch(board).BestMove;
            if (move == Move.NULLMOVE)
                move = legalMoves[0];

            board.MakeMove(move);
            board.ChangePlayer();
        }

        /// <summary>Plays a full game synchronously. Black moves first. Returns the winning piece color.</summary>
        public static int PlayGame(ISearchEngine black, ISearchEngine white)
        {
            var board = CreateStartBoard();
            while (board.GetBoardState() == GameRunning)
                PlayPly(board, black, white);
            return board.GetWinner();
        }

        public static List<Standing> BuildStandings(IList<EngineEntry> pool, IEnumerable<MatchResult> matches)
        {
            var byName = new Dictionary<string, Standing>();
            foreach (var entry in pool)
                byName[entry.Name] = new Standing { Name = entry.Name };

            foreach (var m in matches)
            {
                var sa = byName[m.EntrantA.Name];
                var sb = byName[m.EntrantB.Name];
                sa.Wins += m.AWins; sa.Losses += m.BWins; sa.Draws += m.Draws; sa.Games += m.Games;
                sb.Wins += m.BWins; sb.Losses += m.AWins; sb.Draws += m.Draws; sb.Games += m.Games;
            }

            var standings = new List<Standing>(byName.Values);
            standings.Sort((x, y) => y.Score.CompareTo(x.Score));
            return standings;
        }

        public static string MatchesToCsv(IEnumerable<MatchResult> matches)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EntrantA,EntrantB,Games,A_Wins,B_Wins,Draws,A_Score");
            foreach (var m in matches)
                sb.AppendLine($"{Escape(m.EntrantA.Name)},{Escape(m.EntrantB.Name)},{m.Games}," +
                              $"{m.AWins},{m.BWins},{m.Draws},{Num(m.AScore)}");
            return sb.ToString();
        }

        public static string StandingsToCsv(IEnumerable<Standing> standings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rank,Name,Games,Wins,Draws,Losses,Points,Score");
            var rank = 1;
            foreach (var s in standings)
                sb.AppendLine($"{rank++},{Escape(s.Name)},{s.Games},{s.Wins},{s.Draws}," +
                              $"{s.Losses},{Num(s.Points)},{Num(s.Score)}");
            return sb.ToString();
        }

        private static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Escape(string field)
        {
            if (field.IndexOf(',') < 0 && field.IndexOf('"') < 0 && field.IndexOf('\n') < 0)
                return field;
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
    }
}
