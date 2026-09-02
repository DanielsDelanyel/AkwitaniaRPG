using UnityEngine;

// PASEK UMIEJETNOSCI WIDOCZNY PODCZAS ROZGRYWKI (dol ekranu).
//
// Czysto wizualny - JEDYNYM zrodlem prawdy o tym, co jest przypisane pod jaki
// klawisz, pozostaje panel spod klawisza H (SkillBarUI + jego hotbarArea).
// Ten skrypt tylko kopiuje ten stan na statyczne okienka na dole ekranu
// i dorysowuje cooldown - nic tu sie nie klika.
//
// POKAZUJEMY ZAWSZE WSZYSTKIE 8 OKIENEK (rowniez puste ramki bez przypisanej
// umiejetnosci) - to prostsze (sztywny uklad 1:1 z hotbarem panelu, zero
// przestawiania sie/przesuwania ikon w biegu) i gracz od razu widzi, ktory
// klawisz ma jeszcze wolny. Jesli kiedys wolal(a)bys pokazywac WYLACZNIE
// wypelnione okienka, wystarczy w RefreshFromAssignment dodac
// hudSlots[i].gameObject.SetActive(skill != null).
public class SkillHUD : MonoBehaviour
{
    public static SkillHUD instance;

    [Tooltip("Statyczne okienka paska, w TEJ SAMEJ KOLEJNOSCI co sloty hotbara w panelu " +
             "przypisywania (SkillBarUI -> Hotbar Area) - okienko [0] tego paska zawsze " +
             "pokazuje to samo, co slot [0] tamtego panelu, itd.")]
    public SkillHUDSlot[] hudSlots;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (SkillBarUI.instance != null) SkillBarUI.instance.onHotbarChanged += RefreshFromAssignment;
        RefreshFromAssignment();
    }

    void OnDestroy()
    {
        if (SkillBarUI.instance != null) SkillBarUI.instance.onHotbarChanged -= RefreshFromAssignment;
    }

    void Update()
    {
        if (hudSlots == null || PlayerSkills.instance == null) return;

        foreach (SkillHUDSlot hudSlot in hudSlots)
        {
            if (hudSlot == null || hudSlot.Skill == null) continue;

            float fraction = PlayerSkills.instance.GetCooldownFraction(hudSlot.Skill);
            float seconds = PlayerSkills.instance.GetCooldownRemainingSeconds(hudSlot.Skill);
            hudSlot.UpdateCooldown(fraction, seconds);
        }
    }

    // Kopiuje aktualne przypisania (umiejetnosc + klawisz) z panelu H na sztywno
    // powiazane okienka tego paska. Wywolywane raz na starcie oraz za kazdym razem,
    // gdy gracz cokolwiek zmieni w panelu przypisywania (patrz SkillBarUI.onHotbarChanged).
    public void RefreshFromAssignment()
    {
        if (hudSlots == null || SkillBarUI.instance == null) return;

        SkillBarSlot[] assignmentSlots = SkillBarUI.instance.HotbarSlots;
        if (assignmentSlots == null) return;

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] == null) continue;

            if (i < assignmentSlots.Length && assignmentSlots[i] != null)
                hudSlots[i].SetSkill(assignmentSlots[i].skill, assignmentSlots[i].boundKey);
            else
                hudSlots[i].SetSkill(null, KeyCode.None);
        }
    }
}
