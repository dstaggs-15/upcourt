using System;
using System.Linq;
using System.Drawing;
namespace Upcourt {
#if UPCOURT_TESTS
public static class ArcadeTests {
    static Match Fresh(){Match m=Match.Create(League.Create(0),0,1,0,new Settings(),-1);m.Practice=true;m.Phase="live";Actor user=m.User;user.X=63;user.Y=25;m.BallX=63;m.BallY=25;foreach(var a in m.Roster.Where(a=>a.OnCourt&&a!=user)){a.X=15;a.Y=3+a.Slot*8;}return m;}
    static Actor Opponent(Match m){return m.Roster.First(a=>a.OnCourt&&a.Side!=m.UserSide);}
    static void Step(Match m,int n){for(int i=0;i<n;i++)m.Update(.03f,0,0,false);}
    static double Power(short[] values){return values.Sum(s=>(double)s*s)/values.Length;}
    public static void Run(Action<bool,string> check){
        Match m=Fresh();m.BallX=19;m.User.X=19;CourtCamera cam=new CourtCamera();cam.Follow(m,0);PointF old=cam.Project(47,25,0);float before=cam.FocusX;m.BallX=82;m.BallY=35;cam.Follow(m,.016f);check(cam.FocusX>before&&cam.FocusX<82,"camera smooths instead of snapping");for(int i=0;i<100;i++)cam.Follow(m,.02f);PointF now=cam.Project(47,25,0),ball=cam.Project(82,35,0);check(Math.Abs(now.Y-old.Y)>100,"camera actually moves court projection upcourt");check(ball.X>250&&ball.X<1050&&ball.Y>300&&ball.Y<700,"camera keeps action in frame");m.BallX=10;m.BallY=15;for(int i=0;i<100;i++)cam.Follow(m,.02f);check(cam.FocusX<15&&cam.FocusY<20,"camera follows reversal and lateral play");
        m=Fresh();float x=m.User.X,energy=m.User.Fatigue;m.Spin();check(m.User.X>x&&m.User.BoostTime>0&&m.User.Fatigue<energy,"spin advances and spends energy");m.User.MoveCooldown=0;m.User.Action=0;x=m.User.X;m.StepBack();check(m.User.X<x&&m.User.SeparationTime>0,"stepback creates shooting separation");x=m.User.X;m.StepBack();check(m.User.X==x,"dribble move cooldown prevents spam");
        m=Fresh();m.Jump();check(m.User.JumpTime>0,"manual jump takes off");Step(m,5);check(m.User.JumpHeight>2,"jump has physical height over time");float remaining=m.User.JumpTime;m.Jump();check(m.User.JumpTime==remaining,"cannot jump again in midair");
        m=Fresh();m.User.X=85;m.BallX=85;m.DunkOrLob();check(m.Phase=="shot"&&m.ShotWasDunk&&m.User.JumpTime>0&&m.PointsValue==2,"close finish launches real dunk");m.ShotMade=true;Step(m,24);check(m.HomeScore==2&&m.Roster.Where(a=>a.Side==0).Sum(a=>a.Box.Points)==2,"dunk counts normal two points with coherent box");check(m.Heat[0]>0&&m.Sounds.Contains("dunk"),"dunk builds heat and queues crowd impact");
        m=Fresh();Actor receiver=m.Roster.First(a=>a.OnCourt&&a.Side==0&&a!=m.User);receiver.X=84;receiver.Y=25;m.DunkOrLob();check(m.AlleyPass&&m.Phase=="pass"&&m.AlleyTarget==m.Roster.IndexOf(receiver),"far finish targets an alley oop receiver");int guard=0;while(m.Phase=="pass"&&guard++<100)m.Update(.02f,0,0,false);check(m.Phase=="shot"&&m.ShotWasAlley&&m.ShotWasDunk,"alley catch chains to a dunk");
        m=Fresh();m.CallCut();check(m.CutTarget>=0&&m.CutTime>0&&m.Roster[m.CutTarget].CutTime>0,"cut command changes an actual teammate assignment");m.Hype(0,100,"TEST");check(m.FireTime[0]>0&&m.Heat[0]==100,"earned heat activates on fire");Step(m,10);check(m.FireTime[0]>0&&m.FireTime[0]<12,"on fire duration advances");
        bool rejected=false;
        for(int seed=0;seed<30;seed++){m=Fresh();m.Seed=seed;m.RandomCalls=0;Actor shooter=Opponent(m);shooter.X=64;shooter.Y=25;m.Possession=1;m.Owner=-1;m.Phase="shot";m.ShotActor=m.Roster.IndexOf(shooter);m.FlightDuration=1;m.Flight=.1f;m.ShotMade=true;m.User.X=63;m.User.Y=25;Actor blocker=m.User;m.Block();if(m.ShotBlocked){rejected=true;check(blocker.Box.Blocks==1&&blocker.BlockAttempted&&m.Heat[0]>0,"timed jump rejects a shot and earns heat");Step(m,4);check(blocker.Box.Blocks==1,"airborne block cannot be credited twice");break;}}
        check(rejected,"defensive jump can produce a block");
        bool steal=false,reach=false,caught=false,missed=false,ejected=false,loose=false;int caughtSeed=-1;
        for(int seed=0;seed<80;seed++){
            m=Fresh();m.Seed=seed;m.RandomCalls=0;Actor defender=m.User,carrier=Opponent(m);defender.X=30;defender.Y=25;carrier.X=32;carrier.Y=25;m.Possession=1;m.Owner=m.Roster.IndexOf(carrier);m.Steal();if(defender.Box.Steals>0){steal=true;check(carrier.Box.Turnovers==1&&m.Owner==m.Controlled,"steal transfers actual ownership and credits turnover");}if(defender.Box.Fouls>0)reach=true;
            if(steal&&reach)break;
        }
        check(steal&&reach,"stealing can succeed or draw a reach in foul");
        for(int seed=0;seed<80;seed++){
            m=Fresh();m.Seed=seed;m.RandomCalls=0;Actor victim=Opponent(m);victim.X=65;victim.Y=25;m.RefX=63;m.RefY=-3;m.Punch();check(victim.StunTime>0&&m.User.PunchCooldown>0,"punch has knockback stun and cooldown");
            if(m.User.Flagrants>0){caught=true;caughtSeed=seed;check(m.Phase=="freethrow"&&m.FreeThrows==2&&m.PenaltyPossession==1&&m.User.Box.Fouls==1,"caught punch awards two shots plus possession");float clock=m.Clock;Step(m,2);check(m.Clock==clock,"flagrant free throws stop clock");m.UserSide=-1;guard=0;while(m.Phase=="freethrow"&&guard++<200)m.Update(.03f,0,0,false);check(m.Possession==1&&m.PenaltyPossession==-1,"victim retains possession after flagrant throws");}
            else{missed=true;check(m.Phase=="live"&&m.FreeThrows==0,"unseen punch continues without an automatic foul");}
            if(caught&&missed)break;
        }
        check(caught&&missed,"referee sometimes catches contact and sometimes misses it");
        if(caughtSeed>=0){m=Fresh();m.Seed=caughtSeed;m.RandomCalls=0;Actor offender=m.User,victim=Opponent(m);offender.Flagrants=1;victim.X=65;victim.Y=25;m.RefX=63;m.RefY=-3;m.Punch();ejected=offender.Ejected&&!offender.OnCourt;check(ejected&&m.Roster.Count(a=>a.OnCourt&&a.Side==0)==5,"second caught punch ejects offender and fills real reserve");m.Setup(0,"TEST");check(!offender.OnCourt,"ejected player cannot return on next inbound");}
        for(int seed=0;seed<80;seed++){m=Fresh();m.Seed=seed;m.RandomCalls=0;Actor victim=Opponent(m);m.User.X=30;m.User.Y=25;victim.X=32;victim.Y=25;m.Possession=1;m.Owner=m.Roster.IndexOf(victim);m.Punch();if(m.Phase=="loose"){loose=true;check(!m.LooseFromShot&&victim.Box.Turnovers==1,"unseen punch can knock ball loose without inventing a rebound");Step(m,20);check(m.Roster.Sum(a=>a.Box.Rebounds)==0,"non shot loose ball pickup is not a rebound");break;}}
        check(loose,"punch can create a live loose ball");
        m=Fresh();Actor attacker=m.User,target=Opponent(m);m.RefX=attacker.X;m.RefY=attacker.Y-4;float visible=m.RefereeSight(attacker,target);m.RefX=1;m.RefY=-3;check(m.RefereeSight(attacker,target)<visible,"ref sight probability responds to position");
        m=Fresh();m.Heat[0]=55;m.User.JumpTime=.4f;m.User.Flagrants=1;m.CutTarget=2;m.CutTime=2;SaveData save=new SaveData{Match=m,League=League.Create(0)};save.Settings.Keys=save.Settings.Keys.Take(15).ToArray();save.Settings.Keys[0]="Up";Persistence.Save(save);SaveData loaded=Persistence.Load();check(loaded.Match!=null&&loaded.Match.Heat[0]==55&&loaded.Match.User.Flagrants==1&&loaded.Match.User.JumpTime==.4f,"new arcade state survives save resume");check(loaded.Settings.Keys.Length==21&&loaded.Settings.Keys[0]=="Up"&&loaded.Settings.Keys[19]=="K","old control bindings upgrade without resetting custom keys");check(loaded.Match.Sounds.Count==0,"saved match does not replay stale queued sounds");
        StadiumMixer mixer=new StadiumMixer{Stadium=true,Volume=1};short[] active=new short[22050*2];mixer.Render(active,22050);check(Power(active)>50&&active.Where((v,i)=>i%2==0).Where((v,i)=>v!=active[i*2+1]).Any(),"crowd bed generates sustained stereo noise");mixer.Play("dunk");mixer.Play("steal");check(mixer.VoiceCount>=3,"effects and crowd overlap in one mixer");short[] response=new short[4096];mixer.Render(response,2048);check(Power(response)>0&&response.All(v=>v<short.MaxValue&&v>short.MinValue),"mixed crowd and effects stay inside PCM bounds");mixer.Muted=true;mixer.Render(response,2048);check(response.All(v=>v==0)&&mixer.VoiceCount==0,"mute silences both crowd and effects");mixer.Muted=false;mixer.Suspended=true;mixer.Render(response,2048);check(response.All(v=>v==0),"lost focus silences audio");mixer.Suspended=false;mixer.Paused=true;short[] settle=new short[22050*2];mixer.Render(settle,22050);check(Power(settle)<Power(active)*.2,"pause ducks crowd ambience");
        StadiumMixer effectsOnly=new StadiumMixer{Stadium=true,CrowdVolume=0,EffectsVolume=1,Volume=1};effectsOnly.Render(response,2048);check(response.All(v=>v==0),"crowd slider independently silences bed");effectsOnly.Play("hit");effectsOnly.Render(response,2048);check(Power(response)>0,"court effects still play with crowd muted");
        check(ArcadeSounds.Get("crowd_roar").All(v=>!float.IsNaN(v)&&Math.Abs(v)<=1),"crowd samples finite and bounded");check(ArcadeSounds.Get("ambience").Length==35*ArcadeSounds.Rate,"recorded crowd bed evolves over thirty five seconds");
    }
}
#endif
}
