using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Скрипт для перетаскивания иконок одежды
public class ClothesIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Настройки")]
    public int clothesCategory = -1; // -1 = не отсортировано, 0 = черная, 1 = белая, 2 = цветная
    [Tooltip("Правильная категория для этой одежды: 0 = черная, 1 = белая, 2 = цветная")]
    public int correctCategory = -1; // Правильная категория (настраивается в Inspector)
    public Sprite clothesSprite; // Спрайт одежды
    
    private Image iconImage; // Для изменения цвета при правильной/неправильной сортировке
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private LaundryBasket basket;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Сохраняем оригинальную позицию и родителя
        originalPosition = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;
        
        // Находим корзину
        basket = FindFirstObjectByType<LaundryBasket>();
        
        // Устанавливаем спрайт, если есть
        iconImage = GetComponent<Image>();
        if (iconImage != null && clothesSprite != null)
        {
            iconImage.sprite = clothesSprite;
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;
        
        // Делаем иконку полупрозрачной и поверх всего
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Перемещаем в корень Canvas, чтобы была поверх всего
        rectTransform.SetParent(canvas.transform);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // Перемещаем иконку за курсором
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Проверяем, над какой зоной отпустили
        bool droppedInZone = false;
        
        // Получаем все зоны сортировки
        if (basket != null)
        {
            GameObject[] zones = basket.GetSortingZones();
            if (zones != null)
            {
                for (int i = 0; i < zones.Length; i++)
                {
                    if (zones[i] != null && RectTransformUtility.RectangleContainsScreenPoint(
                        zones[i].GetComponent<RectTransform>(), eventData.position, canvas.worldCamera))
                    {
                        // Попали в зону!
                        clothesCategory = i;
                        basket.OnClothesDroppedInZone(this, i);
                        droppedInZone = true;
                        
                        // Перемещаем иконку в центр зоны
                        RectTransform zoneRect = zones[i].GetComponent<RectTransform>();
                        rectTransform.SetParent(zoneRect);
                        rectTransform.anchoredPosition = Vector2.zero;
                        
                        // Визуальная обратная связь: правильная/неправильная сортировка
                        UpdateVisualFeedback();
                        break;
                    }
                }
            }
        }
        
        // Если не попали в зону - возвращаем на место
        if (!droppedInZone)
        {
            rectTransform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
    }
    
    public void ResetPosition()
    {
        if (originalParent != null)
        {
            rectTransform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
        clothesCategory = -1;
        UpdateVisualFeedback(); // Сбрасываем визуальную обратную связь
    }
    
    // Обновляет визуальную обратную связь (цвет иконки)
    public void UpdateVisualFeedback()
    {
        if (iconImage == null) return;
        
        // Если не отсортировано - обычный цвет
        if (clothesCategory == -1)
        {
            iconImage.color = Color.white;
            return;
        }
        
        // Если отсортировано правильно - зеленый цвет
        if (clothesCategory == correctCategory)
        {
            iconImage.color = new Color(0.5f, 1f, 0.5f); // Светло-зеленый
        }
        else
        {
            // Если неправильно - красный цвет
            iconImage.color = new Color(1f, 0.5f, 0.5f); // Светло-красный
        }
    }
    
    // Проверяет, правильно ли отсортировано
    public bool IsCorrectlySorted()
    {
        return clothesCategory != -1 && clothesCategory == correctCategory;
    }
}
