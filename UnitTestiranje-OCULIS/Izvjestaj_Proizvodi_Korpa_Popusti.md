# Task 2 OCULIS · proizvodi, korpa, popusti i validacija

## Izvršavanje

| Skup | Uspješni | Neuspješni | Preskočeni | Ukupno |
| --- | ---: | ---: | ---: | ---: |
| Dodijeljenih šest testnih klasa | 96 | 3 | 0 | 99 |
| Kompletan skup na grani | 127 | 3 | 0 | 130 |

Kompletni skup pokrio je 831 od 2944 instrumentabilnih linija (28,23%).

Projekti ciljaju .NET 8 i koriste EF Core 8.0.7. Izvršavanje je obavljeno na instaliranom .NET 10 runtimeu uz lokalnu postavku DOTNET_ROLL_FORWARD=Major. Svaki test s bazom koristi zasebnu EF Core InMemory instancu.

## Pokrivenost linija dodijeljenog koda

Izvor je Cobertura XML iz pokretanja samo šest dodijeljenih testne klase. Izvršene linije async metoda pripisane su pripadajućoj produkcijskoj klasi. Mjerenje isključuje Program.cs, Razor prikaze i generisane migracije prema postojećem Coverage.runsettings. Ostale produkcijske klase i dalje su u sirovom izvještaju.

| Produkcijska klasa | Izvršeno / instrumentabilno | Pokrivenost | Nepokrivene linije |
| --- | ---: | ---: | --- |
| AkcijaController | 51 / 51 | 100,00% |  |
| KorpaController | 121 / 121 | 100,00% |  |
| ProizvodController | 77 / 77 | 100,00% |  |
| StavkaKorpeController | 95 / 95 | 100,00% |  |
| Akcija | 8 / 8 | 100,00% |  |
| Korpa | 5 / 5 | 100,00% |  |
| Proizvod | 8 / 8 | 100,00% |  |
| StavkaKorpe | 7 / 7 | 100,00% |  |
| KorpaViewModel | 4 / 4 | 100,00% |  |
| NarudzbaCheckoutViewModel | 7 / 7 | 100,00% |  |
| PopustRezultatViewModel | 5 / 5 | 100,00% |  |
| ProizvodPretragaViewModel | 7 / 7 | 100,00% |  |
| StavkaKorpeViewModel | 6 / 6 | 100,00% |  |
| AkcijaPopustStrategija | 13 / 13 | 100,00% |  |
| KolicinaPopustStrategija | 7 / 7 | 100,00% |  |
| LojalnostPopustStrategija | 10 / 10 | 100,00% |  |
| PopustKalkulatorService | 36 / 36 | 100,00% |  |
| **Ukupno za tabelu** | **467 / 467** | **100,00%** | |

Zajednički model Korisnik ima 4/6 pokrivenih linija u ovom pokretanju. Strategija lojalnosti testira broj narudžbi i bodove. Telefon i prikaz punog imena pripadaju korisničkom profilu.

## Potvrđeni defekti

**KorpaController.Dodaj — spremljeni zbir kasni za izmjenom stavke.** Početna stavka ima količinu 1 i cijenu 19 KM. Dodavanje još dvije jedinice po aktuelnoj cijeni 20 KM ispravno sprema stavku kao 3 × 20 KM. Nakon čišćenja ChangeTrackera i ponovnog čitanja baze, spremljeni zbir korpe iznosi 19 KM. Očekivanje regresijskog testa ostaje 60 KM.

Metoda računa zbir upitom prema bazi prije upisa izmijenjene stavke. Regresija je aktivna, označena kategorijom PoznatiDefekt i izvršava se u oba skupa. Produkcijski kod nije mijenjan u ovom obimu.

