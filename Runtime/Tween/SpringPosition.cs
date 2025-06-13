using UnityEngine;

namespace alpoLib.Util
{
	public class SpringPosition : MonoBehaviour
	{
		public static SpringPosition Current;

		[SerializeField] protected Vector3 target = Vector3.zero;
		[SerializeField] protected float strength = 10f;
		[SerializeField] protected bool worldSpace;
		[SerializeField] protected bool ignoreTimeScale;

		public delegate void OnFinishedDelegate();

		public OnFinishedDelegate OnFinished;

		// Deprecated functionality
		// [SerializeField] [HideInInspector] GameObject eventReceiver = null;
		// [SerializeField] [HideInInspector] public string callWhenFinished;

		private Transform _trans;
		private float _threshold = 0f;

		protected void Start()
		{
			_trans = transform;
		}

		private void Update()
		{
			float delta = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

			if (worldSpace)
			{
				if (_threshold == 0f) _threshold = Mathf.Min((target - _trans.position).magnitude * 0.01f, 0.01f);
				_trans.position = NGUIMath.SpringLerp(_trans.position, target, strength, delta);

				if (_threshold * _threshold >= (target - _trans.position).sqrMagnitude)
				{
					_trans.position = target;
					NotifyListeners();
					enabled = false;
				}
			}
			else
			{
				if (_threshold == 0f) _threshold = Mathf.Min((target - _trans.localPosition).magnitude * 0.01f, 0.01f);
				_trans.localPosition = NGUIMath.SpringLerp(_trans.localPosition, target, strength, delta);

				if (_threshold * _threshold >= (target - _trans.localPosition).sqrMagnitude)
				{
					_trans.localPosition = target;
					NotifyListeners();
					enabled = false;
				}
			}

			// Ensure that the scroll bars remain in sync
			if (mSv != null) mSv.QueueUpdateScrollbars();
		}

		/// <summary>
		/// Immediately finish the animation.
		/// </summary>

		public void Finish()
		{
			if (enabled)
			{
				if (worldSpace) transform.position = target;
				else transform.localPosition = target;

				NotifyListeners();
				enabled = false;

				// Ensure that the scroll bars remain in sync
				if (mSv != null) mSv.QueueUpdateScrollbars();
			}
		}

		/// <summary>
		/// Notify all finished event listeners.
		/// </summary>

		void NotifyListeners()
		{
			Current = this;

			if (onFinished != null) onFinished();

			if (eventReceiver != null && !string.IsNullOrEmpty(callWhenFinished))
				eventReceiver.SendMessage(callWhenFinished, this, SendMessageOptions.DontRequireReceiver);

			Current = null;
		}

		/// <summary>
		/// Start the tweening process.
		/// </summary>

		static public SpringPosition Begin(GameObject go, Vector3 pos, float strength)
		{
			var sp = go.GetComponent<SpringPosition>();
			if (sp == null) sp = go.AddComponent<SpringPosition>();
			sp.target = pos;
			sp.strength = strength;
			sp.onFinished = null;
			if (!sp.enabled) sp.enabled = true;
			return sp;
		}
	}
}