# Akwitania – notatki współpracy z Claude

Ten plik prowadzi Claude. Zbiera, co już zrobiliśmy, jak zbudowany jest projekt i co zostało do zrobienia.
**Claude aktualizuje go po każdej sesji pracy nad grą.** Opis gry dla innych ludzi jest w `README.md`.

Ostatnia aktualizacja: **2026-11-03** (spis zawartości projektu, fabuła)

---

## Projekt w skrócie
- **Akwitania** to Action-RPG 2D top-down w pixel arcie, Unity 6 + URP 2D, C#.
- Repozytorium: https://github.com/DanielsDelanyel/AkwitaniaRPG
- Folder lokalny: `P:\Unity Projects\Projekt_Akwitania`
- Skrypty: `Assets/Scripts` (płasko, ok. 100 plików), narzędzia edytora w `Assets/Scripts/Editor`
- Sceny: patrz „Spis zawartości projektu” niżej.
- Docelowa długość gry: **2–3 h** (zmienione z 0,5–2 h, 2026-11-03).
- Ostatnie commity: V0.5.3 (2026-10-11), V0.5.4 (2026-10-25)

## Architektura i konwencje (trzymać się ich)
- **Bootstrap + additive loading:** lokacje są ładowane i podmieniane przez `LocationManager`. Gracz i menedżery żyją w Bootstrap.
- **Menedżery jako singletony** z `DontDestroyOnLoad` (`LocationManager`, `SoundManager`, `MusicPlayer`, `QuestManager`…).
- **Dane w ScriptableObjectach:** `ItemData`, `Quest`, `LootTable`, `SkillData`, `DialogueNode`. Bazy są w `Resources` (`ItemDatabase`, `QuestDatabase`, `SkillDatabase`). Nowy przedmiot trzeba dodać do bazy: ⋮ → „Znajdz wszystkie przedmioty w projekcie”.
- **Stan świata:** `WorldState` przechowuje znaczniki (flags) i dane NPC, a `SaveManager` zapisuje go do JSON. Obiekty w scenie mają `UniqueId`.
- **Zadania:** gra tylko *zgłasza* zdarzenia (`QuestManager.ReportKill` itd.), a menedżer sam sprawdza, których zadań dotyczą.
- **Blokady ruchu:** `UILock` (licznik powodów). **Walka:** `CombatTracker` (czy ktoś goni gracza). **Komunikaty nad głową:** `PlayerNotice.Show("...")`.
- **Klawisze:** `GameSettings.GetKey("Akcja")` i zdarzenie `OnKeyBindingsChanged`. Nową akcję dodaje się w `DefaultKeyBindings`.
- **Obrażenia:** interfejs `IDamageable` (gracz i stworzenia). **Statusy:** `StatusEffectController` obok `Creature`/`PlayerStats`.
- **Komentarze w kodzie po polsku, BEZ polskich znaków** (ą → a itd.). Część starszych plików ma zepsute kodowanie (`�`), m.in. `NPCStats.cs` i `NPCInteraction.cs`. Przy edycji nie ruszać tych linii.
- Canvas: **Scale With Screen Size, 1920×1080**, Screen Space – Overlay (Bootstrap i MainMenu).

## Jak pracujemy
- Rozmowa po polsku, na luzie.
- Claude edytuje skrypty bezpośrednio w projekcie. **Nie ma Unity, więc nie kompiluje.** Każdą zmianę trzeba sprawdzić w edytorze.
- Sceny i prefaby otwarte w Unity Claude zmienia tylko instrukcjami do wyklikania w Inspektorze, bo jego zmiana mogłaby się pogryźć z niezapisanymi zmianami.
- Gdy prośba brzmi „oceń, nie rób za mnie”, Claude tylko ocenia i podpowiada kierunek, bez gotowych rozwiązań.
- **Zanim Claude zacznie przeglądać pliki, sprawdza „Spis zawartości projektu” niżej.** Po dodaniu nowej mechaniki, przedmiotu, postaci czy sceny dopisuje je do spisu.
- Plan pracy: krótka runda bugfixów → fabuła na papierze → jeden grywalny wycinek gry (np. wieś + las z entem + jaskinia z bossem) → rozbudowa świata. Bez nowych mechanik, chyba że wymaga ich konkretna treść.