**Akcija.PopustPostotak — decimalne vrijednosti neposredno izvan raspona prolaze validaciju.** Oba slučaja s podacima reprodukuju problem. Za −0,01% i 100,01% očekuje se greška validacije, ali DataAnnotations prihvata obje vrijednosti. Model koristi cjelobrojne granice Range atributa i polje tipa double. U istom skupu potvrđeno je da se −1% i 101% odbijaju, a 0% i 100% prihvataju. Obje regresijske vrijednosti ostaju aktivne i označene kategorijom PoznatiDefekt.

## Vanjski podaci i zamjenski objekti

- CSV **kolicine.csv** učitava DynamicData za pragove količinskog popusta. Prazna korpa i količine 2, 3, 4 i 5 razlikuju nedostatan broj jedinica, prvi prag i drugi prag.
- JSON **proizvodi-validacija.json** učitava DynamicData za minimalnu cijenu, dužinu opisa i proizvođača, zalihu i obavezna polja. Test provjerava konkretna neispravna polja.
- DataRow obuhvata dodatne scenarije lojalnosti, pretrage, validacije i nedostajućih identifikatora.
- Strogi Moq za IPopustStrategija provjerava da kalkulator šalje očekivanog kupca i prazne kolekcije. Strogi IPopustKalkulatorService provjerava da se kalkulator poziva u checkoutu za kupca i njegovu korpu te da nema poziva za praznu korpu.
- UserManager i IUserStore omogućavaju izolovan identitet kupca i provjeru pristupa stavkama korpe. ITempDataProvider omogućava provjeru poruka i preusmjerenja iz MVC akcija bez vanjske sesije.

- Parcijalni Moq za ApplicationDbContext simulira neuspješan upis akcije i konkurentnu izmjenu stavke koja i dalje postoji. EF Core InMemory provjerava i putanju u kojoj stavka nestane prije upisa. Validacija se provodi DataAnnotations pravilima, a test ručno prenosi greške u MVC ModelState jer direktni poziv akcije ne pokreće MVC validacijski pipeline.

## Inventar testova

### AkcijaControllerTests

20 slučajeva. Srednja. CRUD, veze s proizvodom, granice popusta i greška pri upisu.

- Create_FormaZaNovuAkciju_NudiProizvode (1)
- Create_GranicniValidniPostotak_SpremaAkciju (2)
- Create_GreskaPohrane_PropagiraIzuzetakBezPorukeOUspjehu (1)
- Create_PostotakIzvanRaspona_VracaModelIIzabraniProizvod (2)
- Delete_NedostajeAkcija_VracaNotFound (2)
- Delete_PostojecaAkcija_VracaPotvrduBezBrisanja (1)
- DeleteConfirmed_PostojeciIliNepostojeciId_CuvaOstaleAkcije (2)
- Edit_IdentifikatoriSeRazlikuju_VracaNotFoundBezPromjene (1)
- Edit_NedostajeAkcija_VracaNotFound (2)
- Edit_PostojecaAkcija_VracaOdabraniProizvod (1)
- Edit_PrazanNaziv_VracaUnosICuvaSpremljenuAkciju (1)
- Edit_ValidnaPromjena_SpremaNoviPopust (1)
- Index_OpstaIAkcijaZaProizvod_SortiraIUcitavaProizvod (1)
- ValidateProperty_DecimalniPostotakIzvanRaspona_OdbijaVrijednost (2, neuspješnih 2)

### KorpaControllerTests

11 slučajeva. Visoka. Vlasništvo, stanje baze, izračun i pozivi servisu za popust.

- Checkout_PopunjenaKorpa_VracaStavkeIPopustZaVlasnika (1)
- Checkout_PraznaKorpa_PreusmjeravaBezPozivaKalkulatora (1)
- Dodaj_KolicinaIzvanZalihe_OstavljaKorpuPraznom (2)
- Dodaj_NepostojeciProizvod_VracaNotFoundBezKreiranjaKorpe (1)
- Dodaj_NovaStavkaNaGraniciZalihe_SpremaStavkuIVracaNaKatalog (1)
- Dodaj_PostojecaStavka_PovecavaKolicinuIAzuriraJedinicnuCijenu (1)
- Dodaj_PostojecaStavka_SpremljeniZbirJe60KM (1, neuspješnih 1)
- Index_PraznaKorpa_VracaPrazanModelBezIzracunaPopusta (1)
- Ukloni_VlasnistvoStavke_BriseSamoStavkuIzVlastiteKorpe (2)

