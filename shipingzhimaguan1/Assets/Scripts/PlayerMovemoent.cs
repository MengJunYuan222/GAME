using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueSystem; // 添加对话系统命名空间

public class PlayerMovement : MonoBehaviour
{

    Animator am;
    Vector3 move;
    private float stopX, stopY;


    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;
    private bool isRunning = false;

    // Start is called before the first frame update
    void Start()
    {
        am = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        // 检查是否在对话中，如果在对话中则禁止移动
        if (IsDialogueActive())
        {
            // 在对话期间只更新动画参数，不执行移动
            am.SetBool("ismoving", false);
            return;
        }

        // 检测Shift键是否按下
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 根据奔跑状态设置速度
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // 设置奔跑动画参数
        am.SetBool("isrunning", isRunning);

        float inputX = Input.GetAxis(axisName: "Vertical");
        float inputY = Input.GetAxis(axisName: "Horizontal");
        Vector3 dir = new Vector3(x: inputX, y:0, z:-inputY);
        move = new Vector3(x: inputX, y:0, z:-inputY);
        if (dir != Vector3.zero)
        {
            am.SetBool(name: "ismoving", value: true);
            transform.position += dir * currentSpeed * Time.deltaTime;

            stopX = inputX;
            stopY = -inputY;

        }

        else
        {
            am.SetBool(name:"ismoving", value: false);
        }

        am.SetFloat(name: "inputX", value: stopX);
        am.SetFloat(name: "inputY", value: -stopY);
    }
    
    // 检查是否在对话中
    private bool IsDialogueActive()
    {
        // 检查对话输入管理器是否存在并且对话是否激活
        if (DialogueInputManager.Instance != null)
        {
            return DialogueInputManager.Instance.IsGlobalDialogueActive;
        }
        
        // 如果没有找到对话管理器，也可以直接检查对话UI管理器
        if (DialogueUIManager.Instance != null)
        {
            return DialogueUIManager.Instance.IsDialogueActive();
        }
        
        return false;
    }
}