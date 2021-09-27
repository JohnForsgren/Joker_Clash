using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AI_and_EnergyReset : MonoBehaviour {

    /*ÖVERGRIPANDE BESKRIVNING AV KLASSEN:
     
    Turknappen är knappen som startar spelet. Den heter "Turknappen" eftersom spelet tidigare var turbaserat, men inte ändrade namn när spelet blev realtidsbaserat. 
    Den hanterar: 
    - Reset av spelarnas energier, och dragande av kort ur kortleken (Gjordes tidigare i början av varje tur) 
    - Hela motståndarspelarens program, vilket inkluderar: 
        - AI:ns olika svårighetsgrader
        - AI:ns handlingar i spelet: 
             - Lägga ut kort 
             - Sikta 
             - Skjuta 
 
     */ 

    // Allmänna variabler 
    public bool harSpeletbörjat = false;
    int antalKortSomAnvänts; // Räknare som kollar hur många kort som attackerat under gällande sekvens. 

    public List<GameObject> allaKort = new List<GameObject>();
    public List<GameObject> exekveringslista = new List<GameObject>();
    public List<GameObject> exekveringslista2 = new List<GameObject>();

    public TextMeshProUGUI spelarensEnergi;

    // AI-variabler 
    public float delay;
    public float resetTid;
    public int AI_layCards; // Antal kort som AI:n :    1. drar ur kortleken i början av varje sekvens.    2. Lägger ut på planen. 
    public int AI_antalResetsPerSekvens; // Räknar hur många energy resets AI:n behöver för att köra om sin sekvens. 
    public int resetRäknare;
    public int returnedEnergyGemsOnReset; 

    // FÖR BAREN som mäter tiden 
    public RectTransform helaDurationBaren;
    public Coroutine baren; // Innehåller coroutinerna för de duration-bars som finns i spelet. 
    public Coroutine AI_sekvens_C; // Innehåller coroutinen för AI-sekvensen. 
    public RectTransform durationBar_RT; // RT står för "RectTransform" för att separera den från Coroutine (C). Denna Array innehåller alla rektanglar som är de duration bars man ser på skärmen (dessa är UI element och inte gameObjects) 
    public RectTransform durationBarFront_RT; // Innehåller främre delen av den fysiska duration-baren på skärmen. Det är denna som kontinuerligt förminskas för att uppdatera duration-baren. 


    private void Start()
    {
        resetRäknare = 1;
        resetTid = 10;
        returnedEnergyGemsOnReset = 10; 
        StartCoroutine(UppdateraEnergyGUI());

    }

    public IEnumerator UppdateraEnergyGUI()  // UI: Kontinuerligt uppdatering av spearens energi. 
    {
        SpelarensScript SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        spelarensEnergi.text = "Energy: " + SS.currentEnergy.ToString() + "\nGems : " + SS.currentGems.ToString();
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(UppdateraEnergyGUI());
    }


    private void OnMouseDown() // Startar spelet när knappen klickas- 
    {   
        if (!harSpeletbörjat) // Förhindrar att knappen klickas mer än 1 gång. 
        {
            Destroy(GetComponent<BoxCollider>()); // Tar bort boxcollidern så att knappen inte kan klickas igen
            GetComponent<SpriteRenderer>().sprite = null; // Gör knappen osynlig. 
            harSpeletbörjat = true; // ANvänds i kortetsScript för att göra korten oanvändbara innan matchen startat. 

            SpelarData_Mono SD_M = GameObject.Find("Scripthållare").GetComponent<SpelarData_Mono>();

            baren = StartCoroutine(KörDurationBar(resetTid, true));
            AI_sekvens_C = StartCoroutine(AI_Sekvens(1));

            MainGameData MGD = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
            if (SD_M.currentLevel <= 5) // Flytar baren så att det finns plats för tutourial-texten. 
            {
                durationBar_RT.transform.position += new Vector3(0, -2.5f, 0);

                MGD.tutourialState = 1;
                MGD.TutourialText();
            }
            else MGD.tutourialFrame.GetComponent<SpriteRenderer>().sprite = null;

        }
    }

    public IEnumerator KörDurationBar(float duration, bool firstTime = false) // Sköter uppdateringen av spelarnas energier. Samma funktion som kortens durationbar-funktion. 
    {
    float currentTid = 0f;
        if (firstTime)
        {
            durationBar_RT = Instantiate(helaDurationBaren) as RectTransform;
            durationBarFront_RT = durationBar_RT.GetChild(1).GetComponent<RectTransform>(); 
        }

        while (currentTid < duration) // Uppdaterar kontinuerligt barens utseende under processen
        {
            durationBarFront_RT.sizeDelta = new Vector2(durationBarFront_RT.sizeDelta.x, 1f - currentTid / duration);// 1-current/total är den formel som gör att baren hela tiden uppdateras korrekt. Detta för att om t.ex 40% av duration har gått är ct/d = 0.4
            currentTid += 0.1f;
            float mängdRöd = 255 * (1 - currentTid / duration);
            float mängdGrön = 255 * (currentTid / duration);
            //durationBarFront_RT.GetComponent<Image>().color = Color.blue;
            durationBarFront_RT.GetComponent<Image>().color = new Color32((byte)mängdRöd, (byte)mängdGrön, 0, 255);
            yield return new WaitForSeconds(0.1f);
        }

        durationBarFront_RT.sizeDelta = new Vector2(durationBarFront_RT.sizeDelta.x, 1); // Resettar baren
        baren = StartCoroutine(KörDurationBar(resetTid)); // Kör om en ny funktion
        EnergyReset(); // Resettar spelarens energi. 

        if (resetRäknare >= AI_antalResetsPerSekvens) // Kör AI:ns sekvens. 
        {
            StopCoroutine(AI_sekvens_C); // Stannar den gamla coroutinen. 
            AI_sekvens_C = StartCoroutine(AI_Sekvens(1));
            resetRäknare = 0;
            Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
            GDaB.FörstörAllaSiktare(2); // Förstör AI:ns siktare. 
            GDaB.DrawCards(2, AI_layCards);
        }
        else { resetRäknare++; }

    }


    public void SättDelayBeroendePåLevel(int level) // AI: Funktion som sätter takten i vilken AI:n spelar, beroende på leveln. 
    {
        float initialDelay = 3.2f; // Delay på level 0. Alla levels bör utgå från detta
        delay = initialDelay-0.1f*level;

        if (level <= 10)
        {
            AI_layCards = 1;// Sätter antalet kort som ska läggas och dras per omgång  
            AI_antalResetsPerSekvens = 3;  // Sätter antalet energyresets som går mellan varje AI-omgång. 
        }
        else if (level <= 20)
        {
            AI_layCards = 2;
            AI_antalResetsPerSekvens = 2; 
        }
        else if (level <= 25)
        {
            AI_layCards = 3;
            AI_antalResetsPerSekvens = 1;
        }
        else
        {
            AI_layCards = 4;
            AI_antalResetsPerSekvens = 1;
        }

    }

    public List<GameObject> ReturneraListaMedKort(int söktSpelare, string hand_plan_both)  // Skapar listor på kort för respektive spelare och respektive plan det ligger på. 
    {
        List<GameObject> söktaKort = new List<GameObject>();

        allaKort.Clear(); // Rensar listan eftersom att nedanstående rad lägger till massa kort i den. Utan Clear blir det massa bugs. 
        allaKort.AddRange(GameObject.FindGameObjectsWithTag("card"));

        foreach (GameObject card in allaKort) // Går igenom hela listan med kort för att plocka ur ett kort i spelare 2:s hand och lägga det på planen! 
        {

            KortetsScript KS = card.GetComponent<KortetsScript>();
            Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();

            if  (KS.tillhörSpelare == söktSpelare && KS.handPlan == hand_plan_both) // Om kortet tillhör rätt spelare och ligger på rätt ställe (Hand/Plan)...
            {
                söktaKort.Add(card); // Lägg till kortet. 
            }
            else if ((hand_plan_both == "Both" && KS.tillhörSpelare == söktSpelare )) // Om funktionen ska hitta BÅDE kort i handen och på planen ... 
            {
                if (KS.handPlan == "Hand" || KS.handPlan == "Plan") // .. och om kortet INTE ligger i deck...
                {
                    söktaKort.Add(card); // ... Så läggs kortet till. 
                }
            }
        }

        return söktaKort; // Returnerar listan med kort som tagits fram. 

    }

    public void AI_LäggKort() // AI: Gör så att AI:n lägger ut kort. 
    {   
  
        exekveringslista = ReturneraListaMedKort(2, "Hand");// Alla kort som ägs av spelare 2 och ligger i handen registreras. 

        foreach (GameObject card in exekveringslista) // Går igenom hela listan med kort för att plocka ur ett kort i spelare 2:s hand och lägga det på planen! 
        {

            KortetsScript KS = card.GetComponent<KortetsScript>();
            Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();

            if (KS.tillhörSpelare == 2 && KS.handPlan == "Hand") // Kortet som betraktas tillhör spelare 2 och ligger i handen läggs det på planen. 
            {
                    
                if (GDaB.ledigPlanSpelare2.Count != 0) GDaB.LäggNerKortet(card, GDaB.ledigPlanSpelare2[0]);// Hitta spelare 2:s lista med LEDIGA PLANBRICKOR - Men gör endast om den är ledig (dvs Count != 0) 
                
                //StartCoroutine(GDaB.SkickaTillGraveyard(card));
                break;
            } 
        }
    }

    public bool AI_IsCardStillUseable(GameObject kortet)
    {
        /*  Funktionen kollar om kortet vid den givna tidpunkten kan användas: 
            dvs har inte förstörts / satts till null, skickats till graveyard, eller tagits över av motståndaren 
            Om något av dessa villkor har skett förstörs kortets siktare och "false" returneras. 
            Notera att funktionen "FörstörSiktare" fungerar även om siktaren inte existerar (I detta fall gör funktionen ingenting). 
         */

        List<GameObject> spelare1Kort = ReturneraListaMedKort(1, "Both");
        if (spelare1Kort.Count == 0) return false; // Om spelare 1 inte har några kort kvar på planen returneras false, eftersom att AI:n inte har något att skjuta på.  

        if (kortet != null)
        {
            KortetsScript KS = kortet.GetComponent<KortetsScript>();

            if (CheckEffectsOnCard(kortet) == false) return false;

            if (KS.tillhörSpelare == 2 && KS.kortetsBricka != null) // KOllar om kortet forfarande ägs av AI:n (inte tagits över) och inte ligger i graveyard. 
            {
                return true;
            }

            else // Om kortet har tagits av spelare 1 eller skickats till graveyard ska siktaren förstöras. 
            {

                Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
                GDaB.FörstörSiktare(KS.kortetsSiktareLagring); 

            }

        }
        return false;
    }

    public bool CheckEffectsOnCard(GameObject kortet) // Returnerar true om kortet kan användas, annars false. 
    {
        KortetsScript KS = kortet.GetComponent<KortetsScript>();

        for (int X = 0; X < KS.aktivaEffekter.Length; X++) // For-loop som går igenom kortets effekter och kollar ifall det har något som hindar det från att aktivera sitt vapen. 
        {
            switch (KS.aktivaEffekter[X])
            {
                case "Stun":
                    Debug.LogError(KS.kortetsFärg + KS.kortetsTal.ToString());
                    return false;
            }
        }

        return true; 
    }

    public IEnumerator AI_Sekvens(int state) // AI: Main-sekvens: Sköter hela AI:ns tur.  
    {
        Descriptions D = new Descriptions();
        bool kanKortetFortfarandeAnvändas;

        switch (state)
        {
            case 1: // STATE = 1: LÄGGER UT KORT  
                int kortSomlagts = 0;
                

                while (kortSomlagts < AI_layCards)
                {
                    yield return new WaitForSeconds(delay);
                    AI_LäggKort();
                    kortSomlagts++;AI_and_EnergyReset
                }

                yield return new WaitForSeconds(delay*0.5f);
                AI_sekvens_C = StartCoroutine(AI_Sekvens(2));   
                break;

            case 2: // STATE = 2: ATTACKERAR


                // =============== VÄLJER UT ETT TILLGÄNGLIGT KORT =============== 
                exekveringslista = ReturneraListaMedKort(2, "Plan"); // Hittar alla kort som tillhör spelare 2 och ligger på planen (för att lägga ut dem)
                GameObject kortetSomSkaAnvändas = null; 
         
                for (int i = exekveringslista.Count - 1; i >= 0; i--)
                {
                    KortetsScript KS_loop = exekveringslista[i].GetComponent<KortetsScript>();
                    if (KS_loop.antalSkottKvar > 0) if (KS_loop.kortetsSiktareLagring == null || KS_loop.kortetsSiktareLagring.Equals(null)) if (CheckEffectsOnCard(exekveringslista[i]) == true) // Om kortet fortfarande har ammunition och inte redan skjuter
                        {
                        kortetSomSkaAnvändas = exekveringslista[i];
                        antalKortSomAnvänts++; 
                        break;
                    }
                }

                yield return new WaitForSeconds(delay);
                kanKortetFortfarandeAnvändas = AI_IsCardStillUseable(kortetSomSkaAnvändas);


                // =============== SKAPA SIKTARE =============== 
                if (kanKortetFortfarandeAnvändas)
                {
                    KortetsScript KS = kortetSomSkaAnvändas.GetComponent<KortetsScript>();
                    KS.SkapaKortetsSiktare();

                    KS.kortetsSiktareLagring.transform.parent = kortetSomSkaAnvändas.transform;

                    //print(" ###### AI: VÄNTAR - INNAN SIKTARE HITTAR TARGET");
                    yield return new WaitForSeconds(delay);
                }


                // =============== SIKTAR ===============
                kanKortetFortfarandeAnvändas = AI_IsCardStillUseable(kortetSomSkaAnvändas);
                if (kanKortetFortfarandeAnvändas)
                {
                    KortetsScript KS = kortetSomSkaAnvändas.GetComponent<KortetsScript>();
                    exekveringslista2 = ReturneraListaMedKort(1, "Both"); // Hitta ett kort som tillhör spelare 1 och ligger antingen i handen eller på planen.  

                    GameObject targetCard = null;

                    for (int i = 0; i < exekveringslista2.Count ; i++) // Går igenom listan med available targets i en for-loop 
                    {   

                        KortetsScript KS_target = exekveringslista2[i].GetComponent<KortetsScript>();
                        if (KS_target.health < KS_target.maxHealth) // Om kortet i listan är skadat ska det prioriteras (dvs target sätts då). Annars slumpas det fram i satsen efter. 
                        {
                            bool väljKortet = D.RNG_PercentChance(60);

                            if (väljKortet)
                            {
                                targetCard = exekveringslista2[i];
                                break;

                            }

                        }

                    }

                    if (targetCard == null) // Om inget skadat kort hittades i for-loopen ovan är det null och ska då slumpas fram. 
                    {
                        int random = Random.Range(0, exekveringslista2.Count); // Hittar en random position bland listan av kort som tagits fram. Notera att Random.Range(0,5) returnerar 0-4, INTE 0-5. ".Count-1" ska alltså inte användas. 
                        if (exekveringslista2.Count != 0) targetCard = exekveringslista2[random];
                    }

                    if (KS.kortetsSiktareLagring != null || KS.kortetsSiktareLagring.Equals(null) == false)
                    {
                        KS.kortetsAimTargetLagring.transform.position = targetCard.transform.position;
                        KS.kortetsAimTargetLagring.transform.parent = targetCard.transform;
                    }

                    yield return new WaitForSeconds(delay);
                }

                kanKortetFortfarandeAnvändas = AI_IsCardStillUseable(kortetSomSkaAnvändas);

                // =============== SKJUTER =============== 
                if (kanKortetFortfarandeAnvändas) // Är null om inget kort hittades. Då ignoreras denna rad och programmet går istället över till att avsluta hela sekvensen. 
                {
                    KortetsScript KS = kortetSomSkaAnvändas.GetComponent<KortetsScript>();

                    if (KS.kortetsSiktareLagring != null || KS.kortetsSiktareLagring.Equals(null) == false)
                    {
                        SiktarensScript SS = KS.GetComponent<KortetsScript>().kortetsSiktareLagring.GetComponent<SiktarensScript>();
                        SS.KollaOmVapnetKanAktiveras();
                    }

                    yield return new WaitForSeconds(delay*0.2f);   

                }

                // =============== IDENTIFIERAR OM DET FINNS FLER TILLGÄNGLIGA KORT - STARTAR ISÅFAL OMM PROCESSEN =============== 
                if (kortetSomSkaAnvändas != null) // Sålänge AI:n kan hitta ett kort på planen fortsätter vi att köra programmet. 
                {

                    AI_sekvens_C = StartCoroutine(AI_Sekvens(2)); 
                }
                else
                {
                    antalKortSomAnvänts = 0; // Resettar variabeln. 
                }

                break; 

        }
        yield return new WaitForSeconds(0.5f);

    }

    public void EnergyReset() // Återställer spelarnas energier och drar kort. Görs varje "Tur" i spelet (dvs genom den bar som finns i mitten på planen) 
    {
        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();
        Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();

        // === Återger energi / Cardpower === 
        SpelarensScript SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        SS.currentEnergy += returnedEnergyGemsOnReset;
        SS.currentGems += returnedEnergyGemsOnReset;
        if (SS.currentEnergy >= SS.maxEnergy) SS.currentEnergy = SS.maxEnergy; // Motverkar att max energy överskrids. 
        if (SS.currentGems >= SS.maxGems) SS.currentGems = SS.maxGems;

        // DRAR KORT
        GDaB.DrawCards(1, 1); // Drar 1 kort

        // ÅTERSTÄLLER AMMUNITION 
        if (GameObject.Find("Scripthållare").GetComponent<MainGameData>().kortSomHarSkjutit.Count != 0)
        {
            foreach (GameObject detKortSomSkjöt in GameObject.Find("Scripthållare").GetComponent<MainGameData>().kortSomHarSkjutit)
            {
                KortetsScript KS = detKortSomSkjöt.GetComponent<KortetsScript>();
                KS.antalSkottKvar = KS.maxAmmunition;//Alla kort som skjöt under förra turen får sin ammunition återställd. 

            }
            GameObject.Find("Scripthållare").GetComponent<MainGameData>().kortSomHarSkjutit.Clear();//Rensar listan eftersom att ovanstående for-sats endast ska köras för de kort som attackerats under [spelaren vars tur det var]'s tur. 
        }
    }
}
