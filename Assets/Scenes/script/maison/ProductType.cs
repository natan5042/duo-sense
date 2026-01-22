using UnityEngine;

/// <summary>
/// Типы продуктов для готовки
/// </summary>
public enum ProductType
{
    None,
    Tomato,      // Помидор
    Onion,       // Лук
    Carrot,      // Морковь
    Cheese,      // Сыр
    Egg,         // Яйцо
    Butter,      // Масло
    Milk,        // Молоко
    Pasta        // Макароны
}

/// <summary>
/// Компонент для пометки продукта на полке
/// </summary>
public class ProductItem : MonoBehaviour
{
    public ProductType productType = ProductType.None;
    public Sprite productSprite; // Спрайт продукта для UI
    public AudioClip productSound; // Звук при взятии (для слепой женщины)
    
    [Header("Позиция на полке")]
    public int shelfRow = 0; // Ряд на полке (0-2)
    public int shelfColumn = 0; // Колонка на полке (0-3)
}

/// <summary>
/// Глобальное состояние готовки
/// </summary>
public static class CookingState
{
    public static ProductType currentProduct = ProductType.None;
    public static bool hasProduct = false;
    
    public static void PickProduct(ProductType type)
    {
        currentProduct = type;
        hasProduct = true;
    }
    
    public static void DropProduct()
    {
        currentProduct = ProductType.None;
        hasProduct = false;
    }
}
