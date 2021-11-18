﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Managers;
using LlamaLibrary.Extensions;
using LlamaLibrary.Logging;
using LlamaLibrary.Memory;
using LlamaLibrary.RemoteAgents;
using LlamaLibrary.RemoteWindows;
using Newtonsoft.Json;
using TreeSharp;

namespace LlamaBotBases.Materia
{
    public class MateriaBase : BotBase
    {
        private static readonly LLogger Log = new LLogger("Materia", Colors.Fuchsia);

        public static Dictionary<int, List<MateriaItem>> MateriaList;
        private static bool _init;
        private PulseFlags _pulseFlags;
        private Composite _root;
        private MateriaSettingsFrm _settings;
        internal static BagSlot ItemToRemoveMateria;
        internal static BagSlot ItemToAffixMateria;
        internal static List<BagSlot> MateriaToAdd;
        internal static MateriaTask MateriaTask = MateriaTask.None;

        public MateriaBase()
        {
            Task.Factory.StartNew(() =>
            {
                Init();
                _init = true;
                Log.Information("INIT DONE");
            });
        }

        public override string Name { get; } = "Materia";

        public override PulseFlags PulseFlags { get; } = PulseFlags.All;

        public override Composite Root => _root;
        public override bool IsAutonomous => true;
        public override bool RequiresProfile => false;
        public override bool WantButton => true;

