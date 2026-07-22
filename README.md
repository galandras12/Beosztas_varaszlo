# Beosztás Varázsló

Böngészőben futó, szerver nélküli munkabeosztás-készítő program HTML/CSS/JavaScript
alapon. Nem igényel internetkapcsolatot, adatbázis-szervert vagy telepítést.

## Használat

Nyisd meg az `index.html` fájlt bármelyik modern böngészőben (Chrome, Edge,
Firefox, Safari) – asztali gépen, laptopon vagy tableten egyaránt. Legegyszerűbb,
ha az egész `Beosztas_varaszlo` mappát egy webszerverrel szolgálod ki (pl.
`npx http-server .`), de dupla kattintással, közvetlenül fájlként megnyitva is
működik.

## Fülek

1. **Beállítások** – a havi kötelező óraszám/munkanap alaptábla (Január: 176 óra
   / 22 nap stb.) és a magyar munkaszüneti napok (automatikusan számolva,
   egyénileg is szerkeszthetők).
2. **Munkacsoportok** – tetszőleges munkaág létrehozása (pl. 12 órás váltásos
   ápolók, 8 órás takarítók, 4 órás részmunkaidősök, hétfő-péntek irodaiak),
   műszaktípusokkal és az egy műszakban szükséges létszámmal.
3. **Dolgozók** – dolgozók felvétele munkacsoporthoz rendelve, munkaidő-arány
   (teljes/rész) és a kötelezően megadandó, évi max. kiadható szabadságnapok
   számának beállításával. A program nem enged ennél többet kiosztani.
4. **Beosztás** – hónap kiválasztása, napi bontású beosztási rács. Irodai
   dolgozóknál hétvégén/ünnepnapon automatikusan nincs munka. A "Bejövő óra"
   oszlopba írható be kézzel, hogy egy dolgozó hány plusz (vagy mínusz) órával
   kezdi a hónapot; az "Egyenleg/köv. hó" oszlop az aznapi maradékot mutatja,
   ami a "Előző havi maradvány másolása" gombbal átvihető a következő hónapra.
   Alul látható a műszak-lefedettség (hányan dolgoznak egy műszakban a
   beállított elváráshoz képest).
5. **Nyomtatás / Export** – A4 fekvő elrendezésű, munkakör szerint ABC sorrendbe
   rendezett nyomtatható táblázat, a hónap számával és nevével a tetején, a
   sorok végén a következő hónapra átvitt órákkal. Exportálható PDF-be és
   JPG-be, illetve a böngésző nyomtatási funkciójával is kinyomtatható
   (több oldalra törve, ha sok a dolgozó).

## Adattárolás

Minden adat (munkacsoportok, dolgozók, beosztások, szabadságok) a böngésző
`localStorage`-ában tárolódik, titkosítás és szerver nélkül. A fejlécben lévő
**"Adatbázis mentése fájlba"** / **"Adatbázis betöltése fájlból"** gombokkal
egyetlen JSON fájlba menthető, illetve visszatölthető a teljes adatbázis –
ez szolgál biztonsági mentésként, illetve más eszközre való átvitelre.
