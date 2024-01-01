using Cysharp.Threading.Tasks;
using Dummy.Scripts;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;
using Logger = NKStudio.Logger;

public class SlidePlane : MonoBehaviour
{
    public Axis DirectionAxis = Axis.Z;
    public bool InvertDirection = false;
    public Vector3 Direction => transform.GetLocalAxis(DirectionAxis) * (InvertDirection ? -1f : 1f);

    public void GetDirections(out Vector3 primary, out Vector3 secondary)
    {
        primary = Direction;
        secondary = Vector3.Cross(primary, Vector3.up);
    }
    
    public float StartFromOrigin = 5f;
    public float EndFromOrigin = 10f;
    public float WidthFromOrigin = 3f;
    public float MoveSpeed = 5f;

    [ReadOnly] public ParabolaByMaximumHeightGenerator EnterParabola;
    [ReadOnly] public ParabolaByMaximumHeightGenerator ExitParabola;

    public Transform EnterLeapPoint; // 진입 시 도약 포인트
    public float EnterHeightFromLeap = 5f;
    public Transform ExitLandPoint; // 탈출 시 착지 포인트 (도약 포인트는 EndFromOrigin 으로 결정)
    public float ExitHeightFromLeap = 5f;

    private void OnValidate() => ValidateParabola();
    [Button]
    private void ValidateParabola()
    {
        var t = transform;
        var origin = t.position;
        GetDirections(out var primaryDirection, out var secondaryDirection);
        
        if (!EnterLeapPoint)
        {
            var obj = new GameObject("EnterLeapPoint");
            var tt = obj.transform;
            tt.position = origin;
            tt.SetParent(t);
            EnterLeapPoint = tt;
        }
        var enterLeap = EnterLeapPoint.position;
        var enterLeapDot = Vector3.Dot(enterLeap - origin, secondaryDirection);
        var enterLand = origin - primaryDirection * StartFromOrigin;

        EnterParabola.Start = enterLeap - enterLeapDot * secondaryDirection;
        EnterParabola.End = enterLand;
        EnterParabola.HighestFromLeap = EnterHeightFromLeap;
        EnterParabola.Generate();
        
        if (!ExitLandPoint)
        {
            var obj = new GameObject("ExitLandPoint");
            var tt = obj.transform;
            tt.position = origin;
            tt.SetParent(t);
            ExitLandPoint = tt;
        }
        var exitLeap = origin + primaryDirection * EndFromOrigin;
        var exitLand = ExitLandPoint.position;
        var exitLandDot = Vector3.Dot(exitLand - origin, secondaryDirection);
        
        ExitParabola.Start = exitLeap;
        ExitParabola.End = exitLand - exitLandDot * secondaryDirection;
        ExitParabola.HighestFromLeap = ExitHeightFromLeap;
        ExitParabola.Generate();
    }

