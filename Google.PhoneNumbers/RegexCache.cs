/*
 * Copyright (C) 2014 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;

namespace Google.PhoneNumbers
{
    internal class RegexCache
    {
        private readonly LruCache<String, JavaRegex> _cache;

        public RegexCache(int size)
        {
            _cache = new LruCache<String, JavaRegex>(size);
        }

        public JavaRegex getRegexForRegex(String regex)
        {
            var Regex = _cache.Get(regex);
            if (Regex == null)
            {
                Regex = new JavaRegex(regex);
                _cache.Add(regex, Regex);
            }
            return Regex;
        }

        // This method is used for testing.
        internal bool ContainsRegex(String regex)
        {
            return _cache.ContainsKey(regex);
        }
    }

    public class LruCache<K, V>
    {
        internal class Entry<TKey, TValue>
        {
            public Entry(TKey k, TValue v)
            {
                key = k;
                value = v;
            }
            public TKey key;
            public TValue value;
        }

        private readonly object _lock = new object();
        private readonly int _capacity;
        private readonly Dictionary<K, LinkedListNode<Entry<K, V>>> _lookUp;
        private readonly LinkedList<Entry<K, V>> _lastAccess; // Most recent is at the end

        public LruCache(int capacity)
        {
            _capacity = capacity;
            _lookUp = new Dictionary<K, LinkedListNode<Entry<K, V>>>();
            _lastAccess = new LinkedList<Entry<K, V>>();
        }

        public bool ContainsKey(K key)
        {
            lock (_lock)
            {
                return _lookUp.ContainsKey(key);
            }
        }

        public V Get(K key)
        {
            lock (_lock)
            {
                LinkedListNode<Entry<K, V>> node;

                if (_lookUp.TryGetValue(key, out node))
                {
                    var value = node.Value.value;
                    _lastAccess.Remove(node);
                    _lastAccess.AddLast(node);
                    return value;
                }
            }

            return default(V);
        }

        public void Add(K key, V val)
        {
            lock (_lock)
            {
                if (_lookUp.Count >= _capacity)
                {
                    RemoveFirst();
                }

                var item = new Entry<K, V>(key, val);
                var node = new LinkedListNode<Entry<K, V>>(item);

                _lastAccess.AddLast(node);
                _lookUp.Add(key, node);
            }
        }


        private void RemoveFirst()
        {
            var node = _lastAccess.First;
            _lastAccess.RemoveFirst();
            _lookUp.Remove(node.Value.key);
        }
    }
}
