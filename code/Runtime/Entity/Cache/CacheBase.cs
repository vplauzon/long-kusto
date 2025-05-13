using Runtime.Entity.RowItem;

namespace Runtime.Entity.Cache
{
    internal abstract class CacheBase<T>
        where T : RowBase
    {
        public CacheBase(T row)
        {
            Row = row;
        }

        public T Row { get; }
    }
}