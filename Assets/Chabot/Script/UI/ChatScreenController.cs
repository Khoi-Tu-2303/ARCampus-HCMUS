using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Managers;
using ChatApp.Models;
using ChatApp.Network;

namespace ChatApp.UI
{
    public class ChatScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text convTitleText;
        [SerializeField] private Button backButton;

        [Header("Message List")]
        [SerializeField] private Transform messageContent;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject assistantMessagePrefab;
        [SerializeField] private GameObject typingIndicatorPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Input Area")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Error")]
        [SerializeField] private TMP_Text errorText;

        [Header("Network Error Popup")]
        [SerializeField] private GameObject networkErrorOverlay;
        [SerializeField] private Button btnReload;
        [SerializeField] private Button btnExit;

        private GameObject _typingIndicatorInstance;
        private string _convId;
        private Action _pendingAction;

        private void Start()
        {
            var conv = ConversationManager.Instance.ActiveConversation;
            if (conv == null) { UIManager.Instance.GoToConversations(); return; }

            _convId = conv.id_conversation;
            convTitleText.text = conv.title ?? "Cuộc trò chuyện";

            backButton.onClick.AddListener(() => UIManager.Instance.GoToConversations());
            sendButton.onClick.AddListener(OnSendClicked);
            inputField.onSubmit.AddListener(_ => OnSendClicked());

            btnReload.onClick.AddListener(OnReloadClicked);
            btnExit.onClick.AddListener(OnExitClicked);

            networkErrorOverlay.SetActive(false);

            ChatManager.Instance.OnMessagesLoaded += RenderAllMessages;
            ChatManager.Instance.OnUserMessageAdded += AppendUserMessage;
            ChatManager.Instance.OnAssistantMessageReceived += OnAssistantReceived;
            ChatManager.Instance.OnSendingStateChanged += OnSendingStateChanged;
            ChatManager.Instance.OnError += ShowError;
            ChatManager.Instance.OnNetworkError += ShowNetworkError;

            LoadMessages();
        }

        private void OnDestroy()
        {
            ChatManager.Instance.OnMessagesLoaded -= RenderAllMessages;
            ChatManager.Instance.OnUserMessageAdded -= AppendUserMessage;
            ChatManager.Instance.OnAssistantMessageReceived -= OnAssistantReceived;
            ChatManager.Instance.OnSendingStateChanged -= OnSendingStateChanged;
            ChatManager.Instance.OnError -= ShowError;
            ChatManager.Instance.OnNetworkError -= ShowNetworkError;
        }

        private void LoadMessages()
        {
            _pendingAction = LoadMessages;
            ChatManager.Instance.LoadMessages(_convId);
        }

        private void RenderAllMessages(List<Message> messages)
        {
            _pendingAction = null;
            foreach (Transform child in messageContent) Destroy(child.gameObject);
            foreach (var msg in messages) SpawnMessage(msg);
            ScrollToBottom();
        }

        private void AppendUserMessage(Message msg) { SpawnMessage(msg); ScrollToBottom(); }

        private void OnAssistantReceived(Message msg)
        {
            _pendingAction = null;
            RemoveTypingIndicator();
            SpawnMessage(msg);
            ScrollToBottom();
        }

        private void SpawnMessage(Message msg)
        {
            var prefab = msg.role == "user" ? userMessagePrefab : assistantMessagePrefab;
            var go = Instantiate(prefab, messageContent);
            var item = go.GetComponent<MessageItem>();
            item.Setup(msg.content);

            if (msg.role == "assistant")
            {
                string target = msg.GetMeta("recommend_target");
                if (!string.IsNullOrEmpty(target))
                {
                    string displayName = GetBuildingName(target);
                    item.SetRecommend("  Đến " + displayName + " ngay", () =>
                    {
                        Debug.Log("Đang dẫn đường đến " + displayName);
                    });
                }
            }
        }
        private string GetBuildingName(string code)
        {
            switch (code)
            {
                case "NDH": return "Nhà Điều Hành";
                case "TOA_A": return "Toà A";
                case "TOA_B": return "Hội trường B";
                case "TOA_C": return "Toà C";
                case "TOA_D": return "Toà D";
                case "TOA_E": return "Toà E";
                case "TOA_F": return "Toà F";
                case "TOA_G": return "Toà G";

                default: return code;
            }
        }
        private void ShowTypingIndicator()
        {
            RemoveTypingIndicator();
            _typingIndicatorInstance = Instantiate(typingIndicatorPrefab, messageContent);
            ScrollToBottom();
        }

        private void RemoveTypingIndicator()
        {
            if (_typingIndicatorInstance == null) return;
            Destroy(_typingIndicatorInstance);
            _typingIndicatorInstance = null;
        }

        private void OnSendClicked()
        {
            string text = inputField.text.Trim();
            if (string.IsNullOrEmpty(text) || ChatManager.Instance.IsSending) return;

            inputField.text = "";
            HideError();

            var conv = ConversationManager.Instance.ActiveConversation;
            var userId = AuthManager.Instance.CurrentUser.id;

            if (conv != null)
            {
                string convId = conv.id_conversation;
                _pendingAction = null;
                SendMessage(convId, text);
            }
            else
            {
                _pendingAction = null;
                SendMessageAutoCreate(userId, text);
            }
        }

        private void SendMessage(string convId, string text)
        {
            ChatManager.Instance.SendMessage(convId, text);
        }

        private void SendMessageAutoCreate(string userId, string text)
        {
            ChatManager.Instance.SendMessageAutoCreate(userId, text);
        }
        private void OnSendingStateChanged(bool sending)
        {
            sendButton.interactable = !sending;
            inputField.interactable = !sending;
            if (sending) ShowTypingIndicator();
            else RemoveTypingIndicator();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        private void ShowError(string err)
        {
            _pendingAction = null;
            RemoveTypingIndicator();
            errorText.text = err;
            errorText.gameObject.SetActive(true);
        }

        private void HideError()
        {
            if (errorText != null)
                errorText.gameObject.SetActive(false);
        }

        private void ShowNetworkError(string err)
        {
            RemoveTypingIndicator();
            networkErrorOverlay.SetActive(true);
        }

        private void OnReloadClicked() => StartCoroutine(RetryConnection());

        private IEnumerator RetryConnection()
        {
            networkErrorOverlay.SetActive(false);
            HideError();
            SetLoading(true);

            yield return APIClient.Instance.Retry();

            float elapsed = 0f;
            const float timeout = 10f;
            while (elapsed < timeout)
            {
                if (APIClient.Instance.IsReady || APIClient.Instance.LoadError != null)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetLoading(false);

            bool failed = !APIClient.Instance.IsReady;
            if (failed)
            {
                networkErrorOverlay.SetActive(true);
                yield break;
            }

            if (_pendingAction != null)
                _pendingAction.Invoke();
            else
                LoadMessages();
        }

        private void SetLoading(bool isLoading)
        {
            btnReload.interactable = !isLoading;
        }

        private void OnExitClicked()
        {
            _pendingAction = null;
            networkErrorOverlay.SetActive(false);
            UIManager.Instance.GoToLogin();
        }
    }
}