        public override void OnButtonPress()
        {
            if (_settings == null || _settings.IsDisposed)
            {
                _settings = new MateriaSettingsFrm();
            }

            try
            {
                _settings.Show();
                _settings.Activate();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        public override void Start()
        {
            _root = new ActionRunCoroutine(r => TestTask1());
        }

        private async Task<bool> TestTask1()
        {
            if (!_init)
            {
                Log.Warning("Wait for initialization to finish");
                return false;
            }

            if (MateriaTask == MateriaTask.Remove)
            {
                if (ItemToRemoveMateria != null && ItemToRemoveMateria.IsValid)
                {
                    await RemoveMateria(ItemToRemoveMateria);
                }
                else
                {
                    Log.Error("Choose an item in the settings and click Remove Materia");
                }
            }

            if (MateriaTask == MateriaTask.Affix)
            {
                if (ItemToAffixMateria != null && ItemToAffixMateria.IsValid)
                {
                    //await AffixMateria(ItemToAffixMateria, MateriaToAdd);
                }
                else
                {
                    Log.Error("Choose an item in the settings and click Affix Materia");
                }
            }

            MateriaTask = MateriaTask.None;

            TreeRoot.Stop("Done playing with Materia");
            return false;
        }

        internal void Init()
        {
            OffsetManager.Init();

            Log.Information("Load Materia.json");
            MateriaList = LoadResource<Dictionary<int, List<MateriaItem>>>(LlamaLibrary.Properties.Resources.Materia);
            Log.Information("Loaded Materia.json");
        }

        private static T LoadResource<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        /*
        public static async Task<bool> AffixMateria(BagSlot bagSlot, List<BagSlot> materiaList)
        {
            Log.Information($"MateriaList count {materiaList.Count}");
            if (bagSlot != null && bagSlot.IsValid)
            {
                Log.Information($"Want to affix Materia to {bagSlot}");

                for (var i = 0; i < materiaList.Count; i++)
                {
                    if (materiaList[i] == null)
                    {
                        break;
                    }

                    Log.Information($"Want to affix materia {i} {materiaList[i]}");

                    if (!materiaList[i].IsFilled)
                    {
                        continue;
                    }

                    var count = MateriaCount(bagSlot);

                    while (materiaList[i].IsFilled && (count == MateriaCount(bagSlot)))
                    {
                        if (!MateriaAttach.Instance.IsOpen)
                        {
                            bagSlot.OpenMeldInterface();
                            await Coroutine.Wait(5000, () => MateriaAttach.Instance.IsOpen);

                            if (!MateriaAttach.Instance.IsOpen)
                            {
                                Log.Error($"Can't open meld window");
                                return false;
                            }

                            // MateriaAttach.Instance.ClickItem(0);
                            // await Coroutine.Sleep(1000);
                            MateriaAttach.Instance.ClickMateria(0);
                            await Coroutine.Wait(7000, () => AgentMeld.Instance.Ready);
                            await Coroutine.Wait(5000, () => MateriaAttachDialog.Instance.IsOpen);
                        }

                        if (!MateriaAttachDialog.Instance.IsOpen)
                        {
                            // MateriaAttach.Instance.ClickItem(0);
                            //await Coroutine.Sleep(1000);
                            MateriaAttach.Instance.ClickMateria(0);
                            await Coroutine.Wait(7000, () => AgentMeld.Instance.Ready);
                            await Coroutine.Wait(5000, () => MateriaAttachDialog.Instance.IsOpen);
                            await Coroutine.Wait(7000, () => AgentMeld.Instance.Ready);
                            if (!MateriaAttachDialog.Instance.IsOpen)
                            {
                                Log.Error($"Can't open meld dialog");
                                return false;
                            }
                        }

                        //Log.Information($"{Offsets.AffixMateriaFunc.ToInt64():X}  {Offsets.AffixMateriaParam.ToInt64():X}   {bagSlot.Pointer.ToInt64():X}  {materiaList[i].Pointer.ToInt64():X}");
                        Log.Information("Wait Ready");
                        await Coroutine.Wait(7000, () => MateriaAttachDialog.Instance.IsOpen);

                        // await Coroutine.Wait(7000, () => AgentMeld.Instance.Ready);
                        // Log.Verbose("Wait CanMeld");
                        // await Coroutine.Wait(7000, () => AgentMeld.Instance.CanMeld);
                        bagSlot.AffixMateria(materiaList[i]);
                        Log.Verbose("Clicked affix wait not Ready");
                        await Coroutine.Wait(7000, () => AgentMeld.Instance.Ready);
                        Log.Verbose("Clicked affix wait Ready");
                        await Coroutine.Wait(7000, () => !AgentMeld.Instance.Ready);

                        // await Coroutine.Sleep(7000);
                        Log.Verbose("Clicked wait window");
                        await Coroutine.Wait(7000, () => !MateriaAttachDialog.Instance.IsOpen);
                        Log.Verbose("Wait 2 windows");
                        await Coroutine.Wait(5000, () => MateriaAttachDialog.Instance.IsOpen || MateriaAttach.Instance.IsOpen);

                        //    await Coroutine.Sleep(1000);

                        while (MateriaAttachDialog.Instance.IsOpen)
                        {
                            Log.Verbose("While window");
                            MateriaAttachDialog.Instance.ClickAttach();
                            await Coroutine.Wait(7000, () => !AgentMeld.Instance.CanMeld);
                            await Coroutine.Wait(7000, () => AgentMeld.Instance.CanMeld);

                            //await Coroutine.Wait(7000, () => !MateriaAttachDialog.Instance.IsOpen);
                            await Coroutine.Wait(7000, () => MateriaAttachDialog.Instance.IsOpen || MateriaAttach.Instance.IsOpen);
                        }

                        if (MateriaAttach.Instance.IsOpen)
                        {
                            Log.Verbose("Closing window");
                            MateriaAttach.Instance.Close();
                            await Coroutine.Wait(7000, () => !MateriaAttach.Instance.IsOpen);

                            //await Coroutine.Wait(7000, () => !AgentMeld.Instance.Ready);
                            //await Coroutine.Sleep(1000);
                        }
                    }

                    if (!materiaList[i].IsFilled)
                    {
                        return false;
                    }
                }
            }

            return true;
        }*/

        public static async Task<bool> RemoveMateria(BagSlot bagSlot)
        {
            if (bagSlot != null && bagSlot.IsValid)
            {
                Log.Information($"Want to remove Materia from {bagSlot}");
                var count = MateriaCount(bagSlot);
                for (var i = 0; i < count; i++)
                {
                    Log.Information($"Removing materia {count - i}");
                    bagSlot.RemoveMateria();
                    await Coroutine.Sleep(6000);
                }
            }

            Log.Information($"Materia now has {MateriaCount(ItemToRemoveMateria)}");

            return true;
        }

        public static List<MateriaItem> Materia(BagSlot bagSlot)
        {
            var materiaType = Core.Memory.ReadArray<ushort>(bagSlot.Pointer + 0x20, 5);
            var materiaLevel = Core.Memory.ReadArray<byte>(bagSlot.Pointer + 0x2A, 5);
            var materia = new List<MateriaItem>();

            for (var i = 0; i < 5; i++)
            {
                if (materiaType[i] > 0)
                {
                    materia.Add(MateriaList[materiaType[i]].First(j => j.Tier == materiaLevel[i]));
                }
            }

            return materia;
        }

        public static bool HasMateria(BagSlot bagSlot)
        {
            var materiaType = Core.Memory.ReadArray<ushort>(bagSlot.Pointer + 0x20, 5);
            for (var i = 0; i < 5; i++)
            {
                if (materiaType[i] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static int MateriaCount(BagSlot bagSlot)
        {
            var materiaType = Core.Memory.ReadArray<ushort>(bagSlot.Pointer + 0x20, 5);
            var count = 0;
            for (var i = 0; i < 5; i++)
            {
                if (materiaType[i] > 0)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public enum MateriaTask
    {
        None,
        Remove,
        Affix
    }
}