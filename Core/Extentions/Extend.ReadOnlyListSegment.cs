using System.Collections;

namespace Core.Extentions;


public class ReadOnlyListSegment<T> : IReadOnlyList<T>
{
    private readonly IReadOnlyList<T> _source;
    private readonly int _offset;
    private readonly int _count;

    public ReadOnlyListSegment(IReadOnlyList<T> source, int offset, int count)
    {
        _source = source;
        _offset = offset;
        _count = count;
    }

    public T this[int index] => _source[_offset + index];
    public int Count => _count;

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return _source[_offset + i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

