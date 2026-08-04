# Beosztás Varázsló – Windows

A repó gyökerében lévő böngészős (HTML/CSS/JS) verzió natív Windows portja: .NET 8 / WPF
asztali alkalmazás, klasszikus Microsoft "Metro" (Modern UI, Windows 8/Windows Phone
jellegű) megjelenéssel.

## Kinézet – Microsoft "Metro" (Modern UI)

Az alkalmazás a Windows 8 / Windows Phone korszakának "Metro" dizájnnyelvét idézi: sík,
tömör (nem áttetsző, árnyék nélküli) felületek, éles - nem lekerekített - sarkok, vivid
kobalt-kék elsődleges szín, világos ("Segoe UI Light") és félkövér tipográfia keveréke a
címekben, és a kijelölt navigációs elemet nem finom kiemelés, hanem egy tömör, tömbszerű
színfolt jelzi (a Fluent/Mica-alapú, lekerekített, üvegesített korábbi megjelenés helyett).
A stílusdefiníciók a `Themes/Styles.xaml`, `Themes/Light.xaml` és `Themes/Dark.xaml`
fájlokban találhatók.

## Megnyitás és futtatás

Ezt a projektet **Visual Studio 2022** (17.8+, ".NET asztali fejlesztés" workload) vagy a
`dotnet` parancssori eszköz segítségével kell megnyitni/lefordítani (a fordítás ebben a
fejlesztői környezetben nem volt elvégezhető, mert itt nincs telepített .NET SDK, és a
Microsoft letöltési szerverei sincsenek engedélyezve a hálózati szabályzat miatt - a NuGet
csomagregisztrum ugyan elérhető volt, de az maga a projekt **egyetlen külső NuGet-csomagot
sem használ**, kizárólag a .NET/WPF beépített API-jaira épül, hogy a build a lehető
legkevesebb hibaforrást tartalmazza).

Teendők az első megnyitáskor:

1. Nyisd meg a `App/Windows/BeosztasVarazslo.sln` fájlt Visual Studióban, vagy futtasd a
   `App/Windows` mappában: `dotnet run --project BeosztasVarazslo`
2. A `gradlew`-hoz hasonló "wrapper" fájl itt nem szükséges - a `.sln`/`.csproj` közvetlenül
   használható, amint telepítve van a .NET 8 SDK ("Windows Desktop" komponenssel).
3. Futtatás Windows 10 (1809+) vagy Windows 11 rendszeren ajánlott.

## Architektúra

