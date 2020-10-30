using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using System;
using System.ComponentModel;
using System.Windows.Media;
using TreeSharp;

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
                                await Coroutine.Wait(4000, () => AgentModule.ToggleAgentInterfaceById(120) == 1);

                            if (await Coroutine.Wait(2000, () => RaptureAtkUnitManager.GetWindowByName("VoteMvp") != null &&
                               RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsValid &&
                               RaptureAtkUnitManager.GetWindowByName("VoteMvp").IsVisible))
                            {
								await Coroutine.Sleep(500);
                                var btn = RaptureAtkUnitManager.GetWindowByName("VoteMvp").FindButton(2+ZanIndex);
                                if (btn != null &&
                                   btn.IsValid &&
                                   btn.Clickable)
                                {
                                    Log($@"点赞:" + btn.Label.Text);
                                    RaptureAtkUnitManager.GetWindowByName("VoteMvp").SendAction(1, (ulong)(2 + ZanIndex), 0);
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
