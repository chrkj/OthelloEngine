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
        private readonly Color m_textColor = new Color(0.9622642f, 0.6748884f, 0.3948915f);
        private static GameObject s_chatPanel;
        private static readonly List<string> m_messagesToLog = new List<string>();

        private void Start()
        {
            s_chatPanel = chatPanel;
            m_scrollRect = console.GetComponent<ScrollRect>();
        }

        private void Update()
        {
            if (m_messagesToLog.Count == 0) return;  
            LogMessage(m_messagesToLog[0]);
            m_messagesToLog.RemoveAt(0);
            m_scrollRect.velocity = new Vector2(0f, 1000f);
        }

        // TODO: Add support for color coded log messages
        private void LogMessage(string text)
        {
            Message newMessage = new Message();
            GameObject newText = Instantiate(textObject, chatPanel.transform);
            string timeStamp = "[" + DateTime.UtcNow.ToString("HH:mm:ss") + "] ";
            newMessage.Text = timeStamp + text;
            newMessage.TextObject = newText.GetComponent<TMP_Text>();
            newMessage.TextObject.text = newMessage.Text;
            newMessage.TextObject.color = m_textColor;
            m_scrollRect.verticalNormalizedPosition = 0f;
        }

        public static void Log(string text)
        {
            m_messagesToLog.Add(text);
        }

        public static void Clear()
        {
            foreach (Transform child in s_chatPanel.transform)
                GameObject.Destroy(child.gameObject);
        }

        [Serializable]
        public class Message
        {
            public string Text;
            public TMP_Text TextObject;
        }
    }
}