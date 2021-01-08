using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private Dictionary<int, Stack<GameObject>> allPools;
    private GameObject container;

    public void SetContainer(GameObject _container)
    {
        container = _container;
    }
    
    private GameObject NewPoolItem(GameObject _itemPrefab)
    {
        var newItem = Instantiate(_itemPrefab, this.transform);
        newItem.SetActive(false);
        return newItem;
    }

    public void CreatePool(GameObject _itemPrefab, int _size)
    {
        if(allPools == null){
            allPools = new Dictionary<int, Stack<GameObject>>();
        }
        var stack = new Stack<GameObject>();
        for (int i = 0; i < _size; i++) {
            stack.Push(NewPoolItem(_itemPrefab));
        }
        print("adding");
        allPools.Add(_itemPrefab.GetInstanceID(), stack);
    }

    public GameObject GetFromPool(GameObject _itemPrefab)
    {
        GameObject poolItem;
        var key = _itemPrefab.GetInstanceID();
        if(allPools[key].Count > 0){
            poolItem = allPools[key].Pop();
        } else {
            poolItem = NewPoolItem(_itemPrefab);
        }
        poolItem.transform.parent = container.transform;
        poolItem.transform.localScale = Vector3.one;
        poolItem.SetActive(true);
        return poolItem;
    }

    public void ReturnToPool(GameObject _itemPrefab, GameObject _item)
    {
        var key = _itemPrefab.GetInstanceID();
        _item.SetActive(false);
        _item.transform.parent = this.transform;
        _item.transform.localScale = Vector3.one;
        _item.transform.rotation = Quaternion.identity;
        if (_item.GetComponent<ParticleSystem>()) {
            _item.GetComponent<ParticleSystem>().Stop();
        }
        allPools[key].Push(_item);
    }

    public void ApplyPrefabScale(GameObject _itemPrefab, GameObject _item) {
        _item.transform.localScale = _itemPrefab.transform.localScale;
    }


}
