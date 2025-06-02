using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace alpoLib.Util
{
    public class CachedUIBehaviour : UIBehaviour
    {
        private RectTransform rectTransform { get; set; }
        public RectTransform CachedRectTransform => rectTransform;

        protected override void Awake()
        {
            var rt = GetComponent<RectTransform>();
            if (rt)
                rectTransform = rt;
        }
    }
}