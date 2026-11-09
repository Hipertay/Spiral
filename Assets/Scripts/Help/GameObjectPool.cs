using DG.Tweening;
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Пул объектов сцены. Экземпляры берутся из готовых кэшей (Caches),
/// а если префаба там нет — кэш создаётся автоматически при первом Spawn.
/// </summary>
public class GameObjectPool : MonoBehaviour
{
	static GameObjectPool Pool;

	[Tooltip("Сколько экземпляров префаба создавать при первом спавне, если его нет в Caches")]
	[SerializeField] private int defaultCacheSize = 8;

	public ObjectCache[] caches;

	public Hashtable activeCachedObjects;

    [Serializable]

	public class ObjectCache
	{
        [SerializeField]
		public GameObject prefab;       
		[SerializeField]
		public int cacheSize = 10;
        [Tooltip("До какого размера можно увеличивать кэш, если все объекты заняты")]
        [SerializeField]
        private int maxCacheSize = 64;

		private int cacheIndex = 0;
		private GameObject[] objects;

        /// <summary>Локальные трансформации префаба — чтобы спавн был как Instantiate(prefab, parent).</summary>
        public Vector3 PrefabLocalPosition { get; private set; }
        public Quaternion PrefabLocalRotation { get; private set; }
        public Vector3 PrefabLocalScale { get; private set; }

		[HideInInspector]
		public void Initialize()
		{
            // Повторная инициализация затёрла бы уже созданные экземпляры.
            if (objects != null && objects.Length > 0) return;

            CapturePrefabTransform();

			objects = new GameObject[Mathf.Max(1, cacheSize)];
			for (int i = 0; i < objects.Length; i++)
                objects[i] = CreateInstance();
		}

        private void CapturePrefabTransform()
        {
            if (prefab == null) return;

            Transform t = prefab.transform;
            PrefabLocalPosition = t.localPosition;
            PrefabLocalRotation = t.localRotation;
            PrefabLocalScale = t.localScale;
        }

        private GameObject CreateInstance()
        {
            if (prefab == null) return null;

            GameObject obj = MonoBehaviour.Instantiate(prefab) as GameObject;
            if (obj == null) return null;

            obj.name = prefab.name + "_pooled";
            obj.SetActive(false);
            return obj;
        }

		public GameObject GetNextObjectInCache()
		{
            if (prefab == null) return null;
            if (objects == null || objects.Length == 0) Initialize();

            // Ищем свободный экземпляр; уничтоженный вручную — пересоздаём.
            for (int i = 0; i < objects.Length; i++)
            {
                int index = (cacheIndex + i) % objects.Length;
                GameObject obj = objects[index];

                if (obj == null)
                {
                    objects[index] = CreateInstance();
                    obj = objects[index];
                }

                if (obj != null && !obj.activeSelf)
                {
                    cacheIndex = (index + 1) % objects.Length;
                    return obj;
                }
            }

            // Все экземпляры заняты — расширяем кэш, чтобы не обрывать уже играющие объекты.
            int limit = Mathf.Max(cacheSize, maxCacheSize);
            if (objects.Length < limit)
            {
                Array.Resize(ref objects, objects.Length + 1);
                objects[objects.Length - 1] = CreateInstance();
                cacheIndex = 0;
                return objects[objects.Length - 1];
            }

            // Дошли до предела — переиспользуем следующий по кругу.
            GameObject fallback = objects[cacheIndex];
            cacheIndex = (cacheIndex + 1) % objects.Length;
            return fallback;
		}               
	}

	void Awake()
	{
        Pool = this;
		activeCachedObjects = new Hashtable();

		if (caches == null) return;

		for (int i = 0; i < caches.Length; i++)
		{
			if (caches[i] == null) continue;
			caches[i].Initialize();
		}
	}

	/// <summary>Спавн объекта в точке мира.</summary>
	public static GameObject Spawn ( GameObject prefab, Vector3 position, Quaternion rotation )
	{
		return SpawnInternal(prefab, position, rotation, null);
	}

	/// <summary>Спавн как Instantiate(prefab, parent): объект сразу становится ребёнком с локальными трансформациями префаба.</summary>
	public static GameObject Spawn ( GameObject prefab, Transform parent )
	{
		return SpawnInternal(prefab, Vector3.zero, Quaternion.identity, parent);
	}

	private static GameObject SpawnInternal ( GameObject prefab, Vector3 position, Quaternion rotation, Transform parent )
	{
		if (prefab == null) return null;

		ObjectCache cache = GetCache(prefab);

		GameObject obj = cache != null ? cache.GetNextObjectInCache() : null;

		// Пула в сцене нет или кэш не создался — обычный Instantiate.
		if (obj == null)
		{
			if (parent != null)
				return GameObject.Instantiate(prefab, parent) as GameObject;

			return GameObject.Instantiate(prefab, position, rotation) as GameObject;
		}

		Transform t = obj.transform;

		if (parent != null)
		{
			t.SetParent(parent, false);
			t.localPosition = cache.PrefabLocalPosition;
			t.localRotation = cache.PrefabLocalRotation;
			t.localScale = cache.PrefabLocalScale;
		}
		else
		{
			// Возвращённый в пул объект мог остаться чьим-то ребёнком — отцепляем.
			if (t.parent != null) t.SetParent(null, false);

			t.SetPositionAndRotation(position, rotation);
		}

		obj.SetActive(true);

		if (Pool != null && Pool.activeCachedObjects != null)
			Pool.activeCachedObjects[obj] = true;

		return obj;
	}

	private static ObjectCache GetCache ( GameObject prefab )
	{
		if (prefab == null || Pool == null) return null;

		ObjectCache cache = Pool.FindCache(prefab);
		if (cache != null) return cache;

		// Префаба нет в Caches — создаём кэш на лету.
		return Pool.CreateCache(prefab, Pool.defaultCacheSize);
	}

	private ObjectCache FindCache ( GameObject prefab )
	{
		if (caches == null) return null;

		for (int i = 0; i < caches.Length; i++)
		{
			if (caches[i] != null && caches[i].prefab == prefab) return caches[i];
		}

		return null;
	}

	private ObjectCache CreateCache ( GameObject prefab, int size )
	{
		ObjectCache cache = new ObjectCache { prefab = prefab, cacheSize = Mathf.Max(1, size) };
		cache.Initialize();

		int length = caches != null ? caches.Length : 0;
		Array.Resize(ref caches, length + 1);
		caches[length] = cache;

		return cache;
	}

	/// <summary>
	/// Вернуть объект в пул (SetActive(false)) или удалить, если он не из пула.
	/// Безопасная замена Destroy для всех спавнутых объектов.
	/// </summary>
	public static void Unspawn( GameObject objectToDestroy )
	{
		if ( objectToDestroy == null ) return;

		if ( Pool != null && Pool.activeCachedObjects != null && Pool.activeCachedObjects.ContainsKey(objectToDestroy) ) 
		{
			// Твины не должны продолжать менять трансформ, пока объект лежит в пуле.
			objectToDestroy.transform.DOKill();

			objectToDestroy.SetActive(false);
			objectToDestroy.transform.SetParent(null, false);

			Pool.activeCachedObjects[objectToDestroy] = false;
		}
		else
		{ 
			// Объект не из пула (например, пул ещё не инициализирован или это обычный Instantiate) —
			// удаляем. Раньше здесь был рекурсивный вызов Unspawn, из-за которого шло переполнение стека.
			Destroy ( objectToDestroy );
		}
	}
}