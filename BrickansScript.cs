using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickansScript : MonoBehaviour {

    /*ÖVERGRIPANDE BESKRIVNING AV KLASSEN:
     
      Programmet sköter "brickorna" på spelplanen och spelarnas händer, dvs där spelarna håller och lägger sina kort. 
      Klassen inkluderar allt som sker då en bricka klickas på, samt egenskaperna som respektive bricka har (exempelvis vilken spelare det tillhör)
         
    */


    // General Variables 
    public List<GameObject> markeradeKortLista = new List<GameObject>();//Denna lista används endast som lagringsvariabel för att kunna rensa listan med markerade kort då ett kort läggs ner. 
    public List<GameObject> brickansKort = new List<GameObject>();

    public GameObject denhärBrickan;
    public SpelarensScript SS;
    public MainGameData LS;
    public Main_Class GDaB;
    public EffektKnappensScript EKS;

    // Player Variables 
    public string handPlan;
    public int tillhörSpelare; 

    // Card variables 
    public int kortetsSpelare;
    public int gällandeEnergy;
    public int kostnadFörAttLäggaUtKortet;
    public string typAvBrickaKortetLågI;
    bool harEnAllieradBrickaKlickats;
    

    void Start()
    {
        Object x = Resources.Load("PreSearing", typeof(GameObject));

        GDaB = GameObject.Find("Scripthållare").GetComponent<Main_Class>();
        LS = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        SS = GameObject.Find("Scripthållare").GetComponent<SpelarensScript>();
        EKS = GameObject.Find("Scripthållare").GetComponent<Main_Class>().skillKnapp.GetComponent<EffektKnappensScript>();

        kostnadFörAttLäggaUtKortet = 10;

    }


    public void OnMouseDown()
    {
        // === Tutourial ===
        MainGameData MGD = GameObject.Find("Scripthållare").GetComponent<MainGameData>();
        if (MGD.tutourialState == 2 && tillhörSpelare == 1 && handPlan == "Plan" && LS.markeradeKort.Count != 0) // ett kort måste vara markerat! 
        {
            MGD.tutourialState = 3;
            MGD.TutourialText();
        }

        // =================
        Descriptions D = new Descriptions();
        harEnAllieradBrickaKlickats = false; 
        if (tillhörSpelare == 1)
        {
            harEnAllieradBrickaKlickats = true;
        }


        if (EKS.initieradAktivEffekt != null) // Om en effekt aktiverats: Denna sats sköter hanteringar av effekter som triggas av att man klickar på en bricka 
        {
            if (harEnAllieradBrickaKlickats)
            {
                if (brickansKort.Count == 0) //Om brickan är tom
                {

                    string effekten = EKS.kortetSomVarMarkerat.GetComponent<KortetsScript>().effekt; 
                    switch (effekten)
                    {
                        case "Mirror Image":
                            GameObject clone = Instantiate(EKS.kortetSomVarMarkerat) as GameObject;
                            clone.GetComponent<KortetsScript>().SättHealth( D.SkillEffect(effekten) );
                            clone.GetComponent<KortetsScript>().kortetsBricka = null;//KLONEN kommer att innehålla EXAKT samma egenskaper som kortet som det KLONAS av. Detta inkluderar "kortetsBricka", vilket skapar ett problem när kortet läggs ner eftersom 
                            //att förskjutningsfunktionen i "LäggNerKortet" då aktiveras, vilket fuckar hela kortraden som kortet låg på. Denna rad löser problemet. 

                            GDaB.LäggNerKortet(clone, denhärBrickan);
                            EKS.kortetSomVarMarkerat.GetComponent<KortetsScript>().UppdateraHealthBar();//Av någon anledning flyttas kortets healthbar till sin klon. Har ingen aning om varför. Denna rad löser problemet eftersom att den flyttar tillbaka baren. 
                            EKS.AvmarkeraNonInstantEffect();
                            break;

                    }
                }

            }
        }

        //=== Om koden kommer in innuti denna if-sats innebär det att brickan har klickats, OCH ett kort är markerat. Detta kort kan sedan hämtas med kommandot "markeradeKort[0]" ===
        if (LS.markeradeKort.Count != 0)
        {   
            
            if (brickansKort.Count == 0)//Om "brickansKort.Count == 0" innebär det att inget kort ligger i brickans "brickansKort"-variabél.
            {

                typAvBrickaKortetLågI = LS.markeradeKort[0].GetComponent<KortetsScript>().kortetsBricka.GetComponent<BrickansScript>().handPlan;

                if (tillhörSpelare == 1) // Kortet kan bara läggas ner på brickor som tillhör spelare 1. 
                {
                    if (SS.HanteraCostFörUtläggning(LS.markeradeKort[0], denhärBrickan)) // HanteraCostFörUtläggning sköter både avdragning av gems och kontroll (returnerar true) om spelaren har tillräckligt med gems. 
                    {
                        GDaB.LäggNerKortet(LS.markeradeKort[0], denhärBrickan); //Lägger ner kortet som var markerat.
                    }
                }
            }
        }
    }

}
