using System;
namespace ScrapGuard.Core
{
    // Region changes the machinery; the six roles still teach a shared visual language.
    public static class RegionalEnemies
    {
        static readonly string[][] names={
            new[]{"절단 집게봇","용접 돌파차","리벳 포대","가스통 보행기","작업대 방패기","정비 조립기"},
            new[]{"궤도 정찰기","폭주 운반차","신호 포대","연료 수송기","침목 방패기","철도 복구기"},
            new[]{"필터 수거봇","폐기물 압축차","폐수 분사기","누출 운반기","밀폐 방패기","정화 조립기"},
            new[]{"서리 절단봇","빙판 쇄빙차","냉각 포대","냉매 운반기","냉각 방패기","냉각 정비기"},
            new[]{"생산 집게봇","프레스 돌파차","조립 포대","압력 운반기","금형 방패기","장갑 조립기"},
            new[]{"축전 정찰봇","코일 돌파차","방전 포대","축전 운반기","절연 방패기","전력 조립기"},
            new[]{"제련 집게봇","주조 돌파차","용광 포대","용탕 운반기","내열 방패기","주조 조립기"},
            new[]{"자석 수거봇","자력 견인차","압착 포대","극성 운반기","자석 방패기","선별 조립기"},
            new[]{"풍차 정찰봇","풍압 돌파차","낙뢰 포대","전하 운반기","피뢰 방패기","송전 정비기"},
            new[]{"궤도 회수봇","추진 화물차","발사 포대","연료 캡슐봇","접안 방패기","화물 조립기"}
        };
        public static readonly EnemyRole[] Signature={EnemyRole.Pursuer,EnemyRole.Charger,EnemyRole.Bomber,EnemyRole.Shield,EnemyRole.Summoner,EnemyRole.Artillery,EnemyRole.Bomber,EnemyRole.Charger,EnemyRole.Artillery,EnemyRole.Summoner};
        public static readonly string[] Advice={
            "절단 집게봇: 멈추면 톱날 범위 밖으로 이동",
            "폭주 운반차: 직선 돌진 예고의 옆으로 이동",
            "누출 운반기: 멈춘 통에서 멀어지세요",
            "냉각 방패기: 방어 방향을 피해 옆으로 접근",
            "장갑 조립기: 방패기를 생산하기 전에 파괴",
            "방전 포대: 가로 방전선의 위아래로 이동",
            "용탕 운반기: 넓은 분출 예고 밖으로 이동",
            "자력 견인차: 옆으로 접근한 뒤 짧게 돌진",
            "낙뢰 포대: 세로 낙뢰선의 좌우로 이동",
            "화물 조립기: 돌진 운반차를 내보내기 전에 파괴"
        };
        public static string Name(int region,EnemyRole role)
        {return names[Math.Max(0,Math.Min(9,region))][(int)role];}
    }

    public sealed partial class Combat
    {
        public int RegionalActions {get;private set;}
        bool Regional(Enemy e,int region){return !Challenge&&!e.finalBoss&&!e.core&&Definition.Region==region;}
        // Uses the same warning budget, damage attribution and recovery as other enemy attacks.
        // No lingering floor pools or additional damage channel is introduced.
        bool RegionalHold(Enemy e,float dt,float distance,V2 direction)
        {
            if(Regional(e,0)&&e.role==EnemyRole.Pursuer&&Elapsed>=25)
            {
                if(e.windup>0){e.windup=Math.Max(0,e.windup-dt);if(e.windup==0)e.attackFlash=.3f;return true;}
                if(e.cooldown<=0&&distance<85&&Warn(e.position,48,1.8f,55,0,0,DamageCause.Laser,DamageSource.EnemyAttack))
                {e.windup=1.8f;e.cooldown=6;RegionalActions++;return true;}
            }
            if(Regional(e,3)&&e.role==EnemyRole.Shield)
            {
                if(e.windup>0){e.windup=Math.Max(0,e.windup-dt);return true;}
                if(e.cooldown<=0&&distance<240){e.direction=direction;e.windup=2.2f;e.cooldown=6;RegionalActions++;return true;}
            }
            return false;
        }
        V2 RegionalChargeDirection(Enemy e,V2 direction)
        {
            if(!Regional(e,1))return direction;
            return Math.Abs(direction.x)>Math.Abs(direction.y)?new V2(Math.Sign(direction.x),0):new V2(0,Math.Sign(direction.y));
        }
        bool RegionalArtillery(Enemy e)
        {
            if(!Regional(e,5)&&!Regional(e,8))return false;
            bool horizontal=Definition.Region==5;
            if(Warn(Position,0,2,100,horizontal?200:28,horizontal?28:200,DamageCause.Electricity,DamageSource.EnemyAttack))RegionalActions++;
            return true;
        }
        float RegionalShieldDamage(Enemy e)
        {
            if(Regional(e,3))
            {
                var toPlayer=(Position-e.position).Normal;
                return e.windup>0&&toPlayer.x*e.direction.x+toPlayer.y*e.direction.y>.25f?.6f:1;
            }
            return V2.Distance(Position,e.position)>125?.45f:1;
        }
    }
}
