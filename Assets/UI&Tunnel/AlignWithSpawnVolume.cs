using UnityEngine;

public class SimpleUIAlign : MonoBehaviour
{
    public BoxCollider targetBox;
    public float distance = 30f;

    void Start()
    {
        if (targetBox != null)
        {
            Vector3 center = targetBox.transform.TransformPoint(targetBox.center);
            transform.position = center + (targetBox.transform.forward * distance);
            transform.rotation = targetBox.transform.rotation * Quaternion.Euler(0, 180, 0);
        }
    }
}