using UnityEngine;

// STALY IDENTYFIKATOR OBIEKTU.
//
// Zapis stanu swiata musi wiedziec, KTORA skrzynia zostala otwarta.
// Nazwa obiektu nie wystarcza (moze byc kilka "CommonChest"), a pozycja
// tez nie (mozesz przesunac skrzynie w edytorze i zapis przestanie pasowac).
//
// Dlatego kazdy obiekt dostaje losowy, niezmienny identyfikator zapisany
// w scenie. Powstaje sam przy dodaniu komponentu.
[DisallowMultipleComponent]
public class UniqueId : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Generowany automatycznie. NIE zmieniaj recznie po pierwszym zapisie - " +
             "stary zapis przestanie rozpoznawac ten obiekt.")]
    private string id = "";

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(id)) Generate();
            return id;
        }
    }

    private void Generate()
    {
        id = System.Guid.NewGuid().ToString("N").Substring(0, 16);
    }

#if UNITY_EDITOR
    // Rejestr sluzy WYLACZNIE do wykrywania duplikatow w edytorze.
    // Kopiujac obiekt (Ctrl+D) dostaniesz kopie z tym samym ID -
    // to tu jest wykrywane i naprawiane.
    private static readonly System.Collections.Generic.Dictionary<string, UniqueId> editorRegistry
        = new System.Collections.Generic.Dictionary<string, UniqueId>();

    void OnValidate()
    {
        // Prefaby w oknie Project nie dostaja ID - dopiero ich egzemplarze w scenie
        if (UnityEditor.EditorUtility.IsPersistent(this)) return;
        if (gameObject.scene.name == null) return;

        if (string.IsNullOrEmpty(id))
        {
            Generate();
            UnityEditor.EditorUtility.SetDirty(this);
            return;
        }

        // Czy ktos inny ma juz to ID?
        if (editorRegistry.TryGetValue(id, out UniqueId other))
        {
            if (other != null && other != this)
            {
                Generate();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        editorRegistry[id] = this;
    }
#endif
}
