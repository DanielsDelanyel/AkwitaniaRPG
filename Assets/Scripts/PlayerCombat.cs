using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    [Header("Ustawienia strzelania")]
    public Transform firePoint; // Miejsce, z którego wylatuje strza³a (np. puste cia³ko przed graczem)
    public GameObject arrowPrefab; // Prefab Twojej lec¹cej strza³y

    [Header("Ustawienia Walki Wrêcz")]
    public GameObject meleeSlashPrefab; // Prefab ciêcia mieczem

    public float fireCooldown = 1f; // Czas oczekiwania miêdzy strza³ami (1 sekunda)

    private float nextFireTime = 0f; // Pamiêta, kiedy bêdzie mo¿na oddaæ kolejny atak

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
        // Sprawdzamy czy klikniêto ORAZ czy min¹³ ju¿ czas cooldownu
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

        // Jeœli £uk
        if (weapon.itemType == ItemType.Bow)
        {
            // POBIERAMY ZA£O¯ON¥ STRZA£Ê
            ItemData ammo = equipment.currentAmmo;

            // Sprawdzamy czy cokolwiek le¿y w ko³czanie
            if (ammo != null && ammo.itemType == ItemType.Ammo)
            {
                if (InventoryUI.instance.ConsumeAmmo())
                {
                    float angle = GetAngleToMouse();

                    // WYSY£AMY PREFAB KONKRETNEJ STRZA£Y!
                    Shoot(angle, ammo.itemPrefab);

                    nextFireTime = Time.time + 1f;
                }
            }
            else
            {
                Debug.Log("Za³ó¿ strza³y do ko³czanu!");
            }
        }
        // Jeœli Broñ Bia³a
        else if (weapon.itemType == ItemType.Weapon1h || weapon.itemType == ItemType.Weapon2h)
        {
            float angle = GetAngleToMouse();
            PerformMeleeAttack(weapon, angle);
        }
    }

    // Dodaliœmy nowy argument: GameObject specificArrowPrefab
    void Shoot(float baseAngle, GameObject specificArrowPrefab)
    {
        float arrowAngle = baseAngle - 45f;
        Quaternion rotation = Quaternion.Euler(0, 0, arrowAngle);

        // Zamiast ogólnego 'arrowPrefab', u¿ywamy tego z ekwipunku!
        Instantiate(specificArrowPrefab, firePoint.position, rotation);
    }
    void PerformMeleeAttack(ItemData weapon, float angle)
    {
        // 1. OBLICZANIE OBRA¯EÑ (Si³a + Obra¿enia Broni)
        int totalStrength = stats.GetTotal(stats.baseSTR, stats.equipSTR);

        // Zliczamy sumê i mno¿ymy przez mno¿nik klasy (np. x1.2 dla Maga)
        float rawDamage = (totalStrength + weapon.damageBonus) * stats.damageMultiplier;

        // Zaokr¹glamy do pe³nych liczb (int), ¿eby UI ³adnie wyœwietla³o cyferki
        int finalDamage = Mathf.RoundToInt(rawDamage);

        // 2. MATEMATYKA PRÊDKOŒCI (Nasz wzór!)
        int totalDexterity = stats.GetTotal(stats.baseZR, stats.equipZR);
        float denominator = Mathf.Max(5f, (totalStrength + totalDexterity) * 0.25f);
        float rawDuration = weapon.weight / denominator;

        float swingDuration = Mathf.Clamp(rawDuration, 0.2f, 2.2f);

        // 3. TWORZENIE CIÊCIA
        GameObject slashObj = Instantiate(meleeSlashPrefab, firePoint.position, Quaternion.identity);
        slashObj.transform.SetParent(this.transform);

        PlayerMeleeAttack meleeScript = slashObj.GetComponent<PlayerMeleeAttack>();
        meleeScript.Setup(weapon, finalDamage, swingDuration, angle);

        nextFireTime = Time.time + swingDuration;
    }

    // Pomocnicza funkcja licz¹ca k¹t kursora

    void Shoot(float baseAngle)
    {
        float arrowAngle = baseAngle - 45f; // Korekta graficzna dla strza³y
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