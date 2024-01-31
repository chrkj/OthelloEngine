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
        public static bool AutoMove;
        public enum MctsType { Sequential = 0, RootParallel = 1, TreeParallel = 2, Testing = 3 }

        public TMP_Dropdown whitePlayer;
        public TMP_Dropdown blackPlayer;
        public TMP_Dropdown whiteMtcsMode;
        public TMP_Dropdown blackMctsMode;
        public TMP_Dropdown playerToStart;
        public TMP_InputField blackDepth;
        public TMP_InputField whiteDepth;
        public TMP_InputField blackIterations;
        public TMP_InputField whiteIterations;
        public TMP_InputField blackTimeLimit;
        public TMP_InputField whiteTimeLimit;
        public TMP_InputField numGamesForSim;
        public Toggle showLegalMoves;
        public Toggle enableAutoMove;
        public Toggle whiteMoveOrdering;
        public Toggle blackMoveOrdering;
        public Toggle whiteIterativeDeepning;
        public Toggle blackIterativeDeepning;
        public Toggle whiteZobristHashing;
        public Toggle blackZobristHashing;
        public TMP_Text whiteCurrentDepth;
        public TMP_Text blackCurrentDepth;
        public TMP_Text blackTimeElapsed;
        public TMP_Text whiteTimeElapsed;
        public TMP_Text blackCurrentSimulation;
        public TMP_Text whiteCurrentSimulation;
        public TMP_Text blackPositionsEvaluated;
        public TMP_Text whitePositionsEvaluated;
        public TMP_Text blackCurrentWinPrediction;
        public TMP_Text whiteCurrentWinPrediction;
        public TMP_Text blackBranchesPruned;
        public TMP_Text whiteBranchesPruned;
        public TMP_Text blackWins;
        public TMP_Text whiteWins;
        public TMP_Text blackZobristSize;
        public TMP_Text whiteZobristSize;
        public TMP_Text draws;
        public TMP_Text currentSim;

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
            var currentDepth = (player == Piece.Black) ? blackCurrentDepth : whiteCurrentDepth;
            var inputMoveOrdering = (player == Piece.Black) ? blackMoveOrdering : whiteMoveOrdering;
            var inputIterativeDeepning = (player == Piece.Black) ? blackIterativeDeepning : whiteIterativeDeepning;
            var timeElapsed = (player == Piece.Black) ? blackTimeElapsed : whiteTimeElapsed;
            var currentSimulation = (player == Piece.Black) ? blackCurrentSimulation : whiteCurrentSimulation;
            var currnetPositionsEvaulated = (player == Piece.Black) ? blackPositionsEvaluated : whitePositionsEvaluated;
            var currentWinPrediction = (player == Piece.Black) ? blackCurrentWinPrediction : whiteCurrentWinPrediction;
            var branchesPruned = (player == Piece.Black) ? blackBranchesPruned : whiteBranchesPruned;
            var mctsTypeInput = (player == Piece.Black) ? blackMctsMode : whiteMtcsMode;
            var mctsType = (player == Piece.Black) ? blackMctsMode.value : whiteMtcsMode.value;
            var zobristHashing = (player == Piece.Black) ? blackZobristHashing : whiteZobristHashing;
            var zobristSize = (player == Piece.Black) ? blackZobristSize : whiteZobristSize;

            if (inputFieldDepth.text.Length == 0)
                inputFieldDepth.text = "1";
            if (inputFieldTimeLimit.text.Length == 0)
                inputFieldTimeLimit.text = "1";
            if (inputFieldIterations.text.Length == 0)
                inputFieldIterations.text = "1";

            switch (playerType)
            {
                case (int)PlayerType.Human:
                    {
                        playerRef = new HumanPlayer(m_board);
                        inputFieldDepth.gameObject.SetActive(false);
                        inputFieldIterations.gameObject.SetActive(false);
                        inputFieldTimeLimit.gameObject.SetActive(false);
                        inputMoveOrdering.gameObject.SetActive(false);
                        inputIterativeDeepning.gameObject.SetActive(false);
                        timeElapsed.gameObject.SetActive(false);
                        currentSimulation.gameObject.SetActive(false);
                        currnetPositionsEvaulated.gameObject.SetActive(false);
                        currentWinPrediction.gameObject.SetActive(false);
                        currentDepth.gameObject.SetActive(false);
                        branchesPruned.gameObject.SetActive(false);
                        mctsTypeInput.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(false);
                        zobristSize.gameObject.SetActive(false);
                        break;
                    }
                case (int)PlayerType.Minimax:
                    {
                        var depth = int.Parse(inputFieldDepth.text);
                        var timeLimit = int.Parse(inputFieldTimeLimit.text);
                        if (depth < 1)
                            depth = 1;
                        playerRef = new AIPlayer(m_board, new MiniMax(depth, timeLimit, inputMoveOrdering.isOn, inputIterativeDeepning.isOn, zobristHashing.isOn));
                        inputFieldDepth.gameObject.SetActive(true);
                        inputFieldIterations.gameObject.SetActive(false);
                        inputFieldTimeLimit.gameObject.SetActive(true);
                        inputMoveOrdering.gameObject.SetActive(true);
                        inputIterativeDeepning.gameObject.SetActive(true);
                        timeElapsed.gameObject.SetActive(true);
                        currentSimulation.gameObject.SetActive(false);
                        currnetPositionsEvaulated.gameObject.SetActive(true);
                        currentWinPrediction.gameObject.SetActive(false);
                        currentDepth.gameObject.SetActive(true);
                        branchesPruned.gameObject.SetActive(true);
                        mctsTypeInput.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(true);
                        if (zobristHashing.isOn)
                            zobristSize.gameObject.SetActive(true);
                        else
                            zobristSize.gameObject.SetActive(false);
                        break;
                    }
                case (int)PlayerType.Mcts:
                    {
                        var timeLimit = int.Parse(inputFieldTimeLimit.text);
                        var iterations = int.Parse(inputFieldIterations.text);
                        if (iterations < 1)
                            iterations = 1;
                        playerRef = new AIPlayer(m_board, new Mcts(iterations, timeLimit, (MctsType)mctsType));
                        inputFieldDepth.gameObject.SetActive(false);
                        inputFieldIterations.gameObject.SetActive(true);
                        inputFieldTimeLimit.gameObject.SetActive(true);
                        inputMoveOrdering.gameObject.SetActive(false);
                        inputIterativeDeepning.gameObject.SetActive(false);
                        timeElapsed.gameObject.SetActive(true);
                        currentSimulation.gameObject.SetActive(true);
                        currnetPositionsEvaulated.gameObject.SetActive(false);
                        currentWinPrediction.gameObject.SetActive(true);
                        currentDepth.gameObject.SetActive(false);
                        branchesPruned.gameObject.SetActive(false);
                        mctsTypeInput.gameObject.SetActive(true);
                        zobristHashing.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(false);
                        zobristSize.gameObject.SetActive(false);
                    }
                    break;
                case (int)PlayerType.Random:
                    {
                        playerRef = new AIPlayer(m_board, new RandomPlay());
                        inputFieldDepth.gameObject.SetActive(false);
                        inputFieldIterations.gameObject.SetActive(false);
                        inputFieldTimeLimit.gameObject.SetActive(false);
                        inputMoveOrdering.gameObject.SetActive(false);
                        inputIterativeDeepning.gameObject.SetActive(false);
                        timeElapsed.gameObject.SetActive(false);
                        currentSimulation.gameObject.SetActive(false);
                        currnetPositionsEvaulated.gameObject.SetActive(false);
                        currentWinPrediction.gameObject.SetActive(false);
                        currentDepth.gameObject.SetActive(false);
                        branchesPruned.gameObject.SetActive(false);
                        mctsTypeInput.gameObject.SetActive(false);
                        zobristHashing.gameObject.SetActive(false);
                        zobristSize.gameObject.SetActive(false);
                        break;
                    }
            }
        }

        public void ToggleLegalMoves()
        {
            m_boardUI.ToggleLegalMoves(showLegalMoves.isOn);
        }

        public void ToggleAutoMove()
        {
            AutoMove = enableAutoMove.isOn;
        }

        public void SetStartingPlayer()
        {
            PlayerToStartNextGame = (playerToStart.value == 0) ? Piece.Black : Piece.White;
        }

    }
}