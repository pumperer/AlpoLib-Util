using System;

namespace alpoLib.Util
{
	public struct RechargeTimerCreateContext
	{
		public Func<int> ValueGetter;
		public Action<int, bool> ValueSetter;

		public Func<DateTimeOffset> NextChargeTickGetter;
		public Action<DateTimeOffset> NextChargeTickSetter;

		public Func<DateTimeOffset> NextCooltimeGetter;
		public Action<DateTimeOffset> NextCooltimeSetter;

		public Func<DateTimeOffset> NowGetter;

		public int MaxValue;
		public int ChargeIntervalSeconds;
		public int CooltimeSeconds;
		//public int RemainingCooltime;
		public int ChargeAmount;
	}

	public class RechargeTimer
	{
		private bool initialized = false;

		private Func<int> valueGetter = null;
		private Action<int, bool> valueSetter = null;
		private int maxValue = 0;

		private int chargeIntervalSeconds = 0;
		private int cooltimeSeconds = 0;
		private int chargeAmount = 0;

		private DateTimeOffset nextCooltimeClearTime;
		private DateTimeOffset nextChargeTime;

		private Func<DateTimeOffset> nextChargeTickGetter = null;
		private Action<DateTimeOffset> nextChargeTickSetter = null;

		private Func<DateTimeOffset> nextCooltimeGetter = null;
		private Action<DateTimeOffset> nextCooltimeSetter = null;

		private Func<DateTimeOffset> nowGetter = null;

		//private bool useCooltime = false;
		private bool runningCoolTime = false;

		public bool Initialized => initialized;

		public int CurrentValue
		{
			get
			{
				return valueGetter?.Invoke() ?? 0;
			}
		}

		private bool UseCoolTime => cooltimeSeconds != 0;

		public void Initialize(RechargeTimerCreateContext context)
		{
			initialized = true;

			valueGetter = context.ValueGetter;
			valueSetter = context.ValueSetter;
			maxValue = context.MaxValue;

			nextChargeTickGetter = context.NextChargeTickGetter;
			nextChargeTickSetter = context.NextChargeTickSetter;

			nextCooltimeGetter = context.NextCooltimeGetter;
			nextCooltimeSetter = context.NextCooltimeSetter;

			nowGetter = context.NowGetter;

			chargeIntervalSeconds = context.ChargeIntervalSeconds;
			cooltimeSeconds = context.CooltimeSeconds;

			chargeAmount = context.ChargeAmount;

			var now = nowGetter.Invoke();

			var nextCharge = nextChargeTickGetter?.Invoke();
			if (nextCharge >= now)
				StartTimer((nextCharge - now).Value.TotalSeconds);

			if (UseCoolTime)
			{
				var nextUseTime = nextCooltimeGetter?.Invoke();
				if (nextUseTime >= now)
					StartCooltimer((nextUseTime - now).Value.TotalSeconds);
			}
		}


		private void StartTimer(double remainTime = 0)
		{
			if (remainTime == 0)
				remainTime = chargeIntervalSeconds;

			nextChargeTime = nowGetter.Invoke().AddSeconds(remainTime);
			nextChargeTickSetter.Invoke(nextChargeTime);
		}

		private void StartCooltimer(double remainTime = 0)
		{
			if (remainTime == 0)
				remainTime = cooltimeSeconds;

			nextCooltimeClearTime = nowGetter.Invoke().AddSeconds(remainTime);
			nextCooltimeSetter.Invoke(nextCooltimeClearTime);
			runningCoolTime = true;
		}

		public void Calc()
		{
			if (!initialized)
				return;

			var currentValue = valueGetter?.Invoke() ?? 0;

			if (currentValue < maxValue || runningCoolTime)
			{
				var now = nowGetter.Invoke();

				var isMaxCoolTime = runningCoolTime && now >= nextCooltimeClearTime;
				var isEndChargeTime = (now - nextChargeTime).TotalSeconds >= -1;
				if (isMaxCoolTime)
				{
					runningCoolTime = false;
					valueSetter?.Invoke(currentValue, true);
				}
				else if (isEndChargeTime)
				{
					var diff = now - nextChargeTime;
					var rechargeDiffTime = (int)(diff.TotalSeconds / chargeIntervalSeconds) + 1;
					var rechargeAmount = rechargeDiffTime * chargeAmount;

					nextChargeTime = nextChargeTime.AddSeconds((double)rechargeDiffTime * chargeIntervalSeconds);
					nextChargeTickSetter.Invoke(nextChargeTime);

					if (currentValue + rechargeAmount > maxValue)
						currentValue = maxValue;
					else
						currentValue += rechargeAmount;

					valueSetter?.Invoke(currentValue, true);
				}
			}
		}

		public bool Add(int value)
		{
			Calc();

			var currentValue = valueGetter?.Invoke();
			currentValue += value;
			valueSetter?.Invoke(currentValue.Value, false);
			nextChargeTickSetter?.Invoke(nextChargeTime);

			return true;
		}

		public bool Use(int value)
		{
			Calc();

			var now = nowGetter.Invoke();

			if (runningCoolTime)
				return false;

			var currentValue = valueGetter?.Invoke() ?? 0;

			if (currentValue >= value)
			{
				if (UseCoolTime)
				{
					StartCooltimer();
					nextCooltimeClearTime = now.AddSeconds(cooltimeSeconds);
					nextCooltimeSetter?.Invoke(nextCooltimeClearTime);
				}

				if (currentValue == maxValue)
					StartTimer();

				currentValue -= value;
				valueSetter?.Invoke(currentValue, false);
				nextChargeTickSetter?.Invoke(nextChargeTime);

				return true;
			}

			return false;
		}

		public bool CanUse()
		{
			// var now = nowGetter.Invoke();
			var currentValue = valueGetter?.Invoke() ?? 0;
			if (runningCoolTime)
				return false;
			return currentValue > 0;
		}

		public int GetCapacity()
		{
			return maxValue - CurrentValue;
		}

		public DateTimeOffset GetDateTimeNextChargeTime()
		{
			if (valueGetter?.Invoke() >= maxValue)
				return DateTimeOffset.MinValue;

			return nextChargeTime;
		}

		public int GetRemainSecondsNextChargeTime()
		{
			if (valueGetter?.Invoke() >= maxValue)
				return 0;

			var diff = nextChargeTime - nowGetter.Invoke();
			return Math.Max(0, (int)diff.TotalSeconds);
		}

		public int GetRemainSecondsCooltime()
		{
			if (!runningCoolTime)
				return 0;
			
			var diff = nextCooltimeClearTime - nowGetter.Invoke();
			if (diff.TotalSeconds > cooltimeSeconds && nextCooltimeClearTime == DateTime.MaxValue)
				return 0;

			return Math.Max(0, (int)diff.TotalSeconds);
		}

		public int GetRemainSecondsToMax()
		{
			if (!initialized)
				return -1;

			var remainCount = maxValue - valueGetter?.Invoke() ?? 0;
			if (remainCount == 0)
				return 0;

			return GetRemainSecondsNextChargeTime() + (chargeIntervalSeconds * (remainCount - 1));
		}
	}
}