using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using System.Threading.Tasks;
using GreyMagic;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using ff14bot.Behavior;
using TreeSharp;
using Action = TreeSharp.Action;
using Buddy.Coroutines;

namespace ff14bot.NeoProfiles
{

    [XmlElement("RepairEquip")]
    public class RepairEquipTag : ProfileBehavior
    {
        private bool _done;

        public override bool IsDone { get { return _done; } }
        
        //装备耐久低于数值修理 1- 99的数值否则会报错
		[DefaultValue(10)]
        [XmlAttribute("Threshhold")]
        public int Threshhold { get; set; }
		
		[DefaultValue(true)]
        [XmlAttribute("UseMoney")]
		public bool UseMoney{ get; set; }
		
		public RepairEquipTag(){
			Log("RepairEquipTag");
		}
		
        protected override Composite CreateBehavior()
        {
			Log("CreateBehavior");
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(r => !Repairing && CanRepair()
                    , new Action(r => {
                        Repairing = true;
                        Log("Should be repairing");
                    })),
                new Decorator(r => Repairing,
                        new PrioritySelector(
                            new Decorator(r => SelectYesno.IsOpen,
                                new Sequence(
                                    new Action(r => Log("At Select Yes/No")),
                                    new Sleep(500),
                                    //new Action(r => Thread.Sleep(1000)),
                                    new Action(r => SelectYesno.ClickYes()),
                                    new Sleep(1000),
                                    new Action(r => Repairing = false),
                                    new Action(r => Repair.Close()),
                                    new Action(r => _done = true)
                                )
                            ),
                            new Decorator(r => Repair.IsOpen,
                                new Sequence(
                                    new Action(r => Log("Window open so repairing")),
                                    new Action(r => Repair.RepairAll())
                                )
                            ),
                            new Decorator(r => !Repair.IsOpen,
                                new ActionRunCoroutine(
                                    r => OpenRepair()
                                )
                            )
                        )
                    ),
                new Decorator(r => !Repairing, new Action(r => {
					if(Repair.IsOpen){
						Repair.Close();
					}
                    _done = true;
                }))
            );
        }
        
        private bool CanRepair()
        {
            return InventoryManager.EquippedItems.Any(item => item.Item != null && item.Item.RepairItemId != 0 && item.Condition < Threshhold);
        }


        public async Task OpenRepair()
        {
			if(!UseMoney){
				ActionManager.ToggleRepairWindow();
			}else {
				Log("Window not open so opening");
				var patternFinder = new PatternFinder(Core.Memory);
				var off = patternFinder.Find("4C 8D 0D ? ? ? ? 45 33 C0 33 D2 48 8B C8 E8 ? ? ? ? Add 3 TraceRelative");
				var func = patternFinder.Find("48 89 5C 24 ? 57 48 83 EC ? 88 51 ? 49 8B F9");
				var vtable = patternFinder.Find("48 8D 05 ? ? ? ? 48 89 03 B9 ? ? ? ? 4C 89 43 ? Add 3 TraceRelative");
            
				Core.Memory.CallInjected64<IntPtr>(func, AgentModule.GetAgentInterfaceById(AgentModule.FindAgentIdByVtable(vtable)).Pointer, 0, 0, off);
			}
			await Coroutine.Wait(5000, () => Repair.IsOpen);
		}

        public static bool Repairing { get; set; }

        public static void Log(string text)
        {
            Logging.Write(Colors.OrangeRed, string.Format("[RepairEquip] {0}", text));
        }

        protected override void OnStart()
		{
			Log("OnStart");
			_done = false;
			Repairing = false;
		}

        protected override void OnResetCachedDone()
        {
			Log("OnResetCachedDone");
            _done = false;
			Repairing = false;
        }

    }

}
