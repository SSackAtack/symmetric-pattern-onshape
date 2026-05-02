# Symmetric Pattern — Scenariusze testowe

## Przygotowanie

1. Utwórz nowy dokument w Onshape
2. W Part Studio utwórz prosty sześcian (np. 10mm × 10mm × 10mm)
3. Dodaj Feature Studio i wklej kod `src/SymmetricPattern.fs`
4. Upewnij się, że custom feature w toolbarze nazywa się **Symmetric Pattern**
5. Utwórz punkty lub wierzchołki o znanym rozstawie (np. 100mm) jako zakres kolumn

---

## Testy

### Test 1: Szyk liniowy — punkty
| Parametr | Wartość |
|---|---|
| Target | Sześcian |
| Typ zakresu | Punkty |
| Start/End | Wierzchołki oddalone o 100mm |
| Kolumny | 5 |
| Wiersze | 1 |

**Oczekiwany wynik:** 5 sześcianów, odstęp 25mm, wzdłuż linii Start→End

---

### Test 2: Szyk liniowy — inny zakres punktów
| Parametr | Wartość |
|---|---|
| Target | Sześcian |
| Typ zakresu | Punkty |
| Start/End | Inna para wierzchołków oddalona o 100mm |
| Kolumny | 5 |
| Wiersze | 1 |

**Oczekiwany wynik:** Identyczny jak Test 1

---

### Test 3: Siatka 2D
| Parametr | Wartość |
|---|---|
| Kolumny | 4 |
| Wiersze | 3 |
| Row end point | Punkt oddalony o 60mm od oryginału |
| Zakładka | Brak |

**Oczekiwany wynik:** 12 elementów w siatce 4×3, odstęp kolumn ~33mm, odstęp wierszy 30mm

---

### Test 4: Stagger 50%
| Parametr | Wartość |
|---|---|
| Kolumny | 4 |
| Wiersze | 2 |
| Row end point | Punkt oddalony o 30mm od oryginału |
| Zakładka | 50% |
| Tryb krawędzi | Pomiń elementy |

**Oczekiwany wynik:** Wiersz 2 przesunięty o ~16.5mm. Elementy wypadające poza 100mm nie powstają.

---

### Test 4a: Grid na Faces — rzędy zostają na tej samej płaszczyźnie
| Parametr | Wartość |
|---|---|
| Target | Face wyciętego otworu |
| Start/End | Dwa wierzchołki wyznaczające górną oś kolumn |
| Kolumny | 5 |
| Wiersze | 2 |
| Row end point | Wierzchołek na tej samej płaszczyźnie co Start/End, w kierunku drugiego rzędu |
| Zakładka | Brak |

**Oczekiwany wynik:** Drugi rząd powstaje na tej samej powierzchni co pierwszy. Kopie nie uciekają na boczną ani dolną ścianę modelu.

---

### Test 4b: Grid — parzyste i nieparzyste rzędy mają stałe marginesy
| Parametr | Wartość |
|---|---|
| Target | Face wyciętego otworu przy górnym lewym narożniku |
| Start/End | Górny lewy i górny prawy wierzchołek obszaru |
| Row end point | Dolny lewy wierzchołek obszaru |
| Kolumny | 4 |
| Wiersze | 2, 3 oraz 7 |
| Zakładka | Brak |

**Oczekiwany wynik:** Dla każdej liczby wierszy powstają wszystkie rzędy. Pierwszy otwór w każdym rzędzie ma taki sam margines poziomy jak w pierwszym rzędzie, a ostatni rząd ma taki sam margines pionowy od dolnej krawędzi jak pierwszy rząd od górnej krawędzi.

---

### Test 5: Stagger własny 25%
| Parametr | Wartość |
|---|---|
| Kolumny | 4 |
| Wiersze | 3 |
| Row end point | Punkt oddalony od oryginału w kierunku rzędów |
| Zakładka | Własny, 25% |
| Tryb krawędzi | Pomiń elementy |

**Oczekiwany wynik:** Nieparzyste wiersze przesunięte o 25% odstępu kolumn.

---

### Test 5a: Redukcja przesuniętych rzędów
| Parametr | Wartość |
|---|---|
| Kolumny | 5 |
| Wiersze | 4 |
| Zakładka | 50% |
| Reduce staggered rows | Włączone |
| Reduce by | 1 |
| Tryb krawędzi | Pomiń elementy |

**Oczekiwany wynik:** Rzędy bez przesunięcia mają 5 elementów. Rzędy przesunięte mają 4 elementy; pierwszy element nadal zaczyna się od przesunięcia `50%`, a brakujący element jest usunięty z prawego końca rzędu.

---

### Test 5b: Ręczne pomijanie wielu instancji
| Parametr | Wartość |
|---|---|
| Kolumny | 5 |
| Wiersze | 4 |
| Skip instances | Włączone |
| Skip count | 2 |
| Skip 1 row / column | 2 / 3 |
| Skip 2 row / column | 4 / 5 |

**Oczekiwany wynik:** Elementy w rzędzie 2 kolumnie 3 oraz rzędzie 4 kolumnie 5 nie powstają. Pozostałe elementy szyku pozostają bez zmian.

---

### Test 6: Edycja parametrów
1. Utwórz szyk 1×5
2. Dwuklik na feature w drzewie historii
3. Zmień kolumny na 10
4. Zatwierdź

**Oczekiwany wynik:** Szyk przelicza się poprawnie — 10 elementów, nowy odstęp.

---

### Test 7: Błędne dane — identyczne punkty
| Parametr | Wartość |
|---|---|
| Start | Wierzchołek A |
| End | Ten sam wierzchołek A |

**Oczekiwany wynik:** Komunikat błędu, brak crash'a

---

### Test 8: Minimalne wartości
| Parametr | Wartość |
|---|---|
| Kolumny | 2 |
| Wiersze | 1 |

**Oczekiwany wynik:** 2 elementy — oryginał na Start, kopia na End

---

## Checklisty

- [ ] Wszystkie testy przechodzą
- [ ] Feature pojawia się w drzewie historii
- [ ] Edycja parametrów działa poprawnie
- [ ] Brak crash'ów przy nieprawidłowych danych
- [ ] UI jest czytelny i logiczny
