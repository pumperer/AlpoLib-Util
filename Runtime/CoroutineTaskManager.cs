using System;
using System.Collections;
using UnityEngine;

namespace alpoLib.Util
{
	/// <summary>
	/// Mono 가 아닌 곳에서 코루틴을 돌리고 싶을 때 사용할 수 있다.
	/// </summary>
	public sealed class CoroutineTaskManager : SingletonMonoBehaviour<CoroutineTaskManager>
	{
		public static Coroutine AddTask(IEnumerator routine)
		{
			try
			{
				if (!Application.isPlaying)
				{
					while (routine.MoveNext())
					{
					}

					return null;
				}

				return Instance.StartCoroutine(routine);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning(e.StackTrace);
				Debug.LogException(e);
				return null;
			}
		}

		public static void RemoveTask(Coroutine task)
		{
			if (Instance != null)
				Instance.StopCoroutine(task);
		}

		public static void ClearTask()
		{
			if (Instance != null)
				Instance.StopAllCoroutines();
		}

		public static void RunDeferred(YieldInstruction yi, Action action)
		{
			AddTask(CR_Deferred(yi, action));
		}
		
		public static void RunDeferred(CustomYieldInstruction yi, Action action)
		{
			AddTask(CR_Deferred(yi, action));
		}

		private static IEnumerator CR_Deferred(YieldInstruction yi, Action action)
		{
			yield return yi;
			action?.Invoke();
		}
		
		private static IEnumerator CR_Deferred(CustomYieldInstruction yi, Action action)
		{
			yield return yi;
			action?.Invoke();
		}

		private void OnDisable()
		{
			StopAllCoroutines();
		}
	}
}