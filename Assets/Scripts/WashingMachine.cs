using UnityEngine;
using System.Collections;
using GameQuests;

public class WashingMachine : MonoBehaviour, IInteractable
{
    [Header("Визуал (Спрайты)")]
    public Sprite emptySprite; // Спрайт пустой стиралки
    public Sprite fullSprite;  // Спрайт стиралки с одеждой
    [Tooltip("Автоматически найден, если не назначен")]
    public SpriteRenderer spriteRenderer; // Ссылка на SpriteRenderer стиралки

    [Header("Звуки")]
    [Tooltip("Автоматически найден, если не назначен")]
    public AudioSource audioSource;
    public AudioClip washLoopClip;
    public AudioClip finishClip;
    public float washDuration = 5f; // Сколько длится стирка

    private bool isWashing = false;
    private bool hasFinished = false;
    
    // Метод для получения текста подсказки в зависимости от состояния
    public string GetHintText()
    {
        // Если стирка идет - показываем что стирка идет
        if (isWashing)
        {
            return "Washing..."; // Показываем что стирка идет
        }
        
        // Если стирка закончена - можно забрать
        if (hasFinished)
        {
            return "Press M to take clothes";
        }
        
        // Если стиралка пустая - можно загрузить (проверяем, есть ли у игрока одежда)
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null && GlobalPlayerState.currentItem == HeldItemType.SortedPile)
        {
            return "Press M to load clothes";
        }
        
        // По умолчанию - стандартная подсказка (всегда показываем что-то)
        return "Press M to use washing machine";
    }
    
    void Awake()
    {
        // Автоматически находим компоненты, если не назначены
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }
    }

    public void Interact(PlayerInteraction player)
    {
        // 1. Если стиралка пустая и у игрока есть Сортированное белье
        if (!isWashing && !hasFinished && GlobalPlayerState.currentItem == HeldItemType.SortedPile)
        {
            StartWash(player);
        }
        // 2. Если стирка закончена - забираем
        else if (hasFinished && GlobalPlayerState.currentItem == HeldItemType.None)
        {
            TakeWetClothes(player);
        }
    }

    void StartWash(PlayerInteraction player)
    {
        // Проверяем, что все компоненты на месте
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"WashingMachine: spriteRenderer не найден на {gameObject.name}! Пытаюсь найти автоматически...");
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (spriteRenderer == null)
            {
                Debug.LogError($"WashingMachine: spriteRenderer не найден! Добавь SpriteRenderer на {gameObject.name}.");
                return;
            }
        }
        
        // Меняем спрайт на полный
        if (fullSprite != null)
        {
            spriteRenderer.sprite = fullSprite;
            Debug.Log($"WashingMachine: Спрайт изменен на полный на {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"WashingMachine: fullSprite не назначен на {gameObject.name}! Стирка начнется, но визуал не изменится.");
        }
        
        // Забираем у игрока вещи
        player.DropItem();

        isWashing = true;

        // Звук стирки
        audioSource.clip = washLoopClip;
        audioSource.loop = true;
        audioSource.Play();
        
        // Обновляем квест
        UpdateQuestStep(1); // Шаг 1: Загрузи одежду в стиралку - выполнен

        // Таймер
        StartCoroutine(WashRoutine());
    }
    
    void UpdateQuestStep(int stepIndex)
    {
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem != null)
        {
            // Ищем квест стирки
            foreach (Quest quest in questSystem.activeQuests)
            {
                if (quest != null && quest.questName == "Faire la lessive")
                {
                    quest.CompleteStep(stepIndex);
                    Debug.Log($"WashingMachine: Шаг {stepIndex} квеста стирки выполнен!");
                    return;
                }
            }
        }
    }

    IEnumerator WashRoutine()
    {
        yield return new WaitForSeconds(washDuration);

        // Стоп стирка, звук финиша
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.PlayOneShot(finishClip); // Проиграть "Дзынь"
        
        isWashing = false;
        hasFinished = true;

        Debug.Log("Стирка готова!");
    }

    void TakeWetClothes(PlayerInteraction player)
    {
        // Проверяем компоненты
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Меняем спрайт обратно на пустой только если он назначен
        if (spriteRenderer != null && emptySprite != null)
        {
            spriteRenderer.sprite = emptySprite;
        }
        
        hasFinished = false;

        // Даем игроку мокрую кучу
        player.PickUpItem(HeldItemType.WetPile);
    }
}