---

## Spis zawartości projektu (stan na 2026-11-03)
Szybka mapa tego, co już jest w grze. Ścieżki względem `Assets/`.

### Sceny (`Scenes/`)
| Scena | Co zawiera |
|---|---|
| `Bootstrap` | gracz, kamera, Canvas z całym UI, menedżery (zostają przez całą grę) |
| `MainMenu` | menu główne, opcje (prefab `OptionsPanel`) |
| `World` | świat zewnętrzny: wieś, zamek z murami i bramą, rzeka, las. **~80 MB**, w trakcie podziału na regiony |
| `Main_Village`, `Forest`, `Castle_and_area` | nowe regiony (2026-11-03), jeszcze puste |
| `VillageHouse_Interior` | wnętrze domu we wsi |
| `MysteriousCave_interior` | prawie pusta (3 KB) |
| `SampleCave.unity` | leży w `Assets/` (nie w `Scenes/`), 0,5 MB – jaskinia z bossem? do sprawdzenia/uporządkowania |
| `SampleScene` | pozostałość, do usunięcia |

### Skrypty (`Scripts/`, 101 plików + 3 w `Editor/`)
- **Gracz:** `TopDownMovement` (ruch, dash), `PlayerStats` (statystyki, poziomy/EXP, klasy), `PlayerCombat`, `PlayerMeleeAttack` (hitbox broni), `PlayerSkills`, `PlayerEquipment`, `PlayerElevation` (mury), `PlayerAudio`, `PlayerHUD`, `PlayerNotice` (napisy nad głową), `HitFlashController` + shader `SpriteHitFlash`, `DamagePopup`, `DeathFadeEffect`
- **Walka i efekty:** `IDamageable`, `StatusEffectController`, `OnHitStatusData`, `CombatTracker`, `Projectile`, `WandSpell`, `FireSparkProjectile`, `PoisonCloudSpell`, `Shockwave`, `WindVortexEffect`, `ActiveSkillEffect`
- **Stworzenia i wrogowie:** `Creature` (HP, nastawienie Neutral/Aggressive…, `onProvoked`), `CreatureAI` (dzik, szarża), `CreatureWander` (owca), `BanditAI` (wręcz, prowokacja/wybaczanie), `ArcherAI` (łucznik), `BossController` (Aztekowiec), `EnemyMeleeAttack`, `EnemyProjectile`, `SummonedCreature` + `SummonedCreatureAI` (przyzwane szkielety), `SpawnZone`, `CreatureLoot` + `LootTable`, `CreatureHoverInfo`, `CreatureStatusIndicator`
- **NPC i dialogi:** `NPCInteraction` (rozmowa, warunki startu dialogu, kara za prowokację), `NPCStats` (sympatia), `DialogueManager`, `DialogueNode` (opcje z flagami), `GiftSlot` (prezenty)
- **Przedmioty, ekwipunek, handel:** `ItemData`, `ItemDatabase`, `ItemFactory`, `ItemPickup`, `RarityUtils`, `InventoryUI`, `InventorySlot`, `InventoryTooltip`, `ContextMenuUI`, `SplitAmountUI`, `ShopManager`, `ShopItemSlot`, `TradeCenterSlot`, `TreasureChest`, `ChestRayEffect`, `LootArcMotion`
- **Zadania:** `Quest`, `QuestDatabase`, `QuestManager`, `QuestProgress`, `QuestLogUI`, `QuestLogEntry`
- **Umiejętności:** `SkillData`, `SkillDatabase`, `SkillTreeUI`, `SkillNodeUI`, `SkillTreeArrow`, `PanZoomContent`, `PanZoomInputArea`, `SkillBarUI`, `SkillBarSlot`, `SkillHUD`, `SkillHUDSlot`
- **Świat, lokacje, zapis:** `LocationManager`, `RegionBorder`, `LocationEntrance`, `LocationSpawnPoint`, `RespawnPoint`, `ScreenFader`, `WorldState`, `UniqueId`, `SaveManager`, `SaveData`, `ElevationZone`, `TunnelOverlay`, `FadeWhenPlayerBehind`
- **Menu i ustawienia:** `MainMenuUI`, `PauseMenuUI`, `DeathScreenUI`, `OptionsMenu`, `OptionsTabsUI`, `GameSettings`, `KeyRebindButton`, `KeyRebindPopup`, `StatsUI`, `FeedbackPanel`, `UIEscapeHandler`, `UILock`
- **Dźwięk:** `SoundManager`, `MusicPlayer`
- **Edytor:** `PixelArtImporterTool`, `PlayFromMainMenu`, `RandomizableStatDrawer`

