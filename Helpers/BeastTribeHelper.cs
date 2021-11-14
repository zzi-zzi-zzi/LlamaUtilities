﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using ff14bot;
using LlamaLibrary.Memory.Attributes;

namespace LlamaLibrary.Helpers
{
    public static class BeastTribeHelper
    {
        private static readonly string Name = "BeastTribeHelper";
        private static readonly Color LogColor = Colors.Gold;
        private static readonly LLogger Log = new LLogger(Name, LogColor);
        private static class Offsets
        {
            [Offset("Search E8 ? ? ? ? BA ? ? ? ? 48 8B C8 48 83 C4 ? E9 ? ? ? ? ? ? ? ? ? ? E9 ? ? ? ? TraceCall")]
            internal static IntPtr GetQuestPointer;
            [Offset("Search 48 8D 81 ? ? ? ? 66 0F 1F 44 00 ? 66 39 50 ? 74 ? 41 FF C0 Add 3 Read32")]
            internal static int DailyQuestOffset;
            [Offset("Search 41 83 F8 ? 72 ? 32 C0 C3 0F B6 40 ? Add 3 Read8")]
            internal static int DailyQuestCount;
            [Offset("Search E8 ? ? ? ? 48 85 C0 74 ? 3A 58 ? TraceCall")]
            internal static IntPtr GetBeastTribeExd;
            [Offset("Search 4C 8D 1D ? ? ? ? 88 44 24 ? Add 3 TraceRelative")]
            internal static IntPtr QuestPointer;
            [Offset("Search 0F B6 9C C8 ? ? ? ? Add 4 Read32")]
            internal static int BeastTribeStart;
            [Offset("Search 66 89 BC C8 ? ? ? ? Add 4 Read32")]
            internal static int BeastTribeRep;
            [Offset("Search 83 FB ? 73 ? E8 ? ? ? ? 8B CB 48 03 C9 0F B6 9C C8 ? ? ? ? Add 2 Read8")]
            internal static int BeastTribeCount;
        }

        private static BeastTribeExd[] _beastTribes;

        static BeastTribeHelper()
        {
            var tribes = new List<BeastTribeExd>();

            for (var i = 1; i <= Offsets.BeastTribeCount; i++)
            {
                var result = Core.Memory.CallInjected64<IntPtr>(Offsets.GetBeastTribeExd, i);
                tribes.Add(Core.Memory.Read<BeastTribeExd>(result));

                //Log($"{Core.Memory.Read<BeastTribeExd>(result)}") ;
            }

            _beastTribes = tribes.ToArray();
        }

        public static void PrintBeastTribes()
        {
            var tribes = GetBeastTribes();

            for (var i = 0; i < tribes.Length; i++)
            {
                Log.Information(tribes[i].Unlocked ? $"{_beastTribes[i].Name} - {tribes[i]} MaxRank: {_beastTribes[i].MaxRank}" : $"{_beastTribes[i].Name} - Not Unlocked");
            }
        }

        public static void PrintDailies()
        {
            var dailies = GetCurrentDailies();
            var accepted = dailies.Count(i => i.Accepted);
            var finished = dailies.Count(i => i.Accepted && i.IsComplete);
            var unfinished = dailies.Where(i => i.Accepted && !i.IsComplete).Select(i => i.ID);

            Log.Information($"Daily quests left: {Offsets.DailyQuestCount - accepted}\n\tAccepted: {accepted}\n\tFinished: {finished}\n\tCurrentDailies: {string.Join(",", unfinished)}");
        }

        public static int DailyQuestAllowance()
        {
            var dailies = GetCurrentDailies();
            var accepted = dailies.Count(i => i.Accepted);
            return Offsets.DailyQuestCount - accepted;
        }

        public static string GetBeastTribeName(int index)
        {
            var result = Core.Memory.CallInjected64<IntPtr>(Offsets.GetBeastTribeExd, index);
            return result != IntPtr.Zero ? Core.Memory.ReadString(result + 0x28, Encoding.UTF8) : "";
        }

        public static int GetBeastTribeMaxRank(int index)
        {
            var result = Core.Memory.CallInjected64<IntPtr>(Offsets.GetBeastTribeExd, index);
            return result != IntPtr.Zero ? Core.Memory.Read<byte>(result + 0x22) : 0;
        }

        public static DailyQuestRead[] GetCurrentDailies()
        {
            Log.Verbose($"{(Offsets.QuestPointer + Offsets.DailyQuestOffset).ToString("X")}");
            return Core.Memory.ReadArray<DailyQuestRead>(Offsets.QuestPointer + Offsets.DailyQuestOffset, Offsets.DailyQuestCount);
        }

        public static BeastTribeStat[] GetBeastTribes()
        {
            Log.Verbose($"{(Offsets.QuestPointer + Offsets.BeastTribeStart).ToString("X")} {Offsets.BeastTribeStart}");
            return Core.Memory.ReadArray<BeastTribeStat>(Offsets.QuestPointer + Offsets.BeastTribeStart, Offsets.BeastTribeCount);
        }

        public static int GetBeastTribeRank(int tribe)
        {
            var tribes = GetBeastTribes();
            return tribes[tribe - 1].Rank;
        }
    }
}