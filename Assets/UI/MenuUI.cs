using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Othello.AI;
using Othello.Core;
using Othello.Utility;

namespace Othello.UI
{
    public class MenuUI : SingletonMono<MenuUI>
    {
        public bool AutoMove => m_AutoMove;
        public int PlayerToStartNextGame => m_PlayerToStartNextGame;
        public Player WhitePlayerNextGame => m_WhitePlayerNextGame;
        public Player BlackPlayerNextGame => m_BlackPlayerNextGame;
        public int NumSimsToRun => int.Parse(m_NumGamesForSim.text);
        public enum MctsType { Sequential = 0, RootParallel = 1, TreeParallel = 2, Testing = 3 }
        
        private Board m_Board;
        private Player m_WhitePlayerNextGame;
        private Player m_BlackPlayerNextGame;
        private int m_PlayerToStartNextGame = Piece.BLACK;
        private bool m_AutoMove;
        private enum PlayerType { Human = 0, Minimax = 1, Mcts = 2, Random = 3 }

        [SerializeField] private TMP_Dropdown m_WhitePlayer;
        [SerializeField] private TMP_Dropdown m_BlackPlayer;
        [SerializeField] private TMP_Dropdown m_WhiteMtcsMode;
        [SerializeField] private TMP_Dropdown m_BlackMctsMode;
        [SerializeField] private TMP_Dropdown m_PlayerToStart;
        [SerializeField] private TMP_InputField m_BlackDepth;
        [SerializeField] private TMP_InputField m_WhiteDepth;
        [SerializeField] private TMP_InputField m_BlackIterations;
        [SerializeField] private TMP_InputField m_WhiteIterations;
        [SerializeField] private TMP_InputField m_BlackTimeLimit;
        [SerializeField] private TMP_InputField m_WhiteTimeLimit;
        [SerializeField] private TMP_InputField m_NumGamesForSim;
        [SerializeField] private Toggle m_ShowLegalMoves;
        [SerializeField] private Toggle m_EnableAutoMove;
        [SerializeField] private Toggle m_WhiteMoveOrdering;
        [SerializeField] private Toggle m_BlackMoveOrdering;
        [SerializeField] private Toggle m_WhiteIterativeDeepning;
        [SerializeField] private Toggle m_BlackIterativeDeepning;
        [SerializeField] private Toggle m_WhiteZobristHashing;
        [SerializeField] private Toggle m_BlackZobristHashing;
        [SerializeField] private TMP_Text m_WhiteCurrentDepth;
        [SerializeField] private TMP_Text m_BlackCurrentDepth;
        [SerializeField] private TMP_Text m_BlackTimeElapsed;
        [SerializeField] private TMP_Text m_WhiteTimeElapsed;
        [SerializeField] private TMP_Text m_BlackCurrentSimulation;
        [SerializeField] private TMP_Text m_WhiteCurrentSimulation;
        [SerializeField] private TMP_Text m_BlackPositionsEvaluated;
        [SerializeField] private TMP_Text m_WhitePositionsEvaluated;
        [SerializeField] private TMP_Text m_BlackCurrentWinPrediction;
        [SerializeField] private TMP_Text m_WhiteCurrentWinPrediction;
        [SerializeField] private TMP_Text m_BlackBranchesPruned;
        [SerializeField] private TMP_Text m_WhiteBranchesPruned;
        [SerializeField] private TMP_Text m_BlackWins;
        [SerializeField] private TMP_Text m_WhiteWins;
        [SerializeField] private TMP_Text m_BlackZobristSize;
        [SerializeField] private TMP_Text m_WhiteZobristSize;
        [SerializeField] private TMP_Text m_Draws;
        [SerializeField] private TMP_Text m_CurrentSim;
        [SerializeField] private TMP_Text m_CurrentPlayer;
        [SerializeField] private TMP_Text m_BlackPieceCount;
        [SerializeField] private TMP_Text m_WhitePieceCount;

        public void Setup(Board board)
        {
            m_Board = board;
            m_BlackDepth.text = "8";
            m_WhiteDepth.text = "8";
            m_BlackIterations.text = "1000";
            m_WhiteIterations.text = "1000";
            m_BlackTimeLimit.text = "1000";
            m_WhiteTimeLimit.text = "1000";
            PlayerSelection(Piece.BLACK);
            PlayerSelection(Piece.WHITE);
        }

