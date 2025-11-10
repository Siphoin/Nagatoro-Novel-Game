using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SNEngine.UI
{
    public class UIEventRelay : MonoBehaviour, IPointerClickHandler
    {
        public event Action OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }
    }
}
