using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils
{
    public class BidirectionalMap<TFirst,TSecond>
    {
        private readonly Dictionary<TFirst, TSecond> _forward = new();
        private readonly Dictionary<TSecond, TFirst> _reverse = new();

        public TSecond this[TFirst first] => _forward[first];
        public TFirst this[TSecond second] => _reverse[second];

        public void Add(TFirst first, TSecond second)
        {
            if (_forward.ContainsKey(first) || _reverse.ContainsKey(second))
            {
                throw new ArgumentException("Identyfikatory muszą być unikalne w obu kierunkach!");
            }

            _forward.Add(first, second);
            _reverse.Add(second, first);
        }

        public bool TryGet(TFirst first, out TSecond second) => _forward.TryGetValue(first, out second);
        public bool TryGet(TSecond second, out TFirst first) => _reverse.TryGetValue(second, out first);

        public bool Contains(TFirst first) => _forward.ContainsKey(first);
        public bool Contains(TSecond second) => _reverse.ContainsKey(second);

        public void Clear()
        {
            _forward.Clear();
            _reverse.Clear();
        }

        public int Count => _forward.Count;
    }
}
