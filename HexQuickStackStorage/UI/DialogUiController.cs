using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HexQuickStackStorage.UI
{
    internal static class DialogUiController
    {
        private static GameObject _deleteConfirmationDialog;
        private static Toggle _dontShowAgainToggle;

        internal static void ShowDeleteConfirmation(InventoryGui inventoryGui, UnityAction onConfirm)
        {
            if (inventoryGui == null)
            {
                Plugin.Log.LogWarning("InventoryGui is null. Unable to show delete confirmation.");
                return;
            }

            if (Menu.instance == null || Menu.instance.m_logoutDialog == null)
            {
                Plugin.Log.LogWarning("Could not find Valheim logout confirmation dialog.");
                return;
            }

            if (_deleteConfirmationDialog == null)
            {
                var template = Menu.instance.m_logoutDialog.gameObject;

                _deleteConfirmationDialog = Object.Instantiate(template, inventoryGui.transform);
                _deleteConfirmationDialog.name = $"{Plugin.PluginGuid}.DeleteConfirmation";

                ConfigureDeleteConfirmationDialog();
            }

            if (_dontShowAgainToggle != null)
            {
                _dontShowAgainToggle.isOn = false;
            }

            ConfigureDeleteConfirmationActions(onConfirm);

            _deleteConfirmationDialog.SetActive(true);
            _deleteConfirmationDialog.transform.SetAsLastSibling();
        }

        private static void ConfigureDeleteConfirmationDialog()
        {
            if (_deleteConfirmationDialog == null)
            {
                return;
            }

            var dialog = _deleteConfirmationDialog.transform.Find("dialog");

            if (dialog == null)
            {
                Plugin.Log.LogWarning("Could not find dialog on delete confirmation.");
                return;
            }

            var message = dialog.Find("Quit")?.GetComponent<TMP_Text>();
            var yesButton = dialog.Find("Button_yes")?.GetComponent<Button>();
            var cancelButton = dialog.Find("Button_no")?.GetComponent<Button>();

            if (message != null)
            {
                message.text = "Delete items?";
            }

            if (yesButton != null)
            {
                yesButton.onClick = new Button.ButtonClickedEvent();
            }

            if (cancelButton != null)
            {
                cancelButton.onClick = new Button.ButtonClickedEvent();
                SetButtonText(cancelButton.gameObject, "Cancel");
            }

            var dialogRect = dialog as RectTransform;

            if (dialogRect != null)
            {
                var dragHandler = dialog.gameObject.GetComponent<DialogDragHandler>();

                if (dragHandler == null)
                {
                    dragHandler = dialog.gameObject.AddComponent<DialogDragHandler>();
                }

                dragHandler.Initialize(dialogRect);
            }

            CreateDontShowAgainToggle(dialog, message, yesButton, cancelButton);
        }

        private static void ConfigureDeleteConfirmationActions(UnityAction onConfirm)
        {
            var dialog = _deleteConfirmationDialog.transform.Find("dialog");

            if (dialog == null)
            {
                return;
            }

            var yesButton = dialog.Find("Button_yes")?.GetComponent<Button>();
            var cancelButton = dialog.Find("Button_no")?.GetComponent<Button>();

            if (yesButton != null)
            {
                yesButton.onClick = new Button.ButtonClickedEvent();
                yesButton.onClick.AddListener(() =>
                {
                    if (_dontShowAgainToggle != null && _dontShowAgainToggle.isOn)
                    {
                        Plugin.SetDeleteConfirmationEnabled(false);
                    }

                    HideDeleteConfirmation();
                    onConfirm?.Invoke();
                });
            }

            if (cancelButton != null)
            {
                cancelButton.onClick = new Button.ButtonClickedEvent();
                cancelButton.onClick.AddListener(HideDeleteConfirmation);
            }
        }

        private static void HideDeleteConfirmation()
        {
            if (_deleteConfirmationDialog != null)
            {
                _deleteConfirmationDialog.SetActive(false);
            }
        }

        private static void SetButtonText(GameObject buttonObject, string text)
        {
            var label = buttonObject.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.text = text;
                label.alignment = TextAlignmentOptions.Center;
                return;
            }

            var legacyLabel = buttonObject.GetComponentInChildren<Text>(true);

            if (legacyLabel != null)
            {
                legacyLabel.text = text;
                legacyLabel.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void CreateDontShowAgainToggle(
            Transform dialog,
            TMP_Text textTemplate,
            Button yesButton,
            Button cancelButton
        )
        {
            if (_dontShowAgainToggle != null)
            {
                return;
            }

            if (textTemplate == null)
            {
                Plugin.Log.LogWarning("Could not find Valheim text template for delete confirmation toggle.");
                return;
            }

            var toggleObject = new GameObject(
                $"{Plugin.PluginGuid}.DontShowAgainToggle",
                typeof(RectTransform),
                typeof(Toggle)
            );

            toggleObject.transform.SetParent(dialog, false);

            var toggleRect = toggleObject.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);
            toggleRect.sizeDelta = new Vector2(290f, 30f);

            float buttonBottom = -80f;

            if (yesButton != null)
            {
                var yesRect = yesButton.transform as RectTransform;

                if (yesRect != null)
                {
                    buttonBottom = yesRect.anchoredPosition.y - (yesRect.rect.height * 0.5f);
                }
            }
            else if (cancelButton != null)
            {
                var cancelRect = cancelButton.transform as RectTransform;

                if (cancelRect != null)
                {
                    buttonBottom = cancelRect.anchoredPosition.y - (cancelRect.rect.height * 0.5f);
                }
            }

            toggleRect.anchoredPosition = new Vector2(0f, buttonBottom - 25f);

            _dontShowAgainToggle = toggleObject.GetComponent<Toggle>();
            _dontShowAgainToggle.isOn = false;

            var checkboxObject = new GameObject(
                $"{Plugin.PluginGuid}.DontShowAgainToggle.CheckboxBackground",
                typeof(RectTransform),
                typeof(Image)
            );

            checkboxObject.transform.SetParent(toggleObject.transform, false);

            var checkboxRect = checkboxObject.GetComponent<RectTransform>();
            checkboxRect.anchorMin = new Vector2(0f, 0.5f);
            checkboxRect.anchorMax = new Vector2(0f, 0.5f);
            checkboxRect.pivot = new Vector2(0f, 0.5f);
            checkboxRect.anchoredPosition = Vector2.zero;
            checkboxRect.sizeDelta = new Vector2(20f, 20f);

            var checkboxImage = checkboxObject.GetComponent<Image>();
            checkboxImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var checkmarkObject = new GameObject(
                $"{Plugin.PluginGuid}.DontShowAgainToggle.Checkmark",
                typeof(RectTransform),
                typeof(Image)
            );

            checkmarkObject.transform.SetParent(checkboxObject.transform, false);

            var checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(4f, 4f);
            checkmarkRect.offsetMax = new Vector2(-4f, -4f);

            var checkmarkImage = checkmarkObject.GetComponent<Image>();
            checkmarkImage.color = Color.white;

            _dontShowAgainToggle.targetGraphic = checkboxImage;
            _dontShowAgainToggle.graphic = checkmarkImage;

            var labelObject = Object.Instantiate(textTemplate.gameObject, toggleObject.transform);
            labelObject.name = $"{Plugin.PluginGuid}.DontShowAgainToggle.Label";

            var labelRect = labelObject.GetComponent<RectTransform>();

            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = new Vector2(30f, 0f);
                labelRect.offsetMax = Vector2.zero;
                labelRect.localScale = Vector3.one;
            }

            var label = labelObject.GetComponent<TMP_Text>();

            if (label != null)
            {
                label.text = "Don't show this confirmation again.";
                label.fontSize = 16f;
                label.fontStyle = FontStyles.Normal;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
            }
        }
    }
}