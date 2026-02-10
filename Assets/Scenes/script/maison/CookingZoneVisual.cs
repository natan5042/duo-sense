using UnityEngine;

/// <summary>
/// Визуальное отображение зоны готовки (стрелка или подсветка), чтобы игрок видел, куда подойти.
/// Добавить на тот же объект, что и CookingGame, или на дочерний. Указать спрайт/объект в инспекторе.
/// </summary>
public class CookingZoneVisual : MonoBehaviour
{
    [Tooltip("Спрайт зоны (стрелка или область). Если не назначен, ищется SpriteRenderer на этом объекте.")]
    public SpriteRenderer zoneSprite;
    [Tooltip("Или любой объект для отображения (включится при старте).")]
    public GameObject zoneObject;

    void Start()
    {
        if (zoneSprite == null) zoneSprite = GetComponent<SpriteRenderer>();
        if (zoneSprite == null) zoneSprite = GetComponentInChildren<SpriteRenderer>(true);
        if (zoneSprite != null) zoneSprite.enabled = true;
        if (zoneObject != null) zoneObject.SetActive(true);
    }
}
