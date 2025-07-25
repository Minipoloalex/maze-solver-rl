using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls clicks on empty cells (to create new walls)
/// </summary>
[RequireComponent(typeof(Collider))]
public class FloorCell : MonoBehaviour,
                         IPointerEnterHandler,
                         IPointerExitHandler,
                         IPointerClickHandler
{
    [Header("Prefabs")]
    public GameObject ghostPrefab;     // transparent cube shown on hover

    [HideInInspector] public MazeController controller;
    [HideInInspector] public Vector2Int posId;

    GameObject _ghost;

    void Awake()
    {
        if (ghostPrefab != null)
        {
            _ghost = Instantiate(ghostPrefab, transform.position, transform.rotation, transform);
            _ghost.transform.localScale = Vector3.one; // Ensure ghost is not double-scaled if parent is scaled
            _ghost.SetActive(false);
        }
        else
        {
            Debug.LogError("FloorCell: GhostPrefab not assigned. Hover effect will be missing.", this);
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (_ghost != null)
        {
            _ghost.SetActive(true);
        }
        else
        {
            Debug.LogWarning("FloorCell: Ghost not set active since it was never created.", this);
        }
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_ghost != null)
        {
            _ghost.SetActive(false);
        }
        else
        {
            Debug.LogWarning("FloorCell: Ghost not set inactive since it was never created.", this);
        }
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (controller != null)
        {
            controller.SwitchFloorToWall(posId, gameObject);
            if (_ghost != null)
            {
                Destroy(_ghost);
            }
            else
            {
                Debug.LogWarning("FloorCell: Ghost not deleted since it was never created.", this);
            }
        }
        else
        {
            Debug.LogError("FloorCell: Spawner reference not set!", this);
        }
    }
}
