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

        private ScrollRect m_scrollRect;
        private static GameObject s_chatPanel;
        private static readonly Queue<Color>  m_messageColor = new Queue<Color>();
        private static readonly Queue<string>  m_messagesToLog = new Queue<string>();
        private static readonly Color m_standardColor = new Color(0.96f, 0.67f, 0.39f);

        private void Start()
        {
            s_chatPanel = chatPanel;
            m_scrollRect = console.GetComponent<ScrollRect>();
        }

        private void Update()
        {
            if (m_messagesToLog.Count == 0) return;  
            LogMessage(m_messagesToLog.Dequeue(), m_messageColor.Dequeue());
            m_scrollRect.velocity = new Vector2(0f, 1000f);
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
            m_scrollRect.verticalNormalizedPosition = 0f;
        }

        public static void Log(string text)
        {
            m_messagesToLog.Enqueue(text);
            m_messageColor.Enqueue(m_standardColor);
        }
        
        public static void Log(string text, Color color)
        {
            m_messagesToLog.Enqueue(text);
            m_messageColor.Enqueue(color);
        }

        public static void Clear()
        {
            foreach (Transform child in s_chatPanel.transform)
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