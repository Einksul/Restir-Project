using UnityEngine;


public class CameraTest : MonoBehaviour
{

    public Vector2Int debugPointCount = new Vector2Int(10, 10);
    public Color debugColor = Color.red;
    public float pointSize = 0.1f;
    public Color arrowColor = Color.yellow;
    public float arrowHeadLength = 0.5f;
    public float arrowHeadAngle = 20.0f;


    void OnDrawGizmos()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return; 

        Transform camT = cam.transform;

        const float Deg2Rad = Mathf.Deg2Rad;


        float halfPlaneHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Deg2Rad);

        float planeHeight = halfPlaneHeight * 2f;
        float planeWidth = planeHeight * cam.aspect;

        Vector3 bottomLeftLocal = new Vector3(-planeWidth / 2f, -halfPlaneHeight, cam.nearClipPlane);
        Gizmos.color = debugColor;

        for (int y = 0; y < debugPointCount.y; y++)
        {
            for (int x = 0; x < debugPointCount.x; x++)
            {
                float tx = (debugPointCount.x > 1) ? x / (float)(debugPointCount.x - 1) : 0.5f;
                float ty = (debugPointCount.y > 1) ? y / (float)(debugPointCount.y - 1) : 0.5f;

                Vector3 pointLocal = bottomLeftLocal + new Vector3(planeWidth * tx, planeHeight * ty, 0f);

                Vector3 pointWorld = camT.position
                                   + camT.right * pointLocal.x
                                   + camT.up * pointLocal.y
                                   + camT.forward * pointLocal.z;

                DrawArrow(camT.position, pointWorld, arrowColor, arrowHeadLength, arrowHeadAngle);
                Gizmos.DrawSphere(pointWorld, pointSize);
            }
        }
    }

    public static void DrawArrow(Vector3 start, Vector3 end, Color color, float length = 0.25f, float angle = 20.0f)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(start, end);
        Vector3 direction = end - start;
        if (direction.magnitude < 0.001f)
        {
            return;
        }

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(angle, 0, 0) * Vector3.back;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(-angle, 0, 0) * Vector3.back;

        Gizmos.DrawLine(end, end + right * length);
        Gizmos.DrawLine(end, end + left * length);
    }
}