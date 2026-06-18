using UnityEngine;

public enum SessionMode
{
    Singleplayer,
    Multiplayer
}

public static class GameSessionState
{
    public static SessionMode Mode = SessionMode.Singleplayer;
}
