using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.RemoteWindows;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("LLJoinDuty2")]
    public class LLJoinDuty2 : ProfileBehavior
    {
        private bool _isDone;

        [XmlAttribute("DutyId")] public int DutyId { get; set; }
        
        [XmlAttribute("Trial")] 
        [DefaultValue(false)]
        public bool Trial { get; set; }
        
        public override bool HighPriority => true;

        public override bool IsDone => _isDone;

        protected override void OnStart()
        {
        }

        protected override void OnDone()
        {
        }

        protected override void OnResetCachedDone()
        {
            _isDone = false;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r => JoinDutyTask(DutyId,Trial));
        }

        private async Task JoinDutyTask(int DutyId,bool OutSized)
        {
			await Coroutine.Wait(20000, () => !CommonBehaviors.IsLoading);
			if(DutyManager.QueueState == QueueState.InDungeon || DutyManager.InInstance){
				if(await Coroutine.Wait(5000, () => !DutyManager.InInstance)){
					return;
				}else{
					_isDone = true;
					return;
				}
			}
			if(!(DutyManager.QueueState == QueueState.InQueue || DutyManager.QueueState == QueueState.JoiningInstance)){
				Logging.WriteDiagnostic("Queuing for Dungeon");
				GameSettingsManager.JoinWithUndersizedParty = OutSized;
				DutyManager.Queue(DataManager.InstanceContentResults[(uint)DutyId]);
				return;
			}
           await Coroutine.Wait(2000, () => (DutyManager.QueueState == QueueState.InQueue || DutyManager.QueueState == QueueState.JoiningInstance));

           Logging.WriteDiagnostic("Queued for Dungeon");

           await Coroutine.Wait(2000, () => (DutyManager.QueueState == QueueState.JoiningInstance)||DutyManager.InInstance);
			
		   if(!await Coroutine.Wait(10000, () => (RaptureAtkUnitManager.GetWindowByName("ContentsFinderConfirm") != null) || DutyManager.InInstance)){
		     return;
		   }

           Logging.WriteDiagnostic("Commencing");
           DutyManager.Commence();
           Logging.WriteDiagnostic("Waiting for Loading");
           await Coroutine.Wait(10000, () => CommonBehaviors.IsLoading || QuestLogManager.InCutscene||DutyManager.InInstance);
			
           if (CommonBehaviors.IsLoading)
           {
               await Coroutine.Wait(-1, () => !CommonBehaviors.IsLoading);
           }

           if (QuestLogManager.InCutscene)
           {
               TreeRoot.StatusText = "InCutscene";
               if (ff14bot.RemoteAgents.AgentCutScene.Instance != null)
               {
                   ff14bot.RemoteAgents.AgentCutScene.Instance.PromptSkip();
                   await Coroutine.Wait(250, () => SelectString.IsOpen);
                   if (SelectString.IsOpen)
                       SelectString.ClickSlot(0);
               }
           }

           Logging.WriteDiagnostic("Should be in duty");

            _isDone = true;
        }
    }
}