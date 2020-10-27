using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.Objects;
using ff14bot.Pathing.Service_Navigation;
using ff14bot.RemoteWindows;
using GreyMagic;
using TreeSharp;
//作者:暴风 QQ:2949517059
namespace Praise
{

    public class PraisePlayer : BotBase
    {
        //获取副本地图ID
        //ClearLog();
        //Log("当前地图ZoneId: {0} ,地图名字: {1} ,中文 {2},坐标{3}, 面向{4}", 
        //    WorldManager.ZoneId, WorldManager.CurrentZoneName, WorldManager.CurrentLocalizedZoneName, Core.Me.Location, Core.Me.Heading);

        private const int MapId = 148;
        //获取说有副本和ID
        //foreach (var item in DataManager.InstanceContentResults.Values)
        //{
        //    Log("副本:{0}，ID:{1} \n",item.ChnName,item.Id);
        //}        
        private InstanceContentResult Mazeresult => DataManager.InstanceContentResults.Values.FirstOrDefault(i => i.Id == 43);
        //监控所有聊天记录包括战斗信息
        //      GamelogManager.MessageRecevied += delegate (object sender, ChatEventArgs args)
        //      {
        //          Log(args.ChatLogEntry.FullLine);
        //      };
        private const string Startstring = "任务开始。";

        private const string Finishstring = "任务完成";

        private string[] SomeSays = {  "/p 辛苦了",
            "/p 辛苦了，已给赞",
            "/p 求赞，辛苦了，同色互赞，TN互赞",
            "/p 互赞，辛苦了",
            "/p 同色互赞，TN互赞 ，辛苦啦" };
        //装备耐久低于数值修理 1- 99的数值否则会报错
        private const float Threshhold = 30;

        //是否使用虚空修理 true 或者 false
        private static bool Void = false;

        //给赞等待时间
        private const int SleepTime = 500;


        private static DateTime IsFinishTime = new DateTime(2019, 01, 01);

        private static DateTime IsStartTime = new DateTime(2019, 01, 01);
        private AtkAddonControl ContentsFinder => RaptureAtkUnitManager.GetWindowByName("ContentsFinder");
        private AtkAddonControl ContentsFinderMenu => RaptureAtkUnitManager.GetWindowByName("ContentsFinderMenu");
        private bool ContentsFinderIsOpen => ContentsFinder != null && ContentsFinder.IsValid && ContentsFinder.IsVisible;
        private bool ContentsFinderMenuIsOpen => ContentsFinderMenu != null && ContentsFinderMenu.IsValid && ContentsFinderMenu.IsVisible;

        private static bool IsFirst = false;

        private static bool IsDo = false;

#if RB_CN
		public static int AgentId = 36;
#else
        public static int AgentId = 36;
#endif

        private Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => NowLoading.IsVisible,
                    new ActionRunCoroutine(async r => await Coroutine.Wait(3000, () => !NowLoading.IsVisible))
                ),


                new Decorator(ret => WorldManager.ZoneId != MapId,
                    new PrioritySelector(
                        new Decorator(ret => DutyManager.DutyReady,
                            new ActionRunCoroutine(async r =>
                            {
                                DutyManager.Commence();

                                await Coroutine.Wait(3000, () => WorldManager.ZoneId == MapId && !NowLoading.IsVisible);

                            })
                        ),

                        new Decorator(ret => !DutyManager.InQueue,
                            new PrioritySelector(
                                new Decorator(ret => CanRepair(),
                                    new PrioritySelector(
                                        new Decorator(ret => !Repair.IsOpen,
                                            new ActionRunCoroutine(async r =>
                                            {
                                                if (Void == true)
                                                {
                                                    OpenRepair();
                                                    await Coroutine.Wait(3000, () => Repair.IsOpen);
                                                }
                                                else
                                                {
                                                    ActionManager.ToggleRepairWindow();
                                                    await Coroutine.Wait(3000, () => Repair.IsOpen);
                                                }
                                            })
                                         ),
                                        new Decorator(ret => Repair.IsOpen,
                                            new ActionRunCoroutine(async r =>
                                            {
                                                Repair.RepairAll();
                                                if (await Coroutine.Wait(4000, () => SelectYesno.IsOpen))
                                                {
                                                    SelectYesno.ClickYes();
                                                }
                                                Repair.Close();
                                                await Coroutine.Wait(3000, () => !Repair.IsOpen);
                                            })
                                        )
                                    )
                                ),
                                new ActionRunCoroutine(async r =>
                                {
                                    DutyManager.Queue(Mazeresult);

                                    await Coroutine.Wait(3000, () => DutyManager.InQueue);

                                })
                            )
                        )
                    )
                ),

