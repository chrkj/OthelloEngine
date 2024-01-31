using System.Collections.Generic;
using Othello.AI;
using Othello.Core;
using UnityEngine;

namespace Othello.UI
{
    public class BoardUI : MonoBehaviour
    {
        public PieceTheme pieceTheme;
        public float pieceScale = 0.3f;
        public float pieceDepth = -0.1f;
        public Color lightColor = new Color(0.93f, 0.93f, 0.82f);
        public Color darkColor = new Color(0.59f, 0.69f, 0.45f);
        public Color highlightColor = new Color(1f, 0.55f, 0.56f);
        public Color lastMoveHighlightColor = new Color(0.47f, 0.55f, 1f);
        public TMPro.TMP_Text playerToMoveUI;
        public TMPro.TMP_Text blackPieceCountUI;
        public TMPro.TMP_Text whitePieceCountUI;
        public Material darkSquareMaterial;
        public Material lightSquareMaterial;
        public static readonly string[] FileChars = { "A", "B", "C", "D", "E", "F", "G", "H" };
        public static bool s_blackAiPlayerCalculating = false;
        public static bool s_whiteAiPlayerCalculating = false;

        private bool m_highLightLegalMoves;
        private MeshRenderer[] m_squareRenderers;
        private SpriteRenderer[] m_pieceRenderers;
        private List<Move> m_currentLegalMoves = new List<Move>();
        private const float m_BoardOffset = -3.5f;
        [SerializeField] private simplerotate blackLoadingWiget;
        [SerializeField] private simplerotate whiteLoadingWiget;

        private void Awake()
        {
            InitBoard();
        }

        private void InitBoard()
        {
            m_squareRenderers = new MeshRenderer[64];
            m_pieceRenderers = new SpriteRenderer[64];
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                    DrawSquare(file, rank);
        }

        public void UpdateUI(Board board, Settings settings)
        {
            UpdateTextUI(board);
            UpdateGameDataUI(settings);
            UpdateLoadingWidget();
        }

        private static void UpdateGameDataUI(Settings settings)
        {
            settings.whiteCurrentDepth.text = "Current depth: " + MiniMax.s_CurrentDepthWhite;
            settings.blackCurrentDepth.text = "Current depth: " + MiniMax.s_CurrentDepthBlack;
            settings.whitePositionsEvaluated.text = "Positions Evaluated: " + MiniMax.s_WhitePositionsEvaluated;
            settings.blackPositionsEvaluated.text = "Positions Evaluated: " + MiniMax.s_BlackPositionsEvaluated;
            settings.whiteCurrentSimulation.text = "Current Simulation: " + Mcts.s_WhiteIterationsRun;
            settings.blackCurrentSimulation.text = "Current Simulation: " + Mcts.s_BlackIterationsRun;
            settings.whiteCurrentWinPrediction.text = "Current Win Prediction: " + Mcts.s_WhiteWinPrediction.ToString("0.##") + " %";
            settings.blackCurrentWinPrediction.text = "Current Win Prediction: " + Mcts.s_BlackWinPrediction.ToString("0.##") + " %";
            settings.whiteTimeElapsed.text = "Time Elapsed: " + AIPlayer.s_WhiteTimeElapsed.Elapsed.TotalMilliseconds.ToString("F0") + "ms";
            settings.blackTimeElapsed.text = "Time Elapsed: " + AIPlayer.s_BlackTimeElapsed.Elapsed.TotalMilliseconds.ToString("F0") + "ms";
            settings.whiteBranchesPruned.text = "Branches Pruned: " + MiniMax.s_WhiteBranchesPruned;
            settings.blackBranchesPruned.text = "Branches Pruned: " + MiniMax.s_BlackBranchesPruned;
            settings.whiteWins.text = "White wins: " + GameManager.m_whiteWins;
            settings.blackWins.text = "Black wins: " + GameManager.m_blackWins;
            settings.draws.text = "Draws: " + GameManager.m_draws;
            settings.currentSim.text = "Sim Nr.: " + GameManager.m_gamesToRun;
            settings.blackZobristSize.text = "Size: " + MiniMax.s_BlackZobristSize * 32 / 8 / 1000000 + "MB";
            settings.whiteZobristSize.text = "Size: " + MiniMax.s_WhiteZobristSize * 32 / 8 / 1000000 + "MB";
        }

