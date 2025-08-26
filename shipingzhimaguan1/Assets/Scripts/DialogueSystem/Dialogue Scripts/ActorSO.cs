using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "New Actor", menuName = "Dialogue System/Actor")]
    public class ActorSO : ScriptableObject
    {
        [Header("角色信息")]
        [Tooltip("角色显示名称")]
        public string actorName;  // 这个名字会显示在对话框中

        [Tooltip("角色头像")]
        public Sprite actorSprite;
    }
}
