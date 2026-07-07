using TMPro;
using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Othello.UI
{
    public class Console : MonoBehaviour
    {
        public GameObject console;
        public GameObject chatPanel;
        public GameObject textObject;

        private ScrollRect m_ScrollRect;
        private RectTransform m_ConsoleRect;
        private Camera m_UiCamera;
        private static GameObject m_ChatPanel;
        private static readonly ConcurrentQueue<(string text, Color color)> m_MessagesToLog = new();
        private static readonly Color m_StandardColor = new(0.96f, 0.67f, 0.39f);

        private void Start()
        {
            m_ChatPanel = chatPanel;
            m_ScrollRect = console.GetComponent<ScrollRect>();
            m_ConsoleRect = console.GetComponent<RectTransform>();
            var canvas = console.GetComponentInParent<Canvas>();
            m_UiCamera = canvas && canvas.worldCamera ? canvas.worldCamera : Camera.main;
        }

        private void Update()
        {
            if (!m_MessagesToLog.TryDequeue(out var message)) return;
            LogMessage(message.text, message.color);
            if (!IsUserScrolling())
                ScrollToBottom();
        }

        private bool IsUserScrolling()
        {
            if (!Input.GetMouseButton(0))
                return false;

            // Pressing the scrollbar selects it, so a held press on anything under the
            // console hierarchy means the user is scrolling manually
            var eventSystem = EventSystem.current;
            var selected = eventSystem ? eventSystem.currentSelectedGameObject : null;
            if (selected && selected.transform.IsChildOf(console.transform))
                return true;

            // Fallback for dragging the log content directly (needs the canvas camera,
            // since this UI lives on a world space canvas)
            return RectTransformUtility.RectangleContainsScreenPoint(m_ConsoleRect, Input.mousePosition, m_UiCamera);
        }

        private void LogMessage(string text, Color color)
        {
            var newMessage = new Message();
            var newText = Instantiate(textObject, chatPanel.transform);
            var timeStamp = "[" + DateTime.UtcNow.ToString("HH:mm:ss") + "] ";
            newMessage.Text = timeStamp + text;
            newMessage.TextObject = newText.GetComponent<TMP_Text>();
            newMessage.TextObject.text = newMessage.Text;
            newMessage.TextObject.color = color;
        }

        private void ScrollToBottom()
        {
            // Position the content directly instead of via verticalNormalizedPosition: the content
            // is pivoted at the top, so offsetting it upward by the overflow height shows the
            // newest line at the bottom of the viewport.
            Canvas.ForceUpdateCanvases();
            m_ScrollRect.velocity = Vector2.zero;
            var content = m_ScrollRect.content;
            var overflow = content.rect.height - m_ScrollRect.viewport.rect.height;
            var position = content.anchoredPosition;
            position.y = Mathf.Max(0f, overflow);
            content.anchoredPosition = position;
        }

        public static void Log(string text)
        {
            m_MessagesToLog.Enqueue((text, m_StandardColor));
        }

        public static void Log(string text, Color color)
        {
            m_MessagesToLog.Enqueue((text, color));
        }

        public static void Clear()
        {
            foreach (Transform child in m_ChatPanel.transform)
                Destroy(child.gameObject);
        }

        [Serializable]
        public class Message
        {
            public string Text;
            public TMP_Text TextObject;
        }
    }
}