    private void OnDrawGizmos()
    {
        GetDirections(out var primaryDirection, out var secondaryDirection);
        var origin = transform.position;
        var enterLeap = EnterLeapPoint.position;
        DrawUtility.DrawWireSphere(enterLeap, 0.1f, 16, DrawUtility.DebugDrawer(Color.yellow));
        DrawUtility.DrawWireSphere(EnterParabola.Start, 0.1f, 16, DrawUtility.DebugDrawer(Color.yellow));
        Debug.DrawLine(enterLeap, EnterParabola.Start, Color.yellow);
        var enterLand = origin - primaryDirection * StartFromOrigin;
        DrawUtility.DrawWireSphere(enterLand, 0.1f, 16, DrawUtility.DebugDrawer(Color.green));
        DrawUtility.DrawWireSphere(EnterParabola.End, 0.1f, 16, DrawUtility.DebugDrawer(Color.green));
        Debug.DrawLine(enterLand, EnterParabola.End, Color.green);
        
        var exitLeap = origin + primaryDirection * EndFromOrigin;
        DrawUtility.DrawWireSphere(exitLeap, 0.1f, 16, DrawUtility.DebugDrawer(Color.magenta));
        DrawUtility.DrawWireSphere(ExitParabola.Start, 0.1f, 16, DrawUtility.DebugDrawer(Color.magenta));
        Debug.DrawLine(exitLeap, ExitParabola.Start, Color.magenta);
        var exitLand = ExitLandPoint.position;
        DrawUtility.DrawWireSphere(exitLand, 0.1f, 16, DrawUtility.DebugDrawer(Color.red));
        DrawUtility.DrawWireSphere(ExitParabola.End, 0.1f, 16, DrawUtility.DebugDrawer(Color.red));
        Debug.DrawLine(exitLand, ExitParabola.End, Color.red);
        
        Debug.DrawLine(enterLand, exitLeap);
        var horizontal = secondaryDirection * WidthFromOrigin;
        Debug.DrawLine(enterLand + horizontal, exitLeap + horizontal);
        Debug.DrawLine(enterLand - horizontal, exitLeap - horizontal);
        Debug.DrawLine(enterLand - horizontal, enterLand + horizontal);
        Debug.DrawLine(exitLeap - horizontal, exitLeap + horizontal);
        
        if (EnterParabola.Parabola.Valid)
        {
            var p = EnterParabola.Parabola;
            const int Divide = 10;
            const float Denom = 1f / Divide;
            var pos = p.Start;
            for (int i = 1; i <= Divide; i++)
            {
                var newPos = p.GetPosition(i * Denom);
                Logger.DrawLine(pos, newPos, Color.yellow);
                pos = newPos;
            }
        }
        if (ExitParabola.Parabola.Valid)
        {
            var p = ExitParabola.Parabola;
            const int Divide = 10;
            const float Denom = 1f / Divide;
            var pos = p.Start;
            for (int i = 1; i <= Divide; i++)
            {
                var newPos = p.GetPosition(i * Denom);
                Logger.DrawLine(pos, newPos, Color.magenta);
                pos = newPos;
            }
        }
        // DrawUtility.DrawWireSphere(EnterParabola.Start);
    }

    [Button]
    private async UniTaskVoid Simulate(float deltaTime = 0.05f, float moveSpeed = 3f)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        var target = obj.transform;
        int dtms = (int)(deltaTime * 1000);
        var enterParabola = EnterParabola.Parabola;
        float t = 0f;
        float x = 0f;
        float length = enterParabola.HorizontalLength;
        while (t < 1f)
        {
            target.position = enterParabola.GetPosition(t);
            // DebugX.Log($"t: {t:F3}, x: {x:F3}, pos: {pos}");
            // DrawUtility.DrawWireSphere(pos, 1f, 16, DrawUtility.DebugDrawer(Color.white));
            await UniTask.Delay(dtms);
            x += deltaTime * moveSpeed;
            t = x / enterParabola.HorizontalLength;
        }
        
        var origin = transform.position;
        GetDirections(out var primaryDirection, out var secondaryDirection);
        var start = origin - primaryDirection * StartFromOrigin;
        var end = origin + primaryDirection * EndFromOrigin;
        target.position = start;
        x = 0f;
        length = EndFromOrigin + StartFromOrigin;
        while (x < length)
        {
            var delta = deltaTime * moveSpeed;
            target.position = Vector3.MoveTowards(target.position, end, delta);
            await UniTask.Delay(dtms);
            x += delta;
        }
        
        var exitParabola = ExitParabola.Parabola;
        x = 0f;
        t = 0f;
        length = exitParabola.HorizontalLength;
        while (t < 1f)
        {
            target.position = exitParabola.GetPosition(t);
            // DebugX.Log($"t: {t:F3}, x: {x:F3}, pos: {pos}");
            // DrawUtility.DrawWireSphere(pos, 1f, 16, DrawUtility.DebugDrawer(Color.white));
            await UniTask.Delay(dtms);
            x += deltaTime * moveSpeed;
            t = x / length;
        }
        
        
        DestroyImmediate(obj);
    }
}
