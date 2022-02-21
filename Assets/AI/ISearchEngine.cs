using Othello.Core;

namespace Othello.AI
{
    public interface ISearchEngine
    {
        public int StartSearch(Board board);
    }
}