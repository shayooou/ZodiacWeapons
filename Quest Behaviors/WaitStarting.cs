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

    [XmlElement("WaitStarting")]
    public class WaitStartingTag : ProfileBehavior
    {
        [DefaultValue(20)]
        [XmlAttribute("OutTime")]
        public int OutTime { get; set; }

        public override bool IsDone { get { return _done; } }

        private static bool _done;

        private static bool _isStart;

        private const string Startstring = "任务开始。";

        private static DateTime IsStartTime = new DateTime(2019, 01, 01);

        private static GamelogManager.MessageRecevied += delegate (object sender, ChatEventArgs args)
                    {
                        if (_isStart && args.ChatLogEntry.MessageType == (MessageType)2105 && args.ChatLogEntry.Contents.Contains(Startstring))
                        {
                            //Logging.Write(Colors.GreenYellow, $@"{GMLastReplyTime} {args.ChatLogEntry.TimeStamp}");
                            if (IsStartTime < args.ChatLogEntry.TimeStamp - TimeSpan.FromSeconds(5))
                            {
                                IsStartTime = args.ChatLogEntry.TimeStamp;
                                _done = true;
                                _isStart = false;
                                Logging.Write(Colors.GreenYellow, $@"Start At {IsStartTime}");
                            }
                        }
                    };

        protected override void OnStart()
		{
			_done = false;
            _isStart = true;
		}

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(async r => {
                await Coroutine.Wait(OutTime, () => _done);
                _done = true;
                _isStart = false;
            });
        }
        
        protected override void OnResetCachedDone()
        {
            _done = false;
            _isStart = true;
        }

        protected override void OnDone()
        {

        }
    }

}
