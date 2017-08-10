using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeStart
{
    class A2SInfoData
    {
        public byte Header { get; }
        public byte Protocol { get; }
        public string Hostname { get; }
        public string Map { get; }
        public string Folder { get; }
        public string Game { get; }
        public short ID { get; }
        public byte Players { get; }
        public byte Maxplayers { get; }
        public byte Bots { get; }
        public char Type { get; }
        public char OS { get; }
        public bool Password { get; }
        public bool VAC { get; }
        public string Version { get; }
        public string IP { get; }

        public A2SInfoData(byte header, byte protocol, string hostname, string map, string folder, string game, short id, byte players, byte maxplayers, byte bots, char type, char os, bool password, bool vac, string version, string ip)
        {
            Header = header;
            Protocol = protocol;
            Hostname = hostname;
            Map = map;
            Folder = folder;
            Game = game;
            ID = id;
            Players = players;
            Maxplayers = maxplayers;
            Bots = bots;
            Type = type;
            OS = os;
            Password = password;
            VAC = vac;
            Version = version;
            IP = ip;
        }
    }
}
