using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform turretZone;readonly RectTransform[] operationNodes=new RectTransform[3];
        float materialSoundAt,shotSoundAt,lastAudioShot;int lastIdentityUses;
        void SetupOperationPresentation()
        {
            turretZone=CombatRing.Create(pickupLayer,250,new Color(.4f,.8f,1,.5f),.014f);
            for(int i=0;i<3;i++)
            {
                operationNodes[i]=Ui.Rect(pickupLayer,"Operation node "+i,0,0,0,0);
                var ring=CombatRing.Create(operationNodes[i],combat.Task==FieldTask.PowerRelay?156:130,new Color(.4f,.8f,1,.5f),.018f);
                ring.anchoredPosition=Vector2.zero;
                var sprite=Ui.Box(operationNodes[i],-22,-22,44,44,Color.white,false);
                sprite.sprite=ArenaArt.Prop(combat.Task==FieldTask.PowerRelay?5:0);sprite.preserveAspect=true;
                Ui.Label(operationNodes[i],(i+1).ToString(),-16,-58,32,28,20,Ui.Mint,TextAnchor.MiddleCenter);
            }
            materialSoundAt=shotSoundAt=lastAudioShot=0;lastIdentityUses=0;
            for(int i=0;i<12;i++)CombatSoundBank.Get(i);
        }
        void RenderOperationPresentation()
        {
            turretZone.gameObject.SetActive(combat.FieldModule==1);turretZone.anchoredPosition=new Vector2(combat.FieldAnchor.x,-combat.FieldAnchor.y);
            turretZone.GetComponent<Graphic>().color=combat.InTurretZone?new Color(.3f,1,.75f,.6f):new Color(.4f,.7f,1,.3f);
            for(int i=0;i<3;i++)
            {
                bool visible=combat.SpecialOperation&&combat.Task!=FieldTask.Convoy&&(combat.TaskActive||combat.TaskCompleted);
                operationNodes[i].gameObject.SetActive(visible);if(!visible)continue;
                var p=combat.OperationNode(i);operationNodes[i].anchoredPosition=new Vector2(p.x,-p.y);
                var ring=operationNodes[i].GetComponentInChildren<CombatRing>();ring.color=i<combat.TaskNode?new Color(.3f,1,.7f,.45f):i==combat.TaskNode?new Color(.4f,.8f,1,.6f):new Color(.4f,.6f,.7f,.16f);
            }
            if(battlePaused||smokeCapturing)return;
            if(combat.ShotFlash>lastAudioShot+.01f&&combat.Elapsed>=shotSoundAt){shotSoundAt=combat.Elapsed+.14f;FeedbackSound(CombatSoundBank.Get(4+combat.Hero));}
            lastAudioShot=combat.ShotFlash;
            if(combat.IdentityUses>lastIdentityUses&&combat.Elapsed>=materialSoundAt){materialSoundAt=combat.Elapsed+.3f;FeedbackSound(CombatSoundBank.Get(9+combat.Hero%3));}
            lastIdentityUses=combat.IdentityUses;
        }
    }
}
