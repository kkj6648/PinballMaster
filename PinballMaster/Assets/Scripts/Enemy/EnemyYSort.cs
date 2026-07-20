using UnityEngine;
using UnityEngine.Rendering;

public class EnemyYSort : MonoBehaviour
{
    [Header("Sorting")]
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private Canvas hpCanvas;

    [SerializeField] private Transform sortPivot;
    [SerializeField] private float precision = 100f;

    [SerializeField] private int enemyOrderOffset;
    [SerializeField] private int hpOrderOffset = 10;

    private void Awake()
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
        }

        if (hpCanvas != null)
        {
            hpCanvas.overrideSorting = true;
        }
    }

    private void LateUpdate()
    {
        float sortY = sortPivot != null ? sortPivot.position.y : transform.position.y;


        int baseOrder = enemyOrderOffset - Mathf.RoundToInt(sortY * precision);



        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = baseOrder;
        }

        if (hpCanvas != null)
        {
            hpCanvas.sortingLayerName = "Enemy";
            hpCanvas.sortingOrder = baseOrder + hpOrderOffset;

        }
    }
}