### Mechaniki w liczbach
- **Statystyki:** Siła, Witalność, Inteligencja, Zręczność, Charyzma. Obrażenia/obrona fizyczne i magiczne, krytyki, mana.
- **Klasy (11, liczone z % podziału statystyk, w tej kolejności):** Łowca ZR>50%, Mag INT>40%, Barbarzyńca SIŁ>45%, Obrońca WIT>45%, Bard CHAR>40%, Skrytobójca SIŁ+ZR, Paladyn SIŁ+INT, Nekromanta INT+WIT, Iluzjonista INT+CHAR, Mnich INT+ZR (hybrydy: suma >65% i różnica ≤15 pkt), Włóczęga (reszta).
- **Statusy:** Poison, Burn, Bleed, Slow, Stun.
- **Typy przedmiotów:** General, Consumable, Ring, Necklace, Weapon1h, Weapon2h, Bow, Second_Hand, Helmet, Armor, Legs, Boots, Ammo, Gift, Wand1h, Wand2h. **Rzadkość:** Common, Rare, Epic, Legendary (losowane statystyki).
- **Cele zadań:** TalkToNpc, KillCreature, KillNamed, CollectItem, DeliverItem, ReachLocation, OpenChest, Custom. **Kategorie:** główne, poboczne, zlecenia.
- **Klawisze domyślne:** Spacja dash, E interakcja, I ekwipunek, V statystyki, L dziennik, G drzewko, H pasek umiejętności, 1–8 hotbar.

### Treść
- **Przedmioty (`Items/`, 21):** mikstury leczenia (mała, zwykła, duża, wielka), piwo, truskawka, ciastko, truskawka-prezent; pierścienie (z rubinem, z szafirami); Fine Coat, Fine Hood, Simple Hood, Poor Duckin foottrooper's padded armor, Wizard's hat, Worn Wizard's hat; tarcze Viking's shield i Viking's chief shield; Ifrit's heart, Life essence, Scroll of death.
- **Broń (`Equipment/`, 16):** miecze Iron, Steel, Fencer's, Templar's, Exorcist's, Richard's, Syrius's, GodsSword; kije QuaterStaff, Wukong's Staff; łuk **HunterBow**; strzały Arrow i **MysticArrow** (pasują do „zestawu mistycznego łowcy” z fabuły); różdżki Novice's Fire Wand, Adept's Strange Wand; Ents_hand (łup z enta).
- **Umiejętności (`Resources/`, 2):** Powstanie Szkieletów (przyzwanie, 35% many), Wietrzny Młyniec (wir, 20 many). Czary różdżek: iskry ognia, trujący obłok.
- **Postacie i stworzenia (`Beings/`):** Aztekowiec (boss), BanditMeele1, BanditRange2 (łucznik), Boar, Sheep, Elder_Ent, Lassi, ShopKeeper (Shulu), Skeleton_0 (przyzwanie), sprite manekina treningowego (bez prefabu). Stare duplikaty `Prefabs/Boar` i `Prefabs/Sheep`.
- **Dialogi (`Dialogues/`):** Lassi 14 węzłów (start, znajoma, przedstawienie, flirt, pytania, prezenty, zadanie testowe), Shulu 2, Ent 1.
- **Zadania:** 1 testowe („przynieś truskawki” dla Lassi).
- **Tilesety (`Tiles/`):** placeholder 32 px, mury 3/4 + nakładka, kafle kolizji murów.

