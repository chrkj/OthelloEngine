using Othello.Core;

namespace Othello.AI
{
    public interface ISearchEngine
    {
        public SearchResult StartSearch(Board board);
    }
}