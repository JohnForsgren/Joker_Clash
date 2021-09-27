using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KortetsScript : MonoBehaviour {

    /*ÖVERGRIPANDE BESKRIVNING AV KLASSEN:
     Klass som hanterar allt som relaterar till korten i spelet: 
        - Att markera och avmarkera kort, inklusive alla olika villkor som finns för i vilkea sammanhang kortet kan flyttas.  
         Allt som sker när man klickar på ett kort.  
        - Förändring av liv för korten: Korten har liv som indikerar hur mycket skada de kan ta innan de dör. 
        - Aktivering av vapen: Varje kort har ett vapen som det kan skjuta projektiler med. Klassen inkluderar att skapa "vapnena" som kortet skjuter med. Vapnena har sedan en egen klass för hur de fungerar. 
        - Aktivering av effekter [FÖR DEMO: Visa detta]  [Förklaring: Effekter är olika sorters "trollformler" som korten kan använda, exempelvis som gör dem starkare genom att skjuta fler projektiler]
        Detta inkluderar: 
	        - Hantering av "duration bars" som visar hur länge respektive effekt är aktiv. 
	        - Majoriteten av alla olika effekter som finns i spelet sköts av denhär klassen. 
     
     */

    // Generella Variabler
    public GameObject dethärKortet; // Alternativ till "this.gameObject"
    public GameObject kortetsBricka;
    public GameObject detMarkeradeKortet;
    public List<GameObject> variabelFörMarkeradeKortLista = new List<GameObject>();//Lagringsvariabel för scriptet "MarkeratKortLista".
    public List<GameObject> markeradeKort = new List<GameObject>();

    public GameObject healthBar;
    public GameObject healthbarensLagringsvariabel;
    public GameObject kortetsSiktare;
    public GameObject kortetsSiktareLagring;
    public GameObject kortetsAimTarget;
    public GameObject kortetsAimTargetLagring;
    public GameObject range;//Range är ett GO som används för AoE-effekter för att visa hur stor range de har. 

    public RectTransform helaDurationBaren; //Här läggs Duration-bar (dvs UI-objektet av samma namn i Duration-bar UI:n) till.
    public RectTransform foregroundDurationBar; 


    public bool ärMarkerat = false;//Krävs för att programmet ska veta att brickansKort markerats om det klickats. 
    public bool harEnAllieradBrickaKlickats;
    public bool liggerKortetÖverst;

    public string handPlan;
    public string kortetsFärg;
    public string kortetsVapen;

    // Effekter 
    public string[] aktivaEffekter = new string[5]; //Innehåller STRÄNGEN med de effekter som korten har på sig. 
    public Coroutine[] aktivaEffekter_C = new Coroutine[5]; //Sköter de faktiska effekterna (t.ex healing breeze som healar kortet kontinuerligt) 
    public Coroutine[] aktivaDurationBars = new Coroutine[5]; // Innehåller coroutinerna för de duration-bars som finns i spelet. 
    public RectTransform[] durationBar_RT = new RectTransform[5]; // RT står för "RectTransform" för att separera den från Coroutine (C). Denna Array innehåller alla rektanglar som är de duration bars man ser på skärmen (dessa är UI element och inte gameObjects) 
    public RectTransform[] durationBarFront_RT = new RectTransform[5]; // Innehåller främre delen av den fysiska duration-baren på skärmen. Det är denna som kontinuerligt förminskas för att uppdatera duration-baren. 

    public int kortetsTal;//Kortets tal.
    public int tillhörSpelare;
    public float antalSkottKvar;
    public int maxAmmunition;
    public int counter;

    public float health;
    public float maxHealth;
    public float HPFörskjutning; 

    public int effektNivå; /* I nuläget används effektnivå för att indikera om effekten för kortet är upplåst (om effektnivån för ett kort är 0 kan effekten inte användas). Dock borde detta INTE
    behövas eftersom att kortet antingen ska ha en effekt eller inte (binärt state) och därför kan man istället kolla "if(kortetsEffekt != null) */

    public string effekt;

    string resultatVidKlick;

    public GameObject highlightSprite;//Variabeln för highlightikonen som visar att brickansKort är markerat. 
    public GameObject highlightSpriteL;//Sprite_Kortet måste av någon anledning lagras i en separat variabel. Gör den inte det fungerar t.ex inte Destroy().
    public GameObject EffectHighlight;
    public GameObject EffectHighlightL;

    public SpelarensScript SS;
    
    public void Start()
    {
        for (int X = 0; X < aktivaEffekter.Length; X++)//Detta borde lösa problement med att programmet inte ursprungligen identifierar att alla brickor har värdena null. Allt som krävs är att funktionen som TAR BORT effekterna också sätter positionens värde till null. 
        {
            aktivaEffekter[X] = null;
            aktivaDurationBars[X] = null;
            durationBar_RT[X] = null;
            durationBarFront_RT[X] = null;

        }

        SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        effektNivå = 2;
        GameObject healthBarensVariabel = Instantiate(healthBar) as GameObject;
        healthbarensLagringsvariabel = healthBarensVariabel;//Lagrar variabeln så att den kan användas utanför startfunktionen. 
        UppdateraHealthBar();//Omedelbart då spelet startar är GDaB inte kapabel att ändra på denna position eftersom att healthbaren inte än skapats. Därför behövs denna rad!
        antalSkottKvar = maxAmmunition;//Tilldelar skottet sin amminition 
        
    }

    public bool KollaStunEffekter() // Om kortet har en effekt "stun" returneras true. 
    {

        for (int X = 0; X < aktivaEffekter.Length; X++)
        {
            switch (aktivaEffekter[X])
            {
                case "Stun":
                    return false; ;

            }
        }
        return true;
    }

    public void ChangeLife(float ändring, string typAvSkada = "Untyped", GameObject kortetSomSkjöt = null) // Viktig funktion: Hanterar all förändring i liv för kortet, dvs både skada och "healing". 
    {
        Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
        KortetsScript KS_somSkjöt;
        Descriptions D = new Descriptions();

        for (int X = 0; X < aktivaEffekter.Length; X++)
        {
            bool chance;
            /*Fråga: Ska vi bryta ur hela for-satsen då en effekt hittas? Svar: Nej, eftersom att alla effekter ska exekveras (t.ex både Contagion och Soul Barbs). 
             Eftersom att switch satsen bara är en sträng i taget kommer effekten som appliceras endast att exekveras en gång.  */
            switch (aktivaEffekter[X])
            {
                case "Protection":
                    chance = D.RNG_PercentChance(75);
                    if (typAvSkada == "Physical" && chance)//Det är 75% chans att ett slumpat tal mellan 1 och 4 INTE är 1. 
                    {
                        ändring = 0; // Gör så att ingen skada ges.
                    }

                    break;
                case "There Can Be Only One":
                    if (typAvSkada == "Physical" && kortetSomSkjöt != null) // Eftersom vi kan ha SKILLS som gör physical damage kommer vi då inte ha något "kortetSomSkjöt" trots att vi delar ut physical damage.
                    {
                        KS_somSkjöt = kortetSomSkjöt.GetComponent<KortetsScript>();
                        if (KS_somSkjöt.kortetsFärg == kortetsFärg) ändring = ändring * (D.SkillEffect(aktivaEffekter[X]) + 100f) / 100f;

                    }
                    break;
                default:
                    break;

            }


        }

        if (kortetSomSkjöt != null) // Går igenom eventuella effekter som påverkar kortet som skjöt.  
        {   
            KS_somSkjöt = kortetSomSkjöt.GetComponent<KortetsScript>();

            for (int X = 0; X < KS_somSkjöt.aktivaEffekter.Length; X++)
            {
                switch (KS_somSkjöt.aktivaEffekter[X])
                {
                    case "Rage":
                        if (typAvSkada == "Physical" && KS_somSkjöt.kortetsFärg == "Spader") ändring = ändring * ( D.SkillEffect(KS_somSkjöt.aktivaEffekter[X]) + 100f ) / 100f ;
                        break;

                    case "Strength to the Powerless":
                        if (typAvSkada == "Physical" && kortetSomSkjöt != null)
                        {
                            KS_somSkjöt = kortetSomSkjöt.GetComponent<KortetsScript>();
                            if (KS_somSkjöt.kortetsTal == 1) ändring -= D.SkillEffect(KS_somSkjöt.aktivaEffekter[X]);
                        }

                        break;

                    default:
                        break;

                }
            }

            if (KS_somSkjöt.tillhörSpelare == 2) // Modifierare av damage för AI:n: Vid lägre nivåer skadar motståndaren mindre, och vid högre nivåer skadar den mer. 
            {
                if (GDaB.currentLevel <= 3) // De första 3 levlerna skadar AI:n mindre så att tutourial blir lättare
                {
                    ändring = 0.5f * ändring;
                }
                else if (GDaB.currentLevel > 3 && GDaB.currentLevel <= 10) // 50% mer skada fram till level 10
                {
                    // Samma skada. 
                }
                else if (GDaB.currentLevel > 11 && GDaB.currentLevel <= 20) // Mellan 11 och 20 skadar AI 50% mer. 
                {
                    ändring = 1.5f * ändring;
                }
                else if (GDaB.currentLevel > 20 && GDaB.currentLevel <= 25) // Mellan 11 och 20 skadar AI 100% mer. 
                {
                    ändring = 2f * ändring;

                }
                else // Efter level 25 skadar AI 150% mer 
                {
                    ändring = 2.5f * ändring;
                }
            }
        }

        health += ändring;

        if (health > maxHealth) health = maxHealth;

        if (health <= 0 && kortetsBricka != null)//Om kortetsBricka är null betyder det att kortet ligger i graveyard. Vi vill då inte skicka den till graveyard igen. 
        {
            StartCoroutine(GDaB.SkickaTillGraveyard(this.gameObject));
        }
        UppdateraHealthBar();
    }

    public void SättHealth(float nummerAttSättaTill)
    {
        Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
        health = nummerAttSättaTill;

        if (health <= 0)
        {
            GDaB.SkickaTillGraveyard(this.gameObject);
        }

        else if (health > maxHealth) //Vi kan inte få mer liv än maxHealth. 
        {
            health = maxHealth; 
            UppdateraHealthBar();
        }


    }



    public void SkapaKortetsSiktare() // Skapar kortets siktare, dvs "vapnet" som spelaren siktar med för att sedan skjuta. 
    {
        GameObject siktarensKopia = Instantiate(kortetsSiktare) as GameObject;
        kortetsSiktareLagring = siktarensKopia;
        kortetsSiktareLagring.GetComponent<SiktarensScript>().transform.position = transform.position; //Sätter siktarens pistion till kortets. 
        kortetsSiktareLagring.GetComponent<SiktarensScript>().kortetSomSkaSkjuta = dethärKortet; //Motverkar att kortet skjuter sig själv (se SkottetsScript). 
        GameObject.Find("Scripthållare").GetComponent<Main_Class>().aktivaSiktare.Add(kortetsSiktareLagring);//Denna variabel används för att globalt göra programmet medveten om att en siktare finns (och att t.ex kort och knappar då temporärt inte får aktiveras)

        GameObject AimTargetkopia = Instantiate(kortetsAimTarget);//Skapar markören som används för att sikta. 
        AimTargetkopia.GetComponent<AimTargetScript>().owningCard = this.gameObject; // Gör så att AimTarget vet vilket kort det tillhör. 
        kortetsAimTargetLagring = AimTargetkopia;
        AimTargetkopia.transform.position = transform.position;// Denna rad har ingen påverkan eftersom positionen sköts i SIKTARENS script (i start). Raden finns dock ifall att. 


    }

    public void UppdateraHealthBar() //Funktionen gör det betydligt mindre rörigt att uppdatera health-baren från ANDRA script. Utan denna funktion måste dessa script hämta massor av variabler: healthBarLagring, health, kortets tal och kortet. 
    {
        healthbarensLagringsvariabel.transform.localScale = new Vector3(1f * health / maxHealth, 1, 1);
        if (tillhörSpelare == 1) { HPFörskjutning = -1; }
        else                     { HPFörskjutning = 1; }
        healthbarensLagringsvariabel.transform.position = transform.position + new Vector3(0, HPFörskjutning*0.53f, 1);

    }

    public void SkapaEffectHighlight() // Skapar en ram runt kortet som indikerar att en effekt håller på att aktiveras. 
    {
        GameObject EffectHighlight = Instantiate(dethärKortet.GetComponent<KortetsScript>().highlightSprite) as GameObject;
        EffectHighlightL = EffectHighlight;
        EffectHighlight.transform.position = transform.position;
        Sprite_Highlight SH = EffectHighlight.GetComponent<Sprite_Highlight>();
        SH.objektetsSprite.sprite = SH.effectSprite;
    }

    public void SkapaHighlight() // Skapar en ram runt kortet som visar att det är markerat. 
    {
        GameObject Sprite_KortetsInstantiate = Instantiate(dethärKortet.GetComponent<KortetsScript>().highlightSprite) as GameObject;//Skapar highlightikonen
        highlightSpriteL = Sprite_KortetsInstantiate;//Krävs för att highlight-Sprite_Kortet ska kunna hanteras i update. (Det går t.ex INTE att skriva kopia.transform.position = transform.position; i update.) 
        highlightSpriteL.transform.position = transform.position;//Sätter highlightens koordinater till kortets. 
    }

    public void SkapaRange(GameObject markeratKort, string effektensNamn) //Range är ett gameObject som används för AoE-effekter för att visa hur stor range de har. 
    {
        GameObject range_Instantiate = Instantiate(range) as GameObject;
        AoE AoE = range_Instantiate.GetComponent<AoE>(); 
        AoE.ägandeKort = markeratKort.gameObject;
        AoE.transform.position = this.transform.position; 
        AoE.typAvEffekt = effektensNamn; 
        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();
        EKS.AvmarkeraNonInstantEffect();
    }

    public void ActivateInstantEffect(string effekten) // Aktivering av de effekter som har omedelbar verkan. 
    {
        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<EffektKnappensScript>();
        Main_Class GDAB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
        Descriptions D = new Descriptions();

        switch (effekten)
        {
            case "Draw Card"://(instant) - Drar 1 kort.
                GDAB.DrawCards(tillhörSpelare, (int)D.SkillEffect(effekten));//Andra argumentet är antal kort som dras. 
                break;
            case "Restoration"://(instant) - Ger kortet fullt liv.
                ChangeLife(D.SkillEffect(effekten));
                break;
            case "Signet of Stamina":
                DurationEffect_Add(effekten);
                break;
            case "Protection":
                SkapaRange(this.gameObject, effekten);
                break;
            case "Rage":
                DurationEffect_Add(effekten);
                break;
        }
    }


    public void SkrivUtEffekter() // Endast för debugging: Skriver ut vilka effekter kortet har (förutsatt att kortet har en effekt)
    {
        string effekterSomPåverkarKortet = " ";

        foreach (string effektF in aktivaEffekter)
        {
            if (effektF != null) effekterSomPåverkarKortet += effektF + " ";//Lägger till kortet i listan på effekter som det påverkas av, plus ett mellanslag. 
        }
    }

    // ÖVERSIKT ÖVER EFFEKT-APPLICERING:
    /*
        = DurationEffect_AddI(): Hanterar endast strängen med kortets effekter. 
            = Om effekten redan finns återappliceras den.
            = Om effekten inte finns appliceras den. Appliceringen görs indirekt genom AppliceraDurationEffect().

        = AppliceraDurationEffect(): Funktionen sköter fenomen som händer i samband med att effekten appliceras, och applicerar sedan indirekt duration-effekten genom att köra StartaEffekt(): 
            = Funktionen triggar "vid applicering"-effekter såsom Soul Barbs 
            = Funktionen bestämmer effektens duration baserat på effektens typ, spelarens attributes samt aktiva effekter (såsom Extend Blessing)

        = StartaEffekt()
            = Funktionen applicerar effekten
         
         */

    public void DurationEffect_Add(string effekt_F)
    {

        // === Tutourial ===
        MainGameData MGD = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        if (MGD.tutourialState == 4 || MGD.tutourialState == 5) // "Begin by selecting a card in your hand"
        {
            MGD.tutourialState = 5;
            MGD.tutourialSubState = 4; 
            MGD.TutourialText();
        }
        // =================

        bool effektenFannsRedan = false;
        print("========== Effekten " + effekt_F + " skulle läggas till");

       
        for (int P = 0; P < aktivaEffekter.Length; P++) //Går igenom listan för att kolla om kortet redan har effekten på sig
        {
            if (aktivaEffekter[P] == effekt_F)
            {
                print("EFFEKTEN FANNS REDAN och måste därför förnyas.");
                StopCoroutine(aktivaEffekter_C[P]);
                StopCoroutine(aktivaDurationBars[P]);
                AppliceraDurationEffect(effekt_F, P, true);
                effektenFannsRedan = true;//For-loopen nedan som startar NYA funktioner körs inte om denna variabel är true. 
                break;
            }
        }


        /*
        = Om effekten INTE redan existerar ska den:
            = läggas in i listan 
            = dess IEnumerator ska köras
            = IEnumeratorn måste lagras i en Coroutine-Array. Smidigast är garanterat om programmet struktureras så att denna Coroutine-Arrayen hela tiden är konsistent med aktivaEffekter, så
            om t.ex effekten "Nightmare" är aktiv i poisition 2 ligger Coroutinen för denna i position 2 i Coroutine-vektorn. 
            */
        if (effektenFannsRedan == false)
        {
            for (int X = 0; X < aktivaEffekter.Length; X++)//Går igenom listan...
            {
                if (aktivaEffekter[X] == null)//... och hittar första positionen där det finns en ledig plats. (Sedan breakar den)
                {
                    print("position " + X.ToString() + " var tom.");
                    AppliceraDurationEffect(effekt_F, X, false);
                    break;//Man kan se då man markerar break att "for" ovan markeras, så loopen bryts ifall vi hittat en ledig ruta. 
                }
            }
        }
    }

    public void AppliceraDurationEffect(string effektenSomSkaAppliceras, int Pos, bool renew = false)
    {
        Descriptions D = new Descriptions();
        float effektensDuration = D.SkillDuration(effektenSomSkaAppliceras);

        // === Aktiverar effekter som appliceras då duration-effects appliceras, såsom Soul Barbs ===
        for (int X = 0; X < aktivaEffekter.Length; X++)
        {
            switch (aktivaEffekter[X])
            {
                case "Soul Barbs":
                    if (effekt != "Soul Barbs") ChangeLife(-3);
                    break;

                case "Extend Duration":
                    effektensDuration += D.SkillEffect(aktivaEffekter[X]);
                    break;

                default:
                    break;
            }
        }

        // === Sätter durations och (EVENTUELLT) initial effects baserat på effekt-strängen ===

     

        // === Applicerar effekten baserat på den sträng och duration som sats === 
        aktivaEffekter[Pos] = effektenSomSkaAppliceras;

        if (renew == false) aktivaEffekter_C[Pos] = StartCoroutine(StartaEffekt(effektenSomSkaAppliceras, effektensDuration, Pos)); // VIKTIG RAD: Lagrar Coroutinen på samma position i aktivaCoroutiner (som motsvarande pos i aktivaEffekter), OCH kör coroutinen (i detta fall UTAN initial effect). 
        else aktivaEffekter_C[Pos] = StartCoroutine(StartaEffekt(effektenSomSkaAppliceras, effektensDuration, Pos, true));
    }


    public string RipEffect(int antalEffekterSomSkaTasBort, string typ = "any") //Tar bort effekten i förtid. 
    {
        int counter = 0;
        bool isAnEffectRemoved = false; // Om en effekt INTE tas bort av funktionen ska funktionen returnera false. Jag har valt att skriva detta som en sträng ifall annan data också ska betraktas. 
        
        while (counter < antalEffekterSomSkaTasBort)//While sats som gör att nedanstående repeteras lika många gånger som antalet effekter som ska tas bort (Strip Enchantment tar t.ex bort 2 st direkt)
        {
            int sistaPosition = 0; 
            for (int X = 0; X < aktivaEffekter.Length; X++)//Går i genom listan för att hitta den sista positionen i lista. Denna tas sedan bort. 
            /*Notera att detta INTE är konsistent med Guild Wars: I guild wars tas alltid den "most recently applied effect bort" först. Detta gäller inte här om t.ex en effekt som ligger
             INNAN sista effekten expirar, eftersom att detta medför att en position i effectarrayn blir tom. Om en ny effekt läggs in i denna lucka blir det INTE den som tas bort i denna
             funktion eftersom att den - trots att den applicerades nyligen - inte ligger sist i effect-arrayn. Detta är dock inte nödvändigtvis ett problem. */
            {
                if (aktivaEffekter[X] != null) sistaPosition = X;    
            }

            if (aktivaEffekter[sistaPosition] != null)
            {
                // >>> TILLÄGG: Då den sista rutan hittas ska vi KOLLA ifall denna ruta är av effekten "typ" (se funktionens argument). Om det t.ex är en blessning och en curse ska tas bort, händer INGENTING: 
                DurationEffect_Remove(sistaPosition); // If-påståendet hindrar effekten att tas bort om den inte finns. På så sätt krashar inte funktionen om kortet har färre effekter än vad som ska tas bort. 
                isAnEffectRemoved = true;
            }
            counter++;

        }
        if (isAnEffectRemoved) return "true";
        else return "false";
    }

    public void DurationEffect_Remove(int P) // Avslutar duration effect. 
    {
        EndEffects(aktivaEffekter[P]);
        if (aktivaEffekter_C[P] != null) StopCoroutine(aktivaEffekter_C[P]);

        if (aktivaDurationBars[P] != null) StopCoroutine(aktivaDurationBars[P]);
        if (durationBar_RT[P] != null) Destroy(durationBar_RT[P].gameObject);//Förstör duration baren. Detta görs även direkt i StartaEffekt()-Ienumeratorn vid det eventuellt fallet att effekten uppdateras. 
        
        aktivaEffekter[P] = null;
    }

    


    public IEnumerator StartaEffekt(string effekt, float duration, int P, bool renew = false) // P = position i listan. Denna position är för varje effekt samma i alla 4 vektorerna: aktivaEffekter_C, aktivaDurationBars, durationBar_RT, durationBarFront_RT.
    {
        
        /**
        Strukturen för själva effekt-funktionen: 
            = Skapa duration bar och uppdatera den i enlighet med dess duration. Detta görs av den separata funktionen "KörDurationBar()". 
            = Kör initial effect (om tillämpbart) - dvs gå igenom en switch-sats där alla effekter som har initial effects är med. Om effekten inte finns där händer inget. 
            = Kör kontinuerlig effekt
                = KOM IHÅG att flera effekter såsom Protector's Defence INTE har någon kontinuerlig uppdatering som körs i IENUMERATORN; Effekten baseras i detta fall istället på att man i samband med att man i 
                ChangeLife-funktionen har en switch-sats som tar reda på ifall kortet innehåller effekten.       
        **/
        float currentTid = 0f;

        Descriptions D = new Descriptions();
       
        if (renew == false) // Triggar effekter som har en initial effekt. 
        {
            switch (effekt)
            {
                case "Veil of Thorns":
                    break;

                case "Immolate":
                    if (kortetsFärg == "Klöver") ChangeLife(-D.SkillEffect(effekt, 1));

                    break;
                case "Fire Blast":

                    ChangeLife(-D.SkillEffect(effekt, 1));
                    break;

                case "Ammo Chart":
                    antalSkottKvar += (int)D.SkillEffect(effekt);
                    maxAmmunition += (int)D.SkillEffect(effekt); //VARNING: Eftersom att denna varaibel INTE skickas vidare vet inte end-effeckt funktionen hur mycket vi minskade 
                   // ...ammunitionen med. Därför kan det lätt orsakas bug om jag ändrar denna rad men glömmer ändra motsvarande rad som återställer ammunitionen i end-effekten. 
                    break;
                case "Signet of Stamina":
                    //VARNING: Samma som Ammo Chart: Eftrsom att health här är en lokal varaibel kan det lätt leda till bug om det ska ändras i motsvarande rad som återställer health i end-funktionen. 
                    maxHealth += D.SkillEffect(effekt);
                    ChangeLife(D.SkillEffect(effekt));
                    break;

                default:
                    print("Kortet saknade initial Effect");
                    break;
            }
        }

        if (health > 0 && kortetsBricka != null)  // Om kortet inte skickades till graveyard av initial-effekten: Starta effekten.  
        {

            if (durationBar_RT[P]) Destroy(durationBar_RT[P].gameObject);//Förstör (om tillämpbart) den tidigare duration-baren som representerade effekten.
            aktivaDurationBars[P] = StartCoroutine(KörDurationBar(duration, P, effekt)); // 

            while (currentTid <= duration) // KOR EVENTUELL DURATION EFFECT
            {
                switch (effekt)
                {
                    case "Immolate":
                        currentTid++;
                        yield return new WaitForSeconds(1f);
                        ChangeLife(-D.SkillEffect(effekt, 2));
                        break;
                    case "Fire Blast":
                        currentTid++;
                        yield return new WaitForSeconds(1f);
                        ChangeLife(-D.SkillEffect(effekt, 2));
                        break;

                    case "Healing Breeze":
                        currentTid++;
                        yield return new WaitForSeconds(1f);
                        ChangeLife(1);
                        break;

                    // === EFFEKTER UTAN KONTINUERLIG EFFEKT (t.ex effekter med enbart end-effects, eller effekter som Protector's Defence eller Extend Duration som triggas på ett annat ställe än här) ===
                    case "Patient Spirit":
                    case "Soul Barbs":
                    case "Protection":
                    case "Rage":
                    case "Ammo Chart":
                    case "There Can Be Only One":
                    case "Extend Duration":
                    case "Signet of Stamina":
                    case "Strength to the Powerless":
                    case "Stun":
                    case "Reckless Haste":
                    case "Barrage":
                        currentTid++;
                        yield return new WaitForSeconds(1f);//Har endast en end-effect
                        break;

                    default:
                        currentTid++;
                        Debug.LogError("Denna funktion ska inte köras om det inte finns en effekt, eftersom att funktionen ovillkorligen startar duration-baren (även om effekt saknas)"); 
                        yield return new WaitForSeconds(0f);
                        break;
                }
            }
        }

        //Hittar positionen för effekten. Detta för att kunna lägga in positionen i den funktion som tar bort effekten. 
        int effektensPosition = 0;
        for (int i = 0; i < aktivaEffekter.Length; i++)//Går igenom listan för att kolla om kortet redan har effekten på sig
        {
            if (aktivaEffekter[i] == effekt)
            {
                print("EFFEKTEN FANNS REDAN och måste därför förnyas.");
                effektensPosition = i;
                break;
            }
        }
        DurationEffect_Remove(effektensPosition);

    }


    public IEnumerator KörDurationBar(float duration, int P, string effekt) // Hantering av duration bar, baren på sidan av kortet som körs då effekten aktiveras. 
    {
        float currentTid = 0f;
        // Skapar duration baren och sätter parent, position, etc 
        durationBar_RT[P] = Instantiate(helaDurationBaren) as RectTransform; 
        durationBar_RT[P].transform.position = this.transform.position + new Vector3(0.5f + 0.1f*P, 0, 0);
        durationBar_RT[P].transform.SetParent (this.transform); 
        durationBarFront_RT[P] = durationBar_RT[P].GetChild(1).GetComponent<RectTransform>(); // VIKTIGT: Detta plockar ut BARNET (child) av ETT PREFAB. I detta fall krävs GetComponent<RectTransform>() eftersom 

        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();
        Descriptions D = new Descriptions();
        string typAvEffekt = D.SkillType(effekt); 

        switch (typAvEffekt)//Sätter färg på duration baren beroende på effektens typ. 
        {
            case "B":
                durationBarFront_RT[P].GetComponent<Image>().color = Color.blue;
                break;
            case "C":
                durationBarFront_RT[P].GetComponent<Image>().color = Color.red;
                break;

        }
        
        while (currentTid < duration)
        {
            durationBarFront_RT[P].sizeDelta = new Vector2(durationBarFront_RT[P].sizeDelta.x, 1f - currentTid / duration);// 1-current/total är den formel som gör att baren hela tiden uppdateras korrekt. Detta för att om t.ex 40% av duration har gått är ct/d = 0.4
            currentTid += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        durationBarFront_RT[P].sizeDelta = new Vector2(durationBarFront_RT[P].sizeDelta.x, 1f - currentTid / duration); // Uppdaterar sista framen så att det inte ligger något kvar av den sista biten av duration-baren. 
    }

    public void EndEffects(string typAvEffektF)
    /*Denna funktion fungerar bra, men det kanske finns fördelar med att använda en inbyggd funktion som triggas då en IEnumerator slutar. */
    {
        Descriptions D = new Descriptions();

        switch (typAvEffektF)//END EFFECTS
        {
            case "Patient Spirit":
                ChangeLife(5);
                break;

            case "Ammo Chart":
                maxAmmunition -= (int)D.SkillEffect(typAvEffektF);
                if (antalSkottKvar > maxAmmunition) antalSkottKvar = maxAmmunition;
                break;
            case "Signet of Stamina":
                maxHealth -= D.SkillEffect(typAvEffektF);
                ChangeLife(-D.SkillEffect(typAvEffektF));
                if (health > maxHealth) SättHealth(maxHealth);
                break;

            default:
                print("En effect avslutades. End-effect saknades.");
                break;
        }

    }


    public void OnMouseDown() // Hanterar klick på kortet 
    {

        // === Tutourial === 
        MainGameData MGD = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        if (MGD.tutourialState == 1 && tillhörSpelare == 1) // "Begin by selecting a card in your hand"
        {
            MGD.tutourialState = 2;
            MGD.TutourialText();
        }
        else if (MGD.tutourialState == 3 && tillhörSpelare == 1 && handPlan == "Plan") // "When the card is on the field, it can use its Skill or Weapon. Select the card again to use it."
        {
            MGD.tutourialState = 4;
            MGD.TutourialText();
        }


        // Start av faktiskta kort-funktionen
        print("======= Ett kort klickades =======");
        print("Kortet tillhör spelare " + tillhörSpelare + " och ligger i " + handPlan + ". Antal liv: " + health.ToString() + ". Antal skott: " + antalSkottKvar.ToString() + " Effekt: " + effekt);
        SkrivUtEffekter();

        // Uppdaterar "SelectedCard" så att spelaren kan avläsa data från det. 
        Sprite_Kortet SC_SelectedCard = GameObject.Find("SelectedCard").GetComponent<Sprite_Kortet>();
        SC_SelectedCard.RitaKortet(kortetsTal, kortetsFärg);
        SC_SelectedCard.GetComponent<SelectedCard_MG>().UppdateraGUI(this.gameObject);

        AI_and_EnergyReset TKS = GameObject.Find("StartMatchButton").GetComponent<AI_and_EnergyReset>();
        if (TKS.harSpeletbörjat == false)
        {
            print("Kortet klickades, men matchen har inte än börjat");
            return; // Vi bryter från funktionen direkt om matchen inte blrjat - då kan vi inte plocka upp kortet. 
        }

        harEnAllieradBrickaKlickats = false;//Variabeln ska vara false by default.
        if (tillhörSpelare == 2)
        {
            harEnAllieradBrickaKlickats = true;
        }

        //======= EN AKTIV EFFEKT ÄR AKTIVERAD =======
        EffektKnappensScript EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();
        Main_Class GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
        SpelarensScript SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        MainGameData LS = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        Descriptions D = new Descriptions();

        bool enemyJokerclicked = false;

        if (kortetsFärg == "Joker")
        {
            if (tillhörSpelare == 2) enemyJokerclicked = true;
        }
 
        if (EKS.initieradAktivEffekt != null)
        {   

            if (true) // Tidigare "enemyJokerclicked == false" för att motverka att effekter används på jokern! 
            {
                KortetsScript KS_förMarkeratKort = EKS.kortetSomVarMarkerat.GetComponent<KortetsScript>();
                print("<X%X%X> En Non-InstantEffect var aktiv.");
                string switchEffekt = EKS.kortetSomVarMarkerat.GetComponent<KortetsScript>().effekt; 
                switch (switchEffekt)
                {
                    // === INSTANT EFFECTS ===   (dvs effekter som appliceras direkt på kortet; dessa är dock non-instant effects i effektKnappensScript eftersom de kräver ett target) 
                    case "Reap Life":
                        if (kortetsFärg == "Klöver")
                        {
                            StartCoroutine(GDaB.SkickaTillGraveyard(dethärKortet));    
                            EKS.AvmarkeraNonInstantEffect();
                        }
                    
                        break;
                    case "Word of Agony":
                        if (health > 0.5f * maxHealth) ChangeLife( -D.SkillEffect(switchEffekt, 1) ); 
                        else ChangeLife( -D.SkillEffect(switchEffekt, 2) );
                        EKS.AvmarkeraNonInstantEffect(this.gameObject, false);// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                        break;

                    case "Last Rites":
                        float fiftyPercentOfMissingHP = (maxHealth - health)*0.75f; 
                        ChangeLife(-fiftyPercentOfMissingHP); 
                        if (health <= 0) health = 1;
                        UppdateraHealthBar();
                        EKS.AvmarkeraNonInstantEffect();
                        break;

                    // === NON-INSTANT EFFECTS ===
                    case "Dark Ritual"://Spelaren väljer ut två av sina EGNA kort. Dessa kort offras så fort 2 kort är utvalda, och spelaren får i retur cardPower. 
                        print("<X%X%X> Kortet vars Non-InstantEffect var aktiverad hade effekten BIP och talet: " + EKS.kortetSomVarMarkerat.GetComponent<KortetsScript>().kortetsTal);
                        if (EKS.utvaldaEffectKort.Contains(dethärKortet) == false && tillhörSpelare == 1)//Vi vill inte lägga in kortet i listan om det redan ligger där!
                        {
                            EKS.utvaldaEffectKort.Add(dethärKortet);
                        }
                        else { print("<X%X%X> Kortet låg redan i listan, alternativt tillhörde kortet moståndarspelaren."); }
                    
                        if (EKS.utvaldaEffectKort.Count == 2)//VIKTIGT: Tidigare hade jag en "counter"-variabel här. Detta var mycket osmidigt eftersom att denna skulle behöva återställas för varje script den användes i vid "AvmarkeraNonInstantEffect()". Detta skulle lätt kunna orsaka stora buggar som hade varit svåra att upptäcka. 
                        {
                            foreach (GameObject objekt in EKS.utvaldaEffectKort)//Gå igenom listan utvaldaEffectKort och för varje kort: skicka kortet till graveyard. 
                            {
                                StartCoroutine(GDaB.SkickaTillGraveyard(objekt));

                            }
                            EKS.AvmarkeraNonInstantEffect(this.gameObject);
                            SS.currentEnergy += D.SkillEffect(switchEffekt);
                            SS.currentGems += D.SkillEffect(switchEffekt, 2);

                        }
                        break;
                    case "Sacrifice": //>>> Eventuellt kan det på sikt vara bra att skapa en FUNKTION som plockar fram markerade kort.

                        if (LS.graveyard.Count != 0)
                        {
                            GameObject kortetsBricka_L = kortetsBricka; // Brickan måste sparas i denna variabel så att programmet kommer ihåg brickan (Den sätts till 0 i funktionen som skickas till graveyard)
                            StartCoroutine(GDaB.SkickaTillGraveyard(this.gameObject));
                            GDaB.LäggNerKortet(LS.graveyard[0], kortetsBricka_L);
                            EKS.AvmarkeraNonInstantEffect(this.gameObject);
                        }
                        else // Om graveyard är tom avmarkerats effekten och cardPower returneras
                        {
                            EKS.AvmarkeraNonInstantEffect(this.gameObject, true);
                        }


                        break;

                    case "Rip Enchantment":
                        string isAnEffectRemoved = RipEffect(1); // RipEffect returnerar sträng "true" om en effekt tas bort.
                        if (isAnEffectRemoved == "true")
                        {
                            // NOTE: Detta utgår från att endast spelare 1 kan aktivera effekten, eftersom att det alltid är spelare 2 som skadas och 1 som healas. 
                            if (tillhörSpelare == 2) ChangeLife(-D.SkillEffect(switchEffekt));
                            else ChangeLife(D.SkillEffect(switchEffekt));

                        }


                        EKS.AvmarkeraNonInstantEffect();

                        break;

                    case "Change of Heart":
                        //KortetsScript KS = EKS.kortetSomVarMarkerat.GetComponent<KortetsScript>();
                        if ( (tillhörSpelare != KS_förMarkeratKort.tillhörSpelare) && kortetsFärg == "Hjärter")//Endast MOTSTÅNDARKORT kan tas över.     // kortetsFärg == "Hjärter"
                        {   
                            // >>>>>>>>> TILLÄGG: För att motverka bugs med andra skills borde funktionen STÄNGA ALLA EFFEKTER PÅ KORTET och STÄNGER VAPNET.
                            GDaB.SlumpaKortetsPosition(dethärKortet, KS_förMarkeratKort.tillhörSpelare);//Kortet som klickades slumpas till en position på planen. Planen som avses ges av [kortet som aktiverade effekten]'s ägare. 
                            EKS.AvmarkeraNonInstantEffect();
                        }
                        break;

                    // === DURATION EFFECTS === 
                    case "Immolate":
                    case "Healing Breeze":
                    case "Patient Spirit":
                    case "Soul Barbs":
                    case "Ammo Chart":
                    case "There Can Be Only One":
                    case "Extend Duration":
                    case "Strength to the Powerless":
                    case "Stun":
                    case "Reckless Haste":
                    case "Barrage":
                        DurationEffect_Add(switchEffekt);
                        EKS.AvmarkeraNonInstantEffect(this.gameObject);
                        break;


                    // === AoE EFFECTS === 
                    case "Annihilation"://Vid kortet som är markerat läggs en Range variabel in. allt som är inom denna range förstörs omedelbart (genom Range-scriptet).

                    case "Fire Blast":
                        SkapaRange(KS_förMarkeratKort.gameObject, switchEffekt);
                        break;

                    default:
                        print("<X%X%X> Den aktiva effekten som var initierad kunde inte användas på KORT, eller så klickades en JOKER, så ingenting händer. ");
                        break;
                }
            }
            else
            {
                //Debug.LogError("Spelare 2:s joker klickades");
                print("Player 2:s joker clicked. Jokers are immune to effects.");

            }
        }


        else // Om ingen effekt är aktiverad: I Detta fall plockas kortet upp om rätt villkor är uppfyllda. 
        {

            if (tillhörSpelare == 2) resultatVidKlick = "Fel Spelare";//Kortet som KLICKADES tillhörde inte spelaren vars tur det var. I detta sammanhang spelar det INGEN ROLL om ett kort är markerat eller inte!
            
            else if (kortetsSiktareLagring != null && tillhörSpelare == 1) resultatVidKlick = "Siktare Aktiv"; //else if (GameObject.Find("Scripthållare").GetComponent<Main_Class>().aktivaSiktare.Count != 0)
               
            else if (liggerKortetÖverst == false) resultatVidKlick = "Ligger inte Överst";
    
            else if (ärMarkerat == true) resultatVidKlick = "Redan Markerat";//I detta fall ska kortet läggas ner igen genom "läggnerKort(detHärKortet, kortetsbricka)"

            else if (GameObject.Find("Scripthållare").GetComponent<MainGameData>().markeradeKort.Count != 0) resultatVidKlick = "Annat Kort Markerat";

            else resultatVidKlick = "Kortet kan Plockas Upp";

            
            switch (resultatVidKlick)
            {
                case "Fel Spelare":
                    print(" <X%X%X> >>>>> Ett motståndarkort klickades. Motståndarkort kan endast klickas om en effekt är aktiv. Inget händer.");
                    break;
                case "Siktare Aktiv":
                    print(" <X%X%X> >>>>> En siktare var aktiv på DETTA kort. Kortets siktare förstörs.");
                    GDaB.FörstörSiktare(kortetsSiktareLagring);
                    break;
                case "Ligger inte Överst":
                    print(" <X%X%X> >>>>> Kortet låg inte överst. Inget händer.");
                    break;
                case "Redan Markerat":
                    print(" <X%X%X> >>>>> Kortet som klickades var redan markerat och läggs därför tillbaka");
                    
                    GDaB.LäggNerKortet(LS.markeradeKort[0], kortetsBricka);//ALLA kort ska INTE avmarkeras här; endast kortet som klickas (därav kallar vi inte LäggNerAllaKort)
                    break;
                case "Annat Kort Markerat":
                    print(" <X%X%X> >>>>> Ett annat kort var markerat, Vi kollar om spelaren har tillräckligt med cardpower.");
                    BrickansScript BS = kortetsBricka.GetComponent<BrickansScript>();
                    
                    if (BS.brickansKort.Count < 6)//Kollar om brickan är full. 
                    {
                        //SS.HanteraCostFörUtläggning(LS.markeradeKort[0], kortetsBricka);//IDé: alla dessa 4 rader skulle kunna läggas in i läggnerKortet så att denna funktion endast lägger ner kortet om spelaren har tilräckligt med energy. 
                        if ( SS.HanteraCostFörUtläggning(LS.markeradeKort[0], kortetsBricka) )
                        {
                            GDaB.LäggNerKortet(LS.markeradeKort[0], kortetsBricka);//Lägger ner kortet som var markerat.
                                                                                   //print("BRICKANS SCRIPT: Kortet" + brickansKort[0].GetComponent<KortetsScript>().kortetsFärg + " " + brickansKort[0].GetComponent<KortetsScript>().kortetsTal.ToString() + " läggs i brickan");
                        }
                    }
                    else { print("<X%X%X> Brickan var full, så kortet kan inte läggas ner där."); }
                    
                    break;

                default: 
                    GameObject.Find("Scripthållare").GetComponent<Main_Class>().PlockaUppKortet(dethärKortet, kortetsBricka); //Notera att "kortetsBricka" i detta sammanhang inte bara används som en variabel för att låta kortet veta i vilken bricka det ligger i; kortetsBricka agerar även som en "SPARNINGSVARIABEL" som hämtas då kortet läggs tillbaka på samma ställe som det tidigare låg. 
                    break;
            }

        
        }

        if (Input.GetKey(KeyCode.F)) // DEBUG-METOD: Om F trycks ner och kortet klickas skickas det till graveyard. 
        {   
            if (LS.markeradeKort.Count != 0) GDaB.LäggNerKortet(LS.markeradeKort[0], kortetsBricka); // Kortets läggs tillbaka i sin bricka för att sedan kunna skickas till graveyard. 
            //Anledningen varför MarkeradeKort.Count kollas är pga att om ett moståndarkort klickas markeras det inte, vilket medför att LS.markeradeKort[0] ger error. 
            StartCoroutine(GDaB.SkickaTillGraveyard(this.gameObject));
        }

    }
	
}
