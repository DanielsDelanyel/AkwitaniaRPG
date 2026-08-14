using UnityEngine;

// Tworzy KONKRETNY EGZEMPLARZ przedmiotu.
//
// Dlaczego to potrzebne:
// ItemData to plik .asset wspoldzielony przez cala gre. Gdybysmy zapisali w nim
// wynik losowania, KAZDY miecz tego typu mia³by te sama wartosc, a w edytorze
// zmiana wsi¹k³aby na sta³e do pliku. Dlatego robimy kopie w pamieci.
public static class ItemFactory
{
    // Glowna funkcja - wolaj ja wszedzie tam, gdzie przedmiot trafia do swiata.
    public static ItemData Create(ItemData template)
    {
        if (template == null) return null;

        // Juz jest egzemplarzem (np. gracz wyrzucil miecz i podnosi go z powrotem)
        if (template.isRuntimeInstance) return template;

        // Nie ma czego losowac - oddajemy oryginal i oszczedzamy pamiec
        if (!template.NeedsRandomization()) return template;

        ItemData copy = Object.Instantiate(template);
        copy.name = template.name;          // bez tego Unity dokleja "(Clone)"
        copy.isRuntimeInstance = true;
        copy.sourceTemplate = template;

        copy.ClearAllRolls();               // czyscimy slady z szablonu...
        copy.RollAllStats();                // ...i rzucamy koscmi dla TEGO egzemplarza

        return copy;
    }

    // Wersja dla list - np. calej zawartosci skrzyni
    public static void CreateInPlace(ref ItemData item)
    {
        item = Create(item);
    }
}