using System;
using System.Collections.Generic;

namespace alpoLib.Util
{
    public class TaskScheduler : SingletonMonoBehaviour<TaskScheduler>
    {
        public interface ITask
        {
			string Name { get; }
			void Reset();
			void Cancel();
		}

        private interface ITaskInternal : ITask
        {
			bool ShouldDestroy { get; }
			void OnUpdate();
			void Pause();
			void Resume();
		}

        public abstract class TaskBase : ITaskInternal
        {
	        public abstract string Name { get; }

	        public abstract void Reset();

	        public abstract void Cancel();
	        
	        public abstract bool ShouldDestroy { get; }

	        public abstract void OnUpdate();

	        public abstract void Pause();

	        public abstract void Resume();

	        private ITimeGetter TimeGetter { get; }
	        
	        protected TaskBase(ITimeGetter timeGetter)
	        {
		        TimeGetter = timeGetter ?? throw new ArgumentNullException(nameof(timeGetter));
	        }

	        protected DateTimeOffset GetTime()
	        {
		        return TimeGetter.GetTime();
	        }
        }

        public class StopWatch : TaskBase
        {
	        private string name;
	        
	        public override string Name => name;
	        public override bool ShouldDestroy => string.IsNullOrEmpty(name) && startDateTime == DateTimeOffset.MaxValue;

	        private DateTimeOffset startDateTime;
	        private DateTimeOffset pauseDateTime = DateTimeOffset.MaxValue;
	        private double totalPauseMS;

	        public StopWatch(string name, ITimeGetter timeGetter) : base(timeGetter)
	        {
		        this.name = name;
		        startDateTime = GetTime();
	        }

	        public double Stop()
	        {
		        var elapsed = CurrentTime();
		        Cancel();
		        return elapsed;
	        }

	        public double CurrentTime()
	        {
		        var totalElapsed = GetTime() - startDateTime;
		        var totalPause = TimeSpan.FromMilliseconds(totalPauseMS);
		        var foregroundElapsed = totalElapsed - totalPause;
		        return foregroundElapsed.TotalSeconds;
	        }

	        public override void Reset()
	        {
		        startDateTime = GetTime();
		        pauseDateTime = DateTimeOffset.MaxValue;
		        totalPauseMS = 0;
	        }

	        public override void Cancel()
	        {
		        name = string.Empty;
		        startDateTime = DateTimeOffset.MaxValue;
	        }
	        
	        public override void OnUpdate()
	        {
	        }

	        public override void Pause()
	        {
		        pauseDateTime = GetTime();
	        }

	        public override void Resume()
	        {
		        var span = GetTime() - pauseDateTime;
		        if (span.TotalSeconds > 0)
			        totalPauseMS += span.TotalMilliseconds;
	        }
        }

		private class Alarm : TaskBase
		{
			private string name;
			private DateTimeOffset target;
			private Action<string> callback;
			
			public override string Name => name;

			public override bool ShouldDestroy => string.IsNullOrEmpty(name) && target == DateTimeOffset.MaxValue && callback == null;

			public Alarm(string name, ITimeGetter timeGetter, DateTimeOffset target, Action<string> callback)
				: base(timeGetter)
			{
				this.name = name;
				this.target = target;
				this.callback = callback;
			}

			public override void Reset()
			{
			}

			public override void Cancel()
			{
				name = string.Empty;
				target = DateTimeOffset.MaxValue;
				callback = null;
			}

			public override void OnUpdate()
			{
				if (ShouldDestroy)
					return;
				
				var span = target - GetTime();
				if (span.TotalSeconds <= 0)
				{
					callback?.Invoke(name);
					Cancel();
				}
			}

			public override void Pause() { }

			public override void Resume() { }
		}

		private class Task : TaskBase
		{
			private string name;
			private Action callback;

			private readonly long intervalTicks;
			private DateTimeOffset lastInvokedTime;

			private TimeSpan pauseDiff;
			
			public override string Name => name;

			public override bool ShouldDestroy => string.IsNullOrEmpty(name) && callback == null;

			public Task(string name, ITimeGetter timeGetter, Action callback, float intervalSeconds)
				: base(timeGetter)
			{
				this.name = name;
				this.callback = callback;
				intervalTicks = (long)(intervalSeconds * 10000000L);
				lastInvokedTime = GetTime();
			}

			public override void Reset()
			{
			}

			public override void Cancel()
			{
				name = string.Empty;
				callback = null;
			}

			public override void OnUpdate()
			{
				if (ShouldDestroy)
					return;
				
				var nextInvokeTime = lastInvokedTime.AddTicks(intervalTicks);
				if (GetTime() >= nextInvokeTime)
				{
					callback?.Invoke();
					lastInvokedTime = nextInvokeTime;
				}
			}

			public override void Pause()
			{
				var nextInvokeTime = lastInvokedTime.AddTicks(intervalTicks);
				pauseDiff = nextInvokeTime - GetTime();
			}

			public override void Resume()
			{
				lastInvokedTime = GetTime().AddTicks(pauseDiff.Ticks);
			}
		}

		private static readonly List<ITaskInternal> tasks = new();

		private int pauseStack = 0;

		private static ITimeGetter timeGetter = new DefaultTimeGetter();
		
		public static void SetTimeGetter(ITimeGetter timeGetter)
		{
			TaskScheduler.timeGetter = timeGetter;
		}
		
		public static StopWatch CreateStopWatch(string name, bool reset = true)
		{
			var already = tasks.Find(t => t.Name == name);
			if (already is StopWatch sw)
			{
				if (reset)
					sw.Reset();
				return sw;
			}

			var newStopWatch = new StopWatch(name, timeGetter);
			tasks.Add(newStopWatch);
			return newStopWatch;
		}
		
		public static ITask CreateAlarm(string name, DateTimeOffset targetTime, Action<string> callback)
		{
			var already = tasks.Find(t => t.Name == name);
			if (already != null)
				return already;

			var newTimer = new Alarm(name, timeGetter, targetTime, callback);
			tasks.Add(newTimer);
			newTimer.OnUpdate();
			return newTimer;
		}

		public static ITask CreateTask(string name, Action task, float intervalSeconds)
		{
			var already = tasks.Find(t => t.Name == name);
			if (already != null)
				return already;

			var newTask = new Task(name, timeGetter, task, intervalSeconds);
			tasks.Add(newTask);
			newTask.OnUpdate();
			return newTask;
		}

		public static void Cancel(string name)
		{
			var timer = tasks.Find(t => t.Name == name);
			timer?.Cancel();
			// tasks.Remove(timer);
		}

		public static double GetCurrent(string name)
		{
			var stopwatch = tasks.Find(t => t.Name == name);
			if (stopwatch is StopWatch sw)
			{
				return sw.CurrentTime();
			}

			return -1;
		}

		public static double Stop(string name)
		{
			var stopwatch = tasks.Find(t => t.Name == name);
			if (stopwatch is StopWatch sw)
			{
				return sw.Stop();
			}

			return -1;
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus)
				Pause();
			else
				Resume();
		}

		public void Pause()
		{
			if (pauseStack == 0)
				tasks.ForEach(t => t.Pause());

			pauseStack++;
		}

		public void Resume()
		{
			pauseStack--;
			if (pauseStack == 0)
				tasks.ForEach(t => t.Resume());
			pauseStack = Math.Max(0, pauseStack);
		}

		private void Update()
		{
			tasks.RemoveAll(t => t.ShouldDestroy);
			tasks.ForEach(t => t.OnUpdate());
		}

		private void OnDestroy()
		{
			tasks.ForEach(t => t.Cancel());
			tasks.Clear();
		}
	}
}