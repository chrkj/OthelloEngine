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
        
        private Canvas _canvas;
        private const float BoardOffset = -3.5f;
        private bool highLightLegalMoves = false;
        private MeshRenderer[] _squareRenderers;
        private SpriteRenderer[] _pieceRenderers;
        private List<int> currentLegalMoves = new List<int>();
        private readonly string[] _fileChars = { "A", "B", "C", "D", "E", "F", "G", "H"};

        private void Awake()
        {
            InitBoard();
        }

        public void InitBoard()
        {
            _canvas = FindObjectOfType<Canvas>();
            _squareRenderers = new MeshRenderer[64];
            _pieceRenderers = new SpriteRenderer[64];
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                    DrawSquare(file, rank);
        }

        private void DrawSquare(int file, int rank)
        {
            var squareColor = (file + rank) % 2 == 0 ? lightColor : darkColor;
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = _fileChars[file] + (rank + 1).ToString();
            square.position = new Vector3(file + BoardOffset, rank + BoardOffset, 0f);

            var squareMaterial = squareColor == lightColor ? lightSquareMaterial : darkSquareMaterial;
            _squareRenderers[Board.GetIndex(file, rank)] = square.gameObject.GetComponent<MeshRenderer>();
            _squareRenderers[Board.GetIndex(file, rank)].material = squareMaterial;
            
            var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
            var pieceRendererTc = pieceRenderer.transform;
            pieceRendererTc.parent = square;
            pieceRendererTc.position = new Vector3(file + BoardOffset, rank + BoardOffset, pieceDepth);
            pieceRendererTc.localScale = new Vector3(pieceScale, pieceScale, 1);
            _pieceRenderers[Board.GetIndex(file, rank)] = pieceRenderer;
            
            if (rank == 0) DrawFileChar(file);
            if (file == 0) DrawRankChar(rank);
        }

        private void DrawRankChar(int rank)
        {
            var fileChar = new GameObject("FileChar" + rank).AddComponent<Text>();
            var fileCharTc = fileChar.transform;
            fileCharTc.SetParent(_canvas.transform);
            fileCharTc.localScale = Vector3.one;
            fileCharTc.position = new Vector3(BoardOffset - 1, BoardOffset + rank, 0);
            fileChar.font = Font.CreateDynamicFontFromOSFont("Oswald-Bold.ttf", 20);
            fileChar.fontSize = 20;
            fileChar.alignment = TextAnchor.MiddleCenter;
            fileChar.text = (rank + 1).ToString();
        }

        private void DrawFileChar(int file)
        {
            var fileChar = new GameObject("FileChar" + _fileChars[file]).AddComponent<Text>();
            var fileCharTc = fileChar.transform;
            fileCharTc.SetParent(_canvas.transform);
            fileCharTc.localScale = Vector3.one;
            fileCharTc.position = new Vector3(file + BoardOffset, BoardOffset - 1, 0);
            fileChar.font = Font.CreateDynamicFontFromOSFont("Oswald-Bold.ttf", 20);
            fileChar.fontSize = 20;
            fileChar.alignment = TextAnchor.MiddleCenter;
            fileChar.text = _fileChars[file];
        }

        public void UpdateBoardUI(Board board)
        {
            for (var rank = 0; rank < 8; rank++)
                for (var file = 0; file < 8; file++)
                {
                    var piece = board.GetPieceColor(file, rank);
                    var sprite = piece == Piece.Empty ? null : pieceTheme.GetSprite(piece);
                    var boardIndex = Board.GetIndex(file, rank);
                    _pieceRenderers[boardIndex].sprite = sprite;
                    UnhighlightSquare(Board.GetIndex(file, rank));
                }
            UpdateUI(board);
        }

        public void MakeMove(int move, HashSet<int> captures, Board board)
        {
            _pieceRenderers[move].sprite = pieceTheme.GetSprite(board.GetCurrentPlayer());
            foreach (var capture in captures)
                _pieceRenderers[capture].sprite = pieceTheme.GetSprite(board.GetCurrentPlayer());
        }

        public void UpdateUI(Board board)
        {
            playerToMoveUI.text = "Player: " + board.GetCurrentPlayerAsString();
            blackPieceCountUI.text = $"Black: {board.GetPieceCountAsString(Piece.Black)}";
            whitePieceCountUI.text = $"White: {board.GetPieceCountAsString(Piece.White)}";
        }

        public bool HasSprite(int index)
        {
            return _pieceRenderers[index].sprite != null;
        }

        public void HighlightSquare(int index)
        {
            _squareRenderers[index].material.color = lastMoveHighlightColor;
        }

        public void UnhighlightSquare(int index)
        {
            _squareRenderers[index].material.color = IsWhiteSquare(index) ? darkColor : lightColor;
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

        public void HighlightLegalMoves(List<int> legalMoves)
        {
            currentLegalMoves = legalMoves;
            if (!highLightLegalMoves) return;
            foreach (var legalMove in legalMoves)
                _squareRenderers[legalMove].material.color = highlightColor;;
        }

        public void UnhighlightLegalMoves(List<int> legalMoves)
        {
            foreach (var legalMove in legalMoves)
                UnhighlightSquare(legalMove);
        }

        public void ToggleLegalMoves(bool isOn)
        {
            highLightLegalMoves = isOn;
            if (!highLightLegalMoves) UnhighlightAll();
            else HighlightLegalMoves(currentLegalMoves);

        }

        private void UnhighlightAll()
        {
            for (int i = 0; i < _squareRenderers.Length; i++)
            {
                _squareRenderers[i].material.color = IsWhiteSquare(i) ? darkColor : lightColor;
            }
        }
    }
}
