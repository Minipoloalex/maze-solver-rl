using UnityEngine;
using UnityEngine.EventSystems;   // for pointer interfaces

/// <summary>
/// Controls clicks on wall cells (to destroy the walls)
/// </summary>
[RequireComponent(typeof(Collider))]
public class WallCell : MonoBehaviour,
                        IPointerEnterHandler,
                        IPointerExitHandler,
                        IPointerClickHandler
{
    [Header("Visual feedback")]
    public Material normalMat;
    public Material hoverMat;

    [HideInInspector] public MazeController controller;
    [HideInInspector] public Vector2Int posId;

    MeshRenderer _mr;

    void Awake()
    {
        _mr = GetComponent<MeshRenderer>();
        if (_mr != null && normalMat != null)
        {
            _mr.material = normalMat;
        }
        else
        {
            if (_mr == null)
            {
                Debug.LogError("WallCell: MeshRenderer component is missing.", this);
            }
            if (normalMat == null)
            {
                Debug.LogWarning("WallCell: NormalMat not assigned.", this);
            }
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (_mr != null && hoverMat != null)
        {
            _mr.material = hoverMat;
        }
        else
        {
            if (_mr == null)
            {
                Debug.LogWarning("WallCell: MeshRenderer component is missing.", this);
            }
            if (hoverMat == null)
            {
                Debug.LogWarning("WallCell: HoverMat not assigned.", this);
            }
        }
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_mr != null && normalMat != null)
        {
            _mr.material = normalMat;
        }
        else
        {
            if (_mr == null)
            {
                Debug.LogWarning("WallCell: MeshRenderer component is missing.", this);
            }
            if (normalMat == null)
            {
                Debug.LogWarning("WallCell: NormalMat not assigned.", this);
            }
        }
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (controller != null)
        {
            controller.SwitchWallToFloor(posId, gameObject);
        }
        else
        {
            Debug.LogError("WallCell: Spawner reference not set!", this);
        }
    }
}
