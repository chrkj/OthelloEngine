using System.Collections.Generic;
using Othello.Core;
using UnityEngine;

namespace Othello.UI
{
    public class BoardUI : MonoBehaviour
    {
        public PieceTheme pieceTheme;
        public float pieceScale = 0.3f;
        public float pieceDepth = -0.1f;
        public Color darkColor = new Color(0.59f, 0.69f, 0.45f);
        public Color lightColor = new Color(0.93f, 0.93f, 0.82f);
        public Color highlightColor = new Color(1f, 0.55f, 0.56f);
        private MeshRenderer[] _squareRenderers;
        private SpriteRenderer[] _pieceRenderers;
        private const float BoardOffset = -3.5f;
        private readonly char[] _fileChars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'};

        public void InitBoard()
        {
            _squareRenderers = new MeshRenderer[64];
            _pieceRenderers = new SpriteRenderer[64];
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                    DrawSquare(file, rank);
        }

        private void DrawSquare(int file, int rank)
        {
            var color = (file + rank) % 2 == 0 ? lightColor : darkColor;
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = _fileChars[file] + (rank + 1).ToString();
            square.position = new Vector3(file + BoardOffset, rank + BoardOffset, 0f);

            var squareMaterial = new Material(Shader.Find("Unlit/Color"));
            squareMaterial.color = color;
            _squareRenderers[Board.GetBoardIndex(file, rank)] = square.gameObject.GetComponent<MeshRenderer>();
            _squareRenderers[Board.GetBoardIndex(file, rank)].material = squareMaterial;
            
            var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
            var pieceRendererTc = pieceRenderer.transform;
            pieceRendererTc.parent = square;
            pieceRendererTc.position = new Vector3(file + BoardOffset, rank + BoardOffset, pieceDepth);
            pieceRendererTc.localScale = new Vector3(pieceScale, pieceScale, 1);
            _pieceRenderers[Board.GetBoardIndex(file, rank)] = pieceRenderer;
        }

        public void UpdateUI(Board board)
        {
            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    var piece = board.GetPiece(file, rank);
                    if (piece == Piece.Empty) continue;
                    var sprite = pieceTheme.GetSprite(piece);
                    var boardIndex = Board.GetBoardIndex(file, rank);
                    var parentPosition = _pieceRenderers[boardIndex].transform.parent.position;
                    _pieceRenderers[boardIndex].sprite = sprite;
                    _pieceRenderers[boardIndex].transform.position = new Vector3(parentPosition.x, parentPosition.y, pieceDepth);
                }
            }
        }

        public void MakeMove(Move move, HashSet<int> captures)
        {
            _pieceRenderers[move.TargetSquare].sprite = pieceTheme.GetSprite(move.Piece);
            foreach (var index in captures)
                _pieceRenderers[index].sprite = pieceTheme.GetSprite(move.Piece);
        }

        public bool HasSprite(int index)
        {
            return _pieceRenderers[index].sprite != null;
        }

        public void HighlightSquare(int index)
        {
            _squareRenderers[index].material.color = highlightColor;
        }

        public void UnhighlightSquare(int index)
        {
            _squareRenderers[index].material.color = IsWhiteSquare(index) ? lightColor : darkColor;
        }

        public static bool IsWhiteSquare(int index)
        {
            var file = index & 7;
            var rank = index >> 3;
            return (file + rank) % 2 == 0;
        }
        
        public static bool IsBlackSquare(int index)
        {
            var file = index & 7;
            var rank = index >> 3;
            return (file + rank) % 2 != 0;
        }

    }
}
