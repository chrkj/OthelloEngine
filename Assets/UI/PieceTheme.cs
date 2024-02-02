using UnityEngine;

using Othello.Core;

namespace Othello.UI
{
    [CreateAssetMenu (menuName = "Theme/PieceTheme")]
    public class PieceTheme : ScriptableObject
    {
        public PieceSprites whitePieces;
        public PieceSprites blackPieces;
        
        public Sprite GetSprite(int piece)
        {
            var pieceSprites = Piece.IsBlack(piece) ? blackPieces : whitePieces;
            return pieceSprites.playerPiece;
        }
        
    }
}