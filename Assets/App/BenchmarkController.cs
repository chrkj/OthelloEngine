using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Othello.AI;
using Othello.Core;
using Console = Othello.UI.Console;

namespace Othello.App
{
    /// <summary>
    /// Drives the in-app AI benchmark. CPU-only pools run on a background thread (games in parallel, so
    /// the UI stays smooth); pools containing a GPU engine run in a main-thread coroutine, ply by ply,
    /// since compute dispatches must happen on the main thread. Colors alternate each game to cancel the
    /// first-move bias, and both paths report through a shared Finish step.
    /// </summary>
    public class BenchmarkController : MonoBehaviour
    {
        private enum Mode { RoundRobin = 0, HeadToHead = 1 }

        [Header("Panel")]
        [SerializeField] private GameObject m_Panel;

        [Header("Setup controls")]
        [Tooltip("Options must be, in order: Round-robin, Head-to-head")]
        [SerializeField] private TMP_Dropdown m_ModeDropdown;
        [SerializeField] private TMP_InputField m_GamesInput;
        [SerializeField] private Button m_AddButton;
        [SerializeField] private Transform m_RowContainer;
        [SerializeField] private BenchmarkEngineRow m_RowPrefab;

        [Header("Run controls")]
        [SerializeField] private Button m_RunButton;
        [SerializeField] private Button m_CancelButton;
        [SerializeField] private Slider m_ProgressBar;
        [SerializeField] private TMP_Text m_StatusText;
        [Tooltip("Spinner GameObject (e.g. with a SimpleRotate) shown while the benchmark runs.")]
        [SerializeField] private GameObject m_Spinner;

        [Header("GPU")]
        [Tooltip("Assign Assets/AI/MctsCompute so GPU MCTS engines can run.")]
        [SerializeField] private ComputeShader m_ComputeShader;

        [Tooltip("Seconds of work per frame before yielding, to keep the app responsive during a run.")]
        [SerializeField] private float m_FrameBudgetSeconds = 0.01f;

        private readonly List<BenchmarkEngineRow> m_Rows = new();
        private bool m_Running;
        private volatile bool m_Cancel;
        private int m_GamesDone;
        private int m_GamesTotal;
        private List<EngineEntry> m_PendingPool;
        private Task<List<MatchResult>> m_BackgroundTask;

        private void Start()
        {
            if (m_AddButton != null) m_AddButton.onClick.AddListener(AddRow);
            if (m_RunButton != null) m_RunButton.onClick.AddListener(OnRun);
            if (m_CancelButton != null) m_CancelButton.onClick.AddListener(() => m_Cancel = true);
            if (m_Panel != null) m_Panel.SetActive(false);
            if (m_Spinner != null) m_Spinner.SetActive(false);
            SetStatus("");
        }

        /// <summary>Hook a menu button's OnClick to this to show/hide the benchmark panel.</summary>
        public void TogglePanel()
        {
            if (m_Panel != null)
                m_Panel.SetActive(!m_Panel.activeSelf);
        }

        public void AddRow()
        {
            if (m_RowPrefab == null || m_RowContainer == null)
                return;
            var row = Instantiate(m_RowPrefab, m_RowContainer);
            row.Init(RemoveRow);
            m_Rows.Add(row);
        }

        private void RemoveRow(BenchmarkEngineRow row)
        {
            m_Rows.Remove(row);
            Destroy(row.gameObject);
        }

        private void OnRun()
        {
            if (m_Running)
                return;

            var configs = new List<EngineConfig>();
            foreach (var row in m_Rows)
                configs.Add(row.ToConfig());

            var mode = (Mode)(m_ModeDropdown != null ? m_ModeDropdown.value : 0);
            var error = Validate(configs, mode);
            if (error != null)
            {
                SetStatus(error);
                return;
            }

            var pool = BuildPool(configs);
            var gamesPerPair = ParseGames();
            var pairings = BuildPairings(pool.Count, mode);

            m_PendingPool = pool;
            m_GamesTotal = pairings.Count * gamesPerPair;
            m_GamesDone = 0;
            m_Cancel = false;
            m_Running = true;
            if (m_RunButton != null)
                m_RunButton.interactable = false;
            if (m_Spinner != null) m_Spinner.SetActive(true);

            if (RequiresGpu(configs))
                StartCoroutine(RunRoutine(pool, pairings, gamesPerPair)); // GPU: main thread, ply by ply
            else
                m_BackgroundTask = Task.Run(() => PlayMatches(pool, pairings, gamesPerPair)); // CPU: background, parallel
        }

        private string Validate(List<EngineConfig> configs, Mode mode)
        {
            if (configs.Count == 0)
                return "Add some engines first.";
            if (mode == Mode.HeadToHead && configs.Count != 2)
                return "Pick exactly two engines for head-to-head.";
            if (mode == Mode.RoundRobin && configs.Count < 2)
                return "Add at least two engines for a round-robin.";
            foreach (var config in configs)
                if (config.RequiresGpu && m_ComputeShader == null)
                    return "Assign the compute shader to run GPU MCTS.";
            return null;
        }

        // Ensures unique entrant names even when two configs are identical, so standings don't merge them.
        private List<EngineEntry> BuildPool(List<EngineConfig> configs)
        {
            var counts = new Dictionary<string, int>();
            var pool = new List<EngineEntry>(configs.Count);
            foreach (var config in configs)
            {
                var name = config.DisplayName();
                if (counts.TryGetValue(name, out var seen))
                {
                    counts[name] = seen + 1;
                    name = $"{name} #{seen + 1}";
                }
                else
                {
                    counts[name] = 1;
                }
                pool.Add(config.ToEntry(name, m_ComputeShader));
            }
            return pool;
        }

