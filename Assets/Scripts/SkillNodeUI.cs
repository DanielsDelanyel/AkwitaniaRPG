using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// POJEDYNCZA IKONKA UMIEJETNOSCI W PANELU DRZEWKA (klawisz G).
//
// Najechanie pokazuje tooltip (jak w ekwipunku, patrz InventoryTooltip).
// Klikniecie probuje kupic umiejetnosc za punkty - jesli sa spelnione
// wymagania i wystarcza punktow. Kliknieta juz-odblokowana umiejetnosc
// na razie nic nie robi (samo RECZNE rzucanie z panelu to kolejny krok,
// jesli bedziesz go chcial - teraz przyzwanie odpala sie klawiszem debug
// w PlayerSkills).
[RequireComponent(typeof(RectTransform))]
public class SkillNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Wyglad")]
    public Image iconImage;

    [Tooltip("Obwodka/tlo pod ikonka - zmienia kolor wg stanu umiejetnosci. Moze byc tym samym " +
             "obiektem co ikonka, jesli nie masz osobnej ramki.")]
    public Image frameImage;

    [Header("Kolory Stanu")]
    public Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color availableColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color unlockedColor = new Color(0.3f, 1f, 0.4f, 1f);

    public SkillData Skill { get; private set; }

    private SkillTreeUI owner;
    private RectTransform rt;

    public RectTransform RectTransform
    {
        get
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            return rt;
        }
    }

    public void Setup(SkillData skill, SkillTreeUI ownerUI)
    {
        Skill = skill;
        owner = ownerUI;

        if (iconImage != null)
        {
            iconImage.sprite = skill != null ? skill.icon : null;
            iconImage.enabled = skill != null && skill.icon != null;
        }

        RefreshVisual();
    }

    // Wolane przez SkillTreeUI po kazdej zmianie (zakup, levelup, otwarcie panelu).
    public void RefreshVisual()
    {
        if (Skill == null || PlayerSkills.instance == null) return;

        bool unlocked = PlayerSkills.instance.IsUnlocked(Skill);
        bool canUnlock = PlayerSkills.instance.CanUnlock(Skill);

        Color stateColor = unlocked ? unlockedColor : (canUnlock ? availableColor : lockedColor);

        if (frameImage != null) frameImage.color = stateColor;

        // Zablokowana (i jeszcze niedostepna) ikonka jest lekko wyszarzona,
        // zeby od razu bylo widac ktore galezie sa jeszcze "za daleko".
        if (iconImage != null) iconImage.color = (unlocked || canUnlock) ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Skill == null || InventoryTooltip.instance == null || PlayerSkills.instance == null) return;

        bool unlocked = PlayerSkills.instance.IsUnlocked(Skill);
        bool canUnlock = PlayerSkills.instance.CanUnlock(Skill);

        InventoryTooltip.instance.ShowTooltip(Skill, unlocked, canUnlock);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryTooltip.instance != null) InventoryTooltip.instance.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Skill == null || PlayerSkills.instance == null) return;

        if (PlayerSkills.instance.TryUnlock(Skill))
        {
            if (owner != null) owner.RefreshAllNodes();

            // Odswiez tooltip od razu, zamiast czekac na ponowne najechanie myszka.
            if (InventoryTooltip.instance != null)
                InventoryTooltip.instance.ShowTooltip(Skill, true, false);
        }
    }
}
