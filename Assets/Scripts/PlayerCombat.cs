using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    [Header("Ustawienia strzelania")]
    public Transform firePoint; // Miejsce, z kt�rego wylatuje strza�a (np. puste cia�ko przed graczem)
    public GameObject arrowPrefab; // Prefab Twojej lec�cej strza�y

    [Header("Ustawienia Walki Wr�cz")]
    public GameObject meleeSlashPrefab; // Prefab ci�cia mieczem

    public float fireCooldown = 1f; // Czas oczekiwania mi�dzy strza�ami (1 sekunda)

    private float nextFireTime = 0f; // Pami�ta, kiedy b�dzie mo�na odda� kolejny atak

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
        // Sprawdzamy czy klikni�to ORAZ czy min�� ju� czas cooldownu
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

        // Je�li �uk
        if (weapon.itemType == ItemType.Bow)
        {
            // POBIERAMY ZA�O�ON� STRZA��
            ItemData ammo = equipment.currentAmmo;

            // Sprawdzamy czy cokolwiek le�y w ko�czanie
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
                Debug.Log("Za�� strza�y do ko�czanu!");
            }
        }
        // Je�li Bro� Bia�a
        else if (weapon.itemType == ItemType.Weapon1h || weapon.itemType == ItemType.Weapon2h)
        {
            float angle = GetAngleToMouse();
            PerformMeleeAttack(weapon, angle);
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
        // 1. OBLICZANIE OBRA�E� (Si�a + Obra�enia Broni)
        int totalStrength = stats.GetTotal(stats.baseSTR, stats.equipSTR);

        // Zliczamy sum� i mno�ymy przez mno�nik klasy (np. x1.2 dla Maga)
        float rawDamage = (totalStrength + weapon.GetDamageBonus()) * stats.GetTotalDamageMultiplier();

        // Zaokr�glamy do pe�nych liczb (int), �eby UI �adnie wy�wietla�o cyferki
        int finalDamage = Mathf.RoundToInt(rawDamage);

        // 2. MATEMATYKA PR�DKO�CI (Nasz wz�r!)
        int totalDexterity = stats.GetTotal(stats.baseZR, stats.equipZR);
        float denominator = Mathf.Max(5f, (totalStrength + totalDexterity) * 0.25f);
        float rawDuration = weapon.weight / denominator;

        float swingDuration = Mathf.Clamp(rawDuration, 0.2f, 2.2f);

        // 3. TWORZENIE CI�CIA
        GameObject slashObj = Instantiate(meleeSlashPrefab, firePoint.position, Quaternion.identity);
        slashObj.transform.SetParent(this.transform);

        PlayerMeleeAttack meleeScript = slashObj.GetComponent<PlayerMeleeAttack>();
        meleeScript.Setup(weapon, finalDamage, swingDuration, angle);

        nextFireTime = Time.time + swingDuration;
    }

    // Pomocnicza funkcja licz�ca k�t kursora

    void Shoot(float baseAngle)
    {
        float arrowAngle = baseAngle - 45f; // Korekta graficzna dla strza�y
        Quaternion rotation = Quaternion.Euler(0, 0, arrowAngle);
        Instantiate(arrowPrefab, firePoint.position, rotation);
    }

    float GetAngleToMouse()
    {
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

}