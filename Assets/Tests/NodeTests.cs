using NUnit.Framework;
using Othello.AI;
using Othello.Core;

namespace Othello.Tests
{
    public class NodeTests
    {
        private static Node CreateExpandedRootNode()
        {
            var board = new Board();
            board.LoadStartPosition();
            var root = new Node(board);
            foreach (var child in root.CreateChildNodes())
            {
                child.Parent = root;
                root.Children.Add(child);
            }
            return root;
        }

        [Test]
        public void CreateChildNodes_CreatesOneChildPerLegalMove()
        {
            Assert.AreEqual(4, CreateExpandedRootNode().Children.Count);
        }

        [Test]
        public void CalculateUct_PrioritizesUnvisitedNodes()
        {
            var root = CreateExpandedRootNode();
            root.NumVisits = 10;
            Assert.AreEqual(int.MaxValue, root.Children[0].CalculateUct());
        }

        [Test]
        public void SelectBestNode_PicksTheChildWithTheHighestWinRatio()
        {
            var root = CreateExpandedRootNode();
            for (var i = 0; i < root.Children.Count; i++)
            {
                root.Children[i].NumVisits = 100;
                root.Children[i].NumWins = i == 2 ? 90 : 10;
            }

            var (bestNode, bestMove) = root.SelectBestNode();

            Assert.AreSame(root.Children[2], bestNode);
            Assert.AreEqual(34, bestMove.Index); // children follow legal move order {20, 29, 34, 43}
        }
    }
}