---

## Dziennik zmian

### Do 2026-09-20 (wcześniejsze sesje)
Handel i sklepy, bonusy klas, dash, skrzynie z łupem po łuku, lokacje (additive), tooltip ekwipunku, losowane statystyki przedmiotów, boss Aztekowiec, uniwersalny łup, walka wręcz z hitboxem broni, menu kontekstowe, `UIEscapeHandler`, pauza, panel feedbacku, menu główne, ekran śmierci i respawn, zapis stanu świata, AI bandyty i łucznika, strefy spawnu, `SoundManager`, bazy przedmiotów i zadań, system zadań, opcje ze zmianą klawiszy, `CombatTracker` i blokada wejść podczas walki.

### 2026-10-11
- **Dzik (`CreatureAI`):** zasięg agro, gubienie agro z uspokojeniem (`deaggroRange`, `loseAggroTime`), zgłaszanie się do `CombatTracker`. Z natury agresywny dzik reaguje dopiero w `aggroRange`.
- **Ent (Elder_Ent):** neutralny NPC-stworzenie. Po uderzeniu wpada w szał, traci 15 sympatii, nie da się z nim rozmawiać i blokuje drzwi. Po zgubieniu agro wybacza.
  - `BanditAI`: sekcja „Prowokacja” (`requireProvocation`, `forgiveAfterLosingAggro`, `loseAggroTime`)
  - `Creature`: zdarzenie `onProvoked`
  - `NPCInteraction`: `affinityPenaltyOnProvoke`
- **`NPCStats`:** sympatia od razu trafia do `WorldState`. Wcześniej ginęła po zmianie lokacji, jeśli gra nie została zapisana.
- **Prefab szkieleta** (`Skeleton_0`) przepięty na aktualne skrypty `SummonedCreature` i `SummonedCreatureAI`, które miały zerwane odwołania.

### 2026-10-12
- **Pamięć w dialogach:** opcje mają pola `setFlag`, `hideIfFlag` i `showOnlyIfFlag` (znaczniki w `WorldState`). `NPCInteraction` ma `startingNodeIfFlag` i `startingNodeFlag`.
- **Lassi:** przedstawienie się jest jednorazowe (znacznik `Lassi_przedstawiony`). Koniec z farmieniem +5 sympatii.

### 2026-10-24 – 2026-10-26
- Canvas Scaler przestawiony na Scale With Screen Size (zrobił użytkownik).
- **`InventoryTooltip`:** okno nie wychodzi poza ekran (`KeepOnScreen`, pole `screenMargin`). To samo dotyczy przypiętych „Szczegółów”.
- **Drzewko umiejętności:** przeciąganie i zoom działają na pustym tle (nowy `PanZoomInputArea`, doklejany automatycznie przez `PanZoomContent`).
- **Opcje → Controls:** lista przebudowana na wiersze `Row_...` (zrobił użytkownik). `KeyRebindButton` sam odświeża napis po zmianie lub resecie. Okno opcji jest prefabem (`Prefabs/OptionsPanel .prefab`) używanym w Bootstrap i MainMenu.
- **`SkillBarUI`:** po zmianie klawisza hotbara odświeża też pasek na dole ekranu (HUD pokazywał stary klawisz).
- **Nowy `FadeWhenPlayerBehind`:** obiekt robi się półprzezroczysty, gdy gracz za nim stoi. Do dodania na prefabie `Trees_shadow_source_0`.

### 2026-10-28
- Powstał ten plik (`CLAUDE.md`).
- **`DialogueManager`:** gdy widocznych opcji jest więcej niż przycisków w scenie, brakujące przyciski są dorabiane w trakcie gry (`EnsureButtonCount`). W scenie było tylko 5 przycisków (`OptionButton_1..5`), więc 6. opcja Lassi („Odchodzę…”) znikała.
- Lassi: użytkownik dodał jednorazowy flirt (znacznik `Lassi_flirt`).

