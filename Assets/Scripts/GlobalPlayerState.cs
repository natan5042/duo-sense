using UnityEngine;

// Petit état global pour ce projet (mains du joueur)
public enum HeldItemType
{
    None = 0,
    SortedPile,
    WetPile
}

public static class GlobalPlayerState
{
    public static HeldItemType currentItem = HeldItemType.None;

    public static void ClearHands()
    {
        currentItem = HeldItemType.None;
    }
}
