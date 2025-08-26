using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering; 
 
 
public class Change_Frame : MonoBehaviour
{
 
    // 目标帧率
    public int FrameRate = 60;
 
 
    // Start is called before the first frame update
    void Start()
    {
        //QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FrameRate;
        // 降低帧率
        // If there isn't any input then we can go back to 12 FPS (every 5 frames).
        // OnDemandRendering.renderFrameInterval = 5;
    }
 
    // Update is called once per frame
    void Update()
    {
        
    }
}