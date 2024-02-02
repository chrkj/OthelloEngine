using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Core
{
    public class Console : MonoBehaviour
    {
        public GameObject console;
        public GameObject chatPanel;
        public GameObject textObject;

        private ScrollRect m_ScrollRect;
        private static GameObject m_ChatPanel;
        private static readonly Queue<Color>  m_MessageColor = new();
        private static readonly Queue<string>  m_MessagesToLog = new();
        private static readonly Color m_StandardColor = new(0.96f, 0.67f, 0.39f);

        private void Start()
        {
            m_ChatPanel = chatPanel;
            m_ScrollRect = console.GetComponent<ScrollRect>();
        }

        private void Update()
        {
            if (m_MessagesToLog.Count == 0) return;  
            LogMessage(m_MessagesToLog.Dequeue(), m_MessageColor.Dequeue());
            m_ScrollRect.velocity = new Vector2(0f, 1000f);
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
            m_ScrollRect.verticalNormalizedPosition = 0f;
        }

        public static void Log(string text)
        {
            m_MessagesToLog.Enqueue(text);
            m_MessageColor.Enqueue(m_StandardColor);
        }
        
        public static void Log(string text, Color color)
        {
            m_MessagesToLog.Enqueue(text);
            m_MessageColor.Enqueue(color);
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