### PopustTests

17 slučajeva. Srednja do visoka. Pragovi, aktivne akcije, izbor popusta, zaokruživanje i izuzetak.

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

### StavkaKorpeControllerTests

22 slučajeva. Visoka. CRUD s povezanim entitetima, validacija količine i dvije putanje konkurentnog upisa.

- Create_FormaZaStavku_NudiPostojeceKorpeIProizvode (1)
- Create_GranicnaKolicina_SpremaStavku (2)
- Create_KolicinaIzvanRaspona_VracaUnosBezUpisa (2)
- Delete_NemaStavke_VracaNotFound (2)
- Delete_PostojecaStavka_VracaPotvrduBezBrisanja (1)
- DeleteConfirmed_PostojeciIliNepostojeciId_CuvaKorpuIProizvod (2)
- Details_NedostajeStavka_VracaNotFound (2)
- Details_PostojecaStavka_VracaCijenuIKolicinu (1)
- Edit_KonkurentnaIzmjenaStavkeKojaPostoji_PropagiraIzuzetak (1)
- Edit_NemaStavke_VracaNotFound (2)
- Edit_NevalidnaKolicina_ZadrzavaUnosBezIzmjene (1)
- Edit_PostojecaStavka_OznacavaPripadajucuKorpuIProizvod (1)
- Edit_RazlicitiIdentifikatori_VracaNotFoundBezUpisa (1)
- Edit_StavkaNestalaPrijeUpisa_VracaNotFound (1)
- Edit_ValidnaKolicina_SpremaIzmjenu (1)
- Index_SpremljenaStavka_UcitavaKorpuIProizvod (1)

### ValidacijaProizvodaTests

8 slučajeva. Niska do srednja. Granice i tačna polja s greškom.

- TryValidateObject_GraniceModelaProizvod_ProvjeravaNazivICijenu (4)
- TryValidateObject_VanjskeGraniceProizvoda_PrijavljujeTacnaPolja (4)

## Procjena udjela

Dodijeljene klase imaju 467 od 2944 instrumentabilnih linija (15,86%). Obim uključuje četiri kontrolera, četiri klase za popuste i njihove modele. Pravila korpe, izolacija identiteta, validacija i konkurentni upisi zahtijevaju složenije pripreme od jednostavnih provjera pojedinačnih svojstava.

Približno jedna petina zahtjevnosti je razumna radna procjena. Tačan udio zavisi od dogovorenog obima i složenosti preostalih cjelina, posebno Identity scenarija. Svih 17 dodijeljenih klasa je obuhvaćeno. Zajednički Identity model Korisnik posmatran je samo u dijelu lojalnosti.

## Ponovno pokretanje

Iz direktorija UnitTestiranje-OCULIS, s instaliranim .NET 8 runtimeom:

```powershell
dotnet test OCULIS.sln --settings OCULIS.Tests/Coverage.runsettings --collect "XPlat Code Coverage" --logger "trx;LogFileName=oculis-svi.trx"
$filter = 'FullyQualifiedName~.ProizvodControllerTests.|FullyQualifiedName~.KorpaControllerTests.|FullyQualifiedName~.PopustTests.|FullyQualifiedName~.ValidacijaProizvodaTests.|FullyQualifiedName~.AkcijaControllerTests.|FullyQualifiedName~.StavkaKorpeControllerTests.'
dotnet test OCULIS.sln --settings OCULIS.Tests/Coverage.runsettings --collect "XPlat Code Coverage" --logger "trx;LogFileName=oculis-moj-dio.trx" --filter $filter
```

Obje komande vraćaju kod 1 dok reproducirani defekti ostaju otvoreni.
