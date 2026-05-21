using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class TestPoolable : MonoBehaviour, IPoolable
    {
        public bool WasActivated { get; private set; }
        public bool WasReturned { get; private set; }
        public void OnGetFromPool() { WasActivated = true; WasReturned = false; }
        public void OnReturnToPool() { WasReturned = true; WasActivated = false; }
    }

    public class ObjectPoolTests
    {
        private GameObject _prefabGo;
        private TestPoolable _prefab;
        private GameObject _poolParentGo;

        [SetUp]
        public void SetUp()
        {
            _prefabGo = new GameObject("Prefab");
            _prefab = _prefabGo.AddComponent<TestPoolable>();
            _poolParentGo = new GameObject("PoolParent");
        }

        [TearDown]
        public void TearDown()
        {
            if (_prefabGo != null) Object.DestroyImmediate(_prefabGo);
            if (_poolParentGo != null) Object.DestroyImmediate(_poolParentGo);
        }

        [Test]
        public void Get_ReturnsActiveObject()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 2, _poolParentGo.transform);
            var obj = pool.Get();
            Assert.IsTrue(obj.gameObject.activeSelf);
        }

        [Test]
        public void Get_CallsOnGetFromPool()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 2, _poolParentGo.transform);
            var obj = pool.Get();
            Assert.IsTrue(obj.WasActivated);
        }

        [Test]
        public void Return_DeactivatesObject()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 2, _poolParentGo.transform);
            var obj = pool.Get();
            pool.Return(obj);
            Assert.IsFalse(obj.gameObject.activeSelf);
        }

        [Test]
        public void Get_AfterReturn_ReusesSameObject()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 1, _poolParentGo.transform);
            var first = pool.Get();
            pool.Return(first);
            var second = pool.Get();
            Assert.AreEqual(first, second);
        }
    }
}
