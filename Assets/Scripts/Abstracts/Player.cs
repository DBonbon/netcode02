using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

public class Player1 : NetworkBehaviour
{   
    public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> PlayerDbId = new NetworkVariable<int>();
    public NetworkVariable<FixedString128Bytes> PlayerImagePath = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Result = new NetworkVariable<int>(0);
    public NetworkVariable<bool> IsWinner = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> HasTurn = new NetworkVariable<bool>(false);

    public List<Card> HandCards { get; set; } = new List<Card>();
    public List<Player1> PlayerToAsk { get; private set; } = new List<Player1>();
    public List<Card> CardsPlayerCanAsk { get; private set; } = new List<Card>();
    public List<Card> Quartets { get; private set; } = new List<Card>();

    public event Action OnPlayerToAskListUpdated;
    public event Action OnCardsPlayerCanAskListUpdated;

    private PlayerUI playerUI;

    public override void OnNetworkSpawn()
    {
        playerUI = GetComponent<PlayerUI>();

        if (IsServer)
        {
            Score.Value = 0;
        }

        Score.OnValueChanged += OnScoreChanged;
        HasTurn.OnValueChanged += OnHasTurnChanged;

        OnScoreChanged(0, Score.Value);
        OnHasTurnChanged(false, HasTurn.Value);
    }

    public void InitializePlayer(string name, int dbId, string imagePath)
    {
        if (IsServer)
        {
            playerName.Value = name;
            PlayerDbId.Value = dbId;
            PlayerImagePath.Value = imagePath;

            UpdateServerUI(name, imagePath);
            BroadcastPlayerDbAttributes();
        }
    }
    
    public void SetPlayerUI(PlayerUI ui)
    {
        playerUI = ui;
    }

    private void UpdateServerUI(string playerName, string playerImagePath)
    {
        if (playerUI != null)
        {
            playerUI.InitializePlayerUI(playerName, playerImagePath);
        }
    }

    public void BroadcastPlayerDbAttributes()
    {
        if (IsServer)
        {
            UpdatePlayerDbAttributes_ClientRpc(playerName.Value.ToString(), PlayerImagePath.Value.ToString());
        }
    }

    [ClientRpc]
    private void UpdatePlayerDbAttributes_ClientRpc(string playerName, string playerImagePath)
    {
        if (playerUI != null)
        {
            playerUI.InitializePlayerUI(playerName, playerImagePath);
        }
    }

    public void AddCardToHand(Card card) 
    {
        if (IsServer) {
            HandCards.Add(card);
            CheckForQuartets();
            SendCardIDsToClient();
            UpdateCardsPlayerCanAsk();  
        }
    }

    public void RemoveCardFromHand(Card card)
    {
        if (card != null && IsServer)
        {
            HandCards.Remove(card);
            UpdateCardsPlayerCanAsk();
            SendCardIDsToClient();
        }
    }
   
