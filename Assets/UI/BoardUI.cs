using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Othello.Core;
using Othello.Utility;
using Othello.Animation;

namespace Othello.UI
{
    public class BoardUI : SingletonMono<BoardUI>
    {
        public bool BlackAiPlayerCalculating;
        public bool WhiteAiPlayerCalculating;
        public readonly string[] FileChars = { "A", "B", "C", "D", "E", "F", "G", "H" };
        
        [SerializeField] private PieceTheme pieceTheme;
        [SerializeField] private float pieceScale = 1.5f;
        [SerializeField] private float pieceDepth = -0.1f;
        [SerializeField] private Color lightColor = new(0.93f, 0.93f, 0.82f);
        [SerializeField] private Color darkColor = new(0.59f, 0.69f, 0.45f);
        [SerializeField] private Color highlightColor = new(1f, 0.55f, 0.56f);
        [SerializeField] private Color lastMoveHighlightColor = new(0.47f, 0.55f, 1f);
        [SerializeField] private SimpleRotate blackLoadingWidget;
        [SerializeField] private SimpleRotate whiteLoadingWidget;
        [SerializeField] private Material darkSquareMaterial;
        [SerializeField] private Material lightSquareMaterial;

        private bool m_HighLightLegalMoves;
        private MeshRenderer[] m_SquareRenderers;
        private SpriteRenderer[] m_PieceRenderers;
        private List<Move> m_CurrentLegalMoves = new();
        private const float BOARD_OFFSET = -3.5f;

        public void InitBoard()
        {
            m_SquareRenderers = new MeshRenderer[64];
            m_PieceRenderers = new SpriteRenderer[64];
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                    DrawSquare(file, rank);
        }

        public void UpdateBoard(Board board)
        {
            UpdateLoadingWidget();
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                {
                    var piece = board.GetPieceColor(file, rank);
                    var sprite = piece == Piece.EMPTY ? null : pieceTheme.GetSprite(piece);
                    var boardIndex = Board.GetIndex(file, rank);
                    m_PieceRenderers[boardIndex].sprite = sprite;
                    UnhighlightSquare(Board.GetIndex(file, rank));
                }
            HighlightLegalMoves(m_CurrentLegalMoves);
        }

        public void SetLegalMoves(Span<Move> legalMoves)
        {
            m_CurrentLegalMoves = legalMoves.ToArray().ToList();
        }

        public void HighlightLastMove(Move move)
        {
            if (move != Move.NULLMOVE)
                HighlightSquare(move.Index);
        }

        private void HighlightLegalMoves(List<Move> legalMoves)
        {
            if (!m_HighLightLegalMoves)
                return;
            foreach (var legalMove in legalMoves)
                m_SquareRenderers[legalMove.Index].material.color = highlightColor;
        }

        public void ToggleLegalMoves(bool isOn)
        {
            m_HighLightLegalMoves = isOn;
            if (!m_HighLightLegalMoves) 
                UnhighlightAll();
            else 
                HighlightLegalMoves(m_CurrentLegalMoves);

        }

        public void UnhighlightAll()
        {
            for (int i = 0; i < m_SquareRenderers.Length; i++)
                m_SquareRenderers[i].material.color = IsWhiteSquare(i) ? darkColor : lightColor;
        }
        
        private void UpdateLoadingWidget()
        {
            blackLoadingWidget.gameObject.SetActive(BlackAiPlayerCalculating);
            whiteLoadingWidget.gameObject.SetActive(WhiteAiPlayerCalculating);
        }

        private void DrawSquare(int file, int rank)
        {
            var squareColor = (file + rank) % 2 == 0 ? lightColor : darkColor;
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = FileChars[file] + (rank + 1);
            square.position = new Vector3(file + BOARD_OFFSET, rank + BOARD_OFFSET, 0f);

            var squareMaterial = squareColor == lightColor ? lightSquareMaterial : darkSquareMaterial;
            m_SquareRenderers[Board.GetIndex(file, rank)] = square.gameObject.GetComponent<MeshRenderer>();
            m_SquareRenderers[Board.GetIndex(file, rank)].material = squareMaterial;

            var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
            var pieceRendererTc = pieceRenderer.transform;
            pieceRendererTc.parent = square;
            pieceRendererTc.position = new Vector3(file + BOARD_OFFSET, rank + BOARD_OFFSET, pieceDepth);
            pieceRendererTc.localScale = new Vector3(pieceScale, pieceScale, 1);
            m_PieceRenderers[Board.GetIndex(file, rank)] = pieceRenderer;
        }

        private void HighlightSquare(int index)
        {
            m_SquareRenderers[index].material.color = lastMoveHighlightColor;
        }

        private void UnhighlightSquare(int index)
        {
            m_SquareRenderers[index].material.color = IsWhiteSquare(index) ? darkColor : lightColor;
        }

        private static bool IsWhiteSquare(int index)
        {
            var file = index & 7;
            var rank = index >> 3;
            return (file + rank) % 2 == 0;
        }

    }
}
