using UnityEngine;
using UnityEngine.UI;

// POJEDYNCZE OKIENKO PASKA UMIEJETNOSCI WIDOCZNEGO PODCZAS ROZGRYWKI (dol ekranu).
//
// To CZYSTO WIZUALNY odpowiednik slotu hotbara z panelu przypisywania (SkillBarSlot
// z Is Palette Slot = false, klawisz H). Nic tu sie nie klika i nic tu sie nie
// przechowuje na stale - kazda klatke/zmiane SkillHUD kopiuje stan z tamtego
// panelu (ktory pozostaje JEDYNYM zrodlem prawdy o przypisaniach).
public class SkillHUDSlot : MonoBehaviour
{
    [Header("Ikona i podpis klawisza")]
    public Image iconDisplay;
    public TMPro.TextMeshProUGUI keyLabelText;

    [Header("Cooldown")]
    [Tooltip("Szara 'naklejka' na ikonie umiejetnosci. Ustaw Image Type = Filled. " +
             "Fill Method = Radial 360 daje klasyczne 'wycinanie tortu' jak w Diablo/WoW, " +
             "Fill Method = Vertical (Fill Origin = Bottom) daje efekt 'napelniania szklanki' " +
             "od dolu - wybierz co bardziej Ci sie podoba, kod dziala z kazdym.")]
    public Image cooldownOverlay;

    [Tooltip("Opcjonalne - liczba sekund do odnowienia. Zostaw puste, jesli niepotrzebne.")]
    public TMPro.TextMeshProUGUI cooldownText;

    public SkillData Skill { get; private set; }

    // Wolane przez SkillHUD.RefreshFromAssignment() - ustawia, JAKA umiejetnosc
    // (i pod jakim klawiszem) to okienko akurat pokazuje. skill == null -> puste okienko.
    public void SetSkill(SkillData skill, KeyCode boundKey)
    {
        Skill = skill;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = skill != null ? skill.icon : null;
            iconDisplay.enabled = skill != null;
            iconDisplay.preserveAspect = true;
        }

        if (cooldownOverlay != null)
        {
            // Ta sama ikona co iconDisplay - naklejka ma "wyciac" dokladnie ten sam ksztalt.
            cooldownOverlay.sprite = skill != null ? skill.icon : null;
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = false;
        }

        if (cooldownText != null) cooldownText.gameObject.SetActive(false);

        if (keyLabelText != null) keyLabelText.text = FormatKeyName(boundKey);
    }

    private string FormatKeyName(KeyCode key)
    {
        if (key == KeyCode.None) return "";

        string keyName = key.ToString();
        // KeyCode.Alpha4 -> "4"
        if (keyName.StartsWith("Alpha")) return keyName.Substring("Alpha".Length);
        return keyName;
    }

    // Wolane co klatke przez SkillHUD, TYLKO gdy Skill != null.
    // fraction01: 1 = wlasnie uzyta (naklejka w calosci zakrywa ikone), 0 = gotowa (brak naklejki).
    public void UpdateCooldown(float fraction01, float secondsRemaining)
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.enabled = fraction01 > 0.001f;
            cooldownOverlay.fillAmount = fraction01;
        }

        if (cooldownText != null)
        {
            bool show = secondsRemaining > 0.05f;
            cooldownText.gameObject.SetActive(show);
            if (show) cooldownText.text = Mathf.CeilToInt(secondsRemaining).ToString();
        }
    }
}
