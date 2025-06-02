using System;

namespace alpoLib.Util
{
    public class DateChangeBroadcaster : SingletonMonoBehaviour<DateChangeBroadcaster>
    {
        public delegate void OnChangeDateDelegate();
        public static event OnChangeDateDelegate OnChangeDateEvent;
        
        protected override void OnAwakeEvent()
        {
            InitializeDateChangeListener();
        }
        
        private void InitializeDateChangeListener()
        {
            var now = DateTime.Now;
            var next = now.AddDays(1);
            var clearedNext = new DateTime(next.Year, next.Month, next.Day, 0, 0, 0, 0);
            TaskScheduler.CreateAlarm("DATE_CHANGE_ALARM", clearedNext, OnDateChangeEvent);
        }
        
        private void OnDateChangeEvent(string name)
        {
            OnChangeDateEvent?.Invoke();
            Invoke(nameof(InitializeDateChangeListener), 1f);
        }
    }
}