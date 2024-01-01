using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CylinderCollider : MonoBehaviour
{
    public float Height = 2f;
    public float Radius = 1f;
    [Range(2, 32)]
    public int Segment = 4;
    public bool IsTrigger;
    public bool ProvidesContacts;
    public PhysicMaterial Material;
    public Quaternion RotationOffset = Quaternion.identity;
    public Vector3 PositionOffset = Vector3.zero;
    public bool DebugMode = true;

    [SerializeField, ReadOnly]
    private List<BoxCollider> _colliders = new();

    private void Awake()
    {
        DebugMode = false;
    }

    private void OnValidate()
    {
        if (DebugMode)
        {
            Build();
        }
    }

    private void Reset()
    {
        foreach (var c in _colliders)
        {
            DestroyImmediate(c.gameObject);
        }

        foreach (var t in transform.GetComponentsInChildren<Transform>())
        {
            if(t == transform) continue;
            DestroyImmediate(t.gameObject);
        }
        _colliders.Clear();
        Build();
    }

    [Button]
    private void Build()
    {
        SetColliderCount(Segment);
        float angle = 180f / Segment;
        
        var rotator = Quaternion.Euler(0f, angle, 0f);
        var oldPoint = Vector3.forward * Radius;
        var newPoint = rotator * oldPoint;
        float width = (newPoint - oldPoint).magnitude;
        float depth = Mathf.Sqrt(4 * Radius * Radius - width * width);
        
        var rotation = RotationOffset;
        var size = new Vector3(width, Height, depth);
        for (int i = 0; i < Segment; i++)
        {
            var c = _colliders[i];
            var ct = c.transform;
            ct.localRotation = rotation;
            ct.localPosition = PositionOffset;
            rotation = rotator * rotation;
            c.size = size;
            c.isTrigger = IsTrigger;
            c.providesContacts = ProvidesContacts;
            c.sharedMaterial = Material;
        }
    }

    private void SetColliderCount(int count)
    {
        Transform t = transform;
        int lastCount = _colliders.Count;
        // 적은 경우 부족한 수만큼 채움
        if (lastCount < count)
        {
            for (int i = lastCount; i < count; ++i)
            {
                var obj = new GameObject($"Segment_{i}");
                obj.transform.SetParent(t, false);
                var c = obj.AddComponent<BoxCollider>();
                _colliders.Add(c);
            }
        }
        else if (lastCount > count)
        {
            for (int i = count; i < lastCount; ++i)
            {
                var c = _colliders[count];
                DestroyImmediate(c.gameObject);
                _colliders.RemoveAt(count);
            }
        }
    }
}
