using System.Collections.Generic;
using TMPro;
using UnityEngine;

// PANEL DRZEWKA UMIEJETNOSCI (klawisz G).
//
// Awatar gracza stoi na srodku "Content" (punkt lokalny 0,0 wzgledem
// avatarAnchor). Kazda profesja (galaz) ma wlasny kierunek (kat) - jej
// wezly BEZ wymagan (poczatek galezi) rysuja sie najblizej awatara, kolejne
// wezly (ktore wymagaja poprzednich) coraz dalej wzdluz tej samej galezi.
//
// Pozycje licza sie SAME z grafu SkillData.requiredSkills (glebokosc = promien,
// profesja = kierunek) - wystarczy dolozyc nowy SkillData i wpisac mu
// requiredSkills, zeby pojawil sie we wlasciwym miejscu drzewka bez
// recznego przestawiania czegokolwiek w Edytorze.
public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI instance;

    // Jedna galaz = jedna profesja + kierunek (w stopniach), w ktorym rysujemy jej wezly.
    [System.Serializable]
    public class ProfessionBranch
    {
        public CharacterClass profession;

        [Tooltip("Kierunek galezi w stopniach. 0 = w prawo, 90 = w gore, 180 = w lewo, 270 (albo -90) = w dol.")]
        public float angleDegrees;
    }

    [Header("Okno")]
    public GameObject skillTreeWindow;

    [Header("Sterowanie")]
    public KeyCode toggleKey = KeyCode.G;

    [Header("Uklad Drzewka")]
    [Tooltip("Obiekt trzymajacy awatar, wszystkie wezly i strzalki - to na nim wisi PanZoomContent.")]
    public RectTransform treeContent;

    [Tooltip("Awatar gracza na srodku drzewka - punkt (lokalny), z ktorego wychodza wszystkie galezie.")]
    public RectTransform avatarAnchor;

    public ProfessionBranch[] branches;

    [Tooltip("Odleglosc miedzy kolejnymi 'pierscieniami' wezlow (kazdy pierscien = jeden krok " +
             "glebiej w wymaganiach danej galezi).")]
    public float radiusStep = 140f;

    [Tooltip("Jak szeroko (w stopniach) rozjezdzaja sie wezly na TEJ SAMEJ glebokosci w obrebie " +
             "jednej galezi, gdy jest ich wiecej niz jeden (rozgalezienie).")]
    public float siblingSpreadDegrees = 26f;

    [Header("Prefaby")]
    public SkillNodeUI nodePrefab;
    public SkillTreeArrow arrowPrefab;

    [Header("Info o Punktach (opcjonalne)")]
    public TextMeshProUGUI skillPointsText;

    private readonly List<SkillNodeUI> spawnedNodes = new List<SkillNodeUI>();
    private readonly List<SkillTreeArrow> spawnedArrows = new List<SkillTreeArrow>();
    private bool treeBuilt;

    public bool IsOpen { get { return skillTreeWindow != null && skillTreeWindow.activeSelf; } }

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        TrySubscribeToSkillEvents();
    }

    void OnDisable()
    {
        if (PlayerSkills.instance != null) PlayerSkills.instance.onSkillsChanged -= RefreshAllNodes;
    }

    void Start()
    {
        if (skillTreeWindow != null) skillTreeWindow.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        if (IsOpen)
        {
            CloseSkillTree();
            return;
        }

        // Nie otwieraj na sile, jesli inne okno (dialog, sklep, ekwipunek, dziennik...) juz trzyma blokade
        if (UILock.IsLocked) return;

        OpenSkillTree();
    }

    // PlayerSkills moze zainicjalizowac sie PO tym oknie (kolejnosc Awake/OnEnable w Unity nie
    // jest gwarantowana miedzy roznymi obiektami) - probujemy podpiac sie tu i przy otwieraniu.
    private void TrySubscribeToSkillEvents()
    {
        if (PlayerSkills.instance == null) return;
        PlayerSkills.instance.onSkillsChanged -= RefreshAllNodes;
        PlayerSkills.instance.onSkillsChanged += RefreshAllNodes;
    }

    // ===============================================================
    // OTWIERANIE / ZAMYKANIE
    // ===============================================================
    public void OpenSkillTree()
    {
        if (skillTreeWindow == null) return;

        skillTreeWindow.SetActive(true);
        UILock.Set("SkillTree", true);

        TrySubscribeToSkillEvents();

        if (!treeBuilt) BuildTree();
        else RefreshAllNodes();
    }

    public void CloseSkillTree()
    {
        if (skillTreeWindow != null) skillTreeWindow.SetActive(false);
        UILock.Set("SkillTree", false);

        if (InventoryTooltip.instance != null) InventoryTooltip.instance.HideTooltip();
    }

    // ===============================================================
    // BUDOWA DRZEWKA (raz - potem tylko RefreshAllNodes przy zmianach)
    // ===============================================================
    private void BuildTree()
    {
        ClearTree();

        if (treeContent == null || nodePrefab == null || branches == null || SkillDatabase.Instance == null)
        {
            Debug.LogWarning("SkillTreeUI: brak treeContent/nodePrefab/branches albo SkillDatabase - drzewko nie zostalo zbudowane.");
            return;
        }

        Dictionary<string, SkillNodeUI> nodesById = new Dictionary<string, SkillNodeUI>();

        foreach (ProfessionBranch branch in branches)
        {
            if (branch == null) continue;

            List<SkillData> skills = SkillDatabase.Instance.GetForProfession(branch.profession);

            // Glebokosc = najdluzsza sciezka wymagan od wezla startowego TEJ galezi.
            Dictionary<SkillData, int> depthById = new Dictionary<SkillData, int>();
            foreach (SkillData skill in skills) ComputeDepth(skill, depthById);

            // Grupujemy po glebokosci, zeby rodzenstwo na tym samym "pierscieniu" rozjechac symetrycznie.
            Dictionary<int, List<SkillData>> byDepth = new Dictionary<int, List<SkillData>>();
            foreach (SkillData skill in skills)
            {
                int depth = depthById.TryGetValue(skill, out int d) ? d : 0;
                if (!byDepth.TryGetValue(depth, out List<SkillData> list))
                {
                    list = new List<SkillData>();
                    byDepth[depth] = list;
                }
                list.Add(skill);
            }

            foreach (KeyValuePair<int, List<SkillData>> kvp in byDepth)
            {
                int depth = kvp.Key;
                List<SkillData> siblings = kvp.Value;

                float radius = (depth + 1) * radiusStep;
                float totalSpread = siblingSpreadDegrees * (siblings.Count - 1);
                float startAngle = branch.angleDegrees - totalSpread / 2f;

                for (int i = 0; i < siblings.Count; i++)
                {
                    SkillData skill = siblings[i];
                    float angle = siblings.Count > 1 ? startAngle + i * siblingSpreadDegrees : branch.angleDegrees;

                    Vector2 pos = PolarToLocal(angle, radius);
                    SkillNodeUI node = SpawnNode(skill, pos);
                    nodesById[skill.GetId()] = node;
                }
            }
        }

        // Strzalki dopiero PO ustawieniu wszystkich wezlow - kazda potrzebuje pozycji obu koncow.
        foreach (SkillNodeUI node in spawnedNodes)
        {
            if (node.Skill.requiredSkills == null || node.Skill.requiredSkills.Length == 0)
            {
                // Wezel startowy galezi - strzalka prosto z awatara.
                SpawnArrow(avatarAnchor, node);
                continue;
            }

            foreach (SkillData required in node.Skill.requiredSkills)
            {
                if (required == null) continue;
                if (nodesById.TryGetValue(required.GetId(), out SkillNodeUI fromNode))
                    SpawnArrow(fromNode.RectTransform, node);
            }
        }

        treeBuilt = true;
        RefreshAllNodes();
    }

    // Rekurencyjnie liczy glebokosc (najdluzsza sciezka wymagan wstecz do wezla bez wymagan).
    private int ComputeDepth(SkillData skill, Dictionary<SkillData, int> cache)
    {
        if (skill == null) return 0;
        if (cache.TryGetValue(skill, out int cached)) return cached;

        if (skill.requiredSkills == null || skill.requiredSkills.Length == 0)
        {
            cache[skill] = 0;
            return 0;
        }

        int maxParentDepth = -1;
        foreach (SkillData req in skill.requiredSkills)
        {
            if (req == null) continue;
            int parentDepth = ComputeDepth(req, cache);
            if (parentDepth > maxParentDepth) maxParentDepth = parentDepth;
        }

        int depth = maxParentDepth + 1;
        cache[skill] = depth;
        return depth;
    }

    private Vector2 PolarToLocal(float angleDegrees, float radius)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 origin = avatarAnchor != null ? avatarAnchor.anchoredPosition : Vector2.zero;
        return origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
    }

    private SkillNodeUI SpawnNode(SkillData skill, Vector2 localPos)
    {
        SkillNodeUI node = Instantiate(nodePrefab, treeContent);
        node.RectTransform.anchoredPosition = localPos;
        node.Setup(skill, this);
        spawnedNodes.Add(node);
        return node;
    }

    private void SpawnArrow(RectTransform from, SkillNodeUI to)
    {
        if (arrowPrefab == null || from == null) return;

        SkillTreeArrow arrow = Instantiate(arrowPrefab, treeContent);
        arrow.transform.SetAsFirstSibling(); // strzalki rysuja sie POD ikonkami, nie na nich
        arrow.TargetSkill = to.Skill;
        arrow.SetEndpoints(from.anchoredPosition, to.RectTransform.anchoredPosition);
        spawnedArrows.Add(arrow);
    }

    private void ClearTree()
    {
        foreach (SkillNodeUI node in spawnedNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        spawnedNodes.Clear();

        foreach (SkillTreeArrow arrow in spawnedArrows)
        {
            if (arrow != null) Destroy(arrow.gameObject);
        }
        spawnedArrows.Clear();
    }

    // ===============================================================
    // ODSWIEZANIE STANU (po zakupie, levelupie, wczytaniu zapisu itd.)
    // ===============================================================
    public void RefreshAllNodes()
    {
        if (PlayerSkills.instance == null) return;

        foreach (SkillNodeUI node in spawnedNodes)
        {
            if (node != null) node.RefreshVisual();
        }

        foreach (SkillTreeArrow arrow in spawnedArrows)
        {
            if (arrow == null || arrow.TargetSkill == null) continue;

            bool unlocked = PlayerSkills.instance.IsUnlocked(arrow.TargetSkill);
            bool canUnlock = PlayerSkills.instance.CanUnlock(arrow.TargetSkill);
            arrow.SetState(unlocked, canUnlock);
        }

        if (skillPointsText != null)
            skillPointsText.text = $"Punkty umiejetnosci: {PlayerSkills.instance.skillPoints}";
    }
}
