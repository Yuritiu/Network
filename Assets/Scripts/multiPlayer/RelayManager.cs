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
    // track if unity services have been initialized
    private static bool servicesInitialized = false;

    private static async Task EnsureUnityServices()
    {
        // initialize services 
        if (!servicesInitialized)
        {
            await UnityServices.InitializeAsync();

            // sign in anonymously if not already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // mark services as initialized
            servicesInitialized = true;
        }
    }

    public static async Task<string> StartHostWithRelayAsync(int maxConnections)
    {
        await EnsureUnityServices();

        // create a relay allocation for the host
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        UnityTransport utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // build relay server data and assign it to transport
        var serverData = new RelayServerData(allocation, "dtls");
        utp.SetRelayServerData(serverData);

        // start host using relay transport
        bool success = NetworkManager.Singleton.StartHost();
        if (!success)
        {
            return null;
        }

        return joinCode;
    }

    public static async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        // make sure unity services and auth are ready
        await EnsureUnityServices();


        // Join allocation
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        // Configure UnityTransport
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var serverData = new RelayServerData(joinAllocation, "dtls");
        utp.SetRelayServerData(serverData);

        bool success = NetworkManager.Singleton.StartClient();

        return success;
    }
}