        private int ParseGames()
            => m_GamesInput != null && int.TryParse(m_GamesInput.text, out var value) ? Mathf.Max(1, value) : 20;

        private static List<(int a, int b)> BuildPairings(int count, Mode mode)
        {
            var pairings = new List<(int, int)>();
            if (mode == Mode.HeadToHead)
            {
                pairings.Add((0, 1));
                return pairings;
            }
            for (var i = 0; i < count; i++)
                for (var j = i + 1; j < count; j++)
                    pairings.Add((i, j));
            return pairings;
        }

        // GPU path: plays ply by ply on the main thread, yielding on a frame-time budget.
        private IEnumerator RunRoutine(List<EngineEntry> pool, List<(int a, int b)> pairings, int gamesPerPair)
        {
            var matches = new List<MatchResult>();
            var lastYield = Time.realtimeSinceStartup;

            foreach (var (ai, bi) in pairings)
            {
                int aWins = 0, bWins = 0, draws = 0;
                for (var g = 0; g < gamesPerPair && !m_Cancel; g++)
                {
                    var aIsBlack = (g & 1) == 0;
                    var black = (aIsBlack ? pool[ai] : pool[bi]).Factory();
                    var white = (aIsBlack ? pool[bi] : pool[ai]).Factory();

                    var board = Arena.CreateStartBoard();
                    while (board.GetBoardState() == Arena.GameRunning && !m_Cancel)
                    {
                        Arena.PlayPly(board, black, white);
                        if (Time.realtimeSinceStartup - lastYield > m_FrameBudgetSeconds)
                        {
                            yield return null;
                            lastYield = Time.realtimeSinceStartup;
                        }
                    }
                    if (m_Cancel)
                        break;

                    var winner = board.GetWinner();
                    if (winner == Piece.EMPTY) draws++;
                    else if ((winner == Piece.BLACK) == aIsBlack) aWins++;
                    else bWins++;
                    m_GamesDone++;
                }

                matches.Add(new MatchResult
                {
                    EntrantA = pool[ai], EntrantB = pool[bi], Games = gamesPerPair,
                    AWins = aWins, BWins = bWins, Draws = draws
                });
                if (m_Cancel)
                    break;
            }

            Finish(matches);
        }

        // CPU path: plays every pairing on a background thread, games within a pairing in parallel.
        private List<MatchResult> PlayMatches(List<EngineEntry> pool, List<(int a, int b)> pairings, int gamesPerPair)
        {
            var matches = new List<MatchResult>();
            foreach (var (ai, bi) in pairings)
            {
                int aWins = 0, bWins = 0, draws = 0;
                Parallel.For(0, gamesPerPair, g =>
                {
                    if (m_Cancel)
                        return;
                    var aIsBlack = (g & 1) == 0;
                    var black = (aIsBlack ? pool[ai] : pool[bi]).Factory();
                    var white = (aIsBlack ? pool[bi] : pool[ai]).Factory();
                    var winner = Arena.PlayGame(black, white);
                    if (winner == Piece.EMPTY) Interlocked.Increment(ref draws);
                    else if ((winner == Piece.BLACK) == aIsBlack) Interlocked.Increment(ref aWins);
                    else Interlocked.Increment(ref bWins);
                    Interlocked.Increment(ref m_GamesDone);
                });

                matches.Add(new MatchResult
                {
                    EntrantA = pool[ai], EntrantB = pool[bi], Games = gamesPerPair,
                    AWins = aWins, BWins = bWins, Draws = draws
                });
                if (m_Cancel)
                    break;
            }
            return matches;
        }

        private void Update()
        {
            if (m_Running)
                UpdateProgress();

            if (m_BackgroundTask == null || !m_BackgroundTask.IsCompleted)
                return;

            var task = m_BackgroundTask;
            m_BackgroundTask = null;
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                m_Running = false;
                if (m_RunButton != null) m_RunButton.interactable = true;
                if (m_Spinner != null) m_Spinner.SetActive(false);
                SetStatus("Benchmark failed — see console.");
                return;
            }
            Finish(task.Result);
        }

        // Shared completion for both paths, always on the main thread.
        private void Finish(List<MatchResult> matches)
        {
            m_Running = false;
            if (m_RunButton != null) m_RunButton.interactable = true;
            if (m_Spinner != null) m_Spinner.SetActive(false);
            if (m_Cancel)
            {
                SetStatus("Cancelled.");
                return;
            }
            UpdateProgress();
            ShowResults(Arena.BuildStandings(m_PendingPool, matches));
        }

        private static bool RequiresGpu(List<EngineConfig> configs)
        {
            foreach (var config in configs)
                if (config.RequiresGpu)
                    return true;
            return false;
        }

        private void UpdateProgress()
        {
            if (m_ProgressBar != null)
                m_ProgressBar.value = m_GamesTotal > 0 ? (float)m_GamesDone / m_GamesTotal : 0f;
            SetStatus($"{m_GamesDone} / {m_GamesTotal}");
        }

        private void ShowResults(List<Standing> standings)
        {
            SetStatus("Done.");
            Console.Log("■■■ Benchmark complete ■■■", Color.cyan);
            foreach (var s in standings)
                Console.Log($"{s.Name}: {s.Wins}-{s.Draws}-{s.Losses} ({s.Score * 100:0.#}%)", Color.cyan);
        }

        private void SetStatus(string text)
        {
            if (m_StatusText != null)
                m_StatusText.text = text;
        }
    }
}
