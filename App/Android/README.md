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

## Kinézet – Google Material 3

Az alkalmazás megjelenése a Google Material 3 (Material You) dizájnrendszert követi: sík
(nem áttetsző) felületek, egyetlen mag-színből (mély indigó-lila) származtatott M3 tónusos
színszerepkörök (`colorPrimary`, `colorPrimaryContainer`, `colorSurfaceVariant` stb.),
szabványos Material 3 komponens-stílusok (`Theme.Material3.Light.NoActionBar`,
`Widget.Material3.CardView.Elevated`, `Widget.Material3.Button.*`) - lekerekített (8-12dp,
M3 "medium" shape), finom kontúrral és kis emelkedéssel rendelkező kártyák, tömör színű felső
sáv (`AppBarLayout` + `Toolbar`) és alatta dokkolt fülsáv (`TabLayout`) fehér alsó
jelölővonallal. A stílusdefiníciók a `res/values/styles.xml` (`Widget.Glass.*` nevek alatt,
de immár valódi M3 tartalommal - a történeti névre csak azért van szükség, mert minden
layout/dialog fájl ezekre hivatkozik) és a `res/values/themes.xml`, `res/values/colors.xml`
fájlokban találhatók.

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
- Hirtelen beteg szabadság (BSZ): egymást váltó csoportoknál automatikus helyettes-keresés
  és beosztás; irodai dolgozóknál egyszerű jelölés, átszervezés nélkül.
- Nyomtatás/Export: A4 fekvő, munkakör szerint ABC sorrendben, a hónap számával/nevével,
  a sorok végén a következő hónapra átvitt órákkal – PDF (több oldalas) és JPG export,
  megosztás más alkalmazásba (FileProvider).
- Adatbázis mentése/betöltése JSON fájlba (Storage Access Framework), az app saját
  Room adatbázisa mellett kényelmi biztonsági mentésként. Ez a JSON a böngészős és a
  Windows verzióval is közös, platformfüggetlen formátum ("beosztas-varazslo-v1") - egy
  itt exportált beosztás bármelyik másik verzióban (vagy másik Android-eszközön)
  importálható, és fordítva (lásd a gyökér README.md "Átjárhatóság" szakaszát).

## Ismert korlátok

- Nem RecyclerView-alapú, virtualizált rács – néhány tucat dolgozóig gördülékeny, nagyon
  nagy létszámnál (több száz fő) lassulhat.
- A Nyomtatás fülön a hónapválasztás független a Beosztás fültől (mindkettő saját
  év/hónap választóval rendelkezik).
- Ezt a modult ebben a munkakörnyezetben nem lehet teljeskörűen lefordítani (nincs Android
  SDK, és a Google Maven (`dl.google.com`) sincs engedélyezve a hálózati szabályzat miatt) -
  Android Studióban való megnyitás után esetlegesen felmerülő fordítási hibákat jelezd
  vissza. A build-eszközlánc (Gradle/Kotlin/KSP verziók) illesztését azonban valódi
  Gradle-lel, a Google Mavenen kívüli forrásokból (Maven Central, Gradle Plugin Portal)
  ellenőriztem.

### Fordítási hibák Android Studio alatt - hibaelhárítás

Ha `:app:kaptDebugKotlin` (vagy hasonló) taszk hibázik `"Provided Metadata instance has
version X.X.X, while maximum supported version is 2.0.0"` üzenettel: ez azt jelenti, hogy a
ténylegesen használt Kotlin fordító újabb, mint amit a `kapt` (a régi, karbantartás alatt
lévő Room annotációfeldolgozó) beépített metaadat-olvasója kezelni tud. A projekt emiatt már
`kapt` helyett `KSP`-t (Kotlin Symbol Processing) használ a Room-hoz - ha mégis felmerülne
hasonló verzióütközés, ellenőrizd, hogy a gyökér `build.gradle.kts`-ben megadott Kotlin
(`org.jetbrains.kotlin.android`) és a `com.google.devtools.ksp` plugin verziói össze
vannak-e hangolva (a KSP verziószáma mindig `<kotlin-verzió>-<ksp-verzió>` alakú, pl. a
Kotlin 2.2.0-hoz a KSP 2.2.0-2.0.2 tartozik).
