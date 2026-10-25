using UnityEngine;
using UnityEngine.EventSystems;

// "LAPACZ" PRZECIAGANIA I SCROLLA DLA PUSTEGO TLA DRZEWKA UMIEJETNOSCI.
//
// Problem: PanZoomContent wisi na obiekcie "Content". Gdy gracz lapie myszka
// PUSTE tlo, Unity trafia w Viewport (rodzica Contentu), a zdarzenie
// przeciagania wedruje tylko W GORE hierarchii (Viewport -> Scroll View ->
// SkillsPanel) - nigdy w dol, do Contentu. Dlatego przesuwanie dzialalo
// tylko po zlapaniu za awatar albo wezel.
//
// Ten skrypt siedzi na Viewporcie i przekazuje przeciaganie/scroll do
// PanZoomContent. NIE trzeba go dodawac recznie - PanZoomContent sam
// dokleja go do swojego rodzica przy starcie.
public class PanZoomInputArea : MonoBehaviour, IDragHandler, IScrollHandler
{
    [HideInInspector] public PanZoomContent target;

    public void OnDrag(PointerEventData eventData)
    {
        if (target != null) target.OnDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (target != null) target.OnScroll(eventData);
    }
}