        public void UpdateManu(Board board)
        {
            SetCurrentDepth(Player.WHITE, MiniMax.s_WhiteCurrentDepth);
            SetCurrentDepth(Player.BLACK, MiniMax.s_BlackCurrentDepth);
            SetPositionEvalCount(Player.WHITE, MiniMax.s_WhitePositionsEvaluated);
            SetPositionEvalCount(Player.BLACK, MiniMax.s_BlackPositionsEvaluated);
            SetCurrentSimulationCount(Player.WHITE, Mcts.s_WhiteIterationsRun);
            SetCurrentSimulationCount(Player.BLACK, Mcts.s_BlackIterationsRun);
            SetWinPrediction(Player.WHITE, Mcts.s_WhiteWinPrediction);
            SetWinPrediction(Player.BLACK, Mcts.s_BlackWinPrediction);
            SetTimeElapsed(Player.WHITE, AIPlayer.s_WhiteTimeElapsed.Elapsed.TotalMilliseconds);
            SetTimeElapsed(Player.BLACK, AIPlayer.s_BlackTimeElapsed.Elapsed.TotalMilliseconds);
            SetBranchPruneCount(Player.WHITE, MiniMax.s_WhiteBranchesPruned);
            SetBranchPruneCount(Player.BLACK, MiniMax.s_BlackBranchesPruned);
            SetPlayerWins(Player.WHITE, GameManager.Instance.WhiteWins);
            SetPlayerWins(Player.BLACK, GameManager.Instance.BlackWins);
            SetGameDraws(GameManager.Instance.Draws);
            SetCurrentSimCount(GameManager.Instance.NumSimsRan);
            SetZobristSize(Piece.WHITE, MiniMax.s_WhiteZobristSize);
            SetZobristSize(Piece.BLACK, MiniMax.s_BlackZobristSize);
            SetCurrentPlayer(board.GetCurrentPlayerAsString());
            SetCurrentPieceCount(Player.BLACK, board.GetPieceCount(Player.BLACK));
            SetCurrentPieceCount(Player.WHITE, board.GetPieceCount(Player.WHITE));
        }

