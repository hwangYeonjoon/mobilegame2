using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform machineActor,machineRotor,escapeArrow;
        Text salvageText,objectiveBearing;
        readonly RectTransform[] mineActors=new RectTransform[12];
        int trackedMachines,trackedWaves,trackedEvolution;
        Vector2 BattleCameraOffset()
        {
            if(combat.Challenge)return new Vector2(0,-menuExtra/2);
            float x=Mathf.Clamp(combat.Position.x-270,combat.WorldLeft,combat.WorldRight-540);
            float y=Mathf.Clamp(combat.Position.y-510-menuExtra/2,combat.WorldTop,combat.WorldBottom-MenuHeight);
            return new Vector2(-x,y);
        }
        bool InBattleView(V2 p,float margin=85)
        {
            var camera=BattleCameraOffset();float x=p.x+camera.x,y=p.y-camera.y;
            return x>=-margin&&x<=540+margin&&y>=-margin&&y<=MenuHeight+margin;
        }
        string BattleBearing(V2 point)
        {
            var delta=point-combat.Position;
            return Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?delta.x>0?"→":"←":delta.y>0?"↓":"↑";
        }
        void SetupAssaultPresentation()
        {
            trackedMachines=trackedWaves=trackedEvolution=0;
            machineActor=Ui.Rect(pickupLayer,"Salvage assembly",0,0,0,0);
            var chassis=Ui.Box(machineActor,-70,-106,140,140,Color.white,false);
            chassis.sprite=ArenaArt.Cell("SalvageMachine-v1",1,1,0,0);chassis.preserveAspect=true;
            machineRotor=CombatRing.Create(machineActor,22,new Color(.4f,1,.8f,.7f),.06f);
            machineRotor.anchoredPosition=new Vector2(0,26);
            Ui.Label(machineActor,"고철 합체",-75,-135,150,29,18,Ui.Gold,TextAnchor.MiddleCenter);
            escapeArrow=Ui.Label(hazardLayer,"↑",0,0,52,58,46,Ui.Mint,TextAnchor.MiddleCenter).rectTransform;
            escapeArrow.pivot=new Vector2(.5f,.5f);
            for(int i=0;i<mineActors.Length;i++)
            {
                mineActors[i]=CombatRing.Create(pickupLayer,28,Ui.Gold,.16f);
                Ui.Label(mineActors[i],"+",-10,-12,20,24,18,Ui.Gold,TextAnchor.MiddleCenter);
            }
            salvageText=Ui.Label(screen,"",24,173,492,25,16,Ui.Mint,TextAnchor.MiddleCenter);
            objectiveBearing=Ui.Label(screen,"",24,718,492,27,16,Ui.Blue,TextAnchor.MiddleCenter);
            StartBattleMusic();
        }
        void RenderAssaultPresentation()
        {
            machineActor.gameObject.SetActive(combat.MachineTime>0);
            machineActor.anchoredPosition=new Vector2(combat.MachinePosition.x,-combat.MachinePosition.y);
            float unfold=Mathf.Clamp01((12-combat.MachineTime)*2);
            machineActor.localScale=Vector3.one*Mathf.Lerp(.2f,1,unfold);
            machineRotor.localEulerAngles=new Vector3(0,0,-combat.Elapsed*260);
            escapeArrow.gameObject.SetActive(combat.WaveWarning>0);
            var exit=combat.WaveCenter+V2.Angle(combat.EscapeAngle)*150;
            escapeArrow.anchoredPosition=new Vector2(exit.x,-exit.y);
            escapeArrow.localEulerAngles=new Vector3(0,0,-combat.EscapeAngle*Mathf.Rad2Deg-90);
            for(int i=0;i<mineActors.Length;i++)
            {
                mineActors[i].gameObject.SetActive(i<combat.Mines.Count);
                if(i<combat.Mines.Count){var p=combat.Mines[i].position;mineActors[i].anchoredPosition=new Vector2(p.x,-p.y);}
            }
            salvageText.text=combat.Challenge?"":combat.MachineTime>0?"고철 합체 가동 · "+Mathf.CeilToInt(combat.MachineTime)+"초": "고철 "+combat.Salvage+" / "+combat.MachineCost+" · 모으면 자동 합체";
            objectiveBearing.text="";
            if(combat.TaskActive&&!InBattleView(combat.TaskPosition,0))
            {
                var delta=combat.TaskPosition-combat.Position;
                string direction=BattleBearing(combat.TaskPosition);
                objectiveBearing.text=direction+" 현장 임무 · "+Mathf.RoundToInt(delta.Length)+"m";
            }
            if(combat.MachinesBuilt>trackedMachines){Track("combat_hook","machine",combat.MachinesBuilt,combat.Elapsed);PulseHaptics(35);trackedMachines=combat.MachinesBuilt;}
            if(combat.WaveNumber>trackedWaves){Track("combat_pacing","wave",combat.WaveNumber,combat.Elapsed);trackedWaves=combat.WaveNumber;}
            if(combat.WeaponEvolution!=trackedEvolution){Track("combat_build","evolution",combat.WeaponEvolution,combat.Elapsed);trackedEvolution=combat.WeaponEvolution;}
            UpdateBattleMusic();
        }
    }
}
