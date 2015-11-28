using System;
using System.Collections.Generic;
using UnityEngine;
using ToolPack;
using System.Collections;



public class ObjectPool : Singleton<ObjectPool> {


	Dictionary<string,GameObject> _prefabDict;
	Dictionary<string,HashSet<GameObject>> _poolDict;
	Dictionary<int,string> _pathDict;
	

	void Awake()
	{
		_prefabDict = new Dictionary<string, GameObject>(); // path vs prefab
		_pathDict   = new Dictionary<int, string>();        //instanceid vs path
		_poolDict   = new Dictionary<string, HashSet<GameObject>>(); //path vs unused gameObject

	}

	public static void Preload(string path,int count)
	{
		var curInstance = ObjectPool.Instance;
		if(curInstance != null)
		{
			curInstance.IPreload(path,count);
		}
	}
	void IPreload(string path,int count)
	{
		GameObject curPrefab = CheckAndAddToPrefabDict(path);

		HashSet<GameObject> curSet = null;
		if(!_poolDict.TryGetValue(path,out curSet))
		{
			curSet = new HashSet<GameObject>();
			_poolDict.Add(path,curSet);
			DoLoad(curPrefab,count,path);
		}else
		{
//			Debug.LogError("Shouldn't preload twice");
		}


	}

	public static void CleanHash()
	{

	}

	public void ITestOut()
	{
//		foreach(var curP in _pathDict)
//		{
//			Debug.Log("path is " + curP.ToString());
//		}
//
//		foreach(var curP in _prefabDict)
//		{
//			Debug.Log ("prefab key is " + curP.Key);
//		}

		foreach(var curP in _poolDict)
		{
			var curCount = curP.Value.Count;
			Debug.Log("pool is " + curP.Key + " count is " + curCount);
		}
	}
	
	void DoLoad(GameObject curPrefab,int count,string path)
	{
		for(int i = 0 ; i < count; i ++)
		{
			var obj = Instantiate<GameObject>(curPrefab);

			DontDestroyOnLoad(obj);
			
			obj.transform.SetParent(this.transform);
			obj.transform.position = new Vector3(-100f,-100f,0);
			StartCoroutine(DoActiveFalse(obj));


			
			int curInstanceId = obj.GetInstanceID();
			
			if(!_pathDict.ContainsKey(curInstanceId))
			{
				_pathDict.Add(curInstanceId,path);
			}else
			{
				Debug.LogWarning("Current Instance has exist");
			}

			if(!_poolDict.ContainsKey(path))
			{
				Debug.LogWarning("Path not exist path is " + path);
			}else
			{
				var curSet = _poolDict[path];
				curSet.Add(obj);
			}

//			yield return null;
		}
	}

	IEnumerator DoActiveFalse(GameObject obj)
	{
		yield return new WaitForSeconds(5f);
		obj.SetActive(false);
		obj.transform.position = new Vector3(0,0,0);
	}
	

	public static void Reset()
	{
		var curInstance = ObjectPool.Instance;
		if(curInstance != null)
		{
			curInstance.Ireset();
		}
	}

	void Ireset()
	{
		_prefabDict.Clear();
		_pathDict.Clear();
		foreach(var curPair in _poolDict)
		{
			var curSet = curPair.Value;
			foreach(var curObj in curSet)
			{
				if(curObj != null)
				{
					Destroy(curObj);
				}else
				{
				
					Debug.LogWarning("GameObject shouldn't be null ,path is " + curPair.Key);
				}
			}

		}
		_poolDict.Clear();
	}

	public static GameObject GetGameObject(string path,bool onlyPooled = false)
	{
		var curInstance = ObjectPool.Instance;
		if(curInstance == null)
		{
			return null;
		}
		return curInstance.IGetGameObject(path,onlyPooled);
	}

	GameObject GenGameObject(string path)
	{
		var curPrefab = Resources.Load<GameObject>(path);
		if(curPrefab != null)
		{
			return Instantiate<GameObject>(curPrefab);
		}else
		{
			Debug.LogError("Prefab not exist ! path is "+ path);
		}
		return null;
	}

