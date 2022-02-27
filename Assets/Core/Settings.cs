using Othello.AI;
using Othello.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Core
{
    public class Settings : MonoBehaviour
    {
        public Player WhitePlayerNextGame;
        public Player BlackPlayerNextGame;
        public int PlayerToStartNextGame = Piece.Black;
        
        public Toggle showLegalMoves;
        public TMP_Dropdown whitePlayer;
        public TMP_Dropdown blackPlayer;
        public TMP_Dropdown playerToStart;
        public TMP_InputField blackDepth;
        public TMP_InputField whiteDepth;
        public TMP_InputField blackIterations;
        public TMP_InputField whiteIterations;
        public TMP_InputField blackTimeLimit;
        public TMP_InputField whiteTimeLimit;

        private Board m_board;
        private BoardUI m_boardUI;
        private enum PlayerType { Human = 0, Minimax = 1, Mcts = 2, Random = 3 }

        public void Setup(Board board, BoardUI boardUI)
        {
            m_board = board;
            m_boardUI = boardUI;
            blackDepth.text = "5";
            whiteDepth.text = "5";
            blackIterations.text = "500";
            whiteIterations.text = "500";
            blackTimeLimit.text = "4000";
            whiteTimeLimit.text = "4000";
            PlayerSelection(Piece.Black);
            PlayerSelection(Piece.White);
        }
        
         public void PlayerSelection(int player)
         {
             ref Player playerRef = ref (player == Piece.Black) ? ref BlackPlayerNextGame : ref WhitePlayerNextGame;
             var playerType = (player == Piece.Black) ? blackPlayer.value : whitePlayer.value;
             var inputFieldIterations = (player == Piece.Black) ? blackIterations : whiteIterations;
             var inputFieldDepth = (player == Piece.Black) ? blackDepth : whiteDepth;
             var inputFieldTimeLimit = (player == Piece.Black) ? blackTimeLimit : whiteTimeLimit;
             
             if (inputFieldDepth.text.Length == 0) inputFieldDepth.text = "1";
             if (inputFieldTimeLimit.text.Length == 0) inputFieldTimeLimit.text = "1";
             if (inputFieldIterations.text.Length == 0) inputFieldIterations.text = "1";

             switch (playerType)
             {
                 case (int)PlayerType.Human:
                     playerRef = new HumanPlayer(m_board);
                     inputFieldDepth.gameObject.SetActive(false);
                     inputFieldIterations.gameObject.SetActive(false);
                     inputFieldTimeLimit.gameObject.SetActive(false);
                     break;
                 case (int)PlayerType.Minimax:
                     var depth = int.Parse(inputFieldDepth.text);
                     if (depth < 1) depth = 1;
                     playerRef = new AIPlayer(m_board, new MiniMax(depth));
                     inputFieldDepth.gameObject.SetActive(true);
                     inputFieldTimeLimit.gameObject.SetActive(false);
                     inputFieldIterations.gameObject.SetActive(false);
                     break;
                 case (int)PlayerType.Mcts:
                     var timeLimit = int.Parse(inputFieldTimeLimit.text);
                     var iterations = int.Parse(inputFieldIterations.text);
                     if (iterations < 1) iterations = 1;
                     playerRef = new AIPlayer(m_board, new MonteCarloTreeSearch(iterations, timeLimit));
                     inputFieldDepth.gameObject.SetActive(false);
                     inputFieldTimeLimit.gameObject.SetActive(true);
                     inputFieldIterations.gameObject.SetActive(true);
                     break;
                 case (int)PlayerType.Random:
                     playerRef = new AIPlayer(m_board, new RandomPlay());
                     inputFieldDepth.gameObject.SetActive(false);
                     inputFieldTimeLimit.gameObject.SetActive(false);
                     inputFieldIterations.gameObject.SetActive(false);
                     break;
             }
        }
        
        public void ToggleLegalMoves()
        {
            m_boardUI.ToggleLegalMoves(showLegalMoves.isOn);
        }
        
        public void SetStartingPlayer()
        {
            PlayerToStartNextGame = playerToStart.value == 0 ? Piece.Black : Piece.White;
        }
    }
}