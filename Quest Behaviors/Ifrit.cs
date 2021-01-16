using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.BotBases;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("Ifrit")]

    public class IfritTag : ProfileBehavior
    {

        [DefaultValue(202)]
        [XmlAttribute("MapId")]
        public int MapId { get; set; }
        
        
        private bool _done;

        public override bool IsDone { get { return _done; } }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => ContentsFinderConfirm.IsOpen || ContentsFinderReady.IsOpen,
                    new ActionRunCoroutine(async r =>
                    {
                        if (ContentsFinderConfirm.IsOpen)
                        {
                            ContentsFinderConfirm.Commence();

                            await Coroutine.Sleep(2700);
                        }
                    })
                ),
                new Decorator(ret => WorldManager.ZoneId != MapId && !NowLoading.IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => RaptureAtkUnitManager.GetWindowByName("ContentsFinder") == null,
                            new ActionRunCoroutine(async r =>
                            {
                                ChatManager.SendChat("/dfinder");

                                await Coroutine.Wait(5000, () => RaptureAtkUnitManager.GetWindowByName("ContentsFinder") != null);
                            })
                        ),
                        new Decorator(ret => RaptureAtkUnitManager.GetWindowByName("ContentsFinder") != null,
                            new ActionRunCoroutine(async r =>
                            {
                                RaptureAtkUnitManager.GetWindowByName("ContentsFinder").SendAction(1, 3, 4);
                            
                                await Coroutine.Wait(2000, () => ContentsFinderConfirm.IsOpen);
                            })
                        )
                    )
                ),
                new Decorator(ret => WorldManager.ZoneId == MapId && !NowLoading.IsVisible && Core.Target == null && !MovementManager.IsMoving && RaptureAtkUnitManager.GetWindowByName("ContentsFinderMenu") == null,
                  new Sequence(
                    new Action(r =>
                    {
                        var objs = GameObjectManager.GameObjects.Where(o => IsValidEnemy(o));
                        if (objs.Any())
                            Poi.Current = new Poi(objs.OrderBy(o => o.CurrentHealthPercent).First(), PoiType.Kill);
                        if (Poi.Current != null && IsValidEnemy(Poi.Current.BattleCharacter))
                            Poi.Current.BattleCharacter.Target();
                    }),
                    new ActionRunCoroutine(async r =>
                    {
                        var objs = GameObjectManager.GameObjects.Where(o => IsValidEnemy(o));
                        if (!objs.Any() && !Core.Me.InCombat)
                        {
							ff14bot.Managers.DutyManager.LeaveActiveDuty();
                            await Coroutine.Wait(20000, () => CommonBehaviors.IsLoading);
							if (CommonBehaviors.IsLoading)
							{
								_done = true;
							}
                        }
                    })
                  )
                )
            );
        }

        public static bool IsValidEnemy(GameObject obj)
        {
            if (obj == null || !(obj is Character)) return false;
            var c = (Character) obj;
            return !c.IsMe && !c.IsDead && c.IsValid && c.IsTargetable && c.IsVisible && c.CanAttack;
        }

        /// <summary>
        /// This gets called when a while loop starts over so reset anything that is used inside the IsDone check
        /// </summary>
        protected override void OnResetCachedDone()
        {
            _done = false;
        }

        protected override void OnDone()
        {

        }
    }
}