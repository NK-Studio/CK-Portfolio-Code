using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MouseUISetting", menuName = "Settings/MouseUISettings", order = 1)]
public class MouseUISetting : ScriptableObject
{
    [Tooltip("공격 마우스UI가 표시되는 시간")]
    public float AttackMouseUIDuration = 3f;

    [Serializable]
    public struct AttackMouseSpriteSettings
    {
        public Sprite Crosshair;
        public Sprite DirectionArrow;
        
        public Sprite OutlineDefault;
        public Sprite OutlineBattle;
    }

    public AttackMouseSpriteSettings DefaultMouseSprites;
    public AttackMouseSpriteSettings EnemyTargetMouseSprites;
}