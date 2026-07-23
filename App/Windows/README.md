# Beosztás Varázsló – Windows

A repó gyökerében lévő böngészős (HTML/CSS/JS) verzió natív Windows portja: .NET 8 / WPF
asztali alkalmazás, modern (Fluent-stílusú, Windows 11 jellegű) megjelenéssel.

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
- **Themes/** – világos/sötét Fluent-stílusú színpaletta (a rendszerbeállítás alapján
  automatikusan kiválasztva induláskor) és egyedi vezérlő-stílusok (lekerekített gombok,
  kártyák, navigációs lista).
- **Helpers/WindowBackdrop.cs** – a címsort a rendszertémához igazítja, és Windows 11
  (22H2+) rendszeren megkísérli a Mica anyagtípust beállítani a DWM API-n keresztül;
  régebbi Windows verzión egyszerűen hatástalan marad.

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
  helyi adatbázisa mellett kényelmi biztonsági mentésként/hordozhatóságként.

## Ismert korlátok

- Ezt a modult nem sikerült ebben a munkakörnyezetben lefordítani/tesztelni (nincs .NET SDK,
  nincs internet a Microsoft letöltési szervereihez) - Visual Studióban/`dotnet build`-del
  való megnyitás után esetlegesen felmerülő fordítási hibákat jelezd vissza.
- A Mica ablakháttér csak Windows 11 (22H2+) rendszeren, és ott is legfeljebb a natív
  címsor sávján válhat láthatóvá, mivel a tartalmi terület (kártyák, navigációs sáv)
  szándékosan átlátszatlan hátteret használ a régebbi Windows verziókon való biztonságos
  megjelenés érdekében.
- A nézetek egyszerű, nem virtualizált (nem RecyclerView/ListView-szerű) elemekből épülnek
  fel kódból - néhány tucat dolgozóig gördülékeny, nagyon nagy létszámnál lassulhat.