    private void OnHasTurnChanged(bool oldValue, bool newValue)
    {
        if (playerUI != null && IsOwner)
        {
            playerUI.UpdateTurnUI(newValue);
        }
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {
        UpdateScoreUI(newValue);
    }

    private void UpdateScoreUI(int score)
    {
        if (playerUI != null)
        {
            playerUI.UpdateScoreUI(score);
        }
    }

    public void IncrementScore()
    {
        Score.Value += 1;
    }

    public void SendCardIDsToClient()
    {
        if (IsServer)
        {
            int[] cardIDs = HandCards.Select(card => card.cardId.Value).ToArray();
            UpdatePlayerHandUI_ClientRpc(cardIDs, OwnerClientId);
        }
    }

    [ClientRpc]
    private void UpdatePlayerHandUI_ClientRpc(int[] cardIDs, ulong targetClient)
    {
        //Debug.Log($"[UpdatePlayerHandUI_ClientRpc] CALLED → NetworkObjectId={NetworkObjectId} OwnerClientId={OwnerClientId} LocalClientId={NetworkManager.Singleton.LocalClientId} targetClient={targetClient}");
        Debug.Log($"[UpdatePlayerHandUI_ClientRpc] CALLED → NetworkObjectId={NetworkObjectId} OwnerClientId={OwnerClientId} LocalClientId={NetworkManager.Singleton.LocalClientId} targetClient={targetClient} on GameObject={gameObject.name}");
        if (NetworkManager.Singleton.LocalClientId == targetClient)
        {
            Debug.Log($"[UpdatePlayerHandUI_ClientRpc] Updating hand UI for targetClient={targetClient}, LocalClientId={NetworkManager.Singleton.LocalClientId}, cardIDs={string.Join(",", cardIDs)}");
            playerUI?.UpdatePlayerHandUIWithIDs(cardIDs.ToList());
        }
    }

    public void UpdateCardsPlayerCanAsk()
    {
        if (CardsPlayerCanAsk == null)
        {
            CardsPlayerCanAsk = new List<Card>();
        }
        else
        {
            CardsPlayerCanAsk.Clear();
        }
       
        var allCardComponents = CardManager.Instance.allSpawnedCards.Select(go => go.GetComponent<Card>()).Where(c => c != null);

        foreach (var card in allCardComponents)
        {
            if (HandCards.Any(handCard => handCard.Suit.Value == card.Suit.Value) && !HandCards.Contains(card))
            {
                CardsPlayerCanAsk.Add(card);
            }
        }
        
        if (IsServer && HasTurn.Value)
        {
            int[] cardIDs = CardsPlayerCanAsk.Select(card => card.cardId.Value).ToArray();
            UpdateCardDropdown_ClientRpc(cardIDs);
        }
    }

    [ClientRpc]
    private void UpdateCardDropdown_ClientRpc(int[] cardIDs)
    {
        if (IsOwner)
        {
            playerUI?.UpdateCardsDropdownWithIDs(cardIDs);
        }
    }

    public void UpdatePlayerToAskList(List<Player1> allPlayers)
    {
        PlayerToAsk.Clear();
        foreach (var potentialPlayer in allPlayers)
        {
            if (potentialPlayer != this)
            {
                PlayerToAsk.Add(potentialPlayer);
            }
        }

        if (IsServer && HasTurn.Value)
        {
            ulong[] playerIDs = PlayerToAsk.Select(player => player.OwnerClientId).ToArray();
            string playerNamesConcatenated = string.Join(",", PlayerToAsk.Select(player => player.playerName.Value.ToString()));
            TurnUIForPlayer_ClientRpc(playerIDs, playerNamesConcatenated);
        }
    }

    [ClientRpc]
    public void TurnUIForPlayer_ClientRpc(ulong[] playerIDs, string playerNamesConcatenated)
    {
        if (IsOwner)
        {
            string[] playerNames = playerNamesConcatenated.Split(',');
            playerUI.UpdatePlayersDropdown(playerIDs, playerNames);
        }
    }
    
    public void CheckForQuartets()
    {
        var groupedBySuit = HandCards.GroupBy(card => card.Suit.Value.ToString());

        foreach (var suitGroup in groupedBySuit)
        {
            if (suitGroup.Count() == 4)
            {
                MoveCardsToQuartetsArea(suitGroup.ToList());
            }
        }
    }

    public void MoveCardsToQuartetsArea(List<Card> quartet)
    {
        Quartets quartetZone = QuartetsManager.Instance.QuartetsInstance.GetComponent<Quartets>();
        if (quartetZone == null)
        {
            Debug.LogError("Quartets zone not found.");
            return;
        }

        foreach (var card in quartet)
        {
            RemoveCardFromHand(card);
            quartetZone.AddCardToQuartets(card);
        }
        IncrementScore();
    }

    public bool IsHandEmpty()
    {
        return HandCards.Count == 0;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    [ServerRpc(RequireOwnership = true)]
    public void OnEventGuessClickServerRpc(ulong selectedPlayerId, int cardId)
    {
        NetworkVariable<int> networkCardId = new NetworkVariable<int>(cardId);
        TurnManager.Instance.OnEventGuessClick(selectedPlayerId, networkCardId);
    }
}