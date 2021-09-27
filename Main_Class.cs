using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main_Class : MonoBehaviour {

    /*ÖVERGRIPANDE BESKRIVNING AV KLASSEN:
     - Detta är spelets main-klass och sköter alla övergripande saker i spelet, inklusive: 
        - Generering av spelplanen
        - Generering av spelarnas kortlekar
        - Hantering av all markering och förflyttning av kort 
        - Hantering av tillstånden för att vinna och förlora spelet. 
         
    */


    // Generella variabler 
    public GameObject kortet;
    public GameObject brickan;
    public GameObject startMatchButton;
    public GameObject skillKnapp;
    public GameObject weaponKnapp;

    public List<GameObject> deck1 = new List<GameObject>();
    public List<GameObject> deck2 = new List<GameObject>();
    public List<GameObject> deckenSomSkaVäljas = new List<GameObject>();
    public List<GameObject> ledigHandSpelare1 = new List<GameObject>();
    public List<GameObject> ledigHandSpelare2 = new List<GameObject>();

    public List<GameObject> ledigPlanSpelare1 = new List<GameObject>();
    public List<GameObject> ledigPlanSpelare2 = new List<GameObject>();

    public List<GameObject> handenSomSkaVäljas = new List<GameObject>();
    public List<GameObject> markeradeKortLista = new List<GameObject>();//Denna lista används endast som lagringsvariabel för att kunna rensa listan med markerade kort då ett kort läggs ner.
    public List<GameObject> aktivaSiktare = new List<GameObject>();


    // AI-variabler:
    int AI_AntalDeckKort = 0; 

    // Generering av Deck / Bricks. 
    float förskjutningBricka;
    float förskjutningTillbaka;
    public float kortförskjutning;
    public int förskjutningsriktning;
    int spriteIndex;
    int slumpatTalTal;
    int slumpatFärgTal;
    int slumpTal;
    
    public Vector3 brickansStart;
    int brickaX;
    int brickaY;
    int räknare;
    int brickrad = 1;
    int counter;

    public SpelarensScript SS;
    public MainGameData LS;

    // === Variabler för lagring av info === 
    public bool successfulLoad; 
    public bool deckAlreadyExists;

    public string[] Kortlek_Färg;
    public int[] Kortlek_Tal;
    public string[] Kortlek_Effekter;
    public string[] Kortlek_Vapen;
    public int currentLevel;

    public string[] tillgängligaEffekter; 

    public List<string> UNLOCKED_Effects;
    public List<string> Available_Effects;
    public List<string> UNLOCKED_Weapons;
    public List<string> Available_Weapons;




    void Start() 
    {
        
        SpelarData_Mono SD_M = GameObject.Find("Scripthållare").GetComponent<SpelarData_Mono>();
        SD_M.LoadProgress();
        currentLevel = SD_M.currentLevel;
        print("LEVELN SOM LADDATS: " + currentLevel.ToString());

        successfulLoad = LoadProgress(); // Detta försöker ladda spelarens nuvarande kortlek. Om laddningen inte fungerar returneras "false" vilket lagras i successfulLoad och får if-satsen nedan att köras. 


        Available_Weapons = new List<string> { "Automatic Crossbow", "Magic Wand", "AK47", "Spear", }; // Förvirra inte dessa listor med listorna med de FAKTISKT UPPLÅSTA effekterna och vapnena! 
        Available_Effects = new List<string> {
            "Dark Ritual", "Last Rites", "Word of Agony", "Restoration", "Rip Enchantment", "Patient Spirit", "There Can Be Only One", "Sacrifice", "Immolate",
            "Soul Barbs", "Healing Breeze","Stun","Rage","Reap Life","Reckless Haste","Annihilation","Extend Duration","Strength to the Powerless","Draw Card",
            "Signet of Stamina","Mirror Image","Fire Blast","Change of Heart","Ammo Chart","Barrage", "Protection",
        };

        SaveProgress(); // Sparar progressen så att Available-listorna uppdateras korrekt. 


        if (successfulLoad == false || Kortlek_Tal[0] < 0) // Används för första gången då ingen kortlek finns för att undvika error.  Kortlek_Tal[0] < 0 kollar om kortleken resettas manuellt under programmets gång (hela listan töms då)
        {
            if (successfulLoad == false) print("successfulLoad == false");
            if (Kortlek_Tal[0] < 0) print("Kortlek_Tal[0] < 0");

            print("========== SPELARENS KORTLEK VAR TOM, SÅ EN NY KORTLEK GENERERAS =========="); 
            Kortlek_Färg = new string[50];
            Kortlek_Tal = new int[50];
            
            for (int i = 0; i < Kortlek_Tal.Length; i++) // Tilldelar -1 till alla tal i listan (ursprunligen är alla låsta.
            {
                Kortlek_Tal[i] = -1; 
            }
            Kortlek_Effekter = new string[50];
            Kortlek_Vapen = new string[50];
            deckAlreadyExists = false; 
            UNLOCKED_Effects = new List<string>() { "Immolate" }; 
            UNLOCKED_Weapons = new List<string> { "Automatic Crossbow" };

            // ========== Genererar ny spelarData ==========
            SD_M.maxUpplåstLevel = 1;
            SD_M.deckCapacity = 30; 
            SD_M.SaveProgress();


        }


        // === Generera egenskaper för AI ===
        GameObject turKnappen = GameObject.Find("StartMatchButton");
        AI_and_EnergyReset TKS = turKnappen.GetComponent<AI_and_EnergyReset>();
        TKS.SättDelayBeroendePåLevel(currentLevel); // Sätter AI:n spelhastighet beroende på level. 
        if (currentLevel <= 6) AI_AntalDeckKort = 5; // Fram tills level 6 är antal kort i AI:ns deck 5. Därefter ökar kortleken .
        else AI_AntalDeckKort = currentLevel;


        SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        LS = GameObject.Find("Scripthållare").GetComponent<MainGameData>();


        // === Generate Deck and Bricks ===
        kortförskjutning = 0.3f;

        GenereraDeckFunktion(1, new Vector3(3f, 3, 0)); // HÄR sätts koordinaterna för deck! 
        GenereraDeckFunktion(2, new Vector3(3f, 2, 0));

        GenereraBrickorFunktion();

        DrawCards(1, 5);
        DrawCards(2, 6);

    }

    public int SkapaAIDeck() // Skapar AI:ns deck beroende på den level som valts. 
    {
        int cardLevel = 1; // Den level som returneras. 

        int minRankForLevel; 
        int maxRankForLevel;



        if (currentLevel <= 5) // Slumpa fram ett värde mellan 0 och 4
        {
            minRankForLevel = 2;
            maxRankForLevel = 5; 
        }
        else if (currentLevel <= 10)
        {
            minRankForLevel = 3;
            maxRankForLevel = 8;
        }
        else if (currentLevel <= 15) 
        {
            minRankForLevel = 5;
            maxRankForLevel = 11;
        }
        else if (currentLevel <= 20)
        {
            minRankForLevel = 8;
            maxRankForLevel = 12;
        }
        else // Slumpa fram ett värde mellan 5 och 10. Ger även en viss chans att kortet blir ett Ess.
        {
            minRankForLevel = 11;
            maxRankForLevel = 13;
        }

        cardLevel = UnityEngine.Random.Range(maxRankForLevel, minRankForLevel+1);
        
        if (cardLevel > 13) cardLevel = 13; // Det största talet vi kan få är 13. 
           
        return cardLevel; 
    }

    public int HittaPositionMedMinus1(int[] gällandeArray)
    {
        if (gällandeArray[0] != -1)
        {
            for (int i = 0; i < gällandeArray.Length; i++)
            {
                if (gällandeArray[i] == -1)
                {
                    return i; 
                }

            }

            Debug.LogError("Fel! Ingen position med 0 hittades i arrayen.");
            return 150;

        }
        else return 6;  

    }

    //=====FOR-LOOP SOM GENERERAR SPELARNAS KORTLEKAR==== GenereraDeckFunktion(5, 1, (2,3,0)
    public void GenereraDeckFunktion(int spelare, Vector3 position)
    {
        int counter = 0;

        int antalKort;
        float förskjutningDeck = 0;

        if (spelare == 1)
        {
            antalKort = HittaPositionMedMinus1(Kortlek_Tal); 
        }
        else antalKort = AI_AntalDeckKort;

        Vector3 sistaPosition = new Vector3(0, 0, 0); // Aanvänds för att placera jokern på rätt plats (sista positionen i deck) 



        for (int i = 0; i < antalKort; i++) //for (int i=0; i< kortStränglista.Length; i++)
        {

            //=====KORTETS TAL, FÄRG, RESPEKTIVE ÄGARE TILLDELAS====
            // === FÄRG OCH TAL ===



            if ((deckAlreadyExists == false || spelare == 2) || Kortlek_Tal[counter] > 0) 
            {
                GameObject kopia = Instantiate(kortet) as GameObject;
                Sprite_Kortet kortetsSpritescript = kopia.GetComponent<Sprite_Kortet>();//För varje script vi ska använda måste vi alltså en lagringsvariabel för det scriptet. 
                KortetsScript KS = kopia.GetComponent<KortetsScript>();
                kopia.transform.position = position;
                kopia.transform.position += new Vector3(förskjutningDeck, 0);//Skapar en förfyttning av korten så att man kan se båda spelarnas kortlekar på sidan av skärmen. 
                förskjutningDeck += 0.10f;


                if (deckAlreadyExists == false || spelare == 2)
                {   
                    // === KORTET GENERERAS (Dvs laddas inte) ===
                    if (spelare == 2)
                    {
                        KS.kortetsTal = SkapaAIDeck();// ATM ska kortleken endast sparas för spelare ETT (Spelare 2:s kortlek genereras ALLTID) 
                    }
                    else
                    {
                        KS.kortetsTal = UnityEngine.Random.Range(3, 6); ; // KS.kortetsTal = Convert.ToInt32(tal);   //Tilldelar kortet sitt tal
                    }

                
                    slumpatFärgTal = UnityEngine.Random.Range(1, 5);
                    switch (slumpatFärgTal)//Tidigare hade jag case "C, D, H och S för det sparade scriptet 
                    {
                        case 1:
                            KS.kortetsFärg = "Klöver";
                            break;
                        case 2:
                            KS.kortetsFärg = "Ruter";
                            break;
                        case 3:
                            KS.kortetsFärg = "Hjärter";
                            break;
                        case 4:
                            KS.kortetsFärg = "Spader";
                            break;
                    }

                    // === EFFEKT OCH VAPEN ===

                    KS.effekt = "Immolate";
                    KS.kortetsVapen = "Automatic Crossbow";

                    if (spelare == 2) KS.kortetsVapen = "Automatic Crossbow"; // Alla spelare 2:s kort är Crossbow för enkelhetens skull (de kan inte använda Magic Wand). 

                    if (spelare == 1) // Lagrar korten lokalt i GDaB så att vi sedan kan spara kortleken. 
                    {
                    
                        print(" =========== SPELARE 1:S KORT LÄGGS I KORTLISTAN =========== " + KS.kortetsFärg + KS.kortetsTal.ToString() + " COUNTER = " + counter.ToString()); 
                        Kortlek_Färg[counter] = KS.kortetsFärg;
                        Kortlek_Tal[counter] = KS.kortetsTal; // <<<<< Eftersom att kortet GENERERAS här ska det göras OAVSETT om talet är 0 eller -1! (Det görs antalet gånger som anges i while-satsen)
                        Kortlek_Effekter[counter] = KS.effekt;
                        Kortlek_Vapen[counter] = KS.kortetsVapen;
                    }

                    

                    counter++;
                
                }

                else // OM KORTLEKEN INTE SKA GENERERAS - DVS KORTLEKEN SKA LADDAS FRÅN SPARADE FILER  
                {
                    // === KORTET LADDAS ===

                    if (Kortlek_Tal[counter] > 0)
                    {

                        KS.kortetsTal = Kortlek_Tal[counter];
                        KS.kortetsFärg = Kortlek_Färg[counter];
                        KS.effekt = Kortlek_Effekter[counter];
                        KS.kortetsVapen = Kortlek_Vapen[counter];
                    }
                    else Debug.LogError("FEL - Ett kort med tal mindre än 1 genereras.");
                    counter++; 

                }

                // === ÖVRIG KORTDATA TILLDELAS (IDENTISK med ovanstående) === 
                KS.tillhörSpelare = spelare;
                if (KS.tillhörSpelare == 2) KS.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
                KS.maxHealth = KS.kortetsTal;
                KS.maxAmmunition = KS.kortetsTal;
                KS.health = KS.maxHealth;
                KS.kortetsBricka = null;

                // =====KORTET RITAS OCH LÄGGS TILL I DECK =====
                kortetsSpritescript.RitaKortet(KS.kortetsTal, KS.kortetsFärg);
                LäggKortetIKortleken(kopia, KS.tillhörSpelare);
                sistaPosition = KS.transform.position; 

            }
            else
            {
                counter++;
            }




        } 

        if (deckAlreadyExists == false && spelare == 1)
        {
            print("=========================== KORTLISTAN SPARAS FÖR SPELARE " + spelare.ToString() + " ===========================");
            print("======================================================");
            foreach (int i in Kortlek_Tal)
            {
                if (i > 0) print(" KORTET SOM SPARAS ÄR: " + i.ToString());
            }
            deckAlreadyExists = true;
            SaveProgress(); 
        }

            
        // === GENERERA JOKER === 
        GameObject joker = Instantiate(kortet) as GameObject;
        KortetsScript KS_J = joker.GetComponent<KortetsScript>();
        Sprite_Kortet SK_J = joker.GetComponent<Sprite_Kortet>();

        KS_J.transform.position = sistaPosition;

        KS_J.tillhörSpelare = spelare;
        KS_J.kortetsFärg = "Joker";
        KS_J.kortetsTal = 15;
        KS_J.maxHealth = 15;
        if (spelare == 2) KS_J.maxHealth += currentLevel; 
        KS_J.health = KS_J.maxHealth; 
        KS_J.maxAmmunition = 5;
        KS_J.kortetsBricka = null;
        KS_J.effekt = "Immolate";
        KS_J.kortetsVapen = "Automatic Crossbow";
        SK_J.RitaKortet(KS_J.kortetsTal, KS_J.kortetsFärg);
        LäggKortetIKortleken(joker, KS_J.tillhörSpelare);
    }

    //===== LAGRA INFORMATION =====
    public void SaveProgress()
    {
        SaveFunctions.SaveDeck(this);
    }

    public bool LoadProgress()
    {
        
        KortData deckLoad = SaveFunctions.LoadDeck();

        if (deckLoad == null) // Om deckload inte returnerar någon data returneras False, vilket får resten av programmet att veta att en deck måste genereras! 
        {
            print("FAILED TO LOAD DATA - NO SCRIPT EXISTS");
            return false;

        }
        else if (deckLoad.deckAlreadyExists == false) // Om deckload inte returnerar någon data returneras False, vilket får resten av programmet att veta att en deck måste genereras! 
        {
            print("FAILED TO LOAD DATA - deckAlreadyExists is " + deckAlreadyExists.ToString());
            return false;

        }
        else
        {
            print("SUCEEDED TO LOAD DATA");
            Kortlek_Färg = deckLoad.Kortlek_Färg;
            Kortlek_Tal = deckLoad.Kortlek_Tal;
            Kortlek_Effekter = deckLoad.Kortlek_Effekter;
            Kortlek_Vapen = deckLoad.Kortlek_Vapen;

            Available_Effects = deckLoad.Available_Effects;
            Available_Weapons = deckLoad.Available_Weapons;
            UNLOCKED_Effects = deckLoad.UNLOCKED_Effects;
            UNLOCKED_Weapons = deckLoad.UNLOCKED_Weapons;
            deckAlreadyExists = true;

            return true;
        }

    }


    public string ReturneraKortlista()
    {
        if (Kortlek_Tal.Length != 0)
        {
            string sträng = "";
            int counter = 0;

            while (counter < Kortlek_Tal.Length)
            {
                sträng += Kortlek_Tal[counter].ToString() + "    ";
                counter++;
            }

            return sträng;
        }
        else return "null";

    }

    public void RandomiseDeck()
    {
        int i = 0;
        Kortlek_Tal = new int[30];

        while (i < Kortlek_Tal.Length)
        {

            Kortlek_Tal[i] = UnityEngine.Random.Range(1, 5);

            i++;
        }

    }

    void OnGUI()
    {
        if (false) // MYCKET ANVÄNDBART FÖR DEBUGGING AV CARD-DECK
        {
            string meddelande = ReturneraKortlista();
            var position = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            var textSize = GUI.skin.label.CalcSize(new GUIContent(meddelande));
            GUI.Label(new Rect(150, 220, textSize.x, textSize.y), meddelande);
        }


    }


    //=====FOR-LOOP SOM GENERERAR BRICKORNA====
    public void GenereraBrickorFunktion()
    {
        for (int i = 0; i < 28; i++)//For-loop som genererar BRICKORNA
        {
            GameObject nyBricka = Instantiate(brickan) as GameObject;
            nyBricka.transform.position = brickansStart + new Vector3(brickaX, brickaY);
            BrickansScript BS = nyBricka.GetComponent<BrickansScript>();

            switch (brickrad)
            {
                case 1:
                    BS.handPlan = "Hand";
                    BS.tillhörSpelare = 2;
                    ledigHandSpelare2.Add(nyBricka);
                    break;
                case 2:
                    BS.handPlan = "Plan";
                    BS.tillhörSpelare = 2;
                    ledigPlanSpelare2.Add(nyBricka);
                    break;
                case 3:
                    BS.handPlan = "Plan";
                    BS.tillhörSpelare = 1;
                    ledigPlanSpelare1.Add(nyBricka);
                    break;
                case 4:
                    BS.handPlan = "Hand";
                    BS.tillhörSpelare = 1;
                    ledigHandSpelare1.Add(nyBricka);
                    break;

            }

            brickaX += 1;
            räknare += 1;
            if (brickaX == 7)
            {
                brickaY -= 2;
                brickaX = 0;
                brickrad++;

            }
            if (räknare == 14)
            {
                brickaY -= 1;
            }

        }
    }

    // === Lägg till i hand/plan-listorna ==
    public void LäggTillIHandPlan(GameObject kortetIFråga, GameObject brickanIFråga)
    {
        if (brickanIFråga.GetComponent<BrickansScript>().handPlan == "Hand")
        {
            if (kortetIFråga.GetComponent<KortetsScript>().tillhörSpelare == 1)
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigHandSpelare1.Add(brickanIFråga);
            }
            else
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigHandSpelare2.Add(brickanIFråga);
            }

        }
        else if (brickanIFråga.GetComponent<BrickansScript>().handPlan == "Plan")
        {
            if (kortetIFråga.GetComponent<KortetsScript>().tillhörSpelare == 1)
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigPlanSpelare1.Add(brickanIFråga);
            }
            else
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigPlanSpelare2.Add(brickanIFråga);
            }
        }
    }

    public void TaBortFrånHandPlan(GameObject kortetIFråga, GameObject brickanIFråga)
    {
        if (brickanIFråga.GetComponent<BrickansScript>().handPlan == "Hand") 
        {
            if (kortetIFråga.GetComponent<KortetsScript>().tillhörSpelare == 1)
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigHandSpelare1.Remove(brickanIFråga);
            }
            else
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigHandSpelare2.Remove(brickanIFråga);
            }

        }
        else
        {
            if (kortetIFråga.GetComponent<KortetsScript>().tillhörSpelare == 1)
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigPlanSpelare1.Remove(brickanIFråga);
            }
            else
            {
                GameObject.Find("Scripthållare").GetComponent<Main_Class>().ledigPlanSpelare2.Remove(brickanIFråga);
            }

        }
    }


    public void FörstörSiktare(GameObject siktaren, int spelareSomFörstörs = 0)
    {   
        print("En siktare förstörs.");

        bool korrektsiktare = true; 

        if (spelareSomFörstörs != 0) 
        {
            SiktarensScript SiS = siktaren.GetComponent<SiktarensScript>();
            KortetsScript KS = SiS.kortetSomSkaSkjuta.GetComponent<KortetsScript>();
            if (KS.tillhörSpelare != spelareSomFörstörs) korrektsiktare = false;

        }

        if (siktaren != null && korrektsiktare) 
        {   

            aktivaSiktare.Remove(siktaren);
            Destroy(siktaren.GetComponent<SiktarensScript>().siktarensAimTargetLagring); 
            Destroy(siktaren);
        }
        


    }
    public void FörstörAllaSiktare(int spelareSomFörstörs = 0) 
    {
        print("<X%X%X> Funktionen som förstör alla siktare aktiveras. Alla siktare förstörs.");
        
        for (int i = aktivaSiktare.Count - 1; i >= 0; i--)
        {
            FörstörSiktare(aktivaSiktare[i], spelareSomFörstörs);
        }
      
    }




    public void TaBortKortetUrBrickan(GameObject gällandeKort)
    {
        KortetsScript KS = gällandeKort.GetComponent<KortetsScript>();
        GameObject gällandeBricka = KS.kortetsBricka;

        if (gällandeBricka != null)
        {
            BrickansScript BS_LÅG = gällandeBricka.GetComponent<BrickansScript>();
            //== Tar bort kortet i brickan ==
            BS_LÅG.brickansKort.Remove(gällandeKort);


            //== HANTERAR VARIABELN "liggerKortetÖverst" ==
            if (BS_LÅG.brickansKort.Count != 0)//Om bricklistan INTE är tom...
            {
                //... sätts alla kort i listan till "liggerKortetÖverst = false", SEDAN sätts kortet som just lades ner till ligger överst. 
                BS_LÅG.brickansKort[BS_LÅG.brickansKort.Count - 1].GetComponent<KortetsScript>().liggerKortetÖverst = true; // Sätter det sista kortet i listan till true. >>> Det sista kortet i en lista X nås alltid av X[X.Count -1]. 

            }

            // == HANTERAR LEDIGA HANBRICKOR/PLANBRICKOR ==
            if (BS_LÅG.brickansKort.Count == 0) //Om brickans kortlista TÖMS ovan (dvs om kortet som plockas upp är det SISTA kortet i listan), ska det läggas in i någon av de 4 "lediga kort" - listorna. 
            {
                //print("<X%X%X> Kortet som plockades upp var det sista kortet i brickan, så brickan läggs till i någon av de 4 'ledigaBrickor'-listorna. ");
                LäggTillIHandPlan(gällandeKort, gällandeBricka);
            }
        }
    }


    public void HanteraFörskjutningIBricka(GameObject kortetSomFlyttas)
    {
        /*  BEGREPP som används: 
         kortetsPositionIBrickan = indexet där kortet ligger i bricklistan
         sistaKortetsPositionIBrickan = indexet för sista kortet i bricklistan
         förskjutningsriktning = ett tal som antingen är 1 eller -1 beroence på hållet kortet ska förskjutas (neråt för spelare 1 och uppåt för spelare 2!) 
         kortförskjutning = given försjutning i början av programmet för hur långt kortets ska förskjutas visuellt på skärmen. 
        */
        KortetsScript KS = kortetSomFlyttas.GetComponent<KortetsScript>();

        if (KS.kortetsBricka != null)//Om kortet har en bricka aktiveras förskjutningsfunktionen!
        {
            BrickansScript BS = KS.kortetsBricka.GetComponent<BrickansScript>();

            // == Flyttar ner alla eventuella kort som låg OVANFÖR kortet som skickades till graveyard == 
            int kortetsPositionIBrickan = BS.brickansKort.IndexOf(kortetSomFlyttas);
            int sistaKortetsPositionIBrickan = BS.brickansKort.Count - 1;
            for (int i = kortetsPositionIBrickan; i <= sistaKortetsPositionIBrickan; i++)//Loopen börjar på "kortetsPositionIBrickan" och går fram till sista positionen i brickan för att FLYTTA NER alla kort som låg över det. 
            {
                if (i > 0)//Om kortet som flyttades var det enda kortet som låg i brickan returneras -1 i kortesPositionIBrickan ovan, vilket skapar kaos. Därför används denna if-sats
                {
                    if (BS.brickansKort[i].GetComponent<KortetsScript>().tillhörSpelare == 1) { förskjutningsriktning = 1; }
                    else { förskjutningsriktning = -1; }
                    BS.brickansKort[i].transform.position += new Vector3(0, kortförskjutning * förskjutningsriktning, 0);//Förskjuter korten så att kortraden återgår till patiansform (dvs utan ett gap där det kort som skickades till graveyard låg)
                    //Notera att vi bara flyttar varje kort ETT steg här, dvs vi flyttar INTE kortet en sträcka motsvarande dess position i brickan. Detta är eftersom att endast ETT kort har flyttats då funktionen kallats. 
                    BS.brickansKort[i].transform.position += new Vector3(0, 0, 1);
                }

            }
            foreach (GameObject kortIBricklistan in BS.brickansKort)
            {
                kortIBricklistan.GetComponent<KortetsScript>().UppdateraHealthBar();
            }
        }

    }

    public void AvmarkeraKort(GameObject kortet) // Avmarkerar kortet som var markerat. 
    {

        KortetsScript KS = kortet.GetComponent<KortetsScript>();
        MainGameData MGD = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();

        if (kortet == EKS.kortetSomVarMarkerat)
        {
            EKS.AvmarkeraNonInstantEffect(kortet);
        }
       
        foreach (Transform child in kortet.transform)
        {
            AimTargetScript aimTarget_Child = child.gameObject.GetComponent<AimTargetScript>();

            if (aimTarget_Child != null)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        if (KS.kortetsSiktareLagring != null) FörstörSiktare(KS.kortetsSiktareLagring);   

        if (MGD.markeradeKort.Count != 0) // Om ett kort är markerat i samband med att det skickas till graveyard måste det först avmarkeras (dvs läggas tillbaka i den bricka där det låg)  
        {
            if (MGD.markeradeKort[0] == kortet) // Avmarkeringen ska endast ske om det kort som skickas till graveyard specifikt är kortet som var markerat 
            {
                LäggNerKortet(MGD.markeradeKort[0], MGD.markeradeKort[0].GetComponent<KortetsScript>().kortetsBricka);
            }

        }

    }

    public IEnumerator SkickaTillGraveyard(GameObject kortet)
    {

        yield return new WaitForSeconds(0.1f);

        KortetsScript KS = kortet.GetComponent<KortetsScript>();


        MainGameData MGD = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();


        // === Tutourial ===
        if (MGD.tutourialState > 0 && kortet == EKS.kortetSomVarMarkerat) // Kollar om kortet höll på att aktivera en effekt.  
        {
            MGD.tutourialState = 1;
            MGD.tutourialSubState = 3;
            MGD.TutourialText();
            
        }
        else if (MGD.markeradeKort.Count != 0) // Samma som ovan, fast kollar om kortet var markerat 
        {
            if (MGD.tutourialState > 0 && MGD.markeradeKort[0] == kortet)
            {
                MGD.tutourialState = 1;
                MGD.tutourialSubState = 3;
                MGD.TutourialText();
            }
        }



        if (kortet == EKS.kortetSomVarMarkerat) // Om kortet hade aktiverat en effekt avmarkeras effekten. 
        {
            EKS.AvmarkeraNonInstantEffect(kortet);
        }
 
        foreach (Transform child in kortet.transform)
        {
            AimTargetScript aimTarget_Child = child.gameObject.GetComponent<AimTargetScript>();

            if (aimTarget_Child != null)
            {
                GameObject.Destroy(child.gameObject);
            }
            
        }

        if (KS.kortetsSiktareLagring != null) FörstörSiktare(KS.kortetsSiktareLagring);  


        if (MGD.markeradeKort.Count != 0) // Om ett kort är markerat i samband med att det skickas till graveyard måste det först avmarkeras (dvs läggas tillbaka i den bricka där det låg)  
        {   
            if (MGD.markeradeKort[0] == kortet) // Avmarkeringen ska dock endast ske om det kort som skickas till graveyard specifikt är kortet som var markerat 
            {
                LäggNerKortet(MGD.markeradeKort[0], MGD.markeradeKort[0].GetComponent<KortetsScript>().kortetsBricka);
            }
            
        }
            
        for (int P = 0; P < KS.aktivaEffekter.Length; P++)//Går igenom listan...
        {
            if (KS.aktivaEffekter[P] != null)//... och hittar samtliga positioner som innehåller den effekt som skulle tas bort. 
            {

                KS.DurationEffect_Remove(P);

                

            }
        }


        // == Flyttar ner alla eventuella kort (ett steg) som låg OVANFÖR kortet som skickades till graveyard == 
        HanteraFörskjutningIBricka(kortet);

        // === Tar bort kortet ur brickan ===
        TaBortKortetUrBrickan(kortet);


        //Hanterar övriga viktiga variabler/metoder för att skicka kortet till graveyard. 
        MGD.graveyard.Add(kortet);
        KS.ärMarkerat = false;
        KS.transform.position = new Vector3(8, -4, 0);//För att förhindra att kortet syns, samt att de träffas av missiler, borde det vara enkelt att bara ändra z-koordinaten.
        KS.health = KS.maxHealth;//Kortet får tillbaka alla sina liv så att det kan återupplivas. 
        KS.UppdateraHealthBar();
        KS.kortetsBricka = null;//Detta är mycket viktigt för att undvika att kortetsBricka läggs till i någon av de 4 "ledigabrickor"-listorna i samband med att kortet på något sätt återupplivas. 
        KS.handPlan = null; // Denna rad krävs eftersom att AI-funktionen i nuläget. Även om detta skulle tas bort i framtiden är denna rad bra att ha för säkerhets skull; Om kortet inte ligger på planen ska det inte registreras som sådant. 


        // == Drar kort från kortlek AI:ns kortlek om AI har max 5 kort ute. == 
        int AI_korträknare = 0; // Räknar AI:ns kort
        List<GameObject> allaKort = new List<GameObject>();
        allaKort.AddRange(GameObject.FindGameObjectsWithTag("card"));

        foreach (GameObject kort in allaKort) // For-loop går igenom listan med alla kort. Om den uppfyller: Spelare==2 och bricka!=null ökar räknare med 1. 
        {
            KortetsScript KS_Loop = kort.GetComponent<KortetsScript>();
            if (KS_Loop.tillhörSpelare == 2 && KS_Loop.kortetsBricka != null) AI_korträknare++;
        }

        if (AI_korträknare < 5)
        {   
            // Gör så att det är en 50% chans att kortet läggs direkt på planen. 
            Descriptions D = new Descriptions();
            bool läggDirektPåPlan = D.RNG_PercentChance(50);
            if (läggDirektPåPlan) DrawCards(2, 1, false);
            else DrawCards(2, 1);


        }

            

        // Kollar Victory/Loss Condition
        if (KS.kortetsFärg == "Joker")//Om jokern skickas till graveyard ska spelet sluta 
        {   
            if (KS.tillhörSpelare == 2) // Om spelare 2:s joker dog vinner man, annars förlorar man.
            {
                GoToEndScreen(true);
            }
            else
            {
                GoToEndScreen(false);
            }
        }
        

    }

    //=====PLOCKA UPP KORT====
    public void PlockaUppKortet(GameObject kortetSomSkaPlockasUpp, GameObject brickanSomKortetLiggerI)//
    {
        KortetsScript KS = kortetSomSkaPlockasUpp.GetComponent<KortetsScript>();
        BrickansScript BS = brickanSomKortetLiggerI.GetComponent<BrickansScript>();

        MainGameData LS = GameObject.Find("Scripthållare").GetComponent<MainGameData>();

        KS.SkapaHighlight();
        KS.ärMarkerat = true;//1. Gör så att kortet är markerat (påverkar i nuläget endast if-sats-loopen då användaren försöker plocka upp kortet. 
        LS.markeradeKort.Add(kortetSomSkaPlockasUpp);//Lägger till kortet i listan med markerade kort.   
        
        
    }

   
    //=====LÄGG NER KORT====
    public void LäggNerKortet(GameObject kortetSomSkaLäggasNer, GameObject brickanSomKortetSkaLäggasI, bool skaBytaSpelare = true) 
    {
        KortetsScript KS = kortetSomSkaLäggasNer.GetComponent<KortetsScript>();
        BrickansScript BS_LIGGER = brickanSomKortetSkaLäggasI.GetComponent<BrickansScript>();
        //SpelarensScript SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        MainGameData LS = GameObject.Find("Scripthållare").GetComponent<MainGameData>();

        HanteraFörskjutningIBricka(kortetSomSkaLäggasNer);
        TaBortKortetUrBrickan(kortetSomSkaLäggasNer); //Tar bort kortet ur brickan som kortet LÅG i. (Detta motsvaras av det som tidigare låg i PlockaUppKort()-funktionen.

        // ==UNDANTAG: DRAWCARD==
        if (LS.markeradeKort.Contains(kortetSomSkaLäggasNer)) 
        {
            LS.markeradeKort.Clear();//Utan denna rad är kortet fortfarande markerat då det läggs ner. 
        }

        // ==UNDANTAG: CHANGE OF HEART==
        if (skaBytaSpelare) { KS.tillhörSpelare = BS_LIGGER.tillhörSpelare; }

        // == 1. Hanterar Highlight Sprite, 2. Hanterar Brickans kortlista, 3. Hanterar ärMarkerat,  4. Sätter kortet visuellt i brickan ==
        Destroy(KS.highlightSpriteL);//1. Förstör kortets highlight. Eftersom att det ENDA som "PlockaUppKortet()" gör med highlighten är att skapa den och sätta dess koordinater, är detta ALLT som behövs för hanteringen av kortets highlight. 
        BS_LIGGER.brickansKort.Add(kortetSomSkaLäggasNer);//2. Lägger kortet i kortets "brickansKort"-lista (så att brickan vet vilket kort som ligger på den). 
        KS.ärMarkerat = false;//3. Gör så att kortet inte längre är markerat (vilket i nuläget endast påverkar HIGHLIGHTSprite_Kortet - listan "MarkeradeKort" sköter ju resten) 
        KS.transform.position = BS_LIGGER.transform.position - new Vector3(0, 0, 1);//4. Lägger VISUELLT kortet i brickan. Anledningen varför "- new Vector3()" används är för att sätta kortet framför brickan för vår onMouseDown-funktion; Genom detta registrerar OnMouseDown alltid kortet istället för brickan då ett kort klickas.
                                                                                    //Ovanstående kan verifieras genom att ändra "- new vector" till "+ new vector". I detta fall kommer INGET kort att kunna klickas; istället kommer brickorna att ligga högst upp och blockera alla kort för musen.

        

        // == HANTERAR VARIABELN "liggerKortetÖverst" ==
        if (BS_LIGGER.brickansKort.Count != 0)
        {
            foreach (GameObject kort in BS_LIGGER.brickansKort)
            {
                kort.GetComponent<KortetsScript>().liggerKortetÖverst = false;
            }    
        }
        KS.liggerKortetÖverst = true;


        // == SKÖTER FÖRSKJUTNING ==
        förskjutningBricka = -(BS_LIGGER.brickansKort.Count - 1) * kortförskjutning;
        if (KS.tillhörSpelare == 2) { förskjutningBricka = förskjutningBricka * (-1); }     
        KS.transform.position += new Vector3(0, förskjutningBricka, 0);
        KS.transform.position -= new Vector3(0, 0, BS_LIGGER.brickansKort.Count);

        // == TILLDELAR HAND/PLAN    ,   TAR BORT FRÅN GRAVEYARD ==
        KS.handPlan = brickanSomKortetSkaLäggasI.GetComponent<BrickansScript>().handPlan;
        KS.kortetsBricka = brickanSomKortetSkaLäggasI;

        GameObject.Find("Scripthållare").GetComponent<MainGameData>().graveyard.Remove(kortetSomSkaLäggasNer);//Tar bort kortet från graveyard. Oftast då funktionen kallas kommer funktionen inte ligga i graveyard. Raden har då ingen effekt, men i övriga fall tillåter denna rad hela funktionen att användas som en "Resurrect"-funktion.


        // == HANTERAR LEDIGA HANBRICKOR/PLANBRICKOR ==
        TaBortFrånHandPlan(kortetSomSkaLäggasNer, brickanSomKortetSkaLäggasI);
        
        // == Uppdatera Healthbar ==
        if (KS.healthbarensLagringsvariabel)//Denna if-sats krävs eftersom att då programmet startar är healthbaren för korten inte definierade, så då denna funktion körs omedelbart vid start returnerar KS.UppdateraHealthBar ett error. Omedelbart EFTERÅT fungerar dock denna uppdatering. 
        {
            KS.UppdateraHealthBar();
        }


    }

    public void SlumpaKortetsPosition(GameObject kortetSomSkaSlumpas, int planenSomKortetSkaSkickasTill) // Funktion för att slumpa kortets position till någon bricka på spelarens plan. 
    {
        List<GameObject> ledigaBrickorLista = new List<GameObject>();
        KortetsScript KS = kortetSomSkaSlumpas.GetComponent<KortetsScript>(); 
        MainGameData LS = GameObject.Find("Scripthållare").GetComponent<MainGameData>();


        if (planenSomKortetSkaSkickasTill == 1)
        {
            ledigaBrickorLista = ledigPlanSpelare1;
        }
        else
        {
            ledigaBrickorLista = ledigPlanSpelare2;
        }

        if (ledigaBrickorLista.Count != 0)//Vi vill bara slumpa positionen om det finns lediga brickor kvar!
        {
            int slumpvariabel = UnityEngine.Random.Range(0, ledigaBrickorLista.Count);
            print("<X%X%X> Antalet lediga brickor på spelare " + KS.tillhörSpelare + ":s plan var:" + ledigaBrickorLista.Count);

            LäggNerKortet(kortetSomSkaSlumpas, ledigaBrickorLista[slumpvariabel]);
        }
        else
        {
            print("<X%X%X> Spelarens plan var FULL, så kortets position kunde inte slumpas.");
        }

        
    }


    public void LäggNerAllaMarkeradeKort() // Lägger ner alla kort. 
    {
        LäggNerKortet(LS.markeradeKort[0],
           LS.markeradeKort[0].GetComponent<KortetsScript>().kortetsBricka);
    }

    public void LäggKortetIKortleken(GameObject kortetSomSkaIDecken, int spelarensDeck)
    {
        KortetsScript KS = kortetSomSkaIDecken.GetComponent<KortetsScript>();
        if (spelarensDeck == 1)
        {
            KS.tillhörSpelare = 1;
            deck1.Add(kortetSomSkaIDecken);
        }
        else
        {
            KS.tillhörSpelare = 2;
            deck2.Add(kortetSomSkaIDecken);
        }


    }

    //=====FUNKTION SOM ALLMÄNT KAN ANVÄNDAS FÖR ATT PLOCKA KORT UR KORTLEKEN====
    public void DrawCards(int spelaren, int antalKort, bool läggIHanden = true)//Enkel att sköta eftersom att variablerna "ledigHandSpelare1/2" används för att hålla kolla på vilka kort i spelare 1:s hand som är lediga! 
    {   
        räknare = 0;

        //===VÄLJER UT VILKEN KORTLEK OCH VILKEN HAND SOM SKA ANVÄNDAS===
        if (spelaren == 1)
        {   
            deckenSomSkaVäljas = deck1;
            if (läggIHanden) handenSomSkaVäljas = ledigHandSpelare1; // Om argumentet läggIHanden = false läggs kortet istället direkt på planen! 
            else handenSomSkaVäljas = ledigPlanSpelare1;
        }
        else
        {   

            deckenSomSkaVäljas = deck2;
            if (läggIHanden) handenSomSkaVäljas = ledigHandSpelare2;
            else handenSomSkaVäljas = ledigPlanSpelare2;

        }
        
        //===DRAR SLUMPMÄSSIGA KORT UR KORTLEKEN OCH PLACERAR DEM I HANDEN===
        
        while (räknare < antalKort)
        {
            if (deckenSomSkaVäljas.Count != 0) // Kollar om det finns kort kvar i kortleken 
            {
                if (handenSomSkaVäljas.Count != 0) // Kollar om handen är full. 
                {
                    slumpatTalTal = UnityEngine.Random.Range(0, deckenSomSkaVäljas.Count);//Slumpar fram ett tal mellan 0 och längden av KORTLEKEN för SPELARE 1
                    GameObject kortetSomDras = deckenSomSkaVäljas[slumpatTalTal];
                    KortetsScript KS = kortetSomDras.GetComponent<KortetsScript>();

                    // === Hinder: Jokern får inte dras om det finns minst X antal kort kvar i kortleken === 
                    if (KS.kortetsFärg == "Joker" && deckenSomSkaVäljas.Count > 3)
                    {
                        // Lägg in en for-loop här som går igenom hela kortleken, och kollar kort för kort om dess färg är joker 
                        foreach (GameObject kort in deckenSomSkaVäljas)
                        {
                            KS = kort.GetComponent<KortetsScript>();
                            if (KS.kortetsFärg != "Joker")
                            {
                                kortetSomDras = kort;
                                break;

                            }
                            // Om vi kommer hit betyder det att ingen "non-joker" hittades, men då har vi redan valt ut ett kort "kortetSomDras = deckenSomSkaVäljas[slumpatTalTal]" ovan. 
                        }

                    }

                    LäggNerKortet(kortetSomDras, handenSomSkaVäljas[0]);
                    deckenSomSkaVäljas.Remove(kortetSomDras);
                }
                else print("Kortet kunde inte dras eftersom att handen verkar vara full");
            }
            else print("Kortet kunde inte dras eftersom att kortleken är tom!"); 

            räknare++;
        }


    }

    public void GoToEndScreen(bool victory) // Skickar spelaren till end-screen då matchen avslutas. victory är true om spelaren vunnit, annars false. 
    {   
        SpelarData_Mono SD_M = GameObject.Find("Scripthållare").GetComponent<SpelarData_Mono>();
        SD_M.LoadProgress();
        SD_M.endGameScreen = true;
        
        if (victory) SD_M.victory = true; 
        else SD_M.victory = false;

        SD_M.SaveProgress();
        SceneManager.LoadScene(0);
    }

    //=====SKAPA KNAPPAR====
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Y))
        {
            SlumpaKortetsPosition(GameObject.Find("Scripthållare").GetComponent<MainGameData>().markeradeKort[0], 1);
        }


        if (Input.GetKeyDown(KeyCode.B))
        {
            GoToEndScreen(true); 
            
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            GoToEndScreen(false);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            DrawCards(1, 1);
            print(PlayerPrefs.GetInt("Audio"));
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomiseDeck();
        }



    }

}
