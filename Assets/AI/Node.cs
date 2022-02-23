using System;
using System.Linq;
using System.Collections.Generic;

namespace Othello.AI
{
    public class Node 
    {
        public Node Parent;
        public State State;
        public readonly List<Node> ChildArray = new List<Node>();

        public Node()
        {
            State = new State();
        }

        public Node(Node node) // Convert to copy method?
        {
            State = new State()
            {
                Board = node.State.Board.Copy(), 
                NumVisits = node.State.NumVisits, 
                NumWins = node.State.NumVisits
            };
        }

        public Node GetRandomChildNode()
        {
            return ChildArray[new Random().Next(ChildArray.Count)];
        }

        public Node GetChildWithHighestScore()
        {
            return ChildArray.OrderByDescending(node => node.State.NumWins / node.State.NumVisits == 0 ? int.MaxValue : node.State.NumVisits).First();
        }
    }
}