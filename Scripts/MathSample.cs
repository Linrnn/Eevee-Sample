using Eevee.Fixed;
using Eevee.Log;
using Eevee.Random;
using UMath = UnityEngine.Mathf;
using UMatrix4x4 = UnityEngine.Matrix4x4;
using UPlane = UnityEngine.Plane;
using UQuaternion = UnityEngine.Quaternion;
using URay = UnityEngine.Ray;
using URect = UnityEngine.Rect;
using UVector2 = UnityEngine.Vector2;
using UVector3 = UnityEngine.Vector3;
using UVector4 = UnityEngine.Vector4;

/// <summary>
/// 确定性库示例代码
/// </summary>
internal sealed class MathSample : UnityEngine.MonoBehaviour
{
    public int times;
    private readonly Fixed64 _epsilon0000001 = 0.000001;
    private readonly Fixed64 _epsilon0001 = 0.001;
    private readonly Fixed64 _epsilon001 = 0.01;
    private readonly Fixed64 _epsilon01 = 0.1;

    private void Update()
    {
        for (int i = 0; i < times; ++i)
        {
            var num0 = RandomRelay.Number();
            var num1 = RandomRelay.Number(-100, 100);
            var num2 = RandomRelay.Number(0, 100);
            var vec2D0 = new Vector2D(RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100));
            var vec2D1 = new Vector2D(RandomRelay.Number(-10, 800), RandomRelay.Number(-10, 800));
            var vec2D2 = new Vector2D(RandomRelay.Number(0, 1000), RandomRelay.Number(0, 1000));
            var vec3D0 = new Vector3D(RandomRelay.Number(0, 10), RandomRelay.Number(0, 10), RandomRelay.Number(0, 10));
            var vec3D1 = new Vector3D(RandomRelay.Number(-10, 10), RandomRelay.Number(-10, 10), RandomRelay.Number(-10, 10));
            var vec3D2 = new Vector3D(RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor());
            var vec3D3 = new Vector3D(RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor());
            var vec4D0 = new Vector4D(RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100));
            var vec4D1 = new Vector4D(RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100), RandomRelay.Number(-100, 100));
            var vec4D10 = new Vector4D(RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor());
            var vec4D11 = new Vector4D(RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor());
            var vec4D12 = new Vector4D(RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor());
            var vec4D13 = new Vector4D(RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor(), RandomRelay.Number(-10, 10).Floor());
            var quaternion0 = RandomRelay.Quaternion();
            var quaternion1 = RandomRelay.Quaternion();
            var rect0 = new Rectangle(in vec2D0, in vec2D2);
            var ray3D0 = new Ray3D(in vec3D0, in vec3D1);
            var plane0 = new Plane(in vec3D0, num2);
            var plane1 = new Plane(in Vector3D.Up, Fixed64.Zero);
            var matrix4X40 = new Matrix4X4(in vec4D10, in vec4D11, in vec4D12, in vec4D13);

            var uPointToNormalized = URect.PointToNormalized((URect)rect0, (UVector2)vec2D1);
            var fPointToNormalized = Maths.PointToNormalized(in rect0, in vec2D1);
            var diffPointToNormalized = uPointToNormalized - fPointToNormalized;
            if (diffPointToNormalized.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] PointToNormalized Diff:{diffPointToNormalized} not 0.");

            var uNormalizedToPoint = URect.NormalizedToPoint((URect)rect0, (UVector2)vec2D1);
            var fNormalizedToPoint = Maths.NormalizedToPoint(in rect0, in vec2D1);
            var diffNormalizedToPoint = uNormalizedToPoint - fNormalizedToPoint;
            if (diffNormalizedToPoint.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] NormalizedToPoint Diff:{diffNormalizedToPoint} not 0.");

            var uSLerp = UQuaternion.Slerp((UQuaternion)quaternion0, (UQuaternion)quaternion1, (float)num0);
            var fSLerp = Maths.SLerp(in quaternion0, in quaternion1, num0);
            var diffSLerp = uSLerp - fSLerp;
            if (diffSLerp.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] SLerp Diff:{diffSLerp} not 0.");

            var uMoveTowards = UVector4.MoveTowards((UVector4)vec4D0, (UVector4)vec4D1, (float)num1);
            var fMoveTowards = Maths.MoveTowards(in vec4D0, in vec4D1, num1);
            var diffMoveTowards = uMoveTowards - fMoveTowards;
            if (diffMoveTowards.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] MoveTowards Diff:{diffMoveTowards} not 0.");

            //var uRotateTowards = UQuaternion.RotateTowards((UQuaternion)quaternion0, (UQuaternion)quaternion1, (float)num1);
            var uRotateTowards = (Quaternion)RotateTowards((UQuaternion)quaternion0, (UQuaternion)quaternion1, (float)num1);
            var fRotateTowards = Maths.RotateTowards(in quaternion0, in quaternion1, num1);
            var diffRotateTowards = uRotateTowards - fRotateTowards;
            if (diffRotateTowards.SqrMagnitude() >= _epsilon01)
                LogRelay.Error($"[Sample] RotateTowards Diff:{diffRotateTowards} not 0.");

            var diffSqrMagnitude = UVector3.SqrMagnitude((UVector3)vec3D0) - vec3D0.SqrMagnitude();
            if (diffSqrMagnitude >= _epsilon0001)
                LogRelay.Error($"[Sample] SqrMagnitude Diff:{diffSqrMagnitude} not 0.");

            var diffClampReflect = UVector3.Reflect((UVector3)vec3D0, (UVector3)vec3D1) - Vector3D.Reflect(in vec3D0, in vec3D1);
            if (diffClampReflect.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] Reflect Diff:{diffClampReflect} not 0.");

            var diffClampProjectOnPlane = UVector3.ProjectOnPlane((UVector3)vec3D0, (UVector3)vec3D1) - Vector3D.ProjectOnPlane(in vec3D0, in vec3D1);
            if (diffClampProjectOnPlane.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] ProjectOnPlane Diff:{diffClampProjectOnPlane} not 0.");

            var diffGetPoint = ray3D0.GetPoint(num2) - ((URay)ray3D0).GetPoint((float)num2);
            if (diffGetPoint.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] GetPoint Diff:{diffGetPoint} not 0.");

            var diffConjugate = quaternion0.Conjugate() - UQuaternion.Inverse((UQuaternion)quaternion0);
            if (diffConjugate.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] Conjugate Diff:{diffConjugate} not 0.");

            var fFromToRotation = Quaternion.FromToRotation(in vec3D0, in vec3D1);
            var uFromToRotation = UQuaternion.FromToRotation((UVector3)vec3D0, (UVector3)vec3D1);
            if ((fFromToRotation + uFromToRotation).SqrMagnitude() >= _epsilon0000001 && (fFromToRotation - uFromToRotation).SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] FromToRotation Add:{fFromToRotation + uFromToRotation}, Diff:{fFromToRotation - uFromToRotation} not 0.");

            var diffTranslated = plane0.Translated(in vec3D1).Distance - UPlane.Translate((UPlane)plane0, (UVector3)vec3D1).distance;
            if (diffTranslated >= _epsilon0001)
                LogRelay.Error($"[Sample] Translated Diff:{diffTranslated} not 0.");

            var diffGetDistanceToPoint = plane0.GetDistanceToPoint(in vec3D1) - ((UPlane)plane0).GetDistanceToPoint((UVector3)vec3D1);
            if (diffGetDistanceToPoint >= _epsilon0001)
                LogRelay.Error($"[Sample] GetDistanceToPoint Diff:{diffGetDistanceToPoint} not 0.");

            var diffClosestPointOnPlane = plane0.ClosestPointOnPlane(in vec3D1) - ((UPlane)plane0).ClosestPointOnPlane((UVector3)vec3D1);
            if (diffClosestPointOnPlane.SqrMagnitude() >= _epsilon0001)
                LogRelay.Error($"[Sample] ClosestPointOnPlane Diff:{diffClosestPointOnPlane} not 0.");

            bool diffSameSide = plane0.SameSide(in vec3D0, in vec3D1) == ((UPlane)plane0).SameSide((UVector3)vec3D0, (UVector3)vec3D1);
            if (!diffSameSide)
                LogRelay.Error("[Sample] SameSide Diff not 0.");

            plane1.RayCast(in ray3D0, out Vector3D fPoint);
            ((UPlane)plane1).Raycast((URay)ray3D0, out float uEnter);
            var diffRayCast = fPoint - ((URay)ray3D0).GetPoint(uEnter);
            if (diffRayCast.SqrMagnitude() >= _epsilon001)
                LogRelay.Error($"[Sample] RayCast Diff:{diffRayCast} not 0.");

            var diffTRS = Matrix4X4.TRS(in vec3D2, in Quaternion.Identity, in vec3D3) - UMatrix4x4.TRS((UVector3)vec3D2, UQuaternion.identity, (UVector3)vec3D3);
            if (diffTRS != Matrix4X4.Zero)
                LogRelay.Error($"[Sample] TRS Diff:{diffTRS} not 0.");

            var diffDeterminant = matrix4X40.Determinant() - ((UMatrix4x4)matrix4X40).determinant;
            if (diffDeterminant >= _epsilon0001)
                LogRelay.Error("[Sample] Determinant Diff not 0.");

            var diffTranspose = matrix4X40.Transpose() - UMatrix4x4.Transpose((UMatrix4x4)matrix4X40);
            if (diffTranspose != Matrix4X4.Zero)
                LogRelay.Error($"[Sample] Transpose Diff:{diffTranspose} not 0.");

            var fInverse = matrix4X40.Inverse();
            var diffInverse = fInverse - UMatrix4x4.Inverse((UMatrix4x4)matrix4X40);
            if (fInverse.Determinant() != 0 && diffInverse.Determinant() >= _epsilon0001)
                LogRelay.Error($"[Sample] Inverse Diff:{diffInverse} not 0.");
        }
    }

    private UVector3 As(in Vector3D value) => (UVector3)value;
    private UQuaternion As(in Quaternion value) => (UQuaternion)value;
    private UMatrix4x4 As(in Matrix4X4 value) => (UMatrix4x4)value;

    private UQuaternion RotateTowards(in UQuaternion from, in UQuaternion to, float maxDelta)
    {
        float num = UQuaternion.Angle(from, to);
        if (UMath.Approximately(num, 0))
            return to;
        return UQuaternion.SlerpUnclamped(from, to, UMath.Min(1, maxDelta / num));
    }
}