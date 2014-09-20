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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Google.PhoneNumbers
{
    class SynchronizedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private static readonly object _lock = new object();
        private Dictionary<TKey, TValue> _inner;

        internal SynchronizedDictionary()
        {
            _inner = new Dictionary<TKey, TValue>();
        }
        internal SynchronizedDictionary(Dictionary<TKey, TValue> inner)
        {
            _inner = inner;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (_lock)
            {
                return _inner.GetEnumerator();   
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock)
            {
                return _inner.GetEnumerator();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                _inner.Add(item.Key, item.Value);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _inner.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                return _inner.ContainsKey(item.Key) && _inner[item.Key].Equals(item.Value);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                return _inner.Remove(item.Key);
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _inner.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }         
        }

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                _inner.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_lock)
            {
                return _inner.ContainsKey(key);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                return _inner.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                return _inner.TryGetValue(key, out value);
            }
        }

        public TValue this[TKey key]
        {
            get {
                lock (_lock)
                {
                    return _inner[key];
                }
            }
            set {
                lock(_lock)
                {
                    _inner[key] = value;
                } 
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                lock (_lock)
                {
                    return _inner.Keys.ToList();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (_lock)
                {
                    return _inner.Values.ToList();
                }
            }
        }
    }
}
