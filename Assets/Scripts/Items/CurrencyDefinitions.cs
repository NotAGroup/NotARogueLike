using UnityEngine;

public enum Currency {
    Gold = 0,
    XP = 1
}

[System.Serializable]
public class CurrencyDefinition {
    [Header("General")]
    public string name;

    [Header("Rendering")]
    public string displayName;
    public string description;
    // prefab for displaying currency in scene
    public GameObject currencyModel;
    // texture for displaying currency in ui
    public Sprite currencySprite;
}

[DisallowMultipleComponent]
public class CurrencyDefinitions : KeyValueStoreComponent<Currency, CurrencyDefinition>
{
}