- **Models/** – egyszerű C# osztályok (munkacsoport, műszaktípus, dolgozó, havi óraszám,
  teljes `AppDatabase`), amelyek egy az egyben JSON-ba szerializálódnak.
- **Services/**
  - `AppDatabaseService` – betöltés/mentés egyetlen titkosítatlan JSON fájlba
    (`%AppData%\BeosztasVarazslo\adatbazis.json`) - ez a program belső, szerver nélküli
    adatbázisa; alapértelmezett munkacsoportok/óraszám-tábla feltöltése első indításkor.
  - `AppRepository` – magas szintű műveletek a memóriában tartott adatbázis fölött, minden
    módosítás után azonnali mentéssel.
  - `HungarianHolidays` – Gauss-algoritmus a húsvéthez kötött magyar mozgó ünnepekhez.
  - `ScheduleCalculator` – kötelező/ledolgozott óra, szabadság-ellenőrzés, műszak-lefedettség.
  - `PrintDocumentBuilder` / `PrintExportService` / `MinimalPdfWriter` – nyomtatható táblázat
    összeállítása, WPF-fel (RenderTargetBitmap + JpegBitmapEncoder) JPEG-be rasztereleve,
    majd a JPEG "oldalképek" egy saját kezűleg írt, minimális PDF-be csomagolása
    (kép-XObjectek DCTDecode szűrővel - így a magyar ékezetes szövegek a valós rendszer-
    betűkészlettel, helyesen jelennek meg, és nincs szükség PDF betűkódolási trükkökre).
- **Views/** – 5 nézet (UserControl) a bal oldali navigációs sávból elérve: Beállítások,
  Munkacsoportok, Dolgozók, Beosztás, Nyomtatás/Export - mindegyik kódból (code-behind)
  építi fel/frissíti a tartalmát, hasonlóan az Android verzióhoz.
- **Themes/** – világos/sötét, klasszikus Microsoft "Metro" (Modern UI) stílusú színpaletta
  (a rendszerbeállítás alapján automatikusan kiválasztva induláskor) és egyedi
  vezérlő-stílusok (éles sarkú, sík "tile" gombok/kártyák, tömör navigációs sáv).
- **Helpers/ThemeDetector.cs** – a rendszer sötét/világos témabeállítását olvassa ki
  induláskor, hogy a megfelelő `Themes/Light.xaml` / `Themes/Dark.xaml` töltődjön be.

## Funkciók (megegyeznek a böngészős/Android verzióval)

- Munkacsoportok (munkaágak): pl. 12 órás váltásos ápolók, 8 órás takarítók, 4 órás
  részmunkaidősök, hétfő-péntek irodaiak - tetszőleges számban létrehozhatók.
- Havi kötelező óraszám/munkanap alaptábla (Január: 176 óra/22 nap stb.), szerkeszthető.
- Magyar munkaszüneti napok automatikusan, egyénileg bővíthető/kikapcsolható.
- Dolgozónként kötelező, évi max. kiadható szabadságnap-keret, amit a beosztás nem enged
  túllépni.
- Kézzel beírható "bejövő óra" (előző havi maradvány), automatikusan számolt egyenleg, egy
  gombbal átvihető a következő hónapra.
- Egy műszakban szükséges létszám beállítása (pl. ápolóknál), lefedettség-kijelzés
  piros/sárga/zöld színezéssel.
- Hirtelen beteg szabadság (BSZ): egymást váltó csoportoknál automatikus helyettes-keresés
  és beosztás; irodai dolgozóknál egyszerű jelölés, átszervezés nélkül.
- Nyomtatás/Export: A4 fekvő, munkakör szerint ABC sorrendben, a hónap számával/nevével, a
  sorok végén a következő hónapra átvitt órákkal - PDF (több oldalas) és JPG export.
- Adatbázis mentése/betöltése JSON fájlba a bal oldali sáv alján, az alkalmazás saját
  helyi adatbázisa mellett kényelmi biztonsági mentésként/hordozhatóságként. Ez a JSON a
  böngészős és az Android verzióval is közös, platformfüggetlen formátum
  ("beosztas-varazslo-v1") - egy itt exportált beosztás bármelyik másik verzióban
  importálható, és fordítva (lásd a gyökér README.md "Átjárhatóság" szakaszát).

## Fordítási hibák Visual Studio alatt - hibaelhárítás

Ebben a fejlesztői környezetben időközben sikerült telepíteni a valódi .NET 8 SDK-t, és
azzal leellenőrizni a kódot: a WPF-független üzleti logika réteg (`Models`, `AppDatabaseService`,
`AppRepository`, `HungarianHolidays`, `ScheduleCalculator`, `MinimalPdfWriter`, `PrintModels`,
`PrintDocumentBuilder`) egy külön, sima `net8.0` class libraryben **hiba és figyelmeztetés
nélkül lefordul**. A WPF-specifikus rész (`net8.0-windows`, `UseWPF=true`) tényleges
fordítása Linuxon nem lehetséges - a `Microsoft.NET.Sdk.WindowsDesktop` build-eszközlánc
(XAML→BAML fordító) kizárólag Windows-on érhető el -, ezért ezt a részt soronkénti, kézi
átvizsgálással ellenőriztük: minden XAML fájl jólformázott, minden `x:Name` hivatkozás
megfelel a code-behind fájlokban használt azonosítóknak, minden metódushívás paraméterezése
egyezik a tényleges definíciókkal, és a kapcsos zárójelek/névterek mindenhol konzisztensek.
Ezzel a módszerrel nem található valódi fordítási hiba a kódban.

Ha Visual Studio mégis több hibakódot jelez és nem indul el a program, a leggyakoribb valódi
okok - érdemes ezeket ellenőrizni:

1. **Hiányzó "​.NET asztali fejlesztés" (".NET desktop development") workload.** Ez a
   leggyakoribb ok: Visual Studio Installerben (nem magában a VS-ben!) ellenőrizd, hogy ez a
   workload be van-e pipálva, és ha nem, telepítsd, majd indítsd újra Visual Studiót.
2. **Nem a .NET 8 SDK van telepítve.** A `dotnet --version` parancsnak `8.x`-et kell mutatnia
   (Visual Studio 2022 17.8 vagy újabb szükséges hozzá).
3. **Elavult NuGet/MSBuild gyorsítótár** a korábbi, esetleg hibás állapotú lefordítási
   kísérletekből: Visual Studióban "Build" → "Clean Solution", majd töröld kézzel a `bin/` és
   `obj/` mappákat a `App/Windows/BeosztasVarazslo` alatt, és fordítsd újra.
4. Ha ezek után is konkrét hibakódok (pl. `CS####`, `MC####`) jelennek meg, másold be a teljes
   hibaüzenetet (Hiba lista/Error List ablak tartalmát) - anélkül a fenti általános
   átvizsgálás a lehető legmesszebb ment el, amit egy Linux-alapú fejlesztői környezetben
   (ahol maga a WPF fordító nem futtatható) el lehetett végezni.

## Ismert korlátok
- A nézetek egyszerű, nem virtualizált (nem RecyclerView/ListView-szerű) elemekből épülnek
  fel kódból - néhány tucat dolgozóig gördülékeny, nagyon nagy létszámnál lassulhat.
