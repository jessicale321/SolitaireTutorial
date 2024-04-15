using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public Sprite[] cardFaces;
    public GameObject cardPrefab;
    public GameObject deckButton;

    public GameObject[] bottomPos;
    public GameObject[] topPos;

    public static string[] suits = new string[] { "C", "D", "H", "S" };
    public static string[] values = new string[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

    public List<string>[] bottoms;
    public List<string>[] tops;
    public List<string> triplesOnDisplay = new List<string>();
    public List<List<string>> deckTriples = new List<List<string>>(); // list of chunks of 3 each

    private List<string> bottom0 = new List<string>();
    private List<string> bottom1 = new List<string>();
    private List<string> bottom2 = new List<string>();
    private List<string> bottom3 = new List<string>();
    private List<string> bottom4 = new List<string>();
    private List<string> bottom5 = new List<string>();
    private List<string> bottom6 = new List<string>();

    public List<string> deck;
    public List<string> discardPile = new List<string>();
    private int deckLocation;
    private int triples;
    private int triplesRemainder;

    // Start is called before the first frame update
    void Start()
    {
        bottoms = new List<string>[] { bottom0, bottom1, bottom2, bottom3, bottom4, bottom5, bottom6 };
        PlayCards();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayCards()
    {
        foreach (List<string> list in bottoms)
        {
            list.Clear();
        }

        deck = GenerateDeck();
        Shuffle(deck);
        Sort();
        StartCoroutine(Deal());
        SortDeckIntoTriples();
    }

    public static List<string> GenerateDeck()
    {
        List<string> newDeck = new List<string>();
        foreach (string s in suits)
        {
            foreach (string v in values)
            {
                newDeck.Add(s + v);
            }
        }

        return newDeck;
    }

    private void Shuffle<T>(List<T> list)
    {
        System.Random random = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            int k = random.Next(n);
            n--;
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    private IEnumerator Deal()
    {
        for (int i = 0; i < 7; i++)
        {

            float yOffset = 0;
            float zOffset = 0.03f; // brings closer to camera, helps with rendering issues

            foreach (string card in bottoms[i]) // only deal cards in the lists in the bottoms[]
            {
                yield return new WaitForSeconds(0.01f); // don't deal all at once, quickly deal one at a time
                GameObject newCard = Instantiate(cardPrefab, new Vector3(bottomPos[i].transform.position.x, bottomPos[i].transform.position.y - yOffset, bottomPos[i].transform.position.z - zOffset), Quaternion.identity, bottomPos[i].transform);
                newCard.name = card;
                newCard.GetComponent<Selectable>().row = i;
                if (card == bottoms[i][bottoms[i].Count - 1]) // the last one
                {
                    newCard.GetComponent<Selectable>().faceUp = true;
                }
                
                yOffset += 0.3f;
                zOffset += 0.03f;
                discardPile.Add(card);
            }
        }
        foreach (string card in discardPile)
        {
            if (deck.Contains(card))
            {
                deck.Remove(card);
            }
        }
        discardPile.Clear();
    }

    private void Sort() // organize into the 7 piles at the bottom
    {
        for (int i = 0; i < 7; i++)
        {
            for (int j = i; j < 7; j++)
            {
                bottoms[j].Add(deck.Last<string>());
                deck.RemoveAt(deck.Count - 1);
            }
        }
    }

    public void SortDeckIntoTriples() // TODO: only draw 1 card at a time instead
    {
        triples = deck.Count / 3;
        triplesRemainder = deck.Count % 3;
        deckTriples.Clear();

        int modifier = 0;
        for (int i = 0; i < triples; i++)
        {
            List<string> myTriples = new List<string>();
            for (int j = 0; j < 3; j++)
            {
                myTriples.Add(deck[j + modifier]);
            }
            deckTriples.Add(myTriples);
            modifier += 3;
        }
        if (triplesRemainder != 0) // not divisible by 3
        {
            List<string> myRemainders = new List<string>();
            modifier = 0;
            for (int k = 0; k < triplesRemainder; k++)
            {
                myRemainders.Add(deck[deck.Count - triplesRemainder + modifier]);
                modifier++;
            }
            deckTriples.Add(myRemainders);
            triples++;
        }
        deckLocation = 0;
    }

    public void DealFromDeck() // draw 3 from deck
    {
        foreach (Transform child in deckButton.transform)
        {
            if (child.CompareTag("Card"))
            {
                deck.Remove(child.name);
                discardPile.Add(child.name);
                Destroy(child.gameObject);
            }
        }

        if (deckLocation < triples)
        {
            triplesOnDisplay.Clear();
            float xOffset = 2.5f;
            float zOffset = -0.2f;

            foreach (string card in deckTriples[deckLocation])
            {
                GameObject newTopCard = Instantiate(cardPrefab, new Vector3(deckButton.transform.position.x + xOffset, deckButton.transform.position.y, deckButton.transform.position.z + zOffset), Quaternion.identity, deckButton.transform);
                xOffset += 0.5f;
                zOffset -= 0.2f;
                newTopCard.name = card;
                triplesOnDisplay.Add(card);
                newTopCard.GetComponent<Selectable>().faceUp = true;
                newTopCard.GetComponent<Selectable>().inDeckPile = true;
            }
            deckLocation++;
        }
        else
        {
            RestackTopDeck();
        }
    }

    private void RestackTopDeck()
    {
        deck.Clear();
        foreach (string card in discardPile)
        {
            deck.Add(card);
        }
        discardPile.Clear();
        SortDeckIntoTriples();
    }
}
