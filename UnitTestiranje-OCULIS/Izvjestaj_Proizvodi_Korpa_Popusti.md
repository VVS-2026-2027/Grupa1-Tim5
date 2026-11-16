# Task 2 OCULIS · proizvodi, korpa, popusti i validacija

## Izvršavanje

| Skup | Uspješni | Neuspješni | Preskočeni | Ukupno |
| --- | ---: | ---: | ---: | ---: |
| Dodijeljene četiri testne klase | 56 | 1 | 0 | 57 |
| Kompletan skup na grani | 87 | 1 | 0 | 88 |

Kompletni skup pokrio je 685 od 2944 instrumentabilnih linija (23,27%).

Projekti ciljaju .NET 8 i koriste EF Core 8.0.7. Izvršavanje je obavljeno na instaliranom .NET 10 runtimeu uz lokalnu postavku DOTNET_ROLL_FORWARD=Major. Svaki test s bazom koristi zasebnu EF Core InMemory instancu.

## Pokrivenost linija dodijeljenog koda

Izvor je Cobertura XML iz pokretanja samo četiri dodijeljene testne klase. Izvršene linije async metoda pripisane su pripadajućoj produkcijskoj klasi. Mjerenje isključuje Program.cs, Razor prikaze i generisane migracije prema postojećem Coverage.runsettings. Ostale produkcijske klase i dalje su u sirovom izvještaju.

| Produkcijska klasa | Izvršeno / instrumentabilno | Pokrivenost | Nepokrivene linije |
| --- | ---: | ---: | --- |
| ProizvodController | 77 / 77 | 100,00% |  |
| KorpaController | 121 / 121 | 100,00% |  |
| AkcijaPopustStrategija | 13 / 13 | 100,00% |  |
| KolicinaPopustStrategija | 7 / 7 | 100,00% |  |
| LojalnostPopustStrategija | 10 / 10 | 100,00% |  |
| PopustKalkulatorService | 36 / 36 | 100,00% |  |
| Proizvod | 8 / 8 | 100,00% |  |
| Akcija | 8 / 8 | 100,00% |  |
| Korpa | 5 / 5 | 100,00% |  |
| StavkaKorpe | 7 / 7 | 100,00% |  |
| ProizvodPretragaViewModel | 7 / 7 | 100,00% |  |
| KorpaViewModel | 4 / 4 | 100,00% |  |
| StavkaKorpeViewModel | 6 / 6 | 100,00% |  |
| PopustRezultatViewModel | 5 / 5 | 100,00% |  |
| NarudzbaCheckoutViewModel | 7 / 7 | 100,00% |  |
| **Ukupno za tabelu** | **321 / 321** | **100,00%** | |

Zajednički model Korisnik ima 4/6 pokrivenih linija u ovom pokretanju. Strategija lojalnosti testira broj narudžbi i bodove. Telefon i prikaz punog imena pripadaju korisničkom profilu.

## Potvrđeni defekt

**KorpaController.Dodaj — spremljeni zbir kasni za izmjenom stavke.** Početna stavka ima količinu 1 i cijenu 19 KM. Dodavanje još dvije jedinice po aktuelnoj cijeni 20 KM ispravno sprema stavku kao 3 × 20 KM. Nakon čišćenja ChangeTrackera i ponovnog čitanja baze, spremljeni zbir korpe iznosi 19 KM. Očekivanje regresijskog testa ostaje 60 KM.

Metoda računa zbir upitom prema bazi prije upisa izmijenjene stavke. Regresija je aktivna, označena kategorijom PoznatiDefekt i izvršava se u oba skupa. Produkcijski kod nije mijenjan u ovom obimu.

## Vanjski podaci i zamjenski objekti

- CSV **kolicine.csv** učitava DynamicData za pragove količinskog popusta. Prazna korpa i količine 2, 3, 4 i 5 razlikuju nedostatan broj jedinica, prvi prag i drugi prag.
- JSON **proizvodi-validacija.json** učitava DynamicData za minimalnu cijenu, dužinu opisa i proizvođača, zalihu i obavezna polja. Test provjerava konkretna neispravna polja.
- DataRow obuhvata dodatne scenarije lojalnosti, pretrage, validacije i nedostajućih identifikatora.
- Strogi Moq za IPopustStrategija provjerava da kalkulator šalje očekivanog kupca i prazne kolekcije. Strogi IPopustKalkulatorService provjerava da se kalkulator poziva u checkoutu za kupca i njegovu korpu te da nema poziva za praznu korpu.
- UserManager i IUserStore omogućavaju izolovan identitet kupca i provjeru pristupa stavkama korpe. ITempDataProvider omogućava provjeru poruka i preusmjerenja iz MVC akcija bez vanjske sesije.

## Inventar testova

### KorpaControllerTests

11 slučajeva. Visoka. Vlasništvo nad korpom, stanje baze, izračun i komunikacija sa servisom za popust.

