using Othello.Core;

namespace Othello.AI
{
    public interface ISearchEngine
    {
        public Move StartSearch(Board board);
    }
}