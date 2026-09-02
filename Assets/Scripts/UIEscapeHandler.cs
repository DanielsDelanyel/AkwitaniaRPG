using UnityEngine;

// JEDYNY skrypt w grze, ktory czyta klawisz Escape.
//
// Zasada: Escape NIGDY niczego nie otwiera. Zamyka najplytsze otwarte okno
// i konczy dzialanie. Jedno nacisniecie = jedna akcja.
//
// Powies to na obiekcie Canvas w scenie Bootstrap.
public class UIEscapeHandler : MonoBehaviour
{
    // Menu pauzy sprawdza to pole. Jesli jest puste, znaczy ze nikt
    // nie czyta klawisza Escape - wtedy pauza obsluzy go awaryjnie sama.
    public static UIEscapeHandler instance;

    [Header("Ustawienia")]
    [Tooltip("Czy Escape ma konczyc rozmowe z NPC? Odznacz, jesli rozmowa " +
             "ma dac sie zamknac wylacznie opcja dialogowa.")]
    public bool escapeClosesDialogue = true;

    [Tooltip("Wypisuje w konsoli, co Escape wlasnie zamknal - do diagnozy.")]
    public bool logActions = false;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Kolejnosc ma znaczenie: od najplytszego okna do najglebszego.
        // Pierwszy trafiony przypadek konczy obsluge (return).

        // -1. EKRAN SMIERCI jest ponad wszystkim. Escape nic tu nie robi -
        //     gracz musi swiadomie wybrac "Ratuj" albo "Menu glowne".
        if (DeathScreenUI.instance != null && DeathScreenUI.instance.IsShowing)
        {
            Log("nic (ekran smierci)");
            return;
        }

        // 0. Menu pauzy jest NA WIERZCHU wszystkiego - ma pierwszenstwo
        if (PauseMenuUI.instance != null && PauseMenuUI.instance.IsOpen)
        {
            PauseMenuUI.instance.HandleEscape();
            Log("menu pauzy");
            return;
        }

        // 1. Menu kontekstowe (prawy przycisk na przedmiocie)
        if (ContextMenuUI.instance != null && ContextMenuUI.instance.IsOpen)
        {
            ContextMenuUI.instance.CloseMenu();
            Log("menu kontekstowe");
            return;
        }

        // 2. Przypiete okno "Szczegoly"
        if (InventoryTooltip.instance != null && InventoryTooltip.instance.IsPinned)
        {
            InventoryTooltip.instance.ForceHide();
            Log("okno szczegolow");
            return;
        }

        // 3. Panel prezentow
        if (DialogueManager.instance != null && DialogueManager.instance.IsGiftPanelOpen)
        {
            DialogueManager.instance.CloseGiftPanel();
            Log("panel prezentow");
            return;
        }

        // 4. Sklep
        if (DialogueManager.instance != null && DialogueManager.instance.IsShopOpen)
        {
            DialogueManager.instance.CloseShopPanel();
            Log("sklep");
            return;
        }

        // 5. Okno statystyk
        if (StatsUI.instance != null && StatsUI.instance.IsOpen)
        {
            StatsUI.instance.CloseStatsWindow();
            Log("okno statystyk");
            return;
        }

        // 6. Dziennik zadan
        if (QuestLogUI.instance != null && QuestLogUI.instance.IsOpen)
        {
            QuestLogUI.instance.CloseQuestLog();
            Log("dziennik zadan");
            return;
        }

        // 7. Ekwipunek
        if (InventoryUI.instance != null && InventoryUI.instance.IsOpen)
        {
            InventoryUI.instance.CloseInventory();
            Log("ekwipunek");
            return;
        }

        // 7.5. Pasek umiejetnosci (przypisywanie skilli pod klawisze)
        if (SkillBarUI.instance != null && SkillBarUI.instance.IsOpen)
        {
            SkillBarUI.instance.ClosePanel();
            Log("pasek umiejetnosci");
            return;
        }

        // 8. Rozmowa - zamykana na koncu, bo jest "pod" pozostalymi oknami
        if (escapeClosesDialogue && DialogueManager.instance != null
            && DialogueManager.instance.IsDialogueOpen)
        {
            DialogueManager.instance.CloseDialogue();
            Log("rozmowe");
            return;
        }

        // 9. Nic nie bylo otwarte - DOPIERO teraz otwieramy menu pauzy.
        // Dzieki temu Escape najpierw zamyka to, co masz przed oczami.
        if (PauseMenuUI.instance != null)
        {
            PauseMenuUI.instance.Open();
            Log("otwarto menu pauzy");
            return;
        }

        Log("nic (brak otwartych okien)");
    }

    private void Log(string what)
    {
        if (logActions) Debug.Log($"Escape zamknal: {what}");
    }
}
