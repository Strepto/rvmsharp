namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using MathNet.Numerics.LinearAlgebra.Double;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Numerics;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public static class SnoutSnoutComparer
{
    public static bool ShowCap(CapData<RvmSnout> snoutCapData1, CapData<RvmSnout> snoutCapData2)
    {
        var rvmSnout1 = snoutCapData1.Primitive;
        var rvmSnout2 = snoutCapData2.Primitive;

        var isSnoutCapTop1 = snoutCapData1.CapIndex == 1;
        var isSnoutCapTop2 = snoutCapData2.CapIndex == 1;

        Ellipse3D ellipseCurrent;
        Ellipse3D ellipseOther;

        MatrixD snout1ToWorld;
        MatrixD worldToSnout2;
        if (snoutCapData1.IsCurrentPrimitive)
        {
            // any snout has larger cap than a snout w zero radius top&bottom
            if (rvmSnout1.RadiusBottom < 0.00001 && rvmSnout1.RadiusTop < 0.00001)
                return false;
            if (rvmSnout2.RadiusBottom < 0.00001 && rvmSnout2.RadiusTop < 0.00001)
                return true;

            // is ellipse1 totally inside ellipse2 ?
            ellipseCurrent = isSnoutCapTop1 ? rvmSnout1.GetTopCapEllipse() : rvmSnout1.GetBottomCapEllipse();
            ellipseOther = isSnoutCapTop2 ? rvmSnout2.GetTopCapEllipse() : rvmSnout2.GetBottomCapEllipse();
            snout1ToWorld = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(rvmSnout1.Matrix).Transpose();
            // these matrices are stored as trans ^^ vv
            worldToSnout2 = VectorAlgebraHelper
                .ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout2.Matrix))
                .Inverse();
        }
        else
        {
            // any snout has larger cap than a snout w zero radius top&bottom
            if (rvmSnout2.RadiusBottom < 0.00001 && rvmSnout2.RadiusTop < 0.00001)
                return false;
            if (rvmSnout1.RadiusBottom < 0.00001 && rvmSnout1.RadiusTop < 0.00001)
                return true;

            ellipseCurrent = isSnoutCapTop2 ? rvmSnout2.GetTopCapEllipse() : rvmSnout2.GetBottomCapEllipse();
            ellipseOther = isSnoutCapTop1 ? rvmSnout1.GetTopCapEllipse() : rvmSnout1.GetBottomCapEllipse();
            snout1ToWorld = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(rvmSnout2.Matrix).Transpose();
            worldToSnout2 = VectorAlgebraHelper
                .ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout1.Matrix))
                .Inverse();
        }

        double aE1 = ellipseCurrent.ellipse2DPolar.semiMajorAxis;
        double bE1 = ellipseCurrent.ellipse2DPolar.semiMinorAxis;
        double x0E1 = ellipseCurrent.ellipse2DPolar.x0;
        double y0E1 = ellipseCurrent.ellipse2DPolar.y0;
        double theta = ellipseCurrent.ellipse2DPolar.theta;

        var ptE1XplaneCoord = new VectorD[4];
        ptE1XplaneCoord[0] = VectorD.Build.Dense(new double[] { aE1 - x0E1, -y0E1, 0.0f, 1.0 });
        ptE1XplaneCoord[1] = VectorD.Build.Dense(new double[] { -x0E1, bE1 - y0E1, 0.0f, 1.0 });
        ptE1XplaneCoord[2] = VectorD.Build.Dense(new double[] { -aE1 - x0E1, -y0E1, 0.0f, 1.0 });
        ptE1XplaneCoord[3] = VectorD.Build.Dense(new double[] { -x0E1, -bE1 - y0E1, 0.0f, 1.0 });

        var cosTheta = Math.Cos(theta);
        var sinTheta = Math.Sin(theta);
        var matRotationEl1 = DenseMatrix.OfArray(
            new[,]
            {
                { cosTheta, sinTheta, 0.0, 0.0 },
                { sinTheta, cosTheta, 0.0, 0.0 },
                { 0.0, 0.0, 1.0, 0.0 },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var matStack =
            ellipseOther.modelToPlaneCoord
            * worldToSnout2
            * snout1ToWorld
            * ellipseCurrent.planeToModelCoord
            * matRotationEl1;

        var ptE1TransformedToXplaneCoordOfE2 = new VectorD[4];
        for (int i = 0; i < 4; i++)
        {
            ptE1TransformedToXplaneCoordOfE2[i] = matStack.Multiply(ptE1XplaneCoord[i]);
        }

        // hide cap if all four points (extremities) of the ellipse (cap) are inside the other cap
        // returns true if all if all points of the current ellipse are inside the other ellipse
        // returns false if there exists at least one point of the current ellipse that is outside the other ellipse
        const int x = 0;
        const int y = 1;
        for (int i = 0; i < 4; i++)
        {
            var px = ptE1TransformedToXplaneCoordOfE2[i][x];
            var py = ptE1TransformedToXplaneCoordOfE2[i][y];

            var d = ConicSectionsHelper.CalcDistancePointEllise(ellipseOther.ellipse2DPolar, px, py);
            if (d > 0.1) // 0.1mm
            {
                return true;
            }
        }

        return false;
    }
}
