using UnityEngine;

// KONTRAKT dla efektow umiejetnosci typu SkillEffectType.Active (patrz
// PlayerSkills.CastActiveEffect). Dzieki temu PlayerSkills nie musi wiedziec,
// czy to Wir Powietrza Mnicha, przyszla Kula Ognia czy cokolwiek innego -
// kazdy taki efekt implementuje wlasny Setup() i sam sobie ogarnia predkosc,
// obrot, kolizje i znikanie po czasie.
//
// Wzorowane na WandSpell (analogiczny kontrakt dla zaklec z rozdzek) - inny
// zestaw parametrow, bo tu doszedl osobny "duration" zamiast zasiegu.
public abstract class ActiveSkillEffect : MonoBehaviour
{
    public abstract void Setup(int damage, float duration, float speed, float aimAngleDegrees);
}
