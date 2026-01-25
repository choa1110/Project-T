using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AttackParameter
{
    public float DamageRate;
    public Vector3 KnockbackDir;
    public float KnockbackRate;
    public float CameraShake;
}

[CreateAssetMenu(fileName = "Attack Parameters", menuName = "Attack Parameters")]
public class AttackParameters : ScriptableObject
{
    public List<AttackParameter> parameters;
}