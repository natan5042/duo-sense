using UnityEngine;
using GameQuests;
using System.Collections.Generic;

/// <summary>
/// При загрузке сцены (дом) удаляет квесты с уровня супермаркета, чтобы они не мешали открытию двери и не висели в списке.
/// Добавить на любой GameObject в сцене дома (например на пустой "SceneSetup").
/// </summary>
public class ClearSupermarketQuestsOnLoad : MonoBehaviour
{
    [Tooltip("Названия квестов для удаления при загрузке этой сцены")]
    public List<string> questTitlesToRemove = new List<string>
    {
        "Traverser l'obstacle",
        "Acheter les produits"
    };

    void Awake()
    {
        if (QuestSystem.Instance == null) return;
        if (questTitlesToRemove == null || questTitlesToRemove.Count == 0) return;
        QuestSystem.Instance.RemoveQuestsByTitles(questTitlesToRemove);
    }
}
