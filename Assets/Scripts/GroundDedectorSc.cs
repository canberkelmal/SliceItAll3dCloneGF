using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundDedectorSc : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Knife"))
        {
            Destroy(other.gameObject);
        }
    }
}
