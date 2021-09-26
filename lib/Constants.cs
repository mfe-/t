﻿namespace t.lib
{
    public class Constants
    {
        public static readonly byte Version = 0b000010;
        public static readonly byte RegisterPlayer = 0b000100;
        public static readonly byte NewPlayer = 0b000101;
        public static readonly byte ErrorOccoured = 0b111111;
        public static readonly byte Ok = 0b000001;
        public static readonly byte StartGame = 0b000110;
        public static readonly byte PlayerWon = 0b001010;
        public static readonly byte NextRound = 0b001000;
        public static readonly byte PlayerReported = 0b000111;
    }
}
