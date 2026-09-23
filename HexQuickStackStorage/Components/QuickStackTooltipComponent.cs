using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HexQuickStackStorage.Components
{
    internal sealed class QuickStackTooltipComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string TooltipText = "Quick Stack is unavailable while a container is open.";

        private GameObject _tooltipObject;
        private bool _tooltipEnabled;

        internal bool TooltipEnabled
        {
            get => _tooltipEnabled;
            set
            {
                _tooltipEnabled = value;

                if (!_tooltipEnabled)
                {
                    HideTooltip();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!TooltipEnabled)
            {
                return;
            }

            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (_tooltipObject != null)
            {
                return;
            }

            GameObject tooltipObject = new GameObject($"{Plugin.PluginGuid}.QuickStackTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tooltipObject.transform.SetParent(transform.parent, false);

            _tooltipObject = tooltipObject;

            RectTransform rectTransform = tooltipObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(360f, 40f);
            rectTransform.position = transform.position + new Vector3(0f, 45f, 0f);

            Image background = tooltipObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.9f);
            background.raycastTarget = false;

            TMP_Text textTemplate = GetComponentInChildren<TMP_Text>(true);

            if (textTemplate == null)
            {
                Destroy(tooltipObject);
                _tooltipObject = null;
                return;
            }

            GameObject textObject = Instantiate(textTemplate.gameObject, tooltipObject.transform);
            textObject.name = "Text";

            RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = new Vector2(8f, 4f);
            textRectTransform.offsetMax = new Vector2(-8f, -4f);
            textRectTransform.localScale = Vector3.one;

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = TooltipText;
            text.fontSize = 16f;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            text.text = TooltipText;
            text.fontSize = 16f;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private void HideTooltip()
        {
            if (_tooltipObject == null)
            {
                return;
            }

            Destroy(_tooltipObject);
            _tooltipObject = null;
        }

        private void OnDisable()
        {
            HideTooltip();
        }
    }
}