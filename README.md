# Pools
### A typesafe, lightweight pooling lib for Unity.
---
Inspired by Signals by [Yanko Oliveira](https://github.com/yankooliveira).

You can find the repository of Signals by Yanko Oliveira [here](https://github.com/yankooliveira/signals).

Uses the ObjectPool of UnityEngine.

Author: [matt-mert](https://github.com/matt-mert)

### Usage:
1) Define your pool, eg:
```c#
public class SomePool : APool<SomeObject> {}
```
2) Initialize your pools, eg on Awake():
```c#
Pools.Get<SomePool>().Initialize(SomePrefab);
```
3) Grab objects from the pool, eg:
```c#
var obj = Pools.Get<SomePool>().Grab();
```
4) Release objects to the pool, eg:
```c#
Pools.Get<SomePool>().Release(obj);
```
5) You also have your local Pool Hub that is specific to a given object instead of using the global one. The syntax is exactly the same.
```c#
PoolHub environmentPools = new PoolHub();
var wall = environmentPools.Get<WallPool>().Grab();
```

