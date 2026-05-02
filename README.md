# Symmetric Pattern — FeatureScript dla Onshape

> Narzędzie do tworzenia liniowych i siatkowych szyków (Pattern) z automatycznym, symetrycznym rozkładem elementów.

## 📁 Plik źródłowy

- `src/SymmetricPattern.fs` — FeatureScript 2945, źródło custom feature **Symmetric Pattern**.
- `docs/Symmetric_Pattern_Quick_Guide.pdf` — krótka instrukcja importowana do dokumentu Onshape przy publikacji.
- `docs/test-cases.md` — scenariusze ręcznych testów w Onshape.

## 🎯 Co robi narzędzie

Symmetric Pattern pozwala na rozmieszczenie kopii bryły lub operacji (np. otworu) wzdłuż linii zdefiniowanej przez dwa punkty (**Start** i **End**), z opcjonalną siatką (Grid) wyznaczaną dodatkowym punktem końcowym rzędów oraz przesunięciem rzędów (Stagger).

### Kluczowe funkcje

| Funkcja | Opis |
|---------|------|
| **Pattern Type** | Wybór celu: `Body` (cała bryła) lub `Faces` (wybrane ściany/operacje) |
| **Start / End Point** | Dwa punkty definiujące oś rozkładu |
| **Columns** | Łączna liczba elementów (oryginał + kopie) |
| **Grid Mode** | `Linear` (jeden rząd) lub `Grid` (wiele rzędów) |
| **Rows / Row End Point** | Liczba rzędów i punkt końcowy osi rzędów |
| **Stagger** | Przesunięcie co drugiego rzędu: `None`, `50%`, `Custom (%)` |
| **Reduce Staggered Rows** | Opcjonalnie zmniejsza liczbę elementów w przesuniętych rzędach, usuwając elementy z prawego końca |
| **Skip Instances** | Ręcznie pomija wybrane instancje przez numer rzędu i kolumny |
| **Edge Mode** | `Skip` — pomija elementy wychodzące poza granicę Start-End |

## 🧮 Logika symetrycznego rozkładu

Narzędzie mierzy pozycję oryginału (centroid z bounding box) i oblicza **symetryczny margines**:

```
d = odległość oryginału od Start point
symmetricSpan = totalLength - 2 × d
colSpacing = symmetricSpan / (Columns - 1)

Start   ├─ d ─┤                          ├─ d ─┤   End
              ●     ●     ●     ●     ●
           oryginał                  lustrzany
                                     margines
```

**Efekt**: Odległość pierwszego elementu od Start = odległość ostatniego od End.

W trybie `Grid` punkt `Row end point` wyznacza końcową granicę osi rzędów, analogicznie do `End point` dla kolumn. Początkiem osi rzędów jest `Start point`, a kierunek rzędów jest rzutowany prostopadle do osi kolumn. Skrypt mierzy margines oryginału od `Start point` po osi rzędów i odbija go przy `Row end point`, dzięki czemu pierwszy i ostatni rząd mają taki sam margines pionowy, a kolejne rzędy nie dryfują poziomo.

Jeśli włączysz `Stagger`, możesz dodatkowo użyć `Reduce staggered rows`. Przesunięte rzędy zachowują pierwszy element na pozycji wynikającej ze stagger offsetu, a wskazana liczba elementów jest usuwana z prawego końca rzędu.

Opcja `Skip instances` pozwala pominąć do 10 konkretnych instancji przez pary `row/column`. Numeracja jest użytkowa, od `1`, więc `row = 2`, `column = 3` oznacza drugi rząd i trzecią kolumnę.

## 🔧 Jak używać

1. Stwórz operację (np. `Extrude` z otworem) w Part Studio
2. Dodaj **Symmetric Pattern** z Feature Studio
3. Wybierz `Target faces` → zaznacz ścianę operacji
4. Wskaż `Start point` i `End point` (wierzchołki szkicu lub krawędzi)
5. Ustaw liczbę `Columns`
6. Opcjonalnie włącz `Grid` → ustaw `Rows`, wskaż `Row end point`, ustaw `Stagger`
7. Jeśli przesunięty rząd wchodzi w prawy margines, włącz `Reduce staggered rows` i ustaw `Reduce by`
8. Jeśli chcesz pominąć konkretne elementy, włącz `Skip instances`, ustaw `Skip count` i podaj pary `row/column`

## ⚙️ Wymagania techniczne

- **FeatureScript**: wersja 2945
- **Zależności**: `onshape/std/common.fs`
- **Kluczowe API**:
  - `evVertexPoint` — odczyt pozycji punktów
  - `evBox3d` — bounding box oryginału (do pomiaru centroidu)
  - `opPattern` — wykonanie szyku z transformacjami

## 🗺️ Roadmapa (planowane)

- [ ] **Weryfikacja symetrii** — przetestować `evBox3d` po wklejeniu do Onshape (ostatnia sesja zakończyła się na tym kroku)
- [ ] **Boolean Trim** — opcjonalne przycinanie elementów wychodzących poza obszar (`AlignmentMode.TRIM`)
- [ ] **Wsparcie operacji z Feature list** — wybieranie elementów z drzewa zamiast geometrii
- [ ] **Ikona SVG** — osadzenie niestandardowej ikony w pasku narzędzi Onshape

## 📋 Znane problemy / do sprawdzenia

> [!IMPORTANT]
> Po edycji pliku `src/SymmetricPattern.fs` lokalnie, **musisz ręcznie skopiować** cały kod i wkleić go do Feature Studio w Onshape (Ctrl+A → Ctrl+V). Plik lokalny i Feature Studio nie synchronizują się automatycznie.

- **Symetria marginesów**: Nowa logika z `evBox3d` + centroid została zaimplementowana, ale wymaga testów w Onshape (wersja w Feature Studio mogła nie zostać zaktualizowana na koniec sesji)
- **Columns = łączna liczba** elementów włącznie z oryginałem (np. `Columns = 5` → oryginał + 4 kopie)
- **Min. Columns = 2** (bo formuła dzieli przez `Columns - 1`)

## 📝 Historia zmian

### Sesja 2026-05-01
1. ✅ Stworzenie podstawowej struktury Feature z enumami i parametrami
2. ✅ Implementacja liniowego szyku opartego na punktach (Start/End)
3. ✅ Usunięcie obsługi krawędzi (Edge) — zbędna, Points wystarcza
4. ✅ Poprawka liczby kolumn (4 zamiast 5) — korekta pętli
5. ✅ Obsługa Grid z Rows, Row End Point i Stagger (Half/Custom)
6. ✅ Implementacja symetrycznego rozkładu: `L/(N+1)` → centroid-based `evBox3d`
7. ✅ Poprawka nazw pól: `patternBodies/patternFaces` → `targetBody/targetFaces`
8. ✅ Redukcja liczby elementów w przesuniętych rzędach (`Reduce staggered rows`)
9. ✅ Ręczne pomijanie instancji przez pary `row/column`
10. 🔲 Test symetrii w Onshape — **wymaga wklejenia najnowszego kodu**
