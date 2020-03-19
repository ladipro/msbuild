// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.Build.Shared;
using Microsoft.Build.Collections;

namespace Microsoft.Build.Construction
{
    /// <summary>
    /// This class will cache string values for loaded Xml files.
    /// </summary>
    [DebuggerDisplay("#Strings={Count}")]
    internal class ProjectStringCache
    {
        private WeakValueDictionary<StringCacheEntry, string> _strings = new WeakValueDictionary<StringCacheEntry, string>();

        /// <summary>
        /// Locking object for this shared cache
        /// </summary>
        private Object _locker = new Object();

        /// <summary>
        /// Obtain the number of entries contained in the cache.
        /// </summary>
        internal int Count
        {
            get
            {
                lock (_locker)
                {
                    _strings.Scavenge();
                    return _strings.Count;
                }
            }
        }

        /// <summary>
        /// Add the given string to the cache or return the existing string if it is already
        /// in the cache.
        /// Constant time operation.
        /// </summary>
        public string Add(string key)
        {
            if (key.Length == 0)
            {
                return String.Empty;
            }

            // see Microsoft.Build.BackEnd.BuildRequestConfiguration.CreateUniqueGlobalProperty
            if (key.StartsWith(MSBuildConstants.MSBuildDummyGlobalPropertyHeader, StringComparison.Ordinal))
            {
                return key;
            }

            lock (_locker)
            {
                StringCacheEntry entry = new StringCacheEntry(key);
                if (!_strings.TryGetValue(entry, out string cachedString))
                {
                    WeakReference<string> weakRef = entry.MakeWeak();
                    _strings.SetWeakReference(entry, weakRef);
                    cachedString = key;
                }

                return cachedString;
            }
        }

        /// <summary>
        /// Find the matching string in the cache.
        /// Constant time operation.
        /// </summary>
        /// <param name="key">String to find in the cache.</param>
        /// <returns>Existing string in the cache, or null if it is not contained.</returns>
        public string Get(string key)
        {
            lock (_locker)
            {
                ErrorUtilities.VerifyThrowArgumentNull(key, "key");

                if (key.Length == 0)
                {
                    return String.Empty;
                }

                if (_strings.TryGetValue(new StringCacheEntry(key), out string cachedString))
                {
                    return cachedString;
                }

                return null;
            }
        }

        /// <summary>
        /// Represents an entry in the ProjectStringCache.
        /// </summary>
        [DebuggerDisplay("Value={Value}")]
        private struct StringCacheEntry
        {
            private string _string;
            private readonly int _hashCode;
            private WeakReference<string> _weakReference;

            internal StringCacheEntry(string str)
            {
                _string = str;
                _hashCode = str.GetHashCode();
                _weakReference = null;
            }

            public WeakReference<string> MakeWeak()
            {
                if (_weakReference == null)
                {
                    _weakReference = new WeakReference<string>(_string);
                    _string = null;
                }
                return _weakReference;
            }

            public string Value
            {
                get
                {
                    if (_string != null)
                    {
                        return _string;
                    }
                    if (_weakReference != null && _weakReference.TryGetTarget(out string value))
                    {
                        return value;
                    }
                    return null;
                }
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                if (obj is StringCacheEntry other)
                {
                    if (_weakReference != null && Object.ReferenceEquals(_weakReference, other._weakReference))
                    {
                        return true;
                    }

                    // StringCacheEntry with a collected weak reference is not equal to any other object.
                    string str1 = this.Value;
                    if (str1 != null)
                    {
                        return str1.Equals(other.Value);
                    }
                }
                return false;
            }
        }
    }
}
