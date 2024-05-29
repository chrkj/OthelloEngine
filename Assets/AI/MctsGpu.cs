using System;
using System.Diagnostics;
using Othello.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var blackPieces = board.GetPiecesBitBoard(Player.BLACK);
            var whitePieces = board.GetPiecesBitBoard(Player.WHITE);
            var currentPlayer = board.GetCurrentPlayer() == 2 ? 1 : 0;
            
            ComputeBuffer pieceBuffer = new ComputeBuffer(2, sizeof(ulong), ComputeBufferType.Default);
            pieceBuffer.SetData(new ulong[] {blackPieces, whitePieces});
            m_ComputeShader.SetBuffer(0, "_Pieces", pieceBuffer);
            
            ComputeBuffer currentPlayerBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            currentPlayerBuffer.SetData(new int[] {currentPlayer});
            m_ComputeShader.SetBuffer(0, "_CurrentPlayer", currentPlayerBuffer);
            
            ComputeBuffer seedBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            seedBuffer.SetData(new int[] {new Random().Next()});
            m_ComputeShader.SetBuffer(0, "_Seed", seedBuffer);
            
            ComputeBuffer resultBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            m_ComputeShader.SetBuffer(0, "_Result", resultBuffer);
            
            m_ComputeShader.Dispatch(0, 1, 1, 1); // execute compute shader
            var result = new int[1];
            resultBuffer.GetData(result);

            Debug.Log(result[0].ToString());
            
            pieceBuffer.Release();
            currentPlayerBuffer.Release();  
            seedBuffer.Release();
            resultBuffer.Release();
            sw.Stop();
            Debug.Log("Time: " + sw.Elapsed.Milliseconds);
        }
    }
}