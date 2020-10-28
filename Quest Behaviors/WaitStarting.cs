using Buddy.Coroutines;
using Clio.XmlEngine;
using System.Threading.Tasks;
using ff14bot.AClasses;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using System;
using System.ComponentModel;
using System.Windows.Media;
using TreeSharp;

namespace ff14bot.NeoProfiles
{

    [XmlElement("WaitStarting")]
    public class WaitStartingTag : ProfileBehavior
    {
        [DefaultValue(20)]
        [XmlAttribute("OutTime")]
        public int OutTime { get; set; }

        public override bool IsDone { get { return _done; } }

        private bool _done;

        private static bool _isStart;

        private const string Startstring = "任务开始。";

        private static DateTime IsStartTime = new DateTime(2019, 01, 01);
        private static EventHandler<ChatEventArgs> ev = delegate (object sender, ChatEventArgs args)
                   {
					   Log("回调测试");
                       if (args.ChatLogEntry.MessageType == (MessageType)2105 && args.ChatLogEntry.Contents.Contains(Startstring))
                       {
                           //Logging.Write(Colors.GreenYellow, $@"{GMLastReplyTime} {args.ChatLogEntry.TimeStamp}");
                           if (IsStartTime < args.ChatLogEntry.TimeStamp - TimeSpan.FromSeconds(5))
                           {
                               IsStartTime = args.ChatLogEntry.TimeStamp;
                               _isStart = true;
                               Log($@"Start At {IsStartTime}");
                           }
                       }
                   };

        private static bool hasAddListenner;

        protected override void OnDone()
        {
			Log("removeLis");
            hasAddListenner = false;
            GamelogManager.MessageRecevied -= ev;
        }
		
		public WaitStartingTag(){
			if (hasAddListenner)
            {
                return;
            }
			Log("addLis");
            hasAddListenner = true;
            GamelogManager.MessageRecevied += ev;
            TreeRoot.OnStop += OnBotStop;
        }
		
        private void OnBotStop(BotBase bot)
        {
			if (hasAddListenner)
            {
				hasAddListenner = false;
				GamelogManager.MessageRecevied -= ev;
			}
            TreeRoot.OnStop -= OnBotStop;
        }

        protected override void OnStart()
		{
			Log("onStart");
			_done = false;
			if (hasAddListenner)
            {
                return;
            }
			Log("addLis");
            hasAddListenner = true;
            GamelogManager.MessageRecevied += ev;
		}

        protected override Composite CreateBehavior()
        {
			Log("CreateBehavior");
            return new ActionRunCoroutine(r => waitStart(_isStart));
        }
		
		private async Task waitStart(bool isEnd){
				if(!isEnd && !_isStart){
					await Coroutine.Wait(OutTime*1000, () => _isStart);
				}
                _done = true;
                _isStart = false;
				Log("Start out");
		}
		
        public static void Log(string text)
        {
            Logging.Write(Colors.OrangeRed, string.Format("[WaitStarting] {0}", text));
        }
        
        protected override void OnResetCachedDone()
        {
			Log("OnResetCachedDone");
            _done = false;
            _isStart = false;
			if (hasAddListenner)
            {
                return;
            }
			Log("addLis");
            hasAddListenner = true;
            GamelogManager.MessageRecevied += ev;
        }
    }

}
