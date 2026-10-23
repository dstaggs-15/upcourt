using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
namespace Upcourt {
public partial class Match {
    [XmlIgnore] public Queue<string> Sounds=new Queue<string>();
    public float[] Heat={0,0},FireTime={0,0};
    public int[] Combo={0,0};
    public float RefX=47,RefY=-3,Impact,ImpactTime,BannerTime,CrowdEnergy,CutTime;
    public float MoveX,MoveY,ShotStartHeight=2;
    public int CutTarget=-1,AlleyTarget=-1,PenaltyPossession=-1;
    public bool DunkAttempt,ShotWasDunk,ShotWasAlley,AlleyPass,LooseFromShot=true;
    public string ArcadeBanner="",FinishStyle="";
    public void Sound(string name){Sounds.Enqueue(name);}
    public void Flash(string text,float impact){ArcadeBanner=text;BannerTime=2.2f;Impact=Math.Max(Impact,impact);ImpactTime=.3f;CrowdEnergy=Math.Max(CrowdEnergy,impact*.5f);}
    public void Hype(int side,float amount,string text){
        if(Contest||side<0||side>1)return;
        Combo[side]++;Heat[side]=Math.Min(100,Heat[side]+amount);
        if(Heat[side]>=100&&FireTime[side]<=0){FireTime[side]=12;Heat[side]=100;Sound("fire");Flash("ON FIRE! / "+(side==0?HomeName:AwayName),3);}
        else if(!string.IsNullOrEmpty(text))Flash(text+(Combo[side]>1?" / COMBO X"+Combo[side]:""),1);
    }
    void TickArcade(float dt,float mx,float my){
        MoveX=mx;MoveY=my;BannerTime=Math.Max(0,BannerTime-dt);ImpactTime=Math.Max(0,ImpactTime-dt);Impact=Math.Max(0,Impact-dt*6);CrowdEnergy=Math.Max(0,CrowdEnergy-dt*.1f);CutTime=Math.Max(0,CutTime-dt);
        RefX+=(Util.Clamp(BallX,9,85)-RefX)*(1-(float)Math.Exp(-dt*1.8));
        if(!Practice&&!Contest&&UserSide>=0&&Phase=="live"&&BallOwner!=null&&BallOwner.Side==UserSide){Actor rough=Roster.FirstOrDefault(a=>a.OnCourt&&a.Side!=UserSide&&a.StunTime<=0&&a.PunchCooldown<=0&&Util.Distance(a.X,a.Y,BallX,BallY)<4);if(rough!=null&&Roll()<dt*.012f)PunchActor(rough);}
        for(int i=0;i<2;i++){FireTime[i]=Math.Max(0,FireTime[i]-dt);if(FireTime[i]<=0)Heat[i]=Math.Max(0,Heat[i]-dt*.7f);}
        foreach(var a in Roster){a.JumpTime=Math.Max(0,a.JumpTime-dt);a.StunTime=Math.Max(0,a.StunTime-dt);a.MoveCooldown=Math.Max(0,a.MoveCooldown-dt);a.PunchCooldown=Math.Max(0,a.PunchCooldown-dt);a.BoostTime=Math.Max(0,a.BoostTime-dt);a.CutTime=Math.Max(0,a.CutTime-dt);if(a.StunTime>0)a.Pose="tackle";else if(a.JumpTime>0){a.Pose="highcatch";if(a.Side!=Possession&&a.JumpHeight>2)TryAirBlock(a);}}
    }
    public void Jump(){
        Actor a=User;if(Over||Contest||a==null||!a.OnCourt||a.StunTime>0||a.JumpTime>0||a.MoveCooldown>0||!(Phase=="live"||Phase=="loose"||Phase=="pass"||Phase=="shot"))return;
        a.JumpTime=.85f;a.BlockAttempted=false;a.MoveCooldown=.9f;a.Action=.28f;a.Pose="highcatch";a.Fatigue=Math.Max(.15f,a.Fatigue-.035f);Sound("jump");
        if(a.Side!=Possession)TryAirBlock(a);
        else if(Phase=="loose")Flash("SKY FOR THE BOARD!",.3f);
    }
    void TryAirBlock(Actor defender){
        if(Phase!="shot"||ShotActor<0||ShotBlocked||FreeThrows>0||defender.BlockAttempted||defender.StunTime>0)return;
        Actor shooter=Roster[ShotActor];float progress=Flight/Math.Max(.01f,FlightDuration);
        bool release=progress<.32f&&Util.Distance(defender.X,defender.Y,shooter.X,shooter.Y)<6;
        bool rim=progress>.58f&&BallHeight<13&&Util.Distance(defender.X,defender.Y,BallX,BallY)<7;
        if(!release&&!rim)return;defender.BlockAttempted=true;
        if(Roll()<Util.Clamp(.48f+(defender.Data.Defense-shooter.Data.Finishing)*.006f,.2f,.88f)){
            ShotBlocked=true;ShotMade=false;defender.Box.Blocks++;Hype(defender.Side,25,"GET THAT OUT!");Sound("block");Sound("crowd_gasp");Flash("REJECTED!",2.3f);Say("BLOCKED / FIND THE LOOSE BALL");
        }
    }
    public void Spin(){
        Actor a=User;if(!CanDribble(a))return;a.Pose="spin";a.Action=.5f;a.MoveCooldown=.65f;a.BoostTime=.5f;a.Fatigue=Math.Max(.15f,a.Fatigue-.045f);
        Burst(a,Attack(a.Side)*3.5f,Math.Abs(MoveY)>.1f?Math.Sign(MoveY)*3:3);BeatDefender(a,.65f);Flash("SPIN CYCLE!",.6f);Sound("squeak");
    }
    public void StepBack(){
        Actor a=User;if(!CanDribble(a))return;a.Pose="juke";a.Action=.28f;a.MoveCooldown=.65f;a.Fatigue=Math.Max(.15f,a.Fatigue-.035f);
        Burst(a,-Attack(a.Side)*5,MoveY*2);a.SeparationTime=1.2f;BeatDefender(a,.4f);Flash("STEP BACK / LET IT FLY",.5f);Sound("squeak");
    }
    bool CanDribble(Actor a){return !Over&&!Contest&&Phase=="live"&&a!=null&&a.StunTime<=0&&a.MoveCooldown<=0&&Owner==Controlled&&a.Side==Possession;}
    void Burst(Actor a,float dx,float dy){a.X=Util.Clamp(a.X+dx,1,93);a.Y=Util.Clamp(a.Y+dy,1,49);SyncBall();}
    void BeatDefender(Actor a,float chance){foreach(var b in Roster.Where(b=>b.OnCourt&&b.Side!=a.Side&&b.StunTime<=0&&Util.Distance(b.X,b.Y,a.X,a.Y)<8)){if(Roll()<chance+(a.Data.Handle-b.Data.Defense)*.005){b.StunTime=.6f;b.Pose="tackle";Hype(a.Side,12,"ANKLE BREAKER");Sound("crowd_gasp");break;}}}
    public void CallCut(){
        if(Over||Contest||Phase!="live"||BallOwner==null||BallOwner.Side!=UserSide)return;
        var runner=Roster.Where(a=>a.OnCourt&&a.Side==UserSide&&a!=BallOwner&&a.StunTime<=0).OrderByDescending(a=>a.Data.Speed-Util.Distance(a.X,a.Y,Hoop(a.Side),25)*.25f).FirstOrDefault();
        if(runner==null)return;CutTarget=Roster.IndexOf(runner);CutTime=3.5f;runner.CutTime=3.5f;runner.BoostTime=1.2f;Flash("CUT! / "+runner.Data.Name,.3f);Sound("call");
    }
    public void DunkOrLob(){
        if(Over||Contest||Phase!="live"||BallOwner==null||BallOwner.Side!=UserSide||BallOwner.StunTime>0)return;
        Actor a=BallOwner;float distance=Util.Distance(a.X,a.Y,Hoop(a.Side),25);
        if(distance<=18){DunkAttempt=true;FinishStyle=a.BoostTime>0?"TURBO JAM":"POWER DUNK";a.JumpTime=.85f;Shoot(.5f);return;}
        Actor target=Roster.Where(b=>b.OnCourt&&b.Side==a.Side&&b!=a&&b.StunTime<=0&&Util.Distance(b.X,b.Y,Hoop(b.Side),25)<20).OrderBy(b=>ContestLevel(b)-b.Data.Finishing*.007f).FirstOrDefault();
        if(target==null){CallCut();Flash("SEND A CUTTER / GET TO THE RIM",.2f);return;}
        BeginPass(target,true);AlleyPass=true;AlleyTarget=Roster.IndexOf(target);target.JumpTime=.85f;Flash("THROW IT UP!",.6f);Sound("jump");
    }
    void FinishAlley(){
        if(!AlleyPass||Owner!=AlleyTarget)return;
        AlleyPass=false;AlleyTarget=-1;
        if(BallOwner!=null&&BallOwner.StunTime<=0&&Util.Distance(BallOwner.X,BallOwner.Y,Hoop(BallOwner.Side),25)<22){DunkAttempt=true;FinishStyle="ALLEY OOP";BallOwner.JumpTime=.85f;Shoot(.5f);ShotWasAlley=true;}
    }
    public float RefereeSight(Actor attacker,Actor victim){
        float d=Util.Distance(attacker.X,attacker.Y,RefX,RefY);float chance=.18f+.66f*Util.Clamp(1-d/85,0,1);
        float vx=attacker.X-RefX,vy=attacker.Y-RefY,den=vx*vx+vy*vy;
        foreach(var blocker in Roster.Where(a=>a.OnCourt&&a!=attacker&&a!=victim)){float t=den<1?0:((blocker.X-RefX)*vx+(blocker.Y-RefY)*vy)/den;if(t>.12f&&t<.90f&&Util.Distance(blocker.X,blocker.Y,RefX+t*vx,RefY+t*vy)<3){chance*=.5f;break;}}
        return Util.Clamp(chance,.12f,.9f);
    }
    public void Punch(){PunchActor(User);}
    void PunchActor(Actor a){
        if(Over||Contest||a==null||!a.OnCourt||a.StunTime>0||a.PunchCooldown>0||!(Phase=="live"||Phase=="loose"))return;
        a.PunchCooldown=1.1f;a.Action=.32f;a.Pose="punch";a.Fatigue=Math.Max(.12f,a.Fatigue-.07f);Sound("swing");
        Actor victim=Roster.Where(b=>b.OnCourt&&b.Side!=a.Side&&!b.Ejected&&Util.Distance(a.X,a.Y,b.X,b.Y)<=6).OrderBy(b=>Util.Distance(a.X,a.Y,b.X,b.Y)).FirstOrDefault();
        if(victim==null){Flash("SWING AND A MISS",.25f);return;}
        float dx=victim.X-a.X,dy=victim.Y-a.Y,d=Math.Max(.1f,Util.Distance(a.X,a.Y,victim.X,victim.Y));victim.X=Util.Clamp(victim.X+dx/d*5,1,93);victim.Y=Util.Clamp(victim.Y+dy/d*5,1,49);victim.StunTime=1.1f;victim.Pose="tackle";Sound("hit");Flash("BOOM!",2.3f);
        bool caught=Roll()<RefereeSight(a,victim);
        if(caught){
            a.Flagrants++;a.Box.Fouls++;TeamFouls[a.Side]++;if(Owner==Roster.IndexOf(a))a.Box.Turnovers++;
            a.Ejected=a.Flagrants>=2||a.Box.Fouls>=6;Charging=false;FreeThrowShooter=Roster.IndexOf(victim);FreeThrows=2;PenaltyPossession=victim.Side;
            if(a.Ejected){a.OnCourt=false;FillLineup(a.Side);}
            BeginFreeThrows();Flash(a.Ejected?"EJECTED! / TWO SHOTS + BALL":"REF SAW IT! / TWO SHOTS + BALL",1.8f);Say("FLAGRANT FOUL / "+a.Data.Name+(a.Ejected?" EJECTED":" WARNING "+a.Flagrants));Sound("whistle");Sound("crowd_boo");
        }else{
            if(Owner==Roster.IndexOf(victim)){victim.Box.Turnovers++;Owner=-1;Charging=false;Phase="loose";LooseFromShot=false;StateTime=.35f;BallX=victim.X;BallY=victim.Y;BallHeight=0;}
            Flash("REF MISSED IT! / PLAY ON",2.3f);Say("NO WHISTLE / PLAY ON");Sound("crowd_gasp");
        }
    }
    void ArcadeBasket(Actor a){
        if(Contest){Sound("score");return;}
        Combo[1-a.Side]=0;Heat[1-a.Side]=Math.Max(0,Heat[1-a.Side]-8);
        Hype(a.Side,ShotWasDunk?34:PointsValue==3?24:16,ShotWasDunk?(ShotWasAlley?"ALLEY OOP!":"BOOMSHAKALAKA!"):PointsValue==3?"FROM DOWNTOWN!":"BUCKET!");
        Sound(ShotWasDunk?"dunk":"score");if(ShotWasDunk){Sound("crowd_roar");Impact=3.4f;ImpactTime=.4f;}
    }
}
}
