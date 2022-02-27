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
        private enum PlayerType { Human = 0, Minimax = 1, Mcts = 2, Random = 3, MctsThreading = 4 }

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
            PlayerSelection(ref WhitePlayerNextGame, (int)PlayerType.Human, Piece.White);
            PlayerSelection(ref BlackPlayerNextGame, (int)PlayerType.Human, Piece.Black);
            whitePlayer.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref WhitePlayerNextGame, whitePlayer.value, Piece.White);
            });
            blackPlayer.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref BlackPlayerNextGame, blackPlayer.value, Piece.Black);
            });
            whiteDepth.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref WhitePlayerNextGame, whitePlayer.value, Piece.White);
            });
            blackDepth.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref BlackPlayerNextGame, blackPlayer.value, Piece.Black);
            });
            whiteIterations.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref WhitePlayerNextGame, whitePlayer.value, Piece.White);
            });
            blackIterations.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref BlackPlayerNextGame, blackPlayer.value, Piece.Black);
            });
            showLegalMoves.onValueChanged.AddListener(delegate { ToggleLegalMoves(showLegalMoves.isOn); });
            playerToStart.onValueChanged.AddListener(delegate { SetStartingPlayer(playerToStart.value); });
        }
        
        private void PlayerSelection(ref Player player, int playerType, int playerColor)
        {
            var inputFieldIterations = playerColor == Piece.Black ? blackIterations : whiteIterations;
            var inputFieldDepth = playerColor == Piece.Black ? blackDepth : whiteDepth;
            var inputFieldTimeLimit = playerColor == Piece.Black ? blackTimeLimit : whiteTimeLimit;
            if (inputFieldIterations.text.Length == 0) inputFieldIterations.text = "1";
            if (inputFieldDepth.text.Length == 0) inputFieldDepth.text = "1";
            if (inputFieldTimeLimit.text.Length == 0) inputFieldTimeLimit.text = "1";
            int iterations;
            int timeLimit;
            switch (playerType)
            {
                case (int)PlayerType.Human:
                    player = new HumanPlayer(m_board);
                    inputFieldIterations.gameObject.SetActive(false);
                    inputFieldDepth.gameObject.SetActive(false);
                    inputFieldTimeLimit.gameObject.SetActive(false);
                    break;
                case (int)PlayerType.Minimax:
                    var depth = int.Parse(inputFieldDepth.text);
                    if (depth < 1) depth = 1;
                    player = new AIPlayer(m_board, new MiniMax(depth));
                    inputFieldDepth.gameObject.SetActive(true);
                    inputFieldTimeLimit.gameObject.SetActive(false);
                    inputFieldIterations.gameObject.SetActive(false);
                    break;
                case (int)PlayerType.Mcts:
                    iterations = int.Parse(inputFieldIterations.text);
                    timeLimit = int.Parse(inputFieldTimeLimit.text);
                    if (iterations < 1) iterations = 1;
                    player = new AIPlayer(m_board, new MonteCarloTreeSearch(iterations, timeLimit));
                    inputFieldIterations.gameObject.SetActive(true);
                    inputFieldTimeLimit.gameObject.SetActive(true);
                    inputFieldDepth.gameObject.SetActive(false);
                    break;
                case (int)PlayerType.MctsThreading:
                    iterations = int.Parse(inputFieldIterations.text);
                    timeLimit = int.Parse(inputFieldTimeLimit.text);
                    if (iterations < 1) iterations = 1;
                    player = new AIPlayer(m_board, new MctsThreading(iterations, timeLimit));
                    inputFieldIterations.gameObject.SetActive(true);
                    inputFieldTimeLimit.gameObject.SetActive(true);
                    inputFieldDepth.gameObject.SetActive(false);
                    break;
                case (int)PlayerType.Random:
                    player = new AIPlayer(m_board, new RandomPlay());
                    inputFieldTimeLimit.gameObject.SetActive(false);
                    inputFieldIterations.gameObject.SetActive(false);
                    inputFieldDepth.gameObject.SetActive(false);
                    break;
            }
        }
        
        private void ToggleLegalMoves(bool isOn)
        {
            m_boardUI.ToggleLegalMoves(isOn);
        }
        
        private void SetStartingPlayer(int player)
        {
            PlayerToStartNextGame = player == 0 ? Piece.Black : Piece.White;
        }
    }
}