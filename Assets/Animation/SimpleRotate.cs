using UnityEngine;

namespace Othello.Animation
{
    public class SimpleRotate : MonoBehaviour 
    {
        private RectTransform m_RectComponent;
        public float rotateSpeed = 200f;
        private float m_CurrentValue;

        private void Start()
        {
            m_RectComponent = GetComponent<RectTransform>();
        }

        private void Update()
        {
            m_CurrentValue = (Time.deltaTime * rotateSpeed) + m_CurrentValue;
            m_RectComponent.transform.rotation = Quaternion.Euler(0f, 0f, -72f * (int)m_CurrentValue);
        }
    }
}