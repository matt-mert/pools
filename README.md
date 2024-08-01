# Pools
### A typesafe, lightweight pooling lib for Unity.
---
Heavily inspired by Signals by Yanko Oliveira, uses Unity Engine's ObjectPool

Author: matt-mert

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


Hit me up on [twitter](https://twitter.com/mertkucukakinci) for any suggestions or questions.
