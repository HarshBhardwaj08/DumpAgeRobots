using System.Collections.Generic;
using UnityEngine;

public static class ClothoidGenerator
{
    // Fresnel approximation using Euler method
    public static List<Vector3> GenerateClothoid(Vector3 start, Vector3 dirStart, Vector3 end, float step = 0.1f, float totalLength = 10f)
    {
        List<Vector3> points = new List<Vector3>();

        dirStart.Normalize();
        Vector3 forward = dirStart;
        Vector3 position = start;

        Vector3 toTarget = end - start;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget);
        float angle = Quaternion.Angle(Quaternion.LookRotation(dirStart), targetRotation);
        float angleSign = Mathf.Sign(Vector3.Cross(dirStart, toTarget).y);
        float maxCurvature = angleSign * angle * Mathf.Deg2Rad / totalLength;

        float curvature = 0f;
        float distance = 0f;

        while (distance < totalLength)
        {
            curvature = maxCurvature * (distance / totalLength); // linearly increasing curvature
            Quaternion rotStep = Quaternion.Euler(0f, curvature * step * Mathf.Rad2Deg, 0f);
            forward = rotStep * forward;

            position += forward * step;
            points.Add(position);
            distance += step;
        }

        return points;
    }
}