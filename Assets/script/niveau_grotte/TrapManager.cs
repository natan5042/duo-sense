using UnityEngine;
using System.Collections;

public class TrapManager : MonoBehaviour
{
    [Header("Liste des Pics")]
    public FallingSpike[] spikes; // Tableau contenant tous tes pics

    [Header("Réglages de la Séquence")]
    public float delayBetweenSpikes = 0.5f; // Temps entre chaque chute de pic
    public float timeBeforeReset = 3.0f;    // Temps d'attente une fois tous tombés avant le reset

    void Start()
    {
        // Lance la boucle infinie du piège
        StartCoroutine(SpikeSequenceRoutine());
    }

    IEnumerator SpikeSequenceRoutine()
    {
        while (true) // Boucle infinie
        {
            // 1. Faire tomber les pics un par un
            foreach (FallingSpike spike in spikes)
            {
                spike.Drop();
                yield return new WaitForSeconds(delayBetweenSpikes); // Attendre un peu avant le suivant
            }

            // 2. Attendre que tout soit fini
            yield return new WaitForSeconds(timeBeforeReset);

            // 3. Remettre tous les pics en place (Reset)
            foreach (FallingSpike spike in spikes)
            {
                spike.ResetPosition();
            }

            // 4. Petite pause avant de recommencer la séquence
            yield return new WaitForSeconds(1f);
        }
    }
}