        public void PlayerSelection(int player)
        {
            ref Player playerRef = ref (player == Piece.BLACK) ? ref m_BlackPlayerNextGame : ref m_WhitePlayerNextGame;
            var playerType = (player == Piece.BLACK) ? m_BlackPlayer.value : m_WhitePlayer.value;
            var inputFieldIterations = (player == Piece.BLACK) ? m_BlackIterations : m_WhiteIterations;
            var inputFieldDepth = (player == Piece.BLACK) ? m_BlackDepth : m_WhiteDepth;
            var inputFieldTimeLimit = (player == Piece.BLACK) ? m_BlackTimeLimit : m_WhiteTimeLimit;
            var currentDepth = (player == Piece.BLACK) ? m_BlackCurrentDepth : m_WhiteCurrentDepth;
            var inputMoveOrdering = (player == Piece.BLACK) ? m_BlackMoveOrdering : m_WhiteMoveOrdering;
            var inputIterativeDeepening = (player == Piece.BLACK) ? m_BlackIterativeDeepning : m_WhiteIterativeDeepning;
            var timeElapsed = (player == Piece.BLACK) ? m_BlackTimeElapsed : m_WhiteTimeElapsed;
            var currentSimulation = (player == Piece.BLACK) ? m_BlackCurrentSimulation : m_WhiteCurrentSimulation;
            var currentPositionsEvaluated = (player == Piece.BLACK) ? m_BlackPositionsEvaluated : m_WhitePositionsEvaluated;
            var currentWinPrediction = (player == Piece.BLACK) ? m_BlackCurrentWinPrediction : m_WhiteCurrentWinPrediction;
            var branchesPruned = (player == Piece.BLACK) ? m_BlackBranchesPruned : m_WhiteBranchesPruned;
            var mctsTypeInput = (player == Piece.BLACK) ? m_BlackMctsMode : m_WhiteMtcsMode;
            var mctsType = (player == Piece.BLACK) ? m_BlackMctsMode.value : m_WhiteMtcsMode.value;
            var zobristHashing = (player == Piece.BLACK) ? m_BlackZobristHashing : m_WhiteZobristHashing;
            var zobristSize = (player == Piece.BLACK) ? m_BlackZobristSize : m_WhiteZobristSize;

            if (inputFieldDepth.text.Length == 0)
                inputFieldDepth.text = "1";
            if (inputFieldTimeLimit.text.Length == 0)
                inputFieldTimeLimit.text = "1";
            if (inputFieldIterations.text.Length == 0)
                inputFieldIterations.text = "1";

            switch (playerType)
            {
                case (int)PlayerType.Human:
                    {
                        playerRef = new HumanPlayer(m_Board);
                        inputFieldDepth.gameObject.SetActive(false);
                        inputFieldIterations.gameObject.SetActive(false);
                        inputFieldTimeLimit.gameObject.SetActive(false);
                        inputMoveOrdering.gameObject.SetActive(false);
                        inputIterativeDeepening.gameObject.SetActive(false);
                        timeElapsed.gameObject.SetActive(false);
                        currentSimulation.gameObject.SetActive(false);
                        currentPositionsEvaluated.gameObject.SetActive(false);
                        currentWinPrediction.gameObject.SetActive(false);
                        currentDepth.gameObject.SetActive(false);
                        branchesPruned.gameObject.SetActive(false);
                        mctsTypeInput.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(false);
                        zobristSize.gameObject.SetActive(false);
                        break;
                    }
                case (int)PlayerType.Minimax:
                    {
                        var depth = int.Parse(inputFieldDepth.text);
                        var timeLimit = int.Parse(inputFieldTimeLimit.text);
                        if (depth < 1)
                            depth = 1;
                        playerRef = new AIPlayer(m_Board, new MiniMax(depth, timeLimit, inputMoveOrdering.isOn, inputIterativeDeepening.isOn, zobristHashing.isOn));
                        inputFieldDepth.gameObject.SetActive(true);
                        inputFieldIterations.gameObject.SetActive(false);
                        inputFieldTimeLimit.gameObject.SetActive(true);
                        inputMoveOrdering.gameObject.SetActive(true);
                        inputIterativeDeepening.gameObject.SetActive(true);
                        timeElapsed.gameObject.SetActive(true);
                        currentSimulation.gameObject.SetActive(false);
                        currentPositionsEvaluated.gameObject.SetActive(true);
                        currentWinPrediction.gameObject.SetActive(false);
                        currentDepth.gameObject.SetActive(true);
                        branchesPruned.gameObject.SetActive(true);
                        mctsTypeInput.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(true);
                        zobristSize.gameObject.SetActive(zobristHashing.isOn);
                        break;
                    }
                case (int)PlayerType.Mcts:
                    {
                        var timeLimit = int.Parse(inputFieldTimeLimit.text);
                        var iterations = int.Parse(inputFieldIterations.text);
                        if (iterations < 1)
                            iterations = 1;
                        playerRef = new AIPlayer(m_Board, new Mcts(iterations, timeLimit, (MctsType)mctsType));
                        inputFieldDepth.gameObject.SetActive(false);
                        inputFieldIterations.gameObject.SetActive(true);
                        inputFieldTimeLimit.gameObject.SetActive(true);
                        inputMoveOrdering.gameObject.SetActive(false);
                        inputIterativeDeepening.gameObject.SetActive(false);
                        timeElapsed.gameObject.SetActive(true);
                        currentSimulation.gameObject.SetActive(true);
                        currentPositionsEvaluated.gameObject.SetActive(false);
                        currentWinPrediction.gameObject.SetActive(true);
                        currentDepth.gameObject.SetActive(false);
                        branchesPruned.gameObject.SetActive(false);
                        mctsTypeInput.gameObject.SetActive(true);
                        zobristHashing.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(false);
                        zobristSize.gameObject.SetActive(false);
                    }
                    break;
                case (int)PlayerType.Random:
                    {
                        playerRef = new AIPlayer(m_Board, new RandomPlay());
                        inputFieldDepth.gameObject.SetActive(false);
                        inputFieldIterations.gameObject.SetActive(false);
                        inputFieldTimeLimit.gameObject.SetActive(false);
                        inputMoveOrdering.gameObject.SetActive(false);
                        inputIterativeDeepening.gameObject.SetActive(false);
                        timeElapsed.gameObject.SetActive(false);
                        currentSimulation.gameObject.SetActive(false);
                        currentPositionsEvaluated.gameObject.SetActive(false);
                        currentWinPrediction.gameObject.SetActive(false);
                        currentDepth.gameObject.SetActive(false);
                        branchesPruned.gameObject.SetActive(false);
                        mctsTypeInput.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(false);
                        zobristSize.gameObject.SetActive(false);
                        break;
                    }
            }
        }

