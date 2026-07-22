# Beosztás Varázsló – Android

A repó gyökerében lévő böngészős (HTML/CSS/JS) verzió natív Android portja, Kotlinban,
Room (SQLite) adatbázissal – ugyanazok a funkciók, telefonon/tableten használva.

## Megnyitás és futtatás

Ezt a projektet **Android Studio**-val kell megnyitni és lefordítani (a fordítás ebben a
fejlesztői környezetben nem volt elvégezhető, mert itt nincs telepített Android SDK és a
Google szerverei sincsenek engedélyezve a hálózati szabályzat miatt – ezért nincs
`gradlew`/`gradle-wrapper.jar` sem becsomagolva).

Teendők az első megnyitáskor:

1. Nyisd meg a `App/Android` mappát Android Studióban ("Open").
2. Amikor Android Studio jelzi, hogy hiányzik a Gradle wrapper, engedd, hogy automatikusan
   létrehozza/kijavítsa (vagy futtasd saját gépeden: `gradle wrapper --gradle-version 8.7`
   a `App/Android` mappában, ha van helyi Gradle-telepítésed).
3. Android Studio letölti a szükséges SDK komponenseket (compileSdk 34, minSdk 26) és
   szinkronizálja a függőségeket (AndroidX, Material Components, Room, Kotlin coroutines –
   ezek Maven Centralról/Google Maven-ről érkeznek).
4. Futtasd egy Android 8.0 (API 26) vagy újabb emulátoron/eszközön.

## Architektúra

- **data/** – Room entitások, DAO-k, `AppDatabase` (egyetlen titkosítatlan SQLite fájl,
  szerver nélkül – ez a program belső adatbázisa), `AppRepository` (magas szintű
  műveletek + JSON export/import biztonsági mentéshez).
- **logic/** – `HungarianHolidays` (Gauss-algoritmus a húsvéthez kötött ünnepekhez),
  `ScheduleCalculator` (kötelező/ledolgozott óra, szabadság-ellenőrzés, lefedettség),
  `PrintRenderer` (A4 fekvő PDF – több oldalas – és JPG generálás `PdfDocument`/`Canvas`
  segítségével, külső könyvtár nélkül).
- **ui/** – `MainActivity` (fülek: Beállítások, Csoportok, Dolgozók, Beosztás, Nyomtatás)
  és az egyes fragmentek. A napi beosztás rács saját nézetépítéssel készül (rögzített
  névoszlop + vízszintesen görgethető nap-oszlopok), Room-ból frissítve minden módosításkor.

## Funkciók (megegyeznek a böngészős verzióval)

- Munkacsoportok (munkaágak): pl. 12 órás váltásos ápolók, 8 órás takarítók, 4 órás
  részmunkaidősök, hétfő-péntek irodaiak – tetszőleges számban létrehozhatók.
- Havi kötelező óraszám/munkanap alaptábla (Január: 176 óra/22 nap stb.), szerkeszthető.
- Magyar munkaszüneti napok automatikusan, egyénileg bővíthető/kikapcsolható.
- Dolgozónként kötelező, évi max. kiadható szabadságnap-keret, amit a beosztás nem
  enged túllépni.
- Kézzel beírható "bejövő óra" (előző havi maradvány), automatikusan számolt egyenleg,
  egy gombbal átvihető a következő hónapra.
- Ápolóknál (vagy bármely csoportnál) beállítható, hányan dolgozzanak egy műszakban –
  a Beosztás fülön lefedettség-kijelzés (piros/zöld/narancs).
- Nyomtatás/Export: A4 fekvő, munkakör szerint ABC sorrendben, a hónap számával/nevével,
  a sorok végén a következő hónapra átvitt órákkal – PDF (több oldalas) és JPG export,
  megosztás más alkalmazásba (FileProvider).
- Adatbázis mentése/betöltése JSON fájlba (Storage Access Framework), az app saját
  Room adatbázisa mellett kényelmi biztonsági mentésként.

## Ismert korlátok

- Nem RecyclerView-alapú, virtualizált rács – néhány tucat dolgozóig gördülékeny, nagyon
  nagy létszámnál (több száz fő) lassulhat.
- A Nyomtatás fülön a hónapválasztás független a Beosztás fültől (mindkettő saját
  év/hónap választóval rendelkezik).
- Ezt a modult nem sikerült ebben a munkakörnyezetben lefordítani/tesztelni (nincs Android
  SDK, nincs internet a Google szerverekhez) – Android Studióban való megnyitás után
  esetlegesen felmerülő fordítási hibákat jelezd vissza.
