# Opis projektu – Akwitania

---

## 🇵🇱 Krótki opis

> Akwitania to dwuwymiarowe RPG budowane krok po kroku, w którym każda rana ma swój kolor. Trucizna, ogień, krwawienie i mróz zostawiają ślad nie tylko na pasku zdrowia, ale i na skórze przeciwnika — inny kolor poświaty, inna ikonka nad głową, inny rytm walki. Świat wciąż rośnie, system po systemie.

---

## 🇬🇧 Short description

> Akwitania is a 2D RPG built one system at a time, where every wound leaves its own mark. Poison, fire, bleeding and frost don't just drain a health bar — they flash a different color, glow a different icon overhead, change the rhythm of the fight. Still early, still growing, one mechanic at a time.

---

## 🇵🇱 Rozbudowany opis

**Akwitania** to solowy, wciąż aktywnie rozwijany projekt dwuwymiarowego RPG w Unity (C#), budowany systemowo — od walki, przez umiejętności, po zapis stanu gry.

**Walka, AI i obrażenia**
- Akcja czasu rzeczywistego z trzema stylami walki: broń biała (jedno- i dwuręczna z dynamicznym hitboxem dopasowującym się do grafiki oręża), łuk z zależną od niego amunicją oraz różdżki z własnymi zaklęciami (m.in. iskry ognia, rosnąca i blaknąca w locie chmura trucizny nekromanty).
- Zróżnicowane zachowania przeciwników: stwory walczące w zwarciu, łucznicy aktywnie flankujący i utrzymujący dystans, mechanika przywoływanych sojuszników oraz starcia z bossami wykorzystującymi ataki obszarowe i odskoki.
- Obrażenia liczone ze statystyk postaci (Siła / Zręczność / Inteligencja), bonusów z przedmiotów i mnożnika wybranej klasy.
- Wspólny interfejs `IDamageable` pozwala tym samym mechanikom działać identycznie na graczu, przeciwnikach i przywołanych stworzeniach.

**System statusów**
- Pięć efektów: Zatrucie, Podpalenie, Krwawienie, Spowolnienie i Ogłuszenie, z obrażeniami tykającymi w czasie i własnymi regułami (np. chmura trucizny nie zatruwa własnego rzucającego ani jego przywołań).
- Trafienie sygnalizowane kolorowym miganiem sprite'a — innym dla każdego typu obrażeń (zielony dla trucizny, pomarańczowy dla ognia, czerwony dla krwawienia, błękitny dla mrozu).
- Aktywne statusy wyświetlane jako ikonki nad głową postaci, dynamicznie centrowane w miarę pojawiania się i znikania kolejnych efektów.

**Interfejs w świecie gry**
- Najechanie kursorem na dowolne stworzenie pokazuje jego nazwę, poziom i pasek zdrowia — bez Canvasu, w całości w przestrzeni świata gry.
- Drzewko umiejętności z punktami do wydania, wsparciem dla interakcji Pan & Zoom, efektami aktywnymi i przywołaniami, cooldownami oraz paskiem szybkiego dostępu (hotbar) z przypisywaniem umiejętności pod klawisze.

**Świat, postęp i optymalizacja**
- Płynne, bezszwowe ładowanie lokacji w tle (additive scene loading), pozwalające na przechodzenie do wnętrz budynków czy jaskiń bez duplikowania menedżerów i obiektów gracza.
- Ekwipunek i przedmioty opierające się o system "Scriptable Objects" z losowanymi bonusami (procentowe wzmocnienia obrażeń, dodatkowe statystyki), co wpływa bezpośrednio na wartość rynkową przedmiotów.
- Złożony system łupów (oparty na wagach szans), z których przedmioty fizycznie wyskakują i spadają na ziemię po animowanych łukach.
- NPC z systemem sympatii reagującym na specyficzne prezenty, sklepami o losowanym asortymencie, dialogami opartymi na węzłach oraz questami śledzącymi zawartość plecaka.
- Pełny system zapisu i wczytywania (JSON) obejmujący postać, ekwipunek wraz z wylosowanymi statystykami, stan świata (otwarte skrzynie, zdarzenia), postawy NPC, questy, odblokowane umiejętności, przypisania hotbara oraz bezpieczne punkty odrodzenia z mechaniką kar po śmierci (utrata części złota).
- Muzyka z płynnym przenikaniem (crossfade) między motywami menu i playlistą świata, oraz zautomatyzowany odtwarzacz kroków analizujący prędkość fizyczną postaci gracza.

Projekt jest w aktywnym rozwoju — tworzony solo, jeden system na raz.

---

## 🇬🇧 Extended description

**Akwitania** is a solo, actively developed 2D RPG built in Unity (C#), grown system by system — from combat, through skills, to full save/load persistence.

**Combat, AI & damage**
- Real-time combat with three weapon styles: melee (one- and two-handed with dynamic hitboxes adjusting to the weapon's sprite), bows paired with their own ammo, and wands carrying pluggable spell effects (fire sparks, a necromancer's poison cloud that grows and fades as it flies).
- Diverse enemy AI: melee grunts, archers that actively flank and maintain distance, a summoned-ally mechanic, and multi-phase bosses utilizing shockwaves and dashes.
- Damage is derived from character stats (Strength / Dexterity / Intelligence), item bonuses, and a class-based multiplier.
- A shared `IDamageable` interface lets the same combat code affect the player, enemies, and summoned creatures identically.

**Status effect system**
- Five effects — Poison, Burn, Bleed, Slow, and Stun — each with damage-over-time ticks and their own rules (a poison cloud, for instance, never poisons its own caster or their summons).
- Every hit triggers a color-coded flash unique to its damage type (green for poison, orange for burn, red for bleed, blue for frost).
- Active statuses appear as icons hovering above a character, dynamically re-centering as effects are added or wear off.

**In-world interface**
- Hovering over any creature reveals its name, level, and health bar — rendered entirely in world space, no Canvas involved.
- A skill tree with spendable points, Pan & Zoom interaction support, active effects and summons, cooldowns, and a hotbar for binding skills to keys.

**World, progression & optimization**
- Seamless additive scene loading for smooth transitions between the overworld and interiors without duplicating player objects or core managers.
- Inventory and items powered by Scriptable Objects with randomized bonuses (damage percentages, extra stats), directly affecting the item's market value.
- A complex, weight-based loot system where dropped items physically arc and bounce into the world.
- NPCs featuring a relationship system that reacts to specific gifts, shops stocking randomized goods, node-based dialogue, and quests that track inventory objectives.
- A full JSON-based save/load system covering the character, inventory with its rolled item stats, world state (opened chests, flags), NPC affinities, quests, unlocked skills, hotbar assignments, and safe respawn points paired with a death penalty mechanic (gold loss).
- Music with crossfading between menu themes and world playlists, plus an automated footstep audio controller analyzing the player's physical velocity.

The project is under active development — built solo, one system at a time.
