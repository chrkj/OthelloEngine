using UnityEngine;

public class rotatetotate : MonoBehaviour 
{
    public float rotateSpeed = 200f;
    private RectTransform rectComponent;

    void Start () 
    {
        rectComponent = GetComponent<RectTransform>();
    }
	
	void Update () 
    {
        float currentSpeed = rotateSpeed * Time.deltaTime;
        rectComponent.Rotate(0f, 0f, currentSpeed);
    }
}