        public void CancelGame()
        {
            if (m_WhitePlayerNextGame is AIPlayer whitePlayer)
                whitePlayer.Cts.Cancel();
            if (m_BlackPlayerNextGame is AIPlayer blackPlayer)
                blackPlayer.Cts.Cancel();
        }

        public void ToggleLegalMoves()
        {
            BoardUI.Instance.ToggleLegalMoves(m_ShowLegalMoves.isOn);
        }

        public void ToggleAutoMove()
        {
            m_AutoMove = m_EnableAutoMove.isOn;
        }

        public void SetStartingPlayer()
        {
            m_PlayerToStartNextGame = (m_PlayerToStart.value == 0) ? Piece.BLACK : Piece.WHITE;
        }

        private void SetCurrentSimCount(int count)
        {
            m_CurrentSim.text = "Sim Nr.: " + count;
        }

        private void SetZobristSize(int player, int size)
        {
            var zobristSizeDisplay = player switch
            {
                Player.BLACK => m_BlackZobristSize,
                Player.WHITE => m_WhiteZobristSize,
                _ => throw new NotImplementedException("Invalid player.")
            };
            zobristSizeDisplay.text = "Size: " + size * 32 / 8 / 1000000 + "MB";
        }

        private void SetGameDraws(int draws)
        {
            m_Draws.text = "Draws: " + draws;
        }

        private void SetPlayerWins(int player, int wins)
        {
            var winsDisplay = player switch
            {
                Player.BLACK => m_BlackWins,
                Player.WHITE => m_WhiteWins,
                _ => throw new NotImplementedException("Invalid player.")
            };
            winsDisplay.text = "White wins: " + wins;
        }

        private void SetBranchPruneCount(int player, int branchPruneCount)
        {
            var branchPruneDisplay = player switch
            {
                Player.BLACK => m_BlackBranchesPruned,
                Player.WHITE => m_WhiteBranchesPruned,
                _ => throw new NotImplementedException("Invalid player.")
            };
            branchPruneDisplay.text = "Branches Pruned: " + branchPruneCount;
        }

        private void SetTimeElapsed(int player, double timeMillis)
        {
            var timeDisplay = player switch
            {
                Player.BLACK => m_BlackTimeElapsed,
                Player.WHITE => m_WhiteTimeElapsed,
                _ => throw new NotImplementedException("Invalid player.")
            };
            timeDisplay.text = "Time Elapsed: " + timeMillis.ToString("F0") + "ms";
        }

        private void SetWinPrediction(int player, double winPrediction)
        {
            var winPredictionDisplay = player switch
            {
                Player.BLACK => m_BlackCurrentWinPrediction,
                Player.WHITE => m_WhiteCurrentWinPrediction,
                _ => throw new NotImplementedException("Invalid player.")
            };
            winPredictionDisplay.text = "Current Win Prediction: " + winPrediction.ToString("0.##") + "%";
        }

        private void SetCurrentSimulationCount(int player, int currentSimCount)
        {
            var simCountDisplay = player switch
            {
                Player.BLACK => m_BlackCurrentSimulation,
                Player.WHITE => m_WhiteCurrentSimulation,
                _ => throw new NotImplementedException("Invalid player.")
            };
            simCountDisplay.text = "Current Simulation: " + currentSimCount;
        }

        private void SetPositionEvalCount(int player, int posEvalCount)
        {
            var posEvalDisplay = player switch
            {
                Player.BLACK => m_BlackPositionsEvaluated,
                Player.WHITE => m_WhitePositionsEvaluated,
                _ => throw new NotImplementedException("Invalid player.")
            };
            posEvalDisplay.text = "Positions Evaluated: " + posEvalCount;
        }

        private void SetCurrentDepth(int player, int currentDepth)
        {
            var depthDisplay = player switch
            {
                Player.BLACK => m_BlackCurrentDepth,
                Player.WHITE => m_WhiteCurrentDepth,
                _ => throw new NotImplementedException("Invalid player.")
            };
            depthDisplay.text = "Current depth: " + currentDepth;
        }

        private void SetCurrentPieceCount(int player, int pieceCount)
        {
            switch (player)
            {
                case Player.BLACK:
                    m_BlackPieceCount.text = "Black: " + pieceCount;
                    break;
                case Player.WHITE:
                    m_WhitePieceCount.text = "White: " + pieceCount;
                    break;
            }
        }

        private void SetCurrentPlayer(string currentPlayer)
        {
            m_CurrentPlayer.text = "Player " + currentPlayer;
        }
        
    }
}