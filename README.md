# Beosztás Varázsló

Munkabeosztás-készítő program több munkaágra (pl. 12 órás váltásban dolgozó ápolók, 8 órás
takarítók, 4 órás részmunkaidősök, hétfőtől péntekig dolgozó irodai munkatársak). A program
három, egymástól független, de azonos funkciókészletet nyújtó formában készült el:

| Verzió | Hol található | Technológia | Futtatás |
|---|---|---|---|
| 🌐 Böngészős | a repó gyökere (`index.html`) | HTML / CSS / JavaScript, nincs build lépés | nyisd meg `index.html`-t bármelyik böngészőben |
| 🤖 Android | [`App/Android/`](App/Android/) | Kotlin, Room (SQLite) | Android Studio ([részletek](App/Android/README.md)) |
| 🪟 Windows | [`App/Windows/`](App/Windows/) | .NET 8 / WPF | Visual Studio / `dotnet` ([részletek](App/Windows/README.md)) |

Mindhárom verzió szerver nélkül, kizárólag helyben fut, és saját, titkosítatlan, egyetlen
fájlban tárolt adatbázissal dolgozik (böngészőben: `localStorage` + JSON export/import;
Androidon: Room/SQLite fájl; Windowson: JSON fájl az `%AppData%` mappában).

## Közös funkciók

Mindhárom verzió ugyanazt az öt fő területet fedi le:

1. **Beállítások** – a havi kötelező óraszám/munkanap alaptábla (Január: 176 óra / 22
   munkanap, Február: 168 óra / 21 nap, ..., lásd az alábbi teljes táblázatot), valamint a
   magyar munkaszüneti napok (automatikusan számolva, egyénileg is bővíthetők/kikapcsolhatók).
2. **Munkacsoportok** – tetszőleges munkaág létrehozása (pl. 12 órás váltásos ápolók, 8 órás
   takarítók, 4 órás részmunkaidősök, hétfő-péntek irodaiak), műszaktípusokkal és az egy
   műszakban szükséges létszámmal (lefedettség-ellenőrzéshez).
3. **Dolgozók** – felvétel munkacsoporthoz rendelve, munkaidő-arány (teljes/rész) és a
   kötelezően megadandó, évi max. kiadható szabadságnapok számának beállításával. A program
   sehol nem enged ennél több szabadságot kiosztani.
4. **Beosztás** – hónap kiválasztása, napi bontású beosztási rács. Irodai dolgozóknál
   hétvégén/ünnepnapon automatikusan nincs munkavégzés. Kézzel beírható, hogy egy dolgozó
   hány plusz (vagy mínusz) órával kezdi a hónapot ("bejövő óra"); az automatikusan számolt
   egyenleg egy gombbal átvihető a következő hónapra. Megjelenik a műszak-lefedettség is
   (hányan dolgoznak egy műszakban a beállított elváráshoz képest).
5. **Nyomtatás / Export** – A4 fekvő elrendezésű, munkakör szerint ABC sorrendbe rendezett
   nyomtatható táblázat, a hónap számával/nevével a tetején, a sorok végén a következő
   hónapra átvitt órákkal. Exportálható PDF-be (több oldalra törve, ha sok a dolgozó) és
   JPG-be.

### Havi kötelező óraszám (alapérték)

| Hónap | Óra | Munkanap | Hónap | Óra | Munkanap |
|---|---|---|---|---|---|
| Január | 176 | 22 | Július | 184 | 23 |
| Február | 168 | 21 | Augusztus | 168 | 21 |
| Március | 152 | 19 | Szeptember | 168 | 21 |
| Április | 168 | 21 | Október | 176 | 22 |
| Május | 168 | 21 | November | 160 | 20 |
| Június | 160 | 20 | December | 160 | 20 |

Ez az érték minden verzióban szerkeszthető, és egy teljes munkaidős dolgozóra vonatkozik –
a munkaidő-arány (pl. részmunkaidő), a kivett szabadság/hiányzás, illetve az előző hónapról
áthozott óra ezt módosítja a tényleges, dolgozónkénti kötelező óraszámhoz képest.

## Melyik verziót érdemes használni?

- **Böngészős**: a leggyorsabb kipróbálásra, bármilyen eszközön (telefon, tablet, gép)
  azonnal fut, nincs telepítés.
- **Android**: ha telefonon/tableten, alkalmazásként (internet nélkül is) szeretnéd
  használni; lásd az [App/Android/README.md](App/Android/README.md) fájlt a fordítási
  lépésekhez.
- **Windows**: ha asztali gépen, natív, modern (Fluent-stílusú) Windows alkalmazásként
  szeretnéd használni; lásd az [App/Windows/README.md](App/Windows/README.md) fájlt a
  fordítási lépésekhez.

Az Android és Windows verziót ebben a fejlesztői környezetben nem lehetett lefordítani és
tesztelni (nincs Android SDK, illetve .NET SDK, és a hozzájuk tartozó letöltési szerverek
sincsenek engedélyezve) – a böngészős verziót viszont teljeskörűen leteszteltem. A másik
két verzió kódját gondosan átnéztem, de az első fordításnál esetlegesen felmerülő hibákat
jelezd vissza.