        private void UpdateLoadingWidget()
        {
            if (s_blackAiPlayerCalculating)
                blackLoadingWiget.gameObject.SetActive(true);
            else
                blackLoadingWiget.gameObject.SetActive(false);

            if (s_whiteAiPlayerCalculating)
                whiteLoadingWiget.gameObject.SetActive(true);
            else
                whiteLoadingWiget.gameObject.SetActive(false);
        }

        private void DrawSquare(int file, int rank)
        {
            var squareColor = (file + rank) % 2 == 0 ? lightColor : darkColor;
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = FileChars[file] + (rank + 1);
            square.position = new Vector3(file + m_BoardOffset, rank + m_BoardOffset, 0f);

            var squareMaterial = squareColor == lightColor ? lightSquareMaterial : darkSquareMaterial;
            m_squareRenderers[Board.GetIndex(file, rank)] = square.gameObject.GetComponent<MeshRenderer>();
            m_squareRenderers[Board.GetIndex(file, rank)].material = squareMaterial;

            var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
            var pieceRendererTc = pieceRenderer.transform;
            pieceRendererTc.parent = square;
            pieceRendererTc.position = new Vector3(file + m_BoardOffset, rank + m_BoardOffset, pieceDepth);
            pieceRendererTc.localScale = new Vector3(pieceScale, pieceScale, 1);
            m_pieceRenderers[Board.GetIndex(file, rank)] = pieceRenderer;
        }

        public void UpdateBoard(Board board)
        {
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                {
                    var piece = board.GetPieceColor(file, rank);
                    var sprite = piece == Piece.Empty ? null : pieceTheme.GetSprite(piece);
                    var boardIndex = Board.GetIndex(file, rank);
                    m_pieceRenderers[boardIndex].sprite = sprite;
                    UnhighlightSquare(Board.GetIndex(file, rank));
                }
            HighlightLegalMoves(m_currentLegalMoves);
            if (board.GetLastMove() != null)
                HighlightSquare(board.GetLastMove().Index);
        }

        public void HighlightLegalMoves(List<Move> legalMoves)
        {
            m_currentLegalMoves = legalMoves;
            if (!m_highLightLegalMoves)
                return;
            foreach (var legalMove in legalMoves)
                m_squareRenderers[legalMove.Index].material.color = highlightColor;
        }

        public void ToggleLegalMoves(bool isOn)
        {
            m_highLightLegalMoves = isOn;
            if (!m_highLightLegalMoves) 
                UnhighlightAll();
            else HighlightLegalMoves(m_currentLegalMoves);

        }

        public void UnhighlightAll()
        {
            for (int i = 0; i < m_squareRenderers.Length; i++)
                m_squareRenderers[i].material.color = IsWhiteSquare(i) ? darkColor : lightColor;
        }

        private void UpdateTextUI(Board board)
        {
            playerToMoveUI.text = "Player: " + board.GetCurrentPlayerAsString();
            blackPieceCountUI.text = $"Black: {board.GetPieceCountAsString(Piece.Black)}";
            whitePieceCountUI.text = $"White: {board.GetPieceCountAsString(Piece.White)}";
        }

        private void HighlightSquare(int index)
        {
            m_squareRenderers[index].material.color = lastMoveHighlightColor;
        }

        private void UnhighlightSquare(int index)
        {
            m_squareRenderers[index].material.color = IsWhiteSquare(index) ? darkColor : lightColor;
        }

        private static bool IsWhiteSquare(int index)
        {
            var file = index & 7;
            var rank = index >> 3;
            return (file + rank) % 2 == 0;
        }

    }
}
