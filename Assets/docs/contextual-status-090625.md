# 🗺️ Contextual Road Map — My Unity Multiplayer Card Game (Current Status)

---

## 1️⃣ GameObjects and Prefabs

### PlayerPrefab
- **NetworkObject**: ✅ Yes
- **Components**:  
    - Player1.cs  
    - NetworkTransform  
    - (PlayerInput? → not used/mentioned)  
    - Other: none specific

### PlayerUIPrefab
- **NetworkObject**: ❌ No (local object only — important detail)
- **Components**:  
    - PlayerUI.cs  
    - Buttons (Guess button, etc.)  
    - CardDisplayParent (container for CardUIPrefabs)  
    - Labels (player name, etc.)

### CardUIPrefab
- **NetworkObject**: ❌ No (local object only)
- **Components**:  
    - CardUI.cs  
    - Image (icon)  
    - Text (card name / value)

### DeckPrefab
- **NetworkObject**: ✅ Yes
- **Components**:  
    - Deck.cs  
    - DeckManager singleton instance manages this

### GameUI
- **NetworkObject**: ❌ No
- **Components**:  
    - Main Canvas  
    - Global UI for turn / status / log

### TurnManager
- GameObject in scene  
- **NetworkObject**: ✅ Yes
- **Components**:  
    - TurnManager.cs

### PlayerManager
- GameObject in scene  
- **NetworkObject**: ✅ Yes
- **Components**:  
    - PlayerManager.cs

---

## 2️⃣ Core Scripts / Classes

### Player1.cs
- Role: player logic, hand of cards
- Holds:
    - `List<Card>` hand  
    - Player name  
    - (Score?) not defined  
- Events:
    - OnCardReceived → `AddCardToHand(Card)`  
    - SendCardIDsToClient() → commented out — this is a key missing part!  
- Network:
    - No NetworkVariable<List<Card>> → cards are only local to server → no sync.

### PlayerUI.cs
- Role: displays player UI
- References:
    - Player1 instance (linked manually or on PlayerUIPrefab instantiation?)  
    - Displays CardUIPrefabs inside CardDisplayParent  
- Events handled:
    - OnPlayerTurn  
    - OnGuessMade  
    - OnCardClicked (?)
    - OnEventGuessClick() → currently throws NRE because CardUIPrefabs are empty!

### Card.cs
- Role: pure data object (no NetworkObject, no ScriptableObject)
- Fields:
    - Suit  
    - Value  
    - ID  

### CardData.cs
- Role: unknown (possibly used in CardUI?)  
- Relation to Card.cs: unclear, possible redundancy?

### CardUI.cs
- Role: displays 1 card visually
- Linked to:
    - CardData or Card? (not fully clear → likely Card)

### Deck.cs
- Role: manages deck of cards
- Functions:
    - ShuffleDeck  
    - RemoveCardFromDeck → returns Card  
    - (ResetDeck?)  
- Network:
    - Deck state not fully exposed → no automatic sync → works on server.

### DeckManager.cs
- Singleton: ✅ Yes  
- Manages:
    - Deck instance (DeckManager.Instance.DeckInstance.GetComponent<Deck>() used in DistributeCards)  
- Network sync: not used → Deck logic is fully server-side.

### PlayerManager.cs
- Role:
    - Spawns PlayerPrefabs (networked)  
    - Spawns PlayerUIPrefabs (local only, not networked)  
- Flow:
    - On client connect → instantiates PlayerPrefab + PlayerUIPrefab  
    - On server start → initializes Deck → calls DistributeCards(players1)  

### TurnManager.cs
- Role:
    - Manages current player turn  
    - Switches turn after guess  
- Network:
    - currentPlayerID → NetworkVariable  
    - Guess flow → ServerRpc  
    - Turn switch → ClientRpc  

---

## 3️⃣ Events / Data flow

### Game start flow
- Server starts → Deck initialized  
- Players join:
    - Server instantiates PlayerPrefab (networked)  
    - Client instantiates PlayerUIPrefab (not networked)  
- DistributeCards() → server calls DistributeCards(players1):
    - Deck → RemoveCardFromDeck() → Player1.AddCardToHand()  
    - ⚠️ No actual message sent to clients about which cards each player received → PlayerUIPrefab remains empty except possibly for Player 1 on host.

### Card distribution
- Current flow:
    - DeckManager → Deck → DistributeCards() → Player1.AddCardToHand()  
    - No CardUIPrefabs instantiated on clients → main reason why PlayerUIPrefab 2 is empty.  
    - The commented-out SendCardIDsToClient() was the missing bridge to notify clients.

### Guess flow
- Player clicks Guess button (PlayerUI → OnEventGuessClick)  
- Flow:
    - PlayerUI sends action (probably via ServerRpc)  
    - Server computes guess result  
    - TurnManager switches turn  
    - Problem:
        - When PlayerUIPrefab is empty, guessing is broken → PlayerUI tries to access CardUIPrefab list → NRE.

### Turn management flow
- TurnManager controls:
    - currentPlayerID (NetworkVariable)  
    - TurnManager notifies all clients whose turn it is  
    - PlayerUIPrefab shows Guess button if it is this player's turn  

---

## 4️⃣ Networking summary

### NetworkObject ownership

| Object            | NetworkObject? | Owner   | Observers |
|-------------------|----------------|---------|-----------|
| PlayerPrefab      | ✅ Yes         | Client  | All       |
| PlayerUIPrefab    | ❌ No          | Client  | Local only|
| CardUIPrefab      | ❌ No          | Client  | Local only|
| DeckPrefab        | ✅ Yes         | Server  | Clients   |
| TurnManager       | ✅ Yes         | Server  | Clients   |
| PlayerManager     | ✅ Yes         | Server  | Clients   |

### NetworkVariables

| Object        | Variable        | Sync to |
|---------------|-----------------|---------|
| Player1       | playerName      | All     |
| Player1       | hand → ❌ not synced | Clients |
| TurnManager   | currentPlayerID  | All     |
| Deck          | cardsLeft → not exposed | -       |

### ClientRpc / ServerRpc calls

| Who calls    | Function          | Target   |
|--------------|-------------------|----------|
| Server → Client | SendCardsToPlayer → commented out | One client |
| Client → Server | GuessCard        | Server   |
| Server → Client | UpdateTurn       | All      |

---

## Final Notes

### Known issues / questions:

- ❌ PlayerUIPrefab 2 does not receive cards → no ClientRpc / NetworkVariable used → main reason for NRE.
- ❌ CardUIPrefabs are instantiated manually on PlayerUIPrefab → but clients are not notified of their hand → hand is purely server-side.
- ⚠️ TurnManager works, but UI interaction fails when PlayerUIPrefab is empty.
- ⚠️ Architecture mismatch:
    - Data is on server (Player1.hand)  
    - UI is on client (PlayerUI + CardUIPrefabs)  
    - No mechanism to bridge these → you commented out SendCardIDsToClient() which is needed.

---

## Recap of next steps

1️⃣ Document this map → update if I missed anything.

2️⃣ Decide the architecture:
- Should Player1.hand be synchronized?
- Or should server send hand updates to each client manually? (simple ClientRpc)

3️⃣ Fix PlayerUIPrefab flow:
- Add back SendCardIDsToClient  
- Make PlayerUI receive card list and instantiate CardUIPrefabs correctly.

4️⃣ Once PlayerUIPrefab works → fix TurnManager → test Guess flow → verify no more NRE.

---

