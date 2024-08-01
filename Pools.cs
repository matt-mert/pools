// ========================================================================================
// Pools - A typesafe, lightweight pooling lib for Unity.
// ========================================================================================
// 2024, Mert Kucukakinci  / http://linkedin.com/mert-kucukakinci
// ========================================================================================
// Heavily inspired by Signals by Yanko Oliveira
//
// Usage:
//    1) Define your pool, eg:
//          SomePool : APool<SomeObject> {}
//    2) Initialize your pools, eg on Awake():
//          Pools.Get<SomePool>().Initialize(someObject);
//    3) Grab objects from the pool, eg:
//          Pools.Get<SomePool>().GrabObject();
//    4) Release objects to the pool, eg:
//          Pools.Get<SomePool>().ReleaseObject(grabbedObject);
//    5) If you don't want to use global Pools, you can have your very own PoolHub
//       instance in your class
//
// ========================================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace mattmert.Utils
{
    /// <summary>
    /// Base interface for Pools
    /// </summary>
    public interface IPool
    {
        string Hash { get; }
    }

    /// <summary>
    /// Pools main facade class for global, game-wide pools
    /// </summary>
    public static class Pools
    {
        public static int DefaultCapacity = 10;
        public static int DefaultMaxSize = 1000;
        
        private static readonly PoolHub Hub = new();

        public static T Get<T>() where T : IPool, new()
        {
            return Hub.Get<T>();
        }

        public static void SetDefaultCapacity(int capacity)
        {
            DefaultCapacity = capacity;
        }

        public static void SetDefaultMaxSize(int maxSize)
        {
            DefaultMaxSize = maxSize;
        }

        public static void ClearPoolFromHash<T>(string poolHash) where T : Component
        {
            Hub.ClearPoolFromHash<T>(poolHash);
        }

        public static void DisposePoolFromHash<T>(string poolHash) where T : Component
        {
            Hub.DisposePoolFromHash<T>(poolHash);
        }
    }

    /// <summary>
    /// A hub for Pools you can implement in your classes
    /// </summary>
    public class PoolHub
    {
        private readonly Dictionary<Type, IPool> _pools = new();

        /// <summary>
        /// Getter for a pool of a given type
        /// </summary>
        /// <typeparam name="T">Type of pool</typeparam>
        /// <returns>The proper pool binding</returns>
        public T Get<T>() where T : IPool, new()
        {
            var poolType = typeof(T);

            if (_pools.TryGetValue(poolType, out var pool))
            {
                return (T)pool;
            }

            return (T)Bind(poolType);
        }

        /// <summary>
        /// Manually provide a PoolHash and clear the corresponding pool
        /// (you most likely want to use a Clear, unless you know exactly
        /// what you are doing)
        /// </summary>
        /// <param name="poolHash">Unique hash for pool</param>
        public void ClearPoolFromHash<T>(string poolHash) where T : Component
        {
            var pool = GetPoolByHash(poolHash);
            if (pool is APool<T> aPool)
            {
                aPool.Clear();
            }
        }

        /// <summary>
        /// Manually provide a PoolHash and dispose the corresponding pool
        /// (you most likely want to use a Dispose, unless you know exactly
        /// what you are doing)
        /// </summary>
        /// <param name="poolHash">Unique hash for signal</param>
        public void DisposePoolFromHash<T>(string poolHash) where T : Component
        {
            var pool = GetPoolByHash(poolHash);
            if (pool is APool<T> aPool)
            {
                aPool.Dispose();
            }
        }

        private IPool Bind(Type poolType)
        {
            if (_pools.TryGetValue(poolType, out var pool))
            {
                Debug.LogError($"Pool already registered for type {poolType}");
                return default;
            }

            pool = (IPool)Activator.CreateInstance(poolType);
            _pools.Add(poolType, pool);
            return pool;
        }
        
        // private IPool Bind<T>() where T : IPool, new()
        // {
        //     return Bind(typeof(T));
        // }
        
        private IPool GetPoolByHash(string signalHash)
        {
            foreach (var signal in _pools.Values)
            {
                if (signal.Hash == signalHash)
                {
                    return signal;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Abstract class for Pools, provides hash by type functionality
    /// </summary>
    public abstract class ABasePool : IPool
    {
        private string _hash;

        /// <summary>
        /// Unique id for this pool
        /// </summary>
        public string Hash
        {
            get
            {
                if (string.IsNullOrEmpty(_hash))
                {
                    _hash = GetType().ToString();
                }

                return _hash;
            }
        }
    }

    /// <summary>
    /// Strongly typed pools
    /// </summary>
    public abstract class APool<T> : ABasePool where T : Component
    {
        private ObjectPool<T> _pool;
        private Transform _root;
        private T _prefab;
        private bool _initialized;

        public void Initialize(T prefab, int capacity = -1, int maxSize = -1, Transform root = null)
        {
            if (_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is already initialized!");
                return;
            }

            if (capacity == -1)
                capacity = Pools.DefaultCapacity;

            if (maxSize == -1)
                maxSize = Pools.DefaultMaxSize;

            _prefab = prefab;
            _root = root == null ? new GameObject($"Pool_{typeof(T).ToString().Split(".")[^1]}").transform : root;
            _pool = new ObjectPool<T>(CreateFunc, ActionOnGet, ActionOnRelease,
                ActionOnDestroy, true, capacity, maxSize);

            _initialized = true;
        }

        public T Grab()
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return default;
            }
            
            return _pool.Get();
        }

        public void Release(T item)
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return;
            }
            
            _pool.Release(item);
        }
        
        public void Prefill(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                var t = _pool.Get();
                _pool.Release(t);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public void Dispose()
        {
            _pool.Dispose();
        }
        
        protected virtual void ActionOnDestroy(T obj)
        {
            UnityEngine.Object.Destroy(obj.gameObject);
        }
        
        protected virtual void ActionOnRelease(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_root);
        }
        
        protected virtual void ActionOnGet(T obj)
        {
            obj.gameObject.SetActive(true);
        }
        
        protected virtual T CreateFunc()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _root);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
