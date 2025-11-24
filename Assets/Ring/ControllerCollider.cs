using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerCollider : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RingHandle"))
        {
            other.transform.parent = transform;
            GetComponent<Collider>().enabled = false;
        }
    }
}
