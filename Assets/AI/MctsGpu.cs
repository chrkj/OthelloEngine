using System;
using Othello.Core;
using UnityEngine;

namespace Othello.AI
{
    public class MctsGpu : ISearchEngine
    {
        private readonly ComputeShader m_ComputeShader;

        public MctsGpu(ComputeShader computeShader)
        {
            m_ComputeShader = computeShader;
        }

        public Move StartSearch(Board board)
        {
            RunGpu(board);
            return new Move();
        }
        
        public void RunGpu(Board board)
        {
            var blackPieces = board.GetPiecesBitBoard(Player.BLACK);
            var whitePieces = board.GetPiecesBitBoard(Player.WHITE);
            var currentPlayer = board.GetCurrentPlayer();
            if (m_ComputeShader == null) 
                return;
            board.m_BlackPieces = 562949953421313;
            board.m_WhitePieces = 791101568;
            
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            board.GenerateLegalMoves(ref legalMoves);
            
            ComputeBuffer pieceBuffer = new ComputeBuffer(2, sizeof(ulong), ComputeBufferType.Default);
            pieceBuffer.SetData(new ulong[] {blackPieces, whitePieces});
            m_ComputeShader.SetBuffer(0, "_Pieces", pieceBuffer);
            
            ComputeBuffer currentPlayerBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            currentPlayerBuffer.SetData(new int[] {currentPlayer});
            m_ComputeShader.SetBuffer(0, "_CurrentPlayer", currentPlayerBuffer);
            
            ComputeBuffer resultBuffer = new ComputeBuffer(60, sizeof(ulong), ComputeBufferType.Default);
            m_ComputeShader.SetBuffer(0, "_Result", resultBuffer);
            
            ComputeBuffer debugBuffer = new ComputeBuffer(60, sizeof(ulong), ComputeBufferType.Default);
            m_ComputeShader.SetBuffer(0, "_Debug", debugBuffer);
            
            ComputeBuffer moveBuffer = new ComputeBuffer(60, sizeof(int), ComputeBufferType.Default);
            m_ComputeShader.SetBuffer(0, "_Moves", moveBuffer);
            
            ComputeBuffer legalBufferBB = new ComputeBuffer(60, sizeof(ulong), ComputeBufferType.Default);
            m_ComputeShader.SetBuffer(0, "_LegalMovesBB", legalBufferBB);
            
            m_ComputeShader.Dispatch(0, 1, 1, 1); // execute compute shader
            var result = new ulong[60];
            resultBuffer.GetData(result);
            var debug = new ulong[60];
            debugBuffer.GetData(debug);
            var moves = new int[60];
            moveBuffer.GetData(moves);
            var movesBB = new ulong[60];
            legalBufferBB.GetData(movesBB);
            
            Debug.Log(result[0].ToString());
            Debug.Log(result[1].ToString());
            Debug.Log(debug[0].ToString());
            
            pieceBuffer.Release();
            resultBuffer.Release();  
            currentPlayerBuffer.Release();
        }
    }
}