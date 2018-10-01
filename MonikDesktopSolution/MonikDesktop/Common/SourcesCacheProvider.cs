﻿using MonikDesktop.Common.Interfaces;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;

namespace MonikDesktop.Common
{
    public class SourcesCacheProvider : ISourcesCacheProvider
    {
        private readonly Dictionary<Uri, ISourcesCache> _dic = new Dictionary<Uri, ISourcesCache>();
        private readonly Func<Uri, ISourcesCache> _cacheFactory;

        public SourcesCacheProvider(Func<Uri, ISourcesCache> cacheFactory)
        {
            _cacheFactory = cacheFactory;
        }

        [Reactive] public ISourcesCache CurrentCache { get; set; }

        public void SetCurrentCacheSource(Uri url)
        {
            CurrentCache = GetCacheByUrl(url);
        }

        private ISourcesCache GetCacheByUrl(Uri url)
        {
            if (!_dic.ContainsKey(url))
            {
                var cache = _cacheFactory(url);
                _dic[url] = cache;
                return cache;
            }
            else
                return _dic[url];
        }
    }
}