	GameObject IGetGameObject(string path,bool onlyPooled = true)
	{

		GameObject curObject = null;
		HashSet<GameObject> curSet = null;
		if(_poolDict.TryGetValue(path,out curSet))
		{
			if(curSet.Count > 0 )
			{
				var curHash = curSet.GetEnumerator();
				GameObject curObj = null;
				int i = 0 ;
				while (curHash.MoveNext())
				{
					i++;
					curObj = curHash.Current;
					if(curObj != null && curObj.activeSelf == false)
					{
						break;
					}
				}

				if(i > 1){
					Debug.LogWarning("Empty num is " + i );
				}

				if(curObj == null)
				{

					Debug.LogWarning("count is " + curSet.Count);
					Debug.LogWarning("GameObject be null unexpected path is " + path );
					curObject = GenGameObject(path);
				}else
				{
					curSet.Remove(curObj);
					curObject = curObj;
				}


			}
		}else
		{
			Debug.LogWarning("PATH NOT IN DICT " + path);
		}

		if (!onlyPooled && curObject == null)
		{
			GameObject curPrefab = CheckAndAddToPrefabDict(path);
			GameObject newObj = Instantiate(curPrefab ) as GameObject;
			Debug.LogWarning("No Object in poll,create path " + path);

			curObject = newObj;
		}

		if(curObject != null)
		{
			int id = curObject.GetInstanceID();
			if(!_pathDict.ContainsKey(id))
			{
				_pathDict.Add(id,path);
			}
			curObject.transform.localPosition = new Vector3(0,0,0);
//			curObject.transform.parent = null;

			curObject.SetActive(true);
		}
		return curObject;
	}

	static void PollObject(GameObject obj,float delay = 0f)
	{
		var curInstance = ObjectPool.Instance;
		if(curInstance == null)
		{
			return;
		}
		curInstance.StartCoroutine(curInstance.DoPool(obj,delay));
	}

	IEnumerator  DoPool(GameObject obj,float delay = 0f)
	{
		yield return new WaitForSeconds(delay);
		var curInstance = ObjectPool.Instance;
		if(curInstance != null)
		{	
			curInstance.IPoolObject(obj);
		}
	

	}
	void IPoolObject(GameObject obj)
	{
		if(obj == null)
		{
			Debug.LogError("obj shouldn't be null");
			return;
		}

		int id = obj.GetInstanceID();
		string path = string.Empty;
		if(!_pathDict.TryGetValue(id,out path))
		{
			Debug.LogWarning("Not exist in pathdict! Obj name is " + obj.name);
			Destroy(obj);
			return;
		}

		HashSet<GameObject> curSet;
		if(!_poolDict.TryGetValue(path,out curSet))
		{
			Debug.LogWarning("Current pool not exist");
			Destroy(obj);
			return;
		}
		obj.SetActive(false);
		obj.transform.SetParent(transform);
//		obj.StopParticle();
		curSet.Add(obj);

	}

	GameObject CheckAndAddToPrefabDict(string path)
	{
		GameObject curPrefab;
		if(!_prefabDict.TryGetValue(path,out curPrefab))
		{
			curPrefab = Resources.Load<GameObject>(path);
			if(curPrefab == null)
			{
				Debug.LogError("Prefab not exist,path is " + path);
			}
			_prefabDict.Add(path,curPrefab);

		}

		return curPrefab;
	}

	public static void PoolDestroy(GameObject obj,float delay = 0f)
	{
		var curInstance = ObjectPool.Instance;
		if(curInstance == null)
		{
			return;
		}



		curInstance.IPoolDestory(obj,delay);
	}

	void IPoolDestory(GameObject obj,float delay = 0f,int nodeCount = 0 )
	{
		if(obj == null)
		{
			return;
		}
		int ID = obj.GetInstanceID();
		if(_pathDict.ContainsKey(ID))
		{
			obj.transform.SetParent(transform);
			PollObject(obj);
		}else
		{
			if(nodeCount == 0 )
			{
				int count = obj.transform.childCount;
				Transform[] curTransforms = new Transform[count];
				for(int i = 0 ; i  < count;++i)
				{
					curTransforms[i] = obj.transform.GetChild(i);
				}

				for(int i = 0 ; i < curTransforms.Length;i ++)
				{
					var curTrans = curTransforms[i];
					var curObj = curTrans.gameObject;
					var curCount = nodeCount+1;
					IPoolDestory(curObj,delay,curCount);
				}


			}
			Destroy(obj,delay);
			
		}
	}


	
	

}
