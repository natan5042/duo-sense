using UnityEngine;

// Manages a row of puzzle levers. Works for both upper (torches) and lower (sound) setups.
public class LeverPuzzleRow : MonoBehaviour
{
    [Header("Lever Row")]
    public PuzzleLever[] levers;      // order left→right
    public int[] correctIndices;      // which lever indices must be ON
    public GameObject chain;          // obstacle to disable when solved

    [Header("Torches (optional, upper row)")]
    public Animator[] torchAnimators; // same order as levers
    public string torchBoolName = "isOn";

    [Header("Sound (optional, lower row)")]
    public AudioSource correctLeverSound;

    private bool[] lastCorrectActive;
    private bool[] lastTorchStates; // для отслеживания предыдущего состояния факелов

    private bool initialized = false;

    void Awake()
    {
        if (levers != null && levers.Length > 0)
        {
            lastCorrectActive = new bool[levers.Length];
            if (torchAnimators != null && torchAnimators.Length > 0)
            {
                lastTorchStates = new bool[torchAnimators.Length];
            }
        }
    }

    void OnEnable()
    {
        // Принудительно выключаем все факелы при включении объекта
        if (torchAnimators != null)
        {
            for (int i = 0; i < torchAnimators.Length; i++)
            {
                if (torchAnimators[i] != null)
                {
                    // Выключаем факел независимо от состояния enabled
                    torchAnimators[i].SetBool(torchBoolName, false);
                    // Принудительно обновляем параметр несколько раз
                    torchAnimators[i].Update(0f);
                    torchAnimators[i].Update(0f);
                }
            }
        }
    }

    void Start()
    {
        // Инициализируем факелы в выключенном состоянии после Start
        InitializeTorches();
    }

    void InitializeTorches()
    {
        if (!initialized && torchAnimators != null && torchAnimators.Length > 0)
        {
            if (lastTorchStates == null)
                lastTorchStates = new bool[torchAnimators.Length];

            for (int i = 0; i < torchAnimators.Length; i++)
            {
                if (torchAnimators[i] != null)
                {
                    // Принудительно выключаем факел, даже если рычаг включен
                    torchAnimators[i].SetBool(torchBoolName, false);
                    // Принудительно обновляем параметр несколько раз
                    torchAnimators[i].Update(0f);
                    torchAnimators[i].Update(0f);
                    if (i < lastTorchStates.Length)
                        lastTorchStates[i] = false;
                }
            }
            initialized = true;
            
            // Принудительно обновляем факелы после небольшой задержки, чтобы убедиться что они выключены
            Invoke(nameof(ForceUpdateTorches), 0.05f);
        }
    }

    void ForceUpdateTorches()
    {
        if (torchAnimators != null && levers != null)
        {
            for (int i = 0; i < torchAnimators.Length && i < levers.Length; i++)
            {
                if (torchAnimators[i] != null && levers[i] != null)
                {
                    bool isCorrectIndex = System.Array.IndexOf(correctIndices, i) >= 0;
                    bool isOn = levers[i].IsOn;
                    bool torchOn = isCorrectIndex && isOn;
                    
                    if (torchAnimators[i].enabled)
                    {
                        torchAnimators[i].SetBool(torchBoolName, torchOn);
                        if (lastTorchStates != null && i < lastTorchStates.Length)
                            lastTorchStates[i] = torchOn;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (levers == null || levers.Length == 0)
            return;

        bool puzzleSolved = true;

        for (int i = 0; i < levers.Length; i++)
        {
            PuzzleLever lever = levers[i];
            if (lever == null) continue;

            bool isCorrectIndex = System.Array.IndexOf(correctIndices, i) >= 0;
            bool isOn = lever.IsOn;

            // Torches: light only if this lever is correct AND on
            if (torchAnimators != null && i < torchAnimators.Length && torchAnimators[i] != null)
            {
                // Инициализируем массив если нужно
                if (!initialized)
                {
                    InitializeTorches();
                }

                // Расширяем массив если нужно
                if (lastTorchStates != null && i >= lastTorchStates.Length)
                {
                    System.Array.Resize(ref lastTorchStates, torchAnimators.Length);
                }

                // Факел горит ТОЛЬКО если рычаг правильный И включен
                bool torchOn = isCorrectIndex && isOn;
                
                // Проверяем, изменилось ли состояние
                if (lastTorchStates != null && i < lastTorchStates.Length)
                {
                    if (lastTorchStates[i] != torchOn)
                    {
                        if (torchAnimators[i].enabled)
                        {
                            torchAnimators[i].SetBool(torchBoolName, torchOn);
                            lastTorchStates[i] = torchOn;
                        }
                    }
                }
            }

            // Sound: play once when a correct lever switches from off -> on
            if (correctLeverSound != null && isCorrectIndex)
            {
                bool nowCorrectActive = isOn;
                if (nowCorrectActive && lastCorrectActive != null && i < lastCorrectActive.Length && !lastCorrectActive[i])
                {
                    if (!correctLeverSound.isPlaying) // не играть, если уже играет
                        correctLeverSound.Play();
                }

                if (lastCorrectActive != null && i < lastCorrectActive.Length)
                    lastCorrectActive[i] = nowCorrectActive;
            }

            // Combination validity: all correct ON, all others OFF
            if (isCorrectIndex && !isOn)
                puzzleSolved = false;
            if (!isCorrectIndex && isOn)
                puzzleSolved = false;
        }

        if (chain)
            chain.SetActive(!puzzleSolved); // hide chain when solved
    }
}

