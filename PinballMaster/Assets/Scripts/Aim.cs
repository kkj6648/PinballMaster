using System.Collections.Generic;
using UnityEngine;

public class Aim : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private SpriteRenderer Sr;
    [SerializeField] private LineRenderer Line;

    [Header("Aim Setting")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float maxDistance = 30f;
    [SerializeField] private int targetHitCount = 2;
    [SerializeField] private float skinWidth = 0.02f;

    [Header("Dash Line")]
    [SerializeField] private float dashMoveSpeed = 2f;
    [SerializeField] private float dashTilingPerUnit = 1.5f;

    private Material lineMaterial;
    private float dashOffset;


    private void Awake()
    {
        if (Sr == null)
        {
            Sr = GetComponent<SpriteRenderer>();
        }

        if (Line == null)
        {
            Line = GetComponent<LineRenderer>();
        }

        if (Line != null)
        {
            Line.useWorldSpace = true;
            Line.numCornerVertices = 8;
            Line.numCapVertices = 8;

            Line.textureMode = LineTextureMode.Tile;


            lineMaterial = new Material(Line.material);
            Line.material = lineMaterial;
        }
    }


    private void Update()
    {
        AnimateDashLine();
    }


    public void DrawAim(Vector2 firePos, Vector2 dir)
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.Get_IsPaused())
        {
            Hide();
            return;
        }

        if (dir.sqrMagnitude <= 0.001f)
        {
            Hide();
            return;
        }

        dir = dir.normalized;

        List<Vector3> points = new List<Vector3>();
        points.Add(firePos);

        Vector2 origin = firePos;
        Vector2 currentDir = dir;

        float remainDistance = maxDistance;
        int hitCount = 0;

        while (hitCount < targetHitCount &&
               remainDistance > 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                currentDir,
                remainDistance,
                hitMask
            );

            if (hit.collider == null)
            {

                Vector2 endPoint =
                    origin +
                    currentDir * remainDistance;

                points.Add(endPoint);
                break;
            }

            Vector2 hitPoint = hit.point;

            points.Add(hitPoint);
            hitCount++;

            remainDistance -= hit.distance;

            Vector2 normal = hit.normal;

            Vector2 reflectedDir =
                Vector2.Reflect(
                    currentDir,
                    normal
                ).normalized;

            if (hit.collider.CompareTag("ReturnWall"))
            {
                currentDir =
                    (firePos - hitPoint).normalized;

                break;
            }

            currentDir = reflectedDir;

            origin =
                hitPoint +
                currentDir * skinWidth;
        }

        DrawLine(points);

        if (points.Count > 1)
        {
            SetAimMarker(
                points[points.Count - 1],
                currentDir
            );
        }
        else
        {
            Hide();
        }
    }


    private void DrawLine(
        List<Vector3> points)
    {
        if (Line == null)
            return;

        Line.enabled = true;
        Line.positionCount = points.Count;

        for (int i = 0;
             i < points.Count;
             i++)
        {
            Line.SetPosition(
                i,
                points[i]
            );
        }

        UpdateDashTiling(points);
    }


    private void UpdateDashTiling(
        List<Vector3> points)
    {
        if (lineMaterial == null)
            return;

        if (points == null ||
            points.Count < 2)
        {
            return;
        }

        float totalLength = 0f;

        for (int i = 1;
             i < points.Count;
             i++)
        {
            totalLength += Vector3.Distance(
                points[i - 1],
                points[i]
            );
        }

        lineMaterial.mainTextureScale =
            new Vector2(
                totalLength *
                dashTilingPerUnit,
                1f
            );
    }


    private void AnimateDashLine()
    {
        if (lineMaterial == null)
            return;

        if (Line == null ||
            !Line.enabled)
        {
            return;
        }

        dashOffset +=
            dashMoveSpeed *
            Time.deltaTime;

        lineMaterial.mainTextureOffset =
            new Vector2(
                -dashOffset,
                0f
            );
    }


    private void SetAimMarker(
        Vector2 pos,
        Vector2 dir)
    {
        if (Sr != null)
        {
            Sr.enabled = true;
        }

        transform.position = pos;

        float angle =
            Mathf.Atan2(
                dir.y,
                dir.x
            ) *
            Mathf.Rad2Deg -
            90f;

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }


    public void Hide()
    {
        if (Line != null)
        {
            Line.enabled = false;
        }

        if (Sr != null)
        {
            Sr.enabled = false;
        }
    }


    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}