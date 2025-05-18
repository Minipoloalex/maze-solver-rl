using UnityEngine;
using UnityEngine.EventSystems;   // for pointer interfaces

[RequireComponent(typeof(Collider))]
public class WallCell : MonoBehaviour,
                        IPointerEnterHandler,
                        IPointerExitHandler,
                        IPointerClickHandler
{
    [Header("Visual feedback")]
    public Material normalMat;
    public Material hoverMat;
    [HideInInspector] public MazeSpawner spawner;
    [HideInInspector] public Vector2Int posId;

    MeshRenderer _mr;

    void Awake()
    {
        _mr = GetComponent<MeshRenderer>();
        _mr.material = normalMat;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        _mr.material = hoverMat;
    }

    public void OnPointerExit(PointerEventData _)
    {
        _mr.material = normalMat;
    }

    public void OnPointerClick(PointerEventData _)
    {
        spawner.SpawnFloorTrigger(gameObject.transform.position, posId);
        Destroy(gameObject);      // visual disappears
    }
}
