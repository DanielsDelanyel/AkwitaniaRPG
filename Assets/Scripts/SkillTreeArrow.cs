using UnityEngine;
using UnityEngine.UI;

// POJEDYNCZA STRZALKA (LINIA) laczaca dwa wezly w panelu Umiejetnosci.
//
// Prefab: Image o pivot (0, 0.5) i zakotwiczeniu Anchor Min/Max = (0.5, 0.5) -
// wtedy anchoredPosition trafia dokladnie tam, gdzie chcemy w ukladzie lokalnym
// obiektu "Content", a rozciagniecie sizeDelta.x + obrot robia reszte.
[RequireComponent(typeof(RectTransform))]
public class SkillTreeArrow : MonoBehaviour
{
    public Image lineImage;

    [Header("Kolory Stanu")]
    [Tooltip("Umiejetnosc jeszcze niedostepna (brak wymagan lub punktow).")]
    public Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);

    [Tooltip("Mozna juz kupic - to jest ta 'podswietlona strzalka'.")]
    public Color availableColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Umiejetnosc juz odblokowana.")]
    public Color unlockedColor = new Color(0.3f, 1f, 0.4f, 1f);

    // Umiejetnosc, DO KTOREJ prowadzi ta strzalka - jej stan decyduje o kolorze.
    [HideInInspector] public SkillData TargetSkill;

    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (lineImage == null) lineImage = GetComponent<Image>();
    }

    public void SetEndpoints(Vector2 fromLocalPos, Vector2 toLocalPos)
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        Vector2 diff = toLocalPos - fromLocalPos;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = fromLocalPos;
        rt.sizeDelta = new Vector2(distance, rt.sizeDelta.y);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetState(bool unlocked, bool canUnlock)
    {
        if (lineImage == null) return;
        lineImage.color = unlocked ? unlockedColor : (canUnlock ? availableColor : lockedColor);
    }
}
