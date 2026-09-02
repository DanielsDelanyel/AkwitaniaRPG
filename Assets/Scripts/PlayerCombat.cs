using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    [Header("Ustawienia strzelania")]
    public Transform firePoint; // Miejsce, z ktorego wylatuje strzala (np. puste cialko przed graczem)
    public GameObject arrowPrefab; // Prefab Twojej lecacej strzaly

    [Header("Ustawienia Walki Wrecz")]
    public GameObject meleeSlashPrefab; // Prefab ciecia mieczem

    public float fireCooldown = 1f; // Czas oczekiwania miedzy strzalami (1 sekunda)

    private float nextFireTime = 0f; // Pamieta, kiedy bedzie mozna oddac kolejny atak

    private PlayerEquipment equipment;
    private PlayerStats stats;
    private Camera mainCam;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        equipment = GetComponent<PlayerEquipment>();
        mainCam = Camera.main;
    }

    void Update()
    {
        // Sprawdzamy czy kliknieto ORAZ czy minal juz czas cooldownu
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            TryAttack();
        }
    }
    void TryAttack()
    {
        ItemData weapon = equipment.currentWeapon;
        if (weapon == null) return;

        // Jesli luk
        if (weapon.itemType == ItemType.Bow)
        {
            // POBIERAMY ZALOZONA STRZALE
            ItemData ammo = equipment.currentAmmo;

            // Sprawdzamy czy cokolwiek lezy w koczanie
            if (ammo != null && ammo.itemType == ItemType.Ammo)
            {
                if (InventoryUI.instance.ConsumeAmmo())
                {
                    float angle = GetAngleToMouse();

                    // WYSYLAMY PREFAB KONKRETNEJ STRZALY!
                    Shoot(angle, ammo.itemPrefab, weapon, ammo);

                    // NOWE: odglos napietej cieciwy, brany z danych LUKU
                    SoundManager.Play(weapon.swingSounds, weapon.soundVolume);

                    nextFireTime = Time.time + fireCooldown;
                }
            }
            else
            {
                Debug.Log("Zaladuj strzaly do koczanu!");
            }
        }
        // Jesli Bron Biala
        else if (weapon.itemType == ItemType.Weapon1h || weapon.itemType == ItemType.Weapon2h)
        {
            float angle = GetAngleToMouse();
            PerformMeleeAttack(weapon, angle);
        }
        // NOWE: rozdzka (jedno- lub dwureczna) - ani machniecie, ani strzala. Wlasny efekt (iskry, chmura...).
        else if (weapon.itemType == ItemType.Wand1h || weapon.itemType == ItemType.Wand2h)
        {
            float angle = GetAngleToMouse();
            PerformWandAttack(weapon, angle);
        }
    }

    // Wystrzal z podaniem obrazen policzonych ze statystyk gracza
    void Shoot(float baseAngle, GameObject specificArrowPrefab, ItemData bow, ItemData ammo)
    {
        float arrowAngle = baseAngle - 45f;
        Quaternion rotation = Quaternion.Euler(0, 0, arrowAngle);

        GameObject arrow = Instantiate(specificArrowPrefab, firePoint.position, rotation);

        // NOWE: strzala dostaje obrazenia od gracza.
        // Wczesniej brala je ze sztywnego pola w prefabie, wiec ani Zrecznosc,
        // ani statystyki luku nie mialy zadnego wplywu na sile strzalu.
        Projectile projectile = arrow.GetComponent<Projectile>();
        if (projectile != null) projectile.SetDamage(CalculateArrowDamage(bow, ammo));
    }

    // Zrecznosc + bonus z luku + bonus z samej strzaly, razy mnoznik klasy
    int CalculateArrowDamage(ItemData bow, ItemData ammo)
    {
        int dexterity = stats.GetTotal(stats.baseZR, stats.equipZR);
        int bowBonus = bow != null ? bow.GetDamageBonus() : 0;
        int ammoBonus = ammo != null ? ammo.GetDamageBonus() : 0;

        float raw = (dexterity + bowBonus + ammoBonus) * stats.GetTotalDamageMultiplier();
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }
    void PerformMeleeAttack(ItemData weapon, float angle)
    {
        // 1. OBLICZANIE OBRAZEN (Sila + Obrazenia Broni)
        int totalStrength = stats.GetTotal(stats.baseSTR, stats.equipSTR);

        // Zliczamy sume i mnozymy przez mnoznik klasy (np. x1.2 dla Maga)
        float rawDamage = (totalStrength + weapon.GetDamageBonus()) * stats.GetTotalDamageMultiplier();

        // Zaokraglamy do pelnych liczb (int), zeby UI ladnie wyswietlalo cyferki
        int finalDamage = Mathf.RoundToInt(rawDamage);

        // 2. MATEMATYKA PREDKOSCI (Nasz wzor!)
        int totalDexterity = stats.GetTotal(stats.baseZR, stats.equipZR);
        float denominator = Mathf.Max(5f, (totalStrength + totalDexterity) * 0.25f);
        float rawDuration = weapon.weight / denominator;

        float swingDuration = Mathf.Clamp(rawDuration, 0.2f, 2.2f);

        // 3. TWORZENIE CIECIA
        GameObject slashObj = Instantiate(meleeSlashPrefab, firePoint.position, Quaternion.identity);
        slashObj.transform.SetParent(this.transform);

        PlayerMeleeAttack meleeScript = slashObj.GetComponent<PlayerMeleeAttack>();
        meleeScript.Setup(weapon, finalDamage, swingDuration, angle);

        nextFireTime = Time.time + swingDuration;
    }

    // NOWE: atak rozdzka. Nie wiemy tu (i nie musimy wiedziec) czy to iskry, chmura
    // trucizny czy cokolwiek innego - kazdy taki efekt implementuje WandSpell.Setup().
    //
    // W odroznieniu od strzal (Shoot()) NIE narzucamy tu zadnej korekty katu przy
    // Instantiate - kazdy prefab zaklecia sam obraca swoja grafike wedlug WLASNEGO,
    // konfigurowalnego pola (patrz Sprite Angle Offset w FireSparkProjectile), zeby
    // rozne assety (iskry, chmura, cokolwiek pozniej) mogly miec inny domyslny kierunek
    // rysunku bez grzebania w tym kodzie za kazdym razem.
    void PerformWandAttack(ItemData wand, float angle)
    {
        if (wand.spellPrefab == null)
        {
            Debug.LogWarning($"{wand.itemName}: brak przypisanego Spell Prefab!");
            return;
        }

        GameObject spellObj = Instantiate(wand.spellPrefab, firePoint.position, Quaternion.identity);

        WandSpell spell = spellObj.GetComponent<WandSpell>();
        if (spell == null)
        {
            Debug.LogError($"{wand.itemName}: Spell Prefab nie ma komponentu implementujacego WandSpell!");
            Destroy(spellObj);
            return;
        }

        spell.Setup(CalculateSpellDamage(wand), wand.spellRange, angle);

        // Odglos rzucenia - bierzemy z tych samych pol co swist broni bialej/napiecie luku.
        SoundManager.Play(wand.swingSounds, wand.soundVolume);

        nextFireTime = Time.time + wand.spellCooldown;
    }

    // Inteligencja + bonus magiczny rozdzki, razy mnoznik klasy (tak samo jak walka wrecz/luk).
    int CalculateSpellDamage(ItemData wand)
    {
        int intelligence = stats.GetTotal(stats.baseINT, stats.equipINT);
        int wandBonus = wand.GetMagicDamageBonus();

        float raw = (intelligence + wandBonus) * stats.GetTotalDamageMultiplier();
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    // Pomocnicza funkcja liczaca kat kursora

    void Shoot(float baseAngle)
    {
        float arrowAngle = baseAngle - 45f; // Korekta graficzna dla strzaly
        Quaternion rotation = Quaternion.Euler(0, 0, arrowAngle);
        Instantiate(arrowPrefab, firePoint.position, rotation);
    }

    float GetAngleToMouse()
    {
        // ZABEZPIECZENIE: mainCam bylo zlapane RAZ w Start(). Jesli cos w scenie
        // (np. przelaczanie na Virtual Camera) niszczy/podmienia oryginalna
        // Main Camera w trakcie gry, ta referencja staje sie "martwa" i kazda
        // proba jej uzycia rzuca MissingReferenceException - co blokowalo
        // KAZDY atak (luk, bron biala, rozdzka), nie tylko strzaly.
        // Unity przeciazyl "==" dla obiektow, wiec ten warunek wykrywa
        // zarowno null, jak i zniszczony-ale-jeszcze-nie-posprzatany obiekt.
        if (mainCam == null) mainCam = Camera.main;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

}
