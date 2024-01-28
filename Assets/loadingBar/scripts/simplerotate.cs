using UnityEngine;
using UnityEngine.UI;

public class simplerotate : MonoBehaviour {

    private RectTransform rectComponent;
    private Image imageComp;
    public float rotateSpeed = 200f;
    private float currentvalue;

    void Start()
    {
        rectComponent = GetComponent<RectTransform>();
        imageComp = rectComponent.GetComponent<Image>();
    }

    void Update()
    {
        currentvalue = currentvalue + (Time.deltaTime * rotateSpeed);
        rectComponent.transform.rotation = Quaternion.Euler(0f, 0f, -72f * (int)currentvalue);
    }
}