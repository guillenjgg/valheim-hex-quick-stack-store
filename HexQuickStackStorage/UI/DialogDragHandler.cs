using UnityEngine;
using UnityEngine.EventSystems;

namespace HexQuickStackStorage.UI
{
    internal class DialogDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _target;
        private Vector2 _pointerOffset;

        internal void Initialize(RectTransform target)
        {
            _target = target;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_target == null)
            {
                return;
            }

            var parent = _target.parent as RectTransform;

            if (parent == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var pointerPosition))
            {
                return;
            }

            _pointerOffset = _target.anchoredPosition - pointerPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_target == null)
            {
                return;
            }

            var parent = _target.parent as RectTransform;

            if (parent == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var pointerPosition))
            {
                return;
            }

            _target.anchoredPosition = pointerPosition + _pointerOffset;
        }
    }
}