### 2026-10-29
- **Placeholder tilesetu:** `Assets/Tiles/tileset_32_placeholder.png`, 512×256, 16×8 kafli po 32 px, przezroczyste tło. Przerobiony z grafiki z Gemini, tymczasowy, bo docelowy tileset robi grafik. Import: Sprite Mode Multiple, PPU 32, Filter Point, Compression None, Slice Grid By Cell Size 32×32.
- **Korony murów (chodnik z blankami):** `Assets/Tiles/wall_tops_32.png`, 256×128, 8×4 kafli po 32 px. Zbudowane z faktur placeholdera: zwieńczenie blanków z ramki muru, chodnik z bruku (wersja 2, pierwsza była rysowana od zera i nie pasowała stylem). Zestaw autotile: krawędzie, rogi, narożniki wewnętrzne, wąski chodnik (proste, zakręty, T, skrzyżowanie, końce) i warianty podłogi.
- **Mury 3/4 (wersja docelowa placeholdera):** `Assets/Tiles/wall_3q_32.png`, 36 kafli 32×32. Zrenderowane jako bryły w widoku 3/4 z faktur placeholdera: tylne blanki, chodnik z bruku, przednie blanki i ściana z cegieł wysoka na 1 kafel. Ściąga ułożenia jest w `sciaga_ukladania.png` (wysłana w czacie). `wall_tops_32.png` (płaski rzut z góry) jest nieaktualny i do usunięcia. Nakładka `wall_3q_front_32.png` (te same numery kafli, same przednie blanki) jest na Tilemapę `Mury_przod` nad graczem. Kafle z blankami mają numery 3, 4, 5, 10, 11, 17, 18, 19, 20, 22, 31–35.
- **Wchodzenie na mury (2026-10-30):** nowe `PlayerElevation` (na graczu, przełącza warstwę fizyki `Player`/`PlayerGora`, sam koryguje poziom na podstawie obrysu muru) i `ElevationZone` (trigger na górze schodów). Kolizje murów na 2 niewidocznych tilemapach: `MuryDol` (obrys muru, blokuje z ziemi) i `MuryGora` (krawędzie chodnika). Composite Collider z Geometry Type = Polygons. Kafle pomocnicze `Assets/Tiles/kolizje_gora_32.png` (pasy 6 px po stronach z blankami, 16 kombinacji N/E/S/W i pełny) służą do malowania `Kolizje_MuryGora`. `Kolizje_MuryDol` najprościej zrobić z kopii Tilemapy `castle` z wyłączonym rendererem. Brama: `TunnelOverlay` na Tilemapie `Tunel_wierzch` (kopia kafli muru nad przejściem, warstwa in front) chowa się, gdy gracz jest na murach. Przejście ma trigger na MuryDol (żeby gracz na murze nad bramą nie „spadał”) i boczne ścianki tunelu. Na mur wchodzi się TYLKO strefą schodów. Kolizje murów ostatecznie z Box Colliderów, a MuryDol z Geometry Type = Polygons. Do zrobienia przez użytkownika w Unity: warstwy, macierz kolizji, tilemapy kolizji, schody.
- **Brama działa (2026-10-30):** słupki `Gate` na warstwie MuryDol; trigger `Tunel_obrys` powiększony na cały wycięty fragment muru (ściana + chodnik + tylne blanki), bo przy 1 kaflu gracz przy przedniej krawędzi „spadał” do tunelu.
- **Mosty (plan):** pomost z kafli na osobnej Tilemapie nad `Water`; woda blokuje przez niewidoczną Tilemapę kolizji, w której pod mostem kafli nie ma; barierki jako cienkie Box Collidery. Prefab tylko dla mostów z logiką (zwodzony, zniszczalny) albo gdy pod mostem da się przejść (wtedy system z murów).
- **Podział świata na regiony (2026-11-03):** nowe sceny `Main_Village`, `Forest`, `Castle_and_area` (puste). Nowy skrypt `RegionBorder` (trigger na krawędzi regionu, przejście bez klawisza E, blokada w walce). Spawny przez istniejące `LocationSpawnPoint` (kilka ID na scenę). `World.unity` urosła do ~80 MB.
- Zadania i milestone'y są w GitHub Issues repo `AkwitaniaRPG` (#1–#53, M1–M8). Lista grafik w dokumencie „Grafiki do Akwitanii”.
- Plan mapy świata: Azgaar (planowanie), potem Wonderdraft/Inkarnate (grafika), potem Krita (postarzenie w stylu mapy Królestw Północy).