- Checkout_PopunjenaKorpa_VracaStavkeIPopustZaVlasnika (1)
- Checkout_PraznaKorpa_PreusmjeravaBezPozivaKalkulatora (1)
- Dodaj_KolicinaIzvanZalihe_OstavljaKorpuPraznom (2)
- Dodaj_NepostojeciProizvod_VracaNotFoundBezKreiranjaKorpe (1)
- Dodaj_NovaStavkaNaGraniciZalihe_SpremaStavkuIVracaNaKatalog (1)
- Dodaj_PostojecaStavka_PovecavaKolicinuIAzuriraJedinicnuCijenu (1)
- Dodaj_PostojecaStavka_SpremljeniZbirJe60KM (1; neuspješnih 1)
- Index_PraznaKorpa_VracaPrazanModelBezIzracunaPopusta (1)
- Ukloni_VlasnistvoStavke_BriseSamoStavkuIzVlastiteKorpe (2)

### PopustTests

17 slučajeva. Srednja do visoka. Pragovi, vremenski prozori, odabir najvećeg popusta, zaokruživanje i izuzetak.

- IzracunajPopustAsync_NepostojeciKorisnik_BacaIzuzetak (1)
- IzracunajPopustAsync_PraznaKorpa_IzvrsavaStrategijeIVracaNulu (1)
- IzracunajPopustAsync_VisePrimjenjivihPopusta_BiraNajveciIZaokruzujeIznos (1)
- IzracunajPopustPostotak_AktivnaAkcijaZaDrugiProizvod_VracaNulu (1)
- IzracunajPopustPostotak_AktivneIstekleITudjeAkcije_BiraNajvecuPrimjenjivu (2)
- IzracunajPopustPostotak_GraniceKolicine_VracaOdgovarajuciPopust (5)
- IzracunajPopustPostotak_PragoviLojalnosti_VracaOdgovarajuciPopust (6)

### ProizvodControllerTests

21 slučajeva. Srednja. Kombinovanje filtera, sortiranje, CRUD i nevalidni MVC modeli.

- Create_FormaZaNoviProizvod_VracaPrazanModel (1)
- Create_NemaNaziva_VracaUnosBezUpisa (1)
- Create_ValidanProizvod_SpremaProizvodIPreusmjerava (1)
- Delete_NemaProizvoda_VracaNotFound (2)
- Delete_PostojeciId_VracaModelZaPotvrduBezBrisanja (1)
- DeleteConfirmed_SelektivnoBrisanje_CuvaDrugiProizvod (2)
- Details_NedostajeProizvod_VracaNotFound (2)
- Details_PostojeciProizvod_VracaCijenuISliku (1)
- Edit_IdentifikatoriSeNePodudaraju_VracaNotFoundBezUpisa (1)
- Edit_NemaProizvoda_VracaNotFound (2)
- Edit_NevalidnaCijena_VracaUnosBezPromjeneBaze (1)
- Edit_PodudaraSeIdentifikator_SpremaNovuCijenu (1)
- Edit_PostojeciId_VracaModelZaUredjivanje (1)
- Index_KombinovaniFilteri_VracaSamoPodudaranjaUTrazenomPoretku (4)

### ValidacijaProizvodaTests

8 slučajeva. Niska do srednja. Granice podataka i provjera tačnih polja s greškom.

- TryValidateObject_GraniceModelaProizvod_ProvjeravaNazivICijenu (4)
- TryValidateObject_VanjskeGraniceProizvoda_PrijavljujeTacnaPolja (4)

## Procjena udjela

Dodijeljene klase imaju 321 od 2944 instrumentabilnih linija (10,90%). Korpa i kalkulator traže složenije pripreme od jednostavnog CRUD-a, pa udio linija služi kao orijentir, a broj već napisanih testova zavisi od trenutka mjerenja. Dodijeljeni obim vjerovatno je ispod petine ukupnog posla.

Najbliža proširenja su AkcijaController (upravljanje akcijama i vezom s proizvodom) i StavkaKorpeController (validacija i konkurentne izmjene stavki). Zajedno dodaju 146 instrumentabilnih linija. Ako na kraju i dalje nedostaje obim, sljedeća povezana cjelina je checkout u NarudzbaController, nakon dogovora o granici s narudžbama.

## Ponovno pokretanje

Iz direktorija UnitTestiranje-OCULIS, s instaliranim .NET 8 runtimeom:

```powershell
dotnet test OCULIS.sln --settings OCULIS.Tests/Coverage.runsettings --collect "XPlat Code Coverage" --logger "trx;LogFileName=oculis-svi.trx"
$filter = 'FullyQualifiedName~.ProizvodControllerTests.|FullyQualifiedName~.KorpaControllerTests.|FullyQualifiedName~.PopustTests.|FullyQualifiedName~.ValidacijaProizvodaTests.'
dotnet test OCULIS.sln --settings OCULIS.Tests/Coverage.runsettings --collect "XPlat Code Coverage" --logger "trx;LogFileName=oculis-moj-dio.trx" --filter $filter
```

Obje komande vraćaju kod 1 dok je reproducirani defekt zbira korpe otvoren.