                new Decorator(ret => WorldManager.ZoneId == MapId,
                  new PrioritySelector(
                      new Decorator(ret => !IsFinish,
                        new PrioritySelector(
                            new Decorator(ret => Core.Target != null && ValidEnemy(Core.Target),
                                new PrioritySelector(
                                    RoutineManager.Current.PreCombatBuffBehavior,
                                    RoutineManager.Current.HealBehavior,
                                    RoutineManager.Current.CombatBuffBehavior,
                                    RoutineManager.Current.CombatBehavior,
                                    RoutineManager.Current.PullBehavior)
                                ),

                            new Decorator(ret => Core.Target == null && IsFirst,
                                new PrioritySelector(
                                    new Decorator(ret => IshasAnyem,
                                        new ActionRunCoroutine(async r =>
                                        {
                                            Poi.Current = new Poi(EnemiesFist, PoiType.Kill);
                                            if (Poi.Current != null && ValidEnemy(Poi.Current.BattleCharacter))
                                                Poi.Current.BattleCharacter.Target();
                                            await Coroutine.Wait(2000, () => Core.Target != null);
                                        })
                                    ),
                                    new Decorator(ret => MovementManager.IsMoving,
                                        new TreeSharp.Action(r => MovementManager.MoveStop()))
                                )
                            ),
                            new Decorator(ret => IsStart && !IsFirst,

                                new ActionRunCoroutine(async r =>
                                {
                                    if (Core.Target == null)
                                    {
                                        await Coroutine.Sleep(300);

                                        if (IshasAnyem)
                                        {
                                            Poi.Current = new Poi(EnemiesFist, PoiType.Kill);
                                            if (Poi.Current != null && ValidEnemy(Poi.Current.BattleCharacter))
                                                Poi.Current.BattleCharacter.Target();
                                            await Coroutine.Wait(2000, () => Core.Target != null);
                                            IsFirst = true;
                                        }
                                    }
                                }))

                        )
                    ),
                   new Decorator(ret => AgentModule.ToggleAgentInterfaceById(120) == 1,
                        new ActionRunCoroutine(async r =>
                        {

                            if (MovementManager.IsMoving) MovementManager.MoveStop();
                            Logging.Write(Colors.GreenYellow, $@"点赞开始");
                            if (await Coroutine.Wait(6000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp") != null &&
                                        RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsValid &&
                                        RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsVisible))
                            {
                                if (await Coroutine.Wait(10000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3) != null &&
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3).Label.Text != "") &&
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3).IsValid)
                                {
                                    await Coroutine.Sleep(SleepTime);
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").SendAction(1, 3, 0);
                                }



                                Logging.Write(Colors.GreenYellow, $@"点赞结束");

                                Random rand = new Random();
                                var _say = SomeSays[rand.Next(SomeSays.Length)];
                                ChatManager.SendChat(_say);

                                await Coroutine.Sleep(6000);
                                DutyManager.LeaveActiveDuty();
                                _isFinish = false;
                                _isStart = false;
                                IsFirst = false;

                                await Coroutine.Wait(20000, () => WorldManager.ZoneId != MapId && !NowLoading.IsVisible);
                            }



                        })),
                   new Decorator(ret => AgentModule.ToggleAgentInterfaceById(59) == 1,
                        new ActionRunCoroutine(async r =>
                        {
                            if (MovementManager.IsMoving) MovementManager.MoveStop();
                            Logging.Write(Colors.GreenYellow, $@"点赞开始");
                            if (await Coroutine.Wait(6000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp") != null &&
                                        RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsValid &&
                                        RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsVisible))
                            {
                                if (await Coroutine.Wait(10000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3) != null &&
                                   RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3).IsValid &&
                                   RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3).Label.Text != ""))
                                {
                                    await Coroutine.Sleep(SleepTime);
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").SendAction(1, 3, 0);
                                }



                                Logging.Write(Colors.GreenYellow, $@"点赞结束");

                                Random rand = new Random();
                                var _say = SomeSays[rand.Next(SomeSays.Length)];
                                ChatManager.SendChat(_say);

                                await Coroutine.Sleep(6000);
                                DutyManager.LeaveActiveDuty();
                                _isFinish = false;
                                _isStart = false;
                                IsFirst = false;
                                await Coroutine.Wait(20000, () => WorldManager.ZoneId != MapId && !NowLoading.IsVisible);
                            }



                        })),
                    new Decorator(ret => IsFinish,
                        new ActionRunCoroutine(async r =>
                        {
                            if (MovementManager.IsMoving) MovementManager.MoveStop();
                            Logging.Write(Colors.GreenYellow, $@"点赞开始");
                            if (RaptureAtkUnitManager.GetWindowByName("VoteMvp") == null)
                                await Coroutine.Wait(10000, () => AgentModule.ToggleAgentInterfaceById(59) == 1 || AgentModule.ToggleAgentInterfaceById(120) == 1);

                            if (await Coroutine.Wait(6000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp") != null &&
                               RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsValid &&
                               RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsVisible))
                            {


                                if (await Coroutine.Wait(10000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3) != null &&
                                   RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3).IsValid &&
                                   RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(3).Label.Text != ""))
                                {
                                    await Coroutine.Sleep(SleepTime);
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").SendAction(1, 3, 0);
                                }

                                Logging.Write(Colors.GreenYellow, $@"点赞结束");

                                Random rand = new Random();
                                var _say = SomeSays[rand.Next(SomeSays.Length)];
                                ChatManager.SendChat(_say);

                                await Coroutine.Sleep(6000);
                                DutyManager.LeaveActiveDuty();
                                _isFinish = false;
                                _isStart = false;
                                IsFirst = false;
                                await Coroutine.Wait(20000, () => WorldManager.ZoneId != MapId && !NowLoading.IsVisible);

                            }
                        }))
                    )
                )

            );
        }
        private static GameObject EnemiesFist
        {
            get
            {
                return GameObjectManager.GameObjects.Where(o => ValidEnemy(o)).OrderBy
                (o => o.MaxHealth).FirstOrDefault();
            }
        }
        private static bool _isFinish = false;


        private bool IsFinish
        {
            get
            {
                if (!_isFinish)
                {


                    GamelogManager.MessageRecevied += delegate (object sender, ChatEventArgs args)
                    {
                        //if (args.ChatLogEntry.MessageType == (MessageType)2112 && args.ChatLogEntry.Contents.Contains("完成时间"))
                        //{
                        //    Logging.Write(Colors.GreenYellow, $@"{GMLRTime} {args.ChatLogEntry.TimeStamp}");
                        //    if (GMLRTime < args.ChatLogEntry.TimeStamp - TimeSpan.FromSeconds(5))
                        //    {                            
                        //        GMLRTime = args.ChatLogEntry.TimeStamp;
                        //        _isFinish = true;
                        //    }
                        //}

                        if (args.ChatLogEntry.MessageType == MessageType.NPCAnnouncements && args.ChatLogEntry.Contents.Contains(Finishstring))
                        {
                            if (IsFinishTime < args.ChatLogEntry.TimeStamp - TimeSpan.FromSeconds(5))
                            {
                                IsFinishTime = args.ChatLogEntry.TimeStamp;
                                _isFinish = true;
                                Logging.Write(Colors.GreenYellow, $@"Finish {IsFinishTime}  {_isFinish}");
                            }
                        }
                    };

                }
                return _isFinish;
            }
        }
        private static bool _isStart = false;
        private bool IsStart
        {
            get
            {
                if (!_isStart)
                {
                    GamelogManager.MessageRecevied += delegate (object sender, ChatEventArgs args)
                    {
                        if (args.ChatLogEntry.MessageType == (MessageType)2105 && args.ChatLogEntry.Contents.Contains(Startstring))
                        {
                            //Logging.Write(Colors.GreenYellow, $@"{GMLastReplyTime} {args.ChatLogEntry.TimeStamp}");
                            if (IsStartTime < args.ChatLogEntry.TimeStamp - TimeSpan.FromSeconds(5))
                            {
                                IsStartTime = args.ChatLogEntry.TimeStamp;
                                _isStart = true;
                                Logging.Write(Colors.GreenYellow, $@"Start {IsStartTime}  {_isStart}");
                            }
                        }
                    };
                }
                return _isStart;
            }
        }

        private static bool IshasAnyem
        {
            get
            {
                return GameObjectManager.GameObjects.Where(e => ValidEnemy(e)).Any();
            }
        }
        public static bool ValidEnemy(GameObject e)
        {
            if (!(e is BattleCharacter) || e == null) return false;

            var em = e as BattleCharacter;

            return !em.IsMe && em.IsAlive && em.IsValid && em.IsTargetable && em.IsVisible && em.CanAttack;
        }

        private static bool CanRepair()
        {
            return InventoryManager.EquippedItems.Where(r => r.IsFilled && r.Condition < Threshhold).Count() > 0;
        }


        private static void OpenRepair()
        {
            var patternFinder = new PatternFinder(Core.Memory);
            var off = patternFinder.Find("4C 8D 0D ? ? ? ? 45 33 C0 33 D2 48 8B C8 E8 ? ? ? ? Add 3 TraceRelative");
            var func = patternFinder.Find("48 89 5C 24 ? 57 48 83 EC ? 88 51 ? 49 8B F9");

            Core.Memory.CallInjected64<IntPtr>(func, AgentModule.GetAgentInterfaceById(AgentId).Pointer, 0, 0, off);
        }


        public override void Start()
        {
            _isFinish = false;
            _isStart = false;
            Logging.Write(Colors.GreenYellow, $@"运行开始");
            Navigator.PlayerMover = new SlideMover();
            Navigator.NavigationProvider = new ServiceNavigationProvider();
            _root = CreateBehavior();
        }

        public override void Stop()
        {
            Logging.Write(Colors.GreenYellow, $@"运行结束");
            _isFinish = false;
            _isStart = false;
        }

        public override string Name => "行会令刷赞";
        public override PulseFlags PulseFlags => PulseFlags.All;
        public override Composite Root => _root;
        public override bool IsAutonomous => true;

        private Composite _root;
        public override bool RequiresProfile => false;

    }

}