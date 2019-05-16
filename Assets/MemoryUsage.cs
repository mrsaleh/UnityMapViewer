using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryUsage : MonoBehaviour
{

    private Text m_label;
    // Use this for initialization
    void Start()
    {
        m_label = this.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        m_label.text = "Usage: " + (GC.GetTotalMemory(true) / 1024 / 1024).ToString()+ "MB";

    }
}
