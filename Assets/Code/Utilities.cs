using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct HandData : INetworkSerializable
{
    public Handedness handedness;
    public bool isServer;

    public HandData(Handedness handedness, bool isServer)
    {
        this.handedness = handedness;
        this.isServer = isServer;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Serialize the two fields directly
        serializer.SerializeValue(ref handedness);
        serializer.SerializeValue(ref isServer);
    }
}
public enum Handedness
{
    Left,
    Right
}

public enum HapticType
{
    One,
    Three
}