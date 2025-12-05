using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Networking.Transport.Relay;

public static class RelayManager
{
    private static bool servicesInitialized = false;

    private static async Task EnsureUnityServices()
    {
        if (!servicesInitialized)
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Relay] Signed in as player {AuthenticationService.Instance.PlayerId}");
            }

            servicesInitialized = true;
        }
    }

    public static async Task<string> StartHostWithRelayAsync(int maxConnections)
    {
        await EnsureUnityServices();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log($"[Relay] Host allocation created. Join code: {joinCode}");

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var serverData = new RelayServerData(allocation, "dtls");
        utp.SetRelayServerData(serverData);

        bool success = NetworkManager.Singleton.StartHost();
        if (!success)
        {
            Debug.LogError("[Relay] Failed to start host.");
            return null;
        }

        return joinCode;
    }

    public static async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        await EnsureUnityServices();

        Debug.Log($"[Relay] Joining Relay allocation with code: {joinCode}");

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var serverData = new RelayServerData(joinAllocation, "dtls");
        utp.SetRelayServerData(serverData);

        bool success = NetworkManager.Singleton.StartClient();
        if (!success)
        {
            Debug.LogError("[Relay] Failed to start client.");
        }

        return success;
    }
}
