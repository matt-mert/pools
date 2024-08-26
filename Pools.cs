// ========================================================================================
// Pools - A typesafe, lightweight pooling lib for Unity.
// ========================================================================================
// 2024, Mert Kucukakinci  / http://github.com/matt-mert
// ========================================================================================
// Inspired by Signals by Yanko Oliveira, uses Unity Engine's ObjectPool
// You can find the repository of Signals at https://github.com/yankooliveira/signals
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

namespace mattmert.PoolSystem
{
    public interface IPool
    {
        string Hash { get; }
    }
    
    public static class Pools
    {
        public static int DefaultCapacity = 10;
        public static int DefaultMaxSize = 1000;
        
        private static readonly PoolHub _hub = new();
        
        public static T Get<T>() where T : IPool, new()
        {
            return _hub.Get<T>();
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
            _hub.ClearPoolFromHash<T>(poolHash);
        }
        
        public static void DisposePoolFromHash<T>(string poolHash) where T : Component
        {
            _hub.DisposePoolFromHash<T>(poolHash);
        }
    }
    
    public class PoolHub
    {
        private readonly Dictionary<Type, IPool> _pools = new();
        
        public T Get<T>() where T : IPool, new()
        {
            var poolType = typeof(T);
            
            if (_pools.TryGetValue(poolType, out var pool))
            {
                return (T)pool;
            }
            
            return (T)Bind(poolType);
        }
        
        public void ClearPoolFromHash<T>(string poolHash) where T : Component
        {
            var pool = GetPoolByHash(poolHash);
            if (pool is APool<T> aPool)
            {
                aPool.Clear();
            }
        }
        
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
        
        private IPool Bind<T>() where T : IPool, new()
        {
            return Bind(typeof(T));
        }
        
        private IPool GetPoolByHash(string boardHash)
        {
            foreach (var pool in _pools.Values)
            {
                if (pool.Hash == boardHash)
                {
                    return pool;
                }
            }
            
            return null;
        }
    }
    
    public abstract class ABasePool : IPool
    {
        private string _hash;
        
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
            
            if (prefab == null)
            {
                Debug.LogError($"Pool of type {typeof(T)} could not be initialized! Given prefab is null!");
                return;
            }
            
            if (capacity == -1)
                capacity = Pools.DefaultCapacity;
            
            if (maxSize == -1)
                maxSize = Pools.DefaultMaxSize;
            
            _prefab = prefab;
            _root = root == null ? new GameObject($"Pool_{prefab.gameObject.name}").transform : root;
            _pool = new ObjectPool<T>(CreateFunc, ActionOnGet, ActionOnRelease,
                ActionOnDestroy, true, capacity, maxSize);
            
            OnInitialize();
            _initialized = true;
        }
        
        protected virtual void OnInitialize() {}
        
        public T Grab()
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return default;
            }
            
            var item = _pool.Get();
            OnGrab(item);
            return item;
        }
        
        protected virtual void OnGrab(T item) {}
        
        public void Release(T item)
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return;
            }
            
            OnRelease(item);
            _pool.Release(item);
        }
        
        protected virtual void OnRelease(T item) {}
        
        public void Prefill(int amount)
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return;
            }
            
            for (var i = 0; i < amount; i++)
            {
                var t = _pool.Get();
                _pool.Release(t);
            }
        }
        
        public void Clear()
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return;
            }
            
            _pool.Clear();
            OnClear();
        }
        
        protected virtual void OnClear() {}
        
        public void Dispose()
        {
            if (!_initialized)
            {
                Debug.LogError($"Pool of type {typeof(T)} is not initialized yet!");
                return;
            }
            
            _pool.Dispose();
            OnDispose();
        }
        
        protected virtual void OnDispose() {}
        
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