---

## Otwarte sprawy
- [ ] Sprawdzić w Unity, czy `FadeWhenPlayerBehind` jest dodany do prefabu drzewa i działa.
- [ ] MainMenu → `MainMenuUI` → **Options Back Button** jest puste. Podpiąć `BackOptionsButton`.
- [ ] Usunąć zbędne `Global Volume`: 2 w prefabie opcji (pod `FullScreenText (TMP)` i `FullScreenToggle`) i 1 w Bootstrap pod Canvasem.
- [ ] `OptionsMenu` → lista Key Rebind Buttons ma stare wpisy. Ustawić rozmiar 0, bo już niepotrzebna.
- [ ] Lassi: opcjonalny węzeł powitania „znajomy” (`Lassi_Start_Znajomy`) i przepięcie powrotów z `ask_for_place`, `ask_for_someone` i `flirt`.
- [ ] Dialog Lassi: po przedstawieniu się w menu `Lassi_Introduced` brakuje opcji zadania, a zadanie testowe startuje dwa razy (na opcji w Start i w Intro).
- [ ] Zabity ent wraca po ponownym wejściu do lokacji, bo jego śmierć nie jest zapisywana.
- [ ] Ramki UI jako 9-slice (Sprite Editor → Border, Image Type: Sliced).
- [ ] Okno dialogu: `OptionsArea` (wysokość 337, odstęp 50) mieści ok. 5 opcji. Przy większej liczbie przyciski wyjdą poza ramkę, więc zmniejszyć odstęp, powiększyć obszar albo dodać stałe przyciski 6–8.

## Pomysły na później (po pierwszym wycinku gry)
- Poziomy trudności. Na łatwym np. 1,5 s przerwy dzika po szarży; ta przerwa jest teraz w kodzie, ale nie działa.
- Warunki dialogów zależne od sympatii (np. ent wypomina cios).
- Pixel-perfect skalowanie UI (Reference Resolution 640×360).
- Playtest ze znajomymi przez `FeedbackPanel`.

## Muzyka
Plan nauki teorii muzyki i kompozycji z Claude w MuseScore Studio (MuseHub zainstalowany). Odłożone na później, szczegóły są w pamięci Claude.

## Fabuła (stan konceptu)
Pełny scenariusz jest w Notion: **Akwitania → Lore** (wersja 3, 2026-11-03), schemat blokowy jako artefakt „Scenariusz Akwitanii”.
- Bohater **Wojciech** z wioski. Prolog: trening milicji u **Bogdana** (sołtys, instruktor), Rozelia (sympatia), atak rabusiów, Bogdan zatruty.
- Akt 1: antidotum od druidki **Lassi** za obietnicę powrotu, kruk śledzi gracza; wyręb drwali (**Boromir**): atak / układ (boss **Khaz'ka**, czyli Aztekowiec) / oba. Rabusie przechodzą na służbę barona.
- Akt 2: wieś (ciasteczka dla Rozelii, stolarz Tomasz i obóz bandytów), prośba Lassi, zwiadowcy **Wastera**, przyjazd barona z kordonem, Wojciech jedzie do zamku.
- Akt 3 (zamek) do napisania.
- Szacowany czas: prolog 15–20 min, akt 1 25–40 min, akt 2 30–45 min.
