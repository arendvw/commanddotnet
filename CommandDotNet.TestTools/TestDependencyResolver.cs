﻿using System;
using System.Collections;
using System.Collections.Generic;
using CommandDotNet.Builders;

namespace CommandDotNet.TestTools
{
    public class TestDependencyResolver : IDependencyResolver, IEnumerable<object>
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public object Resolve(Type type)
        {
            if (!_services.ContainsKey(type))
            {
                throw new Exception($"Dependency not registered: {type}");
            }
            return _services[type];
        }

        public bool TryResolve(Type type, out object item)
        {
            return _services.TryGetValue(type, out item);
        }

        public void Add(object service)
        {
            // don't allow accidental overwrite
            _services.Add(service.GetType(), service);
        }

        public IEnumerator<object> GetEnumerator()
        {
            return _services.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}