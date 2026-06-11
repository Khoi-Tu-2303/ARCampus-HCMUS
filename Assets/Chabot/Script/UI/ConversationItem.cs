using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Models;

namespace ChatApp.UI
{
    public class ConversationItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private TMP_InputField renameInputField;
        [SerializeField] private GameObject renamePanel;

        private Action _onSelect;
        private Action<string> _onRename;
        private Action _onDelete;

        void Awake()
        {
            renamePanel.SetActive(false);
        }

        public void Setup(Conversation conv,
            Action onSelect, Action<string> onRename, Action onDelete)
        {
            titleText.text = conv.title ?? "Cuộc trò chuyện mới";
            _onSelect = onSelect;
            _onRename = onRename;
            _onDelete = onDelete;

            selectButton.onClick.AddListener(() => _onSelect?.Invoke());
            deleteButton.onClick.AddListener(() => _onDelete?.Invoke());
            renameButton.onClick.AddListener(ShowRenamePanel);

            renamePanel.SetActive(false);
        }

        private void ShowRenamePanel()
        {
            selectButton.gameObject.SetActive(false);
            renamePanel.SetActive(true);
            AlignRenamePanelElements();
            renameInputField.text = titleText.text;
            renameInputField.Select();
        }

        private void AlignRenamePanelElements()
        {
            CopyRect(
                selectButton.GetComponent<RectTransform>(),
                renameInputField.GetComponent<RectTransform>()
            );

            RectTransform confirmRect = renamePanel
                .transform.Find("Btn_ConfirmRename")
                .GetComponent<RectTransform>();
            CopyRect(
                renameButton.GetComponent<RectTransform>(),
                confirmRect
            );

            RectTransform cancelRect = renamePanel
                .transform.Find("Btn_CancelRename")
                .GetComponent<RectTransform>();
            CopyRect(
                deleteButton.GetComponent<RectTransform>(),
                cancelRect
            );
        }

        private static void CopyRect(RectTransform src, RectTransform dst)
        {
            if (src.parent == dst.parent)
            {
                dst.anchorMin = src.anchorMin;
                dst.anchorMax = src.anchorMax;
                dst.pivot = src.pivot;
                dst.anchoredPosition = src.anchoredPosition;
                dst.sizeDelta = src.sizeDelta;
                return;
            }

            Vector3[] corners = new Vector3[4];
            src.GetWorldCorners(corners);

            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);

            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

            RectTransform dstParent = dst.parent as RectTransform;
            Vector2 localCenter;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dstParent,
                RectTransformUtility.WorldToScreenPoint(null, worldCenter),
                null,
                out localCenter
            );

            dst.anchorMin = new Vector2(0.5f, 0.5f);
            dst.anchorMax = new Vector2(0.5f, 0.5f);
            dst.pivot = new Vector2(0.5f, 0.5f);
            dst.anchoredPosition = localCenter;

            float scaleX = dstParent != null ? dstParent.lossyScale.x : 1f;
            float scaleY = dstParent != null ? dstParent.lossyScale.y : 1f;
            dst.sizeDelta = new Vector2(
                scaleX > 0 ? worldWidth / scaleX : worldWidth,
                scaleY > 0 ? worldHeight / scaleY : worldHeight
            );
        }

        public void ConfirmRename()
        {
            string newTitle = renameInputField.text.Trim();
            if (!string.IsNullOrEmpty(newTitle))
            {
                _onRename?.Invoke(newTitle);
                titleText.text = newTitle;        
            }
            renamePanel.SetActive(false);
            selectButton.gameObject.SetActive(true);
        }

        public void CancelRename()
        {
            renamePanel.SetActive(false);
            selectButton.gameObject.SetActive(true); 
        }

        public void SetTitle(string title) => titleText.text = title;

        void OnDestroy()
        {
            if (renamePanel != null)
                Destroy(renamePanel);
        }
    }
}