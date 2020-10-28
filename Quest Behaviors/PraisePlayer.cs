using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Behavior;
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
using Action = TreeSharp.Action;
using Clio.Utilities;
using Clio.XmlEngine;

namespace ff14bot.NeoProfiles
{

    [XmlElement("PraisePlayer")]
    public class PraisePlayerTag : ProfileBehavior
    {
        [DefaultValue(3)]
        [XmlAttribute("ZanIndex")]
        public int ZanIndex { get; set; }

        private bool _done;

        public override bool IsDone { get { return _done; } }

        protected override Composite CreateBehavior()
        { return new ActionRunCoroutine(async r =>
                        {
                            try{
                            if (MovementManager.IsMoving) MovementManager.MoveStop();
                            if (RaptureAtkUnitManager.GetWindowByName("VoteMvp") == null)
                                await Coroutine.Wait(10000, () => AgentModule.ToggleAgentInterfaceById(59) == 1 || AgentModule.ToggleAgentInterfaceById(120) == 1);

                            if (await Coroutine.Wait(6000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp") != null &&
                               RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsValid &&
                               RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsVisible))
                            {
                                var btn = RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(2+ZanIndex);
                                if (btn != null &&
                                   btn.IsValid &&
                                   btn.Label.Text != "")
                                {
                                    Log($@"点赞:" + btn.Label.Text);
                                    await Coroutine.Sleep(100);
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").SendAction(1, 2+ZanIndex, 0);
                                }

                            }
                            Log( $@"点赞结束");
                            }catch(Exception){
                                Log($@"点赞异常");
                            }

                            _done = true;
                        });
        }

        public static void Log(string text)
        {
            Logging.Write(Colors.OrangeRed, string.Format("[PraisePlayer] {0}", text));
        }

        protected override void OnStart()
		{
			_done = false;
		}

        protected override void OnResetCachedDone()
        {
            _done = false;
        }
    }

}
