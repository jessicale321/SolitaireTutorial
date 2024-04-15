using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UserInput : MonoBehaviour
{
    public GameObject slot1;
    private GameManager gameManager;
    private float timer;
    private float doubleClickTime = 0.3f;
    private int clickCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        slot1 = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (clickCount == 1)
        {
            timer += Time.deltaTime;
        }
        if (clickCount == 3)
        {
            timer = 0;
            clickCount = 1;
        }
        if (timer > doubleClickTime)
        {
            timer = 0;
            clickCount = 0;
        }
        GetMouseClick();
    }

    private void GetMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -10));
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {
                if (hit.collider.CompareTag("Deck"))
                {
                    Deck();
                }
                else if (hit.collider.CompareTag("Card"))
                {
                    Card(hit.collider.gameObject);
                }
                else if (hit.collider.CompareTag("Top"))
                {
                    Top(hit.collider.gameObject);
                }
                else if (hit.collider.CompareTag("Bottom"))
                {
                    Bottom(hit.collider.gameObject);
                }
            }
        }
    }

    private void Deck()
    {
        gameManager.DealFromDeck();
    }

    private void Card(GameObject selected)
    {
        if (!selected.GetComponent<Selectable>().faceUp)
        {
            if (!Blocked(selected))
            {
                selected.GetComponent<Selectable>().faceUp = true;
                slot1 = this.gameObject;
            }
        }
        else if (selected.GetComponent<Selectable>().inDeckPile)
        {
            if (!Blocked(selected))
            {
                if (slot1 == selected)
                {
                    if (DoubleClick())
                    {

                    }
                }
                else
                {
                    slot1 = selected;
                }
            }
        }

        if (slot1 == this.gameObject)
        {
            slot1 = selected;
        }

        else if (slot1 != selected)
        {
            // new card is eligible to stack on the old card
            if (Stackable(selected))
            {
                Stack(selected);
            }
            else
            {
                slot1 = selected;
            }    
        }

        else if (slot1 == selected) // if same card is clicked twice
        {
            if (DoubleClick())
            {
                AutoStack(selected);
            }
        }
    }

    private void Top(GameObject selected)
    {
        if (slot1.CompareTag("Card"))
        {
            if (slot1.GetComponent<Selectable>().value == 1)
            {
                Stack(selected);
            }
        }
    }

    private void Bottom(GameObject selected)
    {
        if (slot1.CompareTag("Card"))
        {
            if (slot1.GetComponent<Selectable>().value == 13)
            {
                Stack(selected);
            }
        }
    }

    private bool Stackable(GameObject selected)
    {
        Selectable s1 = slot1.GetComponent<Selectable>();
        Selectable s2 = selected.GetComponent<Selectable>();

        if (!s2.inDeckPile)
        {
            if (s2.top) // if in top pile, must stack suited Ace to King
            {
                if (s1.suit == s2.suit || (s1.value == 1 && s2.suit == null))
                {
                    if (s1.value == s2.value + 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else // if in bottom pile, must stack alternate colors King to Ace
            {
                if (s1.value == s2.value - 1)
                {
                    bool card1isRed = true;
                    bool card2isRed = true;

                    if (s1.suit == "C" || s1.suit == "S")
                    {
                        card1isRed = false;
                    }
                    if (s2.suit == "C" || s2.suit == "S")
                    {
                        card2isRed = false;
                    }

                    if (card1isRed == card2isRed)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void Stack(GameObject selected)
    {
        Selectable s1 = slot1.GetComponent<Selectable>();
        Selectable s2 = selected.GetComponent<Selectable>();
        float yOffset = 0.3f;

        if (s2.top || (!s2.top && s1.value == 13))
        {
            yOffset = 0;
        }

        slot1.transform.position = new Vector3(selected.transform.position.x, selected.transform.position.y - yOffset, selected.transform.position.z - 0.01f);
        slot1.transform.parent = selected.transform; // makes the children move with the parents

        if (s1.inDeckPile) // removes the cards from the top pile to prevent duplicate cards
        {
            gameManager.triplesOnDisplay.Remove(slot1.name);
        }
        else if (s1.top && s2.top && s1.value == 1) // allows movement of cards btwn top spots
        {
            gameManager.topPos[s1.row].GetComponent<Selectable>().value = 0;
            gameManager.topPos[s1.row].GetComponent<Selectable>().suit = null;
        }
        else if (s1.top) // keeps track of the current value of the top decks as a card has been removed
        {
            gameManager.topPos[s1.row].GetComponent<Selectable>().value = s1.value - 1;
        }
        else // removes card string from the appropriate bottom list
        {
            gameManager.bottoms[s1.row].Remove(slot1.name);
        }

        s1.inDeckPile = false;
        s1.row = s2.row;

        if (s2.top) // moves a card to the top and assigns the top's value and suit
        {
            gameManager.topPos[s1.row].GetComponent<Selectable>().value = s1.value;
            gameManager.topPos[s1.row].GetComponent<Selectable>().suit = s1.suit;
            s1.top = true;
        }
        else
        {
            s1.top = false;
        }

        // after completing move, reset slot1 to be essentially null, being null would break the logic
        slot1 = this.gameObject;
    }

    private bool Blocked(GameObject selected)
    {
        Selectable s2 = selected.GetComponent<Selectable>();
        if (s2.inDeckPile == true)
        {
            if (s2.name == gameManager.triplesOnDisplay.Last())
            {
                return false;
            }
            else
            {
                Debug.Log(s2.name + " is blocked by " + gameManager.triplesOnDisplay.Last());
                return true;
            }
        }
        else
        {
            if (s2.name == gameManager.bottoms[s2.row].Last())
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    private bool DoubleClick()
    {
        if (timer < doubleClickTime && clickCount == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void AutoStack(GameObject selected)
    {
        for (int i = 0; i < gameManager.topPos.Length; i++)
        {
            Selectable stack = gameManager.topPos[i].GetComponent<Selectable>();
            if (selected.GetComponent<Selectable>().value == 1) // if it's an ace
            {
                if (gameManager.topPos[i].GetComponent<Selectable>().value == 0) // and top position is empty
                {
                    slot1 = selected;
                    Stack(stack.gameObject);
                    break;
                }
            }
            else
            {
                if ((gameManager.topPos[i] .GetComponent<Selectable>().suit == slot1.GetComponent<Selectable>().suit) && (gameManager.topPos[i].GetComponent<Selectable>().value == slot1.GetComponent<Selectable>().value - 1)) 
                {
                    // if it's the last card (has no children)
                    if (HasNoChildren(slot1))
                    {
                        slot1 = selected;
                        // find a top spot that matches the condition for auto stacking if it exists
                        string lastCardName = stack.suit + stack.value.ToString();
                        if (stack.value == 1)
                        {
                            lastCardName = stack.suit + "A";
                        }
                        if (stack.value == 11)
                        {
                            lastCardName = stack.suit + "J";
                        }
                        if (stack.value == 12)
                        {
                            lastCardName = stack.suit + "Q";
                        }
                        if (stack.value == 13)
                        {
                            lastCardName = stack.suit + "K";
                        }
                        GameObject lastCard = GameObject.Find(lastCardName); // expensive
                        Stack(lastCard);
                        break;
                    }
                } 
            }
        }
    }

    private bool HasNoChildren(GameObject card)
    {
        int i = 0;
        foreach (Transform child in card.transform)
        {
            i++;
        }
        if (i == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
