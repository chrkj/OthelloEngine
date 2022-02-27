using System.Collections.Generic;
using Othello.Core;
using UnityEngine;
using UnityEngine.UI;

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
        
        private Canvas m_canvas;
        private bool m_highLightLegalMoves;
        private MeshRenderer[] m_squareRenderers;
        private SpriteRenderer[] m_pieceRenderers;
        private List<Move> m_currentLegalMoves = new List<Move>();
        private const float m_BoardOffset = -3.5f;
        private readonly string[] m_FileChars = { "A", "B", "C", "D", "E", "F", "G", "H"};

        private void Awake()
        {
            InitBoard();
        }

        private void InitBoard()
        {
            m_canvas = FindObjectOfType<Canvas>();
            m_squareRenderers = new MeshRenderer[64];
            m_pieceRenderers = new SpriteRenderer[64];
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                    DrawSquare(file, rank);
        }

        private void DrawSquare(int file, int rank)
        {
            var squareColor = (file + rank) % 2 == 0 ? lightColor : darkColor;
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = m_FileChars[file] + (rank + 1);
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
            
            if (rank == 0) DrawFileChar(file);
            if (file == 0) DrawRankChar(rank);
        }

        private void DrawRankChar(int rank)
        {
            var fileChar = new GameObject("FileChar" + rank).AddComponent<Text>();
            var fileCharTc = fileChar.transform;
            fileCharTc.SetParent(m_canvas.transform);
            fileCharTc.localScale = Vector3.one;
            fileCharTc.position = new Vector3(m_BoardOffset - 1, m_BoardOffset + rank, 0);
            fileChar.font = Font.CreateDynamicFontFromOSFont("Oswald-Bold.ttf", 20);
            fileChar.fontSize = 20;
            fileChar.alignment = TextAnchor.MiddleCenter;
            fileChar.text = (rank + 1).ToString();
        }

        private void DrawFileChar(int file)
        {
            var fileChar = new GameObject("FileChar" + m_FileChars[file]).AddComponent<Text>();
            var fileCharTc = fileChar.transform;
            fileCharTc.SetParent(m_canvas.transform);
            fileCharTc.localScale = Vector3.one;
            fileCharTc.position = new Vector3(file + m_BoardOffset, m_BoardOffset - 1, 0);
            fileChar.font = Font.CreateDynamicFontFromOSFont("Oswald-Bold.ttf", 20);
            fileChar.fontSize = 20;
            fileChar.alignment = TextAnchor.MiddleCenter;
            fileChar.text = m_FileChars[file];
        }

        public void UpdateBoardUI(Board board)
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
            UpdateTextUI(board);
            HighlightLegalMoves(m_currentLegalMoves);
            if (board.GetLastMove() != null)
                HighlightSquare(board.GetLastMove().Index);
        }

        public void MakeMove(Move move, HashSet<Move> captures, Board board)
        {
            m_pieceRenderers[move.Index].sprite = pieceTheme.GetSprite(board.GetCurrentPlayer());
            foreach (var capture in captures)
                m_pieceRenderers[capture.Index].sprite = pieceTheme.GetSprite(board.GetCurrentPlayer());
        }

        public void UpdateTextUI(Board board)
        {
            playerToMoveUI.text = "Player: " + board.GetCurrentPlayerAsString();
            blackPieceCountUI.text = $"Black: {board.GetPieceCountAsString(Piece.Black)}";
            whitePieceCountUI.text = $"White: {board.GetPieceCountAsString(Piece.White)}";
        }

        public bool HasSprite(int index)
        {
            return m_pieceRenderers[index].sprite != null;
        }

        public void HighlightSquare(int index)
        {
            m_squareRenderers[index].material.color = lastMoveHighlightColor;
        }

        public void UnhighlightSquare(int index)
        {
            m_squareRenderers[index].material.color = IsWhiteSquare(index) ? darkColor : lightColor;
        }

        private static bool IsWhiteSquare(int index)
        {
            var file = index & 7;
            var rank = index >> 3;
            return (file + rank) % 2 == 0;
        }

        public void HighlightLegalMoves(List<Move> legalMoves)
        {
            m_currentLegalMoves = legalMoves;
            if (!m_highLightLegalMoves) return;
            foreach (var legalMove in legalMoves)
                m_squareRenderers[legalMove.Index].material.color = highlightColor;
        }

        public void UnhighlightLegalMoves(List<Move> legalMoves)
        {
            foreach (var legalMove in legalMoves)
                UnhighlightSquare(legalMove.Index);
        }

        public void ToggleLegalMoves(bool isOn)
        {
            m_highLightLegalMoves = isOn;
            if (!m_highLightLegalMoves) UnhighlightAll();
            else HighlightLegalMoves(m_currentLegalMoves);

        }

        public void UnhighlightAll()
        {
            for (int i = 0; i < m_squareRenderers.Length; i++)
            {
                m_squareRenderers[i].material.color = IsWhiteSquare(i) ? darkColor : lightColor;
            }
        }
    }
}
