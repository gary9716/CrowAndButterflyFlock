using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableInit : MonoBehaviour {

	void Awake()
    {
        gameObject.SetActive(false);
    }
}
