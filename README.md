# Opis projektu – Akwitania

---

## 🇵🇱 Krótki opis[Akwitania_opis_projektu.md](https://github.com/user-attachments/files/31809908/Akwitania_opis_projektu.md)


> Akwitania to dwuwymiarowe RPG budowane krok po kroku, w którym każda rana ma swój kolor. Trucizna, ogień, krwawienie i mróz zostawiają ślad nie tylko na pasku zdrowia, ale i na skórze przeciwnika — inny kolor poświaty, inna ikonka nad głową, inny rytm walki. Świat wciąż rośnie, system po systemie.

---

## 🇬🇧 Short description

> Akwitania is a 2D RPG built one system at a time, where every wound leaves its own mark. Poison, fire, bleeding and frost don't just drain a health bar — they flash a different color, glow a different icon overhead, change the rhythm of the fight. Still early, still growing, one mechanic at a time.

---

## 🇵🇱 Rozbudowany opis

**Akwitania** to solowy, wciąż aktywnie rozwijany projekt dwuwymiarowego RPG w Unity (C#), budowany systemowo — od walki, przez umiejętności, po zapis stanu gry.

**Walka i obrażenia**
- Akcja czasu rzeczywistego z trzema stylami walki: broń biała (jedno- i dwuręczna), łuk z zależną od niego amunicją oraz różdżki z własnymi zaklęciami (m.in. iskry ognia, rosnąca i blaknąca w locie chmura trucizny nekromanty).
- Obrażenia liczone ze statystyk postaci (Siła / Zręczność / Inteligencja), bonusów z przedmiotów i mnożnika wybranej klasy.
- Wspólny interfejs `IDamageable` pozwala tym samym mechanikom działać identycznie na graczu, przeciwnikach i przywołanych stworzeniach.

**System statusów**
- Pięć efektów: Zatrucie, Podpalenie, Krwawienie, Spowolnienie i Ogłuszenie, z obrażeniami tykającymi w czasie i własnymi regułami (np. chmura trucizny nie zatruwa własnego rzucającego ani jego przywołań).
- Trafienie sygnalizowane kolorowym miganiem sprite'a — innym dla każdego typu obrażeń (zielony dla trucizny, pomarańczowy dla ognia, czerwony dla krwawienia, błękitny dla mrozu).
- Aktywne statusy wyświetlane jako ikonki nad głową postaci, dynamicznie centrowane w miarę pojawiania się i znikania kolejnych efektów.

**Interfejs w świecie gry**
- Najechanie kursorem na dowolne stworzenie pokazuje jego nazwę, poziom i pasek zdrowia — bez Canvasu, w całości w przestrzeni świata gry.
- Drzewko umiejętności z punktami do wydania, efektami aktywnymi i przywołaniami, cooldownami i paskiem szybkiego dostępu (hotbar) z przypisywaniem umiejętności pod klawisze oraz paginowaną paletą odblokowanych zdolności.

**Świat i postęp**
- Przeciwnicy ze zróżnicowanym AI (zwykłe stwory, łucznicy, bandyci) oraz mechanika przywoływanych sojuszników.
- Ekwipunek i przedmioty z losowanymi bonusami (procentowe wzmocnienia obrażeń, dodatkowe statystyki), NPC z systemem relacji i sklepami o losowanym towarze, dialogi oraz questy z celami śledzącymi zawartość ekwipunku.
- Pełny system zapisu i wczytywania (JSON) obejmujący postać, ekwipunek wraz z wylosowanymi statystykami, stan świata (otwarte skrzynie, zdarzenia), NPC, questy, odblokowane umiejętności i przypisania hotbara — oraz punkty odrodzenia.
- Muzyka z płynnym przenikaniem (crossfade) między motywami menu i rozgrywki, z zapleczem pod utwory przypisane do konkretnych obszarów, oraz efekty dźwiękowe ze wspólną głośnością.

Projekt jest w aktywnym rozwoju — tworzony solo, jeden system na raz.

---

## 🇬🇧 Extended description

**Akwitania** is a solo, actively developed 2D RPG built in Unity (C#), grown system by system — from combat, through skills, to full save/load persistence.

**Combat & damage**
- Real-time combat with three weapon styles: melee (one- and two-handed), bows paired with their own ammo, and wands carrying pluggable spell effects (fire sparks, a necromancer's poison cloud that grows and fades as it flies).
- Damage is derived from character stats (Strength / Dexterity / Intelligence), item bonuses, and a class-based multiplier.
- A shared `IDamageable` interface lets the same combat code affect the player, enemies, and summoned creatures identically.

**Status effect system**
- Five effects — Poison, Burn, Bleed, Slow, and Stun — each with damage-over-time ticks and their own rules (a poison cloud, for instance, never poisons its own caster or their summons).
- Every hit triggers a color-coded flash unique to its damage type (green for poison, orange for burn, red for bleed, blue for frost).
- Active statuses appear as icons hovering above a character, dynamically re-centering as effects are added or wear off.

**In-world interface**
- Hovering over any creature reveals its name, level, and health bar — rendered entirely in world space, no Canvas involved.
- A skill tree with spendable points, active effects and summons, cooldowns, and a hotbar for binding skills to keys, backed by a paginated palette of everything currently unlocked.

**World & progression**
- Enemies with distinct AI behaviors (generic creatures, archers, melee bandits), plus a summoned-ally mechanic.
- Inventory and items with randomized bonuses (damage percentages, extra stats), NPCs with a relationship system and shops stocking randomized goods, dialogue, and quests that track inventory objectives.
- A full JSON-based save/load system covering the character, inventory with its rolled item stats, world state (opened chests, flags), NPCs, quests, unlocked skills, hotbar assignments, and respawn points.
- Music with crossfading between menu and gameplay themes (with groundwork for area-specific playlists), plus sound effects sharing a global volume control.

The project is under active development — built solo, one system at a time.
