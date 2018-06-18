using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System;

/// <summary>
/// 모든 벨류를 기본값으로 초기화 해주는 함수 구현을 요구하는 인터페이스
/// </summary>
public interface IReset {
    void Reset();
}

/// <summary>
/// Thread-Safety하지 않은 오브젝트 풀
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> where T : class, IReset {

    Stack<T> stack;
    private Func<T> objectMaker;
    
    public ObjectPool(Func<T> _objectMaker, int preAllocateSize){

        if (_objectMaker == null) throw new Exception("Need Object Maker");

        this.objectMaker = _objectMaker;
        stack = new Stack<T>(preAllocateSize);

        for (int i = 0; i < preAllocateSize; i++) {
            stack.Push(_objectMaker());
        }
    }

    public T GetObject() {
        if (stack.Peek() != null)
        {
            return stack.Pop();
        }
        else {
            return objectMaker();
        }
    }

    public void PutObject(T item) {
        item.Reset();
        stack.Push(item);
    }
}


public class ConcurrentObjectPool<T> where T : class, IReset {

    private ConcurrentBag<T> bag;
    private Func<T> objectMaker;

    public ConcurrentObjectPool(Func<T> _objectMaker, int preAllocateSize) {
        if (_objectMaker == null) throw new Exception("Need Object Maker");

        bag = new ConcurrentBag<T>();
        objectMaker = _objectMaker;

        for (int i = 0; i < preAllocateSize; i++) {
            bag.Add(objectMaker());
        }
    }


    public T GetObject()
    {
        T result;

        if (bag.TryTake(out result))
        {
            return result;
        }
        else
        {
            return objectMaker();
        }
    }

    public void PutObject(T item) {
        bag.Add(item);
    }
}