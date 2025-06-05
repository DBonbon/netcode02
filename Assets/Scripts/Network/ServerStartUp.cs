using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;


public class ServerStartUp : MonoBehaviour
{
    public static event System.Action ClientInstance;
    private const string InternalServerIP = "0.0.0.0";
    private string _externalServerIP = "0.0.0.0";
    private ushort _serverPort = 7777;
    private string _externalConnectionString =>$"{_externalServerIP}:{_serverPort}";

    private IMultiplayService _multiplayService;
    const int _multiplayServiceTimeout = 20000;

    private string _allocationId;
    private MultiplayEventCallbacks _serverCallbacks;
    private IServerEvents _serverEvents;

    private BackfillTicket _localBackFillTicket;
    private CreateBackfillTicketOptions _createBackfillTickerOptions;
    private const int _tickerCheckMs = 1000;
    private MatchmakingResults _matchmakingPayload;
    private bool _backfilling = false;
    async void Start()
    {
        bool server = false;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-dedicatedServer")
            {
                server = true;
            }
            if (args[i] == "-port" && (i + 1 < args.Length))
            {
                _serverPort = (ushort)int.Parse(args[i +1 ]);

            }

            if (args[i] == "-ip" && (i + 1 < args.Length))
            {
                _externalServerIP = args[i +1];
            }
        }

        if (server)
        {
            StartServer();
            await StartServerServices();
        }
        else
        {
            ClientInstance?.Invoke();
        }
        
    }

    private void StartServer()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(InternalServerIP, _serverPort);
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    async Task StartServerServices()
    {
        await UnityServices.InitializeAsync();
        try
        {
            _multiplayService = MultiplayService.Instance;
            await _multiplayService.StartServerQueryHandlerAsync(
                (ushort)ConnectionApprovalHandler.MaxPlayers, 
                serverName: "n/a",
                gameType: "n/a",
                buildId: "0", 
                map: "n/a"
            );
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Something went wrong trying to set up the SWP Service:\n{ex}");
        }

        try
        {
            _matchmakingPayload = await GetMatchmakerPayload(_multiplayServiceTimeout);
            if (_matchmakingPayload != null)
            {
                //Debug.Log($"Got payload: {_matchmakingPayload}");
                await StartBackfill(_matchmakingPayload);
            }
            else
            {
                Debug.LogWarning($"Getting the MatchMaker Payload out, Starting with Defaults.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Something went wrong trying to set up the Allocation & Backfill services:\n{ex}");
        }
    }

    private async Task<MatchmakingResults> GetMatchmakerPayload(int timeout)
    {
        var matchmakerPayloadTask = SubscribeAndAwaitMatchMakerAllocation();
        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(timeout))==matchmakerPayloadTask)
        {
            return matchmakerPayloadTask.Result;
        }

        return null;
    }

    private async Task<MatchmakingResults> SubscribeAndAwaitMatchMakerAllocation()
    {
        if (_multiplayService == null) return null;
        _allocationId = null;
        _serverCallbacks = new MultiplayEventCallbacks();
        _serverCallbacks.Allocate += OnMultiplayAllocation;
        _serverEvents = await _multiplayService.SubscribeToServerEventsAsync(_serverCallbacks);
        _allocationId = await AwaitAllocationId(); //this tells us when server was allocted, so we can ge info now about the match
        var mmPayload = await GetMatchmakerAllocationPayloadAsync();
        return mmPayload;
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        //Debug.Log($"OnAllocation: {allocation.AllocationId}");
        if (string.IsNullOrEmpty(allocation.AllocationId)) return;
        _allocationId = allocation.AllocationId;
    }

    private async Task<string> AwaitAllocationId()
    {
        var config = _multiplayService.ServerConfig;
        /*Debug.Log("Awaiting allocation. Server config is:\n" +
            $" -ServerId:{config.ServerId}\n" +
            $" -AllocationId: {config.AllocationId}\n" +
            $" -Port: {config.Port}\n" +
            $" -QPort: {config.QueryPort}\n" +
            $" -logs: {config.ServerLogDirectory}\n"
            );*/

        while (string.IsNullOrEmpty(_allocationId))
        {
            var configId = config.AllocationId;
            if (!string.IsNullOrEmpty(configId) && string.IsNullOrEmpty(_allocationId)) //if we found one but haven't allocation it yet
            {
                _allocationId = configId;
                break;
            }

            await Task.Delay(100);
        }

        return _allocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        try
        {
            var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
            var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
            //Debug.Log($"{nameof(GetMatchmakerAllocationPayloadAsync)}:\n{modelAsJson}");
            return payloadAllocation;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Something went wrong trying to get the MatchMaker Payload in GetMatchmakerAllocationPayloadAsync:\n{ex}");
        }

        return null;
    }

    private async Task StartBackfill(MatchmakingResults payload)
    {
        var backfillProperties = new BackfillTicketProperties(payload.MatchProperties);
        new BackfillTicket { Id = payload.MatchProperties.BackfillTicketId, Properties = backfillProperties};
        await BeginBackfilling(payload);

    }

    private async Task BeginBackfilling(MatchmakingResults payload)
    {
        var matchProperties = payload.MatchProperties;
        
        if (string.IsNullOrEmpty(_localBackFillTicket.Id))
        {
            _createBackfillTickerOptions = new CreateBackfillTicketOptions
        {
            Connection = _externalConnectionString ,
            QueueName = payload.QueueName,
            Properties = new BackfillTicketProperties(matchProperties)
        };
            _localBackFillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(_createBackfillTickerOptions);
        }
        _backfilling = true;
        # pragma warning disable 4014
        BackfillLoop();
        # pragma warning restore 4014
    }

    private async Task BackfillLoop()
    {
        while (_backfilling && NeedsPlayers())
        {
            if (!NeedsPlayers())
            {
                _localBackFillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(_localBackFillTicket.Id);
                _localBackFillTicket.Id = null;
                _backfilling = false;
                return;
            }

            await Task.Delay(_tickerCheckMs);
        }

        _backfilling = false;
    }

    private void ClientDisconnected(ulong clientId)
    {
        if (!_backfilling && NetworkManager.Singleton.ConnectedClients.Count > 0 && NeedsPlayers())
        {
            BeginBackfilling(_matchmakingPayload);
        }
    }

    private bool NeedsPlayers()
    {
        return NetworkManager.Singleton.ConnectedClients.Count < ConnectionApprovalHandler.MaxPlayers;
    }

    private void Dispose()
    {
        _serverCallbacks.Allocate -= OnMultiplayAllocation;
        _serverEvents?.UnsubscribeAsync();
    }
}
