using System.Collections;
using UnityEngine;

namespace alpoLib.Util
{
	public abstract class SingletonMonoBehaviour<T> :  MonoBehaviour, IDontDestroyGameObject where T : MonoBehaviour
	{
		public virtual bool Preserve => false;
		public GameObject GameObject => gameObject;
		
		public static T Instance { get; protected set; }

		protected bool awakeComplete = false;

		[SerializeField] protected bool dontDestroy = false;

		public static bool IsExist
		{
			get
			{
				if (Instance == null || Instance.gameObject == null)
					return false;
				return true;
			}
		}

		protected virtual void OnAwakeEvent() { }
		protected virtual IEnumerator OnStartEvent() { yield return null; }
		protected virtual void OnDestroyEvent() { }

		private bool destroyingByDuplicated;

		public static void Init(bool dontDestroy)
		{
			if (IsExist)
				return;

			var go = new GameObject(typeof(T).Name);
			var comp = go.AddComponent<T>();
			if (comp is not SingletonMonoBehaviour<T> s)
				return;
			
			s.dontDestroy = dontDestroy;
			s.ProcessDontDestroy();
		}

		public static void Init<TW>(bool dontDestroy) where TW : T
		{
			if (IsExist)
				return;

			var go = new GameObject(typeof(TW).Name);
			var comp = go.AddComponent<TW>();
			if (comp is not SingletonMonoBehaviour<TW> s)
				return;

			s.dontDestroy = dontDestroy;
			s.ProcessDontDestroy();
		}
            
		
		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				destroyingByDuplicated = true;
				Destroy(gameObject);
				Debug.LogWarning($"An instance of {name} singleton already exists... destroy new one.");
			}
			else
			{
				Instance = this as T;
				if (dontDestroy)
				{
					ProcessDontDestroy();
				}

				OnAwakeEvent();
				awakeComplete = true;
			}
		}

		private void ProcessDontDestroy()
		{
			DontDestroyOnLoad(gameObject);
			DontDestroyGameObject.AddDontDestroyGameObject(this);
		}

		private IEnumerator Start()
		{
			yield return new WaitUntil(() => awakeComplete);
			yield return OnStartEvent();
		}

		private void OnDestroy()
		{
			if (!destroyingByDuplicated)
			{
				OnDestroyEvent();
				Instance = null;
			}

			destroyingByDuplicated = false;
		}
	}
}