using System;
using System.Collections.Generic;
using System.Linq;
namespace Upocourt  {
    public class Actor  {
        public Athlete Data=new Athlete();
        public Stats Box=new Stats();
        public int Side,Slot;
        public bool OnCourt;
        public float X,Y,Fatigue=1,Action,Step,VX,VY;
        public string Pose="idle";
        public float EffectiveSpeed  {
            get {
                return (11+Data.Speed*.075f)*(.68f+.32f*Fatigue);
            }
        }
    }
    // Adapter preserves Downfield's original 24 x 30 sprite geometry and animations.
    public class Player  {
        public int Primary=-1,Secondary=-1,Uniform,Team,Number;
        public float VX,VZ,Step,PoseTime;
        public string Pose="idle";
        public bool HasBall;
    }
    public class Match  {
        public int Version=1,HomeClub,AwayClub,UserSide,FixtureId=-1,HomeScore,AwayScore,Quarter=1,QuarterLength=90,Overtimes,Difficulty=1,Possession,Owner=-1,Controlled,LastPasser=-1,ShotActor=-1,PassTarget=-1,PointsValue,FreeThrows,FreeThrowShooter=-1,DefenseScheme,PlayIndex,Seed=4141,RandomCalls;
        public int[] Timeouts= {
            3,3
        }
        ,TeamFouls= {
            0,0
        };
        public float Clock=90,ShotClock=24,Backcourt=8,StateTime=1.2f,Charge,Flight,FlightDuration,BallX,BallY,BallHeight,StartX,StartY,TargetX,TargetY,ShotChance,PassAge,PossessionTime,PlayTime,ScreenTime;
        public bool Over,Practice,Contest,Charging,ShotMade,ShotBlocked,AutoSubs=true,ShotAssist,ResultCommitted,CrossedHalf;
        public string Phase="inbound",Message="TIP OFF",ShotFeedback="",HomeName="",AwayName="";
        public int HomeColor,AwayColor;
        public List<Actor> Roster=new List<Actor>();
        public List<string> Log=new List<string>();
        public Play ActivePlay=Play.Builtins()[0];
        public int ContestShots,ContestPoints,ContestBest;
        public float ContestClock=70;
        public List<int> ContestRacks=new List<int>();
        public Actor BallOwner  {
            get {
                return Owner>=0&&Owner<Roster.Count?Roster[Owner]:null;
            }
        }
        public Actor User  {
            get {
                return Controlled>=0&&Controlled<Roster.Count?Roster[Controlled]:null;
            }
        }
        double Roll() {
            Random r=new Random(unchecked(Seed+RandomCalls++*7919));
            return r.NextDouble();
        }
        public static Match Create(League league,int home,int away,int user,Settings settings,int fixture) {
            Match m=new Match();
            m.HomeClub=home;
            m.AwayClub=away;
            m.UserSide=user<0?-1:user==home?0:1;
            m.HomeName=league.Clubs[home].Name;
            m.AwayName=league.Clubs[away].Name;
            m.HomeColor=league.Clubs[home].Color;
            m.AwayColor=league.Clubs[away].Color;
            m.FixtureId=fixture;
            m.QuarterLength=settings.QuarterSeconds;
            m.Clock=m.QuarterLength;
            m.Difficulty=settings.Difficulty;
            m.AutoSubs=settings.AutoSubs;
            m.ShotAssist=settings.ShotAssist;
            m.Seed=league.Season*100003+fixture*31+home*17+away*13;
            for(int side=0;side<2;side++) {
                int team=side==0?home:away;
                var squad=league.Squad(team);
                foreach(var p in squad) {
                    Actor a=new Actor();
                    a.Data=p.Copy();
                    Club club=league.Clubs[team];
                    a.Data.Shooting=Math.Min(99,a.Data.Shooting+club.Offense);
                    a.Data.Passing=Math.Min(99,a.Data.Passing+club.Offense);
                    a.Data.Defense=Math.Min(99,a.Data.Defense+club.Defense);
                    a.Data.Stamina=Math.Min(99,a.Data.Stamina+club.Conditioning*2);
                    a.Side=side;
                    a.Slot=Array.IndexOf(league.Clubs[team].Lineup,p.Id);
                    a.OnCourt=a.Slot>=0&&p.InjuryRounds==0;
                    m.Roster.Add(a);
                }
                m.FillLineup(side);
            }
            m.Possession=0;
            m.Setup(0,"TIP OFF / HOME BALL");
            return m;
        }
        void FillLineup(int side) {
            var on=Roster.Where(a=>a.Side==side&&a.OnCourt&&a.Data.InjuryRounds==0&&a.Box.Fouls<6).ToList();
            foreach(var a in Roster.Where(a=>a.Side==side))if(a.Data.InjuryRounds>0||a.Box.Fouls>=6)a.OnCourt=false;
            HashSet<int> slots=new HashSet<int>(on.Select(a=>a.Slot));
            for(int slot=0;slot<5;slot++)if(!slots.Contains(slot)) {
                Actor best=Roster.Where(a=>a.Side==side&&!a.OnCourt&&a.Data.InjuryRounds==0&&a.Box.Fouls<6).OrderByDescending(a=>a.Data.OVR+(a.Data.Eligible(slot)?12:0)).FirstOrDefault();
                if(best!=null) {
                    best.Slot=slot;
                    best.OnCourt=true;
                }
            }
        }
        float Hoop(int side) {
            return side==0?88.75f:5.25f;
        }
        float Attack(int side) {
            return side==0?1:-1;
        }
        float Mirror(float x,int side) {
            return side==0?x:94-x;
        }
        public bool ThreePoint(float x,float y,int side) {
            return Math.Abs(y-25)>22&&Math.Abs(x-Hoop(side))<14||Util.Distance(x,y,Hoop(side),25)>23.75f;
        }
        void Say(string text) {
            Message=text;
            Log.Insert(0,text);
            if(Log.Count>40)Log.RemoveAt(40);
        }
        public void Setup(int side,string why) {
            Possession=side;
            Phase="inbound";
            StateTime=.9f;
            ShotClock=24;
            Backcourt=8;
            CrossedHalf=false;
            PossessionTime=0;
            PlayTime=0;
            Charging=false;
            Charge=0;
            LastPasser=-1;
            ShotActor=-1;
            PassTarget=-1;
            BallHeight=0;
            AutoRotate();
            for(int t=0;t<2;t++) {
                FillLineup(t);
                foreach(var a in Roster.Where(a=>a.OnCourt&&a.Side==t)) {
                    float x=t==side?19+a.Slot*4:39+a.Slot*5;
                    float y=new float[] {
                        25,7,43,17,34
                    }
                    [Math.Max(0,a.Slot)];
                    a.X=Mirror(x,side);
                    a.Y=y;
                }
            }
            Actor handler=Roster.Where(a=>a.Side==side&&a.OnCourt).OrderBy(a=>a.Slot).FirstOrDefault();
            Owner=handler==null?-1:Roster.IndexOf(handler);
            if(UserSide==side)Controlled=Owner;
            else SwitchNearest();
            Say(why);
            SyncBall();
        }
        void AutoRotate() {
            if(!AutoSubs)return;
            foreach(int side in new[] {
                0,1
            }
            )foreach(var a in Roster.Where(a=>a.Side==side&&a.OnCourt).ToList()) {
                var bench=Roster.Where(b=>b.Side==side&&!b.OnCourt&&b.Data.InjuryRounds==0&&b.Box.Fouls<6&&b.Data.Eligible(a.Slot)).OrderByDescending(b=>b.Fatigue+b.Data.OVR*.006f).FirstOrDefault();
                if(bench!=null&&(a.Fatigue<.58f||a.Box.Fouls>=6||a.Box.Fouls>=4&&Quarter<4||a.Box.Seconds>QuarterLength*4*a.Data.Rotation/40f&&bench.Fatigue>a.Fatigue+.08f)) {
                    bench.Slot=a.Slot;
                    bench.OnCourt=true;
                    a.OnCourt=false;
                }
            }
        }
        public bool Substitute(int outId,int inId) {
            if(Phase=="live"||Phase=="shot"||Phase=="pass"||Phase=="loose")return false;
            Actor a=Roster.Find(p=>p.Data.Id==outId),b=Roster.Find(p=>p.Data.Id==inId);
            if(a==null||b==null||a.Side!=UserSide||b.Side!=UserSide||!a.OnCourt||b.OnCourt||b.Data.InjuryRounds>0||b.Box.Fouls>=6||!b.Data.Eligible(a.Slot))return false;
            b.OnCourt=true;
            b.Slot=a.Slot;
            a.OnCourt=false;
            if(Owner==Roster.IndexOf(a))Owner=Roster.IndexOf(b);
            if(Controlled==Roster.IndexOf(a))Controlled=Roster.IndexOf(b);
            return true;
        }
        public void SwitchNearest() {
            var list=Roster.Where(a=>a.OnCourt&&a.Side==UserSide).OrderBy(a=>Util.Distance(a.X,a.Y,BallX,BallY)).ToList();
            if(list.Count>0)Controlled=Roster.IndexOf(list[0]);
        }
        void SyncBall() {
            if(BallOwner!=null) {
                BallX=BallOwner.X;
                BallY=BallOwner.Y;
            }
        }
        public void StartShot() {
            if(Over)return;
            if(Phase=="freethrow"&&FreeThrowShooter>=0&&Roster[FreeThrowShooter].Side==UserSide) {
                Charging=true;
                Charge=0;
                return;
            }
            if(Phase!="live"||BallOwner==null||BallOwner.Side!=UserSide)return;
            Charging=true;
            Charge=0;
            BallOwner.Pose="fake";
        }
        public float Meter  {
            get {
                return Charge<=1?Charge:Math.Max(0,2-Charge);
            }
        }
        public void ReleaseShot() {
            if(!Charging)return;
            Charging=false;
            if(Phase=="freethrow") {
                float quality=1-Math.Abs(Meter-.72f)*1.4f;
                ResolveFreeThrow(quality);
                return;
            }
            Shoot(Meter);
        }
        public float ContestLevel(Actor shooter) {
            float value=0;
            foreach(var defender in Roster.Where(a=>a.OnCourt&&a.Side!=shooter.Side)) {
                float d=Util.Distance(defender.X,defender.Y,shooter.X,shooter.Y);
                value=Math.Max(value,Math.Max(0,(7-d)/7f)*(defender.Data.Defense/100f)*(defender.Action>0?1.3f:1));
            }
            return Util.Clamp(value,0,1);
        }
        public void Shoot(float timing) {
            Actor a=BallOwner;
            if(a==null||Phase!="live")return;
            Charging=false;
            float d=Util.Distance(a.X,a.Y,Hoop(a.Side),25);
            float contest=ContestLevel(a);
            PointsValue=ThreePoint(a.X,a.Y,a.Side)?3:2;
            bool inside=d<10;
            float ideal=inside?.5f:.72f,window=.055f+(a.Data.Shooting-60)*.0008f;
            bool green=Math.Abs(timing-ideal)<window;
            float quality=ShotAssist?.85f:Math.Max(.05f,1-Math.Abs(timing-ideal)*2.3f);
            ShotChance=Util.Clamp((inside?.57f+(a.Data.Finishing-70)*.008f:.40f+(a.Data.Shooting-70)*.009f)-(PointsValue==3?.12f:0)-Math.Max(0,d-26)*.025f-contest*.48f-(1-a.Fatigue)*.20f,.025f,.97f)*quality;
            if(green&&contest<.45f&&d<29)ShotChance=Math.Max(ShotChance,.96f);
            if(a.Side==UserSide)ShotChance=Util.Clamp(ShotChance+(1-Difficulty)*.04f,.02f,.98f);
            ShotFeedback=(green?"GREEN":timing<ideal-.08?"EARLY":timing>ideal+.08?"LATE":"GOOD")+" / "+(contest>.65?"SMOTHERED":contest>.3?"CONTESTED":"OPEN")+" / "+(int)(ShotChance*100)+"%";
            a.Box.FGA++;
            if(PointsValue==3)a.Box.TPA++;
            ShotActor=Owner;
            ShotMade=Roll()<ShotChance;
            ShotBlocked=false;
            Actor def=Roster.Where(b=>b.OnCourt&&b.Side!=a.Side).OrderBy(b=>Util.Distance(a.X,a.Y,b.X,b.Y)).FirstOrDefault();
            if(def!=null&&def.Action>0&&Util.Distance(a.X,a.Y,def.X,def.Y)<4&&Roll()<.18+(def.Data.Defense-70)*.005) {
                ShotBlocked=true;
                ShotMade=false;
                def.Box.Blocks++;
            }
            if(!ShotBlocked&&def!=null&&Util.Distance(a.X,a.Y,def.X,def.Y)<5&&Roll()<.045+(def.Action>0?.09:0)) {
                def.Box.Fouls++;
                TeamFouls[def.Side]++;
                FreeThrowShooter=Owner;
                FreeThrows=ShotMade?1:PointsValue;
                if(!ShotMade) {
                    a.Box.FGA--;
                    if(PointsValue==3)a.Box.TPA--;
                }
            }
            StartX=a.X;
            StartY=a.Y;
            TargetX=Hoop(a.Side);
            TargetY=25;
            Flight=0;
            FlightDuration=.65f+d*.014f;
            Phase="shot";
            Owner=-1;
            BallHeight=2;
            a.Pose=inside?"highcatch":"throw";
            a.Action=.55f;
            Say(ShotBlocked?"BLOCKED!":inside?(d<4&&a.Data.Finishing>80?"DUNK ATTEMPT":"AT THE RIM"):"SHOT AWAY");
        }
        public void Pass(int slot,bool lob) {
            Actor a=BallOwner;
            if(Phase!="live"||a==null||a.Side!=UserSide)return;
            Actor target=slot<0?Roster.Where(b=>b.OnCourt&&b.Side==a.Side&&b!=a).OrderByDescending(b=>b.X*Attack(a.Side)-ContestLevel(b)*20).FirstOrDefault():Roster.Find(b=>b.OnCourt&&b.Side==a.Side&&b.Slot==slot);
            if(target==null||target==a)return;
            BeginPass(target,lob);
        }
        void BeginPass(Actor target,bool lob) {
            Actor a=BallOwner;
            if(a==null)return;
            LastPasser=Owner;
            PassTarget=Roster.IndexOf(target);
            PassAge=0;
            StartX=a.X;
            StartY=a.Y;
            TargetX=target.X;
            TargetY=target.Y;
            Flight=0;
            FlightDuration=Util.Distance(a.X,a.Y,target.X,target.Y)/(lob?55:80);
            FlightDuration=Math.Max(.18f,FlightDuration);
            PointsValue=lob?1:0;
            Phase="pass";
            Owner=-1;
            a.Pose="throw";
            a.Action=.35f;
        }
        public void Skill() {
            if(Phase!="live"||User==null||User.Side!=Possession||Owner!=Controlled||User.Action>0)return;
            if(Charging) {
                Charging=false;
                Charge=0;
                User.Action=.35f;
                User.Pose="fake";
                Say("PUMP FAKE");
                foreach(var defender in Roster.Where(b=>b.OnCourt&&b.Side!=Possession&&Util.Distance(b.X,b.Y,User.X,User.Y)<6))if(Roll()<.5) {
                    defender.Action=.8f;
                    defender.Pose="highcatch";
                }
                return;
            }
            User.Action=.45f;
            User.Pose="juke";
            User.Fatigue=Math.Max(.2f,User.Fatigue-.025f);
            foreach(var b in Roster.Where(b=>b.OnCourt&&b.Side!=Possession&&Util.Distance(b.X,b.Y,User.X,User.Y)<5)) {
                if(Roll()<.45+(User.Data.Handle-b.Data.Defense)*.01)b.Action=.4f;
                else if(Roll()<.16) {
                    User.Box.Turnovers++;
                    b.Box.Steals++;
                    TakeBall(b,"STRIPPED");
                    break;
                }
            }
        }
        public void Screen() {
            if(Phase!="live"||User==null)return;
            if(User.Side==Possession) {
                ScreenTime=2.5f;
                Say("SCREEN CALLED / USE THE SPACE");
            }
            else {
                User.Pose="block";
                User.Action=.55f;
            }
        }
        public void Steal() {
            Actor user=User,a=BallOwner;
            if(Phase!="live"||user==null||a==null||user.Side==a.Side||user.Action>0)return;
            user.Action=.5f;
            user.Pose="block";
            if(Util.Distance(user.X,user.Y,a.X,a.Y)>5)return;
            double roll=Roll();
            if(roll<.12+(user.Data.Defense-a.Data.Handle)*.008) {
                user.Box.Steals++;
                a.Box.Turnovers++;
                TakeBall(user,"STEAL / GO!");
            }
            else if(roll>.78) {
                user.Box.Fouls++;
                TeamFouls[user.Side]++;
                if(TeamFouls[user.Side]>=5) {
                    FreeThrowShooter=Owner;
                    FreeThrows=2;
                    BeginFreeThrows();
                }
                else Setup(a.Side,"REACH-IN FOUL / INBOUND");
            }
        }
        public void Block() {
            if(User==null||User.Side==Possession||User.Action>0)return;
            User.Action=.65f;
            User.Pose="highcatch";
            if(Phase=="shot"&&ShotActor>=0&&Flight<.3f) {
                Actor shooter=Roster[ShotActor];
                if(Util.Distance(User.X,User.Y,shooter.X,shooter.Y)<5&&Roll()<.3+(User.Data.Defense-70)*.007&&!ShotBlocked) {
                    ShotBlocked=true;
                    ShotMade=false;
                    User.Box.Blocks++;
                    Say("BLOCKED!");
                }
            }
        }
        void TakeBall(Actor a,string text) {
            Possession=a.Side;
            Owner=Roster.IndexOf(a);
            ShotClock=24;
            Backcourt=8;
            CrossedHalf=Mirror(a.X,a.Side)>47;
            PossessionTime=PlayTime=0;
            Phase="live";
            LastPasser=-1;
            Charging=false;
            if(a.Side==UserSide)Controlled=Owner;
            else SwitchNearest();
            Say(text);
            SyncBall();
        }
        public bool Timeout() {
            if(Over||UserSide<0||Timeouts[UserSide]<=0||Phase=="shot"||Phase=="pass"||Possession!=UserSide)return false;
            Timeouts[UserSide]--;
            foreach(var a in Roster)a.Fatigue=Math.Min(1,a.Fatigue+.15f);
            Setup(Possession,"TIMEOUT / RECOVERED ENERGY");
            return true;
        }
        void BeginFreeThrows() {
            Owner=-1;
            Phase="freethrow";
            StateTime=1.1f;
            Charging=false;
            Charge=0;
            Actor shooter=Roster[FreeThrowShooter];
            shooter.X=Mirror(75,shooter.Side);
            shooter.Y=25;
            BallX=shooter.X;
            BallY=25;
            if(shooter.Side==UserSide)Controlled=FreeThrowShooter;
            Say("FREE THROWS / "+FreeThrows+" REMAIN / HOLD SHOOT");
        }
        void ResolveFreeThrow(float quality) {
            Actor a=Roster[FreeThrowShooter];
            a.Box.FTA++;
            bool made=Roll()<Util.Clamp(.55f+(a.Data.Shooting-65)*.011f,.35f,.95f)*quality;
            if(made) {
                a.Box.FTM++;
                a.Box.Points++;
                AddScore(a.Side,1);
            }
            FreeThrows--;
            Charging=false;
            Charge=0;
            ShotFeedback=made?"FREE THROW GOOD":"FREE THROW MISSED";
            if(FreeThrows>0) {
                StateTime=1.1f;
                Say(ShotFeedback+" / "+FreeThrows+" REMAIN");
            }
            else {
                int side=a.Side;
                FreeThrowShooter=-1;
                Setup(1-side,ShotFeedback);
            }
        }
        void AddScore(int side,int pts) {
            if(side==0)HomeScore+=pts;
            else AwayScore+=pts;
        }
        void ResolveShot() {
            Actor shooter=Roster[ShotActor];
            if(ShotMade) {
                shooter.Box.FGM++;
                shooter.Box.Points+=PointsValue;
                if(PointsValue==3)shooter.Box.TPM++;
                AddScore(shooter.Side,PointsValue);
                if(LastPasser>=0&&LastPasser!=ShotActor&&PassAge<4)Roster[LastPasser].Box.Assists++;
                if(Contest) {
                    ContestPoints+=ContestShots%5==4?2:1;
                    NextContest();
                    return;
                }
                if(FreeThrows>0) {
                    BeginFreeThrows();
                    return;
                }
                Setup(1-shooter.Side,PointsValue==3?"THREE!":"BUCKET!");
            }
            else {
                if(Contest) {
                    NextContest();
                    return;
                }
                if(FreeThrows>0) {
                    BeginFreeThrows();
                    return;
                }
                Phase="loose";
                StateTime=.55f;
                BallHeight=0;
                BallX=Util.Clamp(TargetX+(float)(Roll()-.5)*12,1,93);
                BallY=Util.Clamp(25+(float)(Roll()-.5)*14,1,49);
                Say(ShotBlocked?"BLOCK / LOOSE BALL":"OFF THE IRON / REBOUND!");
            }
        }
        public void Update(float dt,float moveX,float moveY,bool sprint) {
            if(Over)return;
            dt=Math.Min(.04f,Math.Max(0,dt));
            PassAge+=dt;
            StateTime-=dt;
            ScreenTime=Math.Max(0,ScreenTime-dt);
            foreach(var a in Roster) {
                a.Action=Math.Max(0,a.Action-dt);
                if(a.Action==0)a.Pose="idle";
                if(a.OnCourt) {
                    a.Fatigue=Math.Max(.18f,a.Fatigue-dt*(sprint&&a==User?.011f:.003f)*(1.6f-a.Data.Stamina/100f));
                    if(Phase=="live"||Phase=="shot"||Phase=="pass"||Phase=="loose") {
                        a.Box.Seconds+=dt;
                        a.Box.Games=1;
                    }
                }
                else a.Fatigue=Math.Min(1,a.Fatigue+dt*.018f);
            }
            if(Charging) {
                Charge=Math.Min(1.99f,Charge+dt*(Phase=="freethrow"?.72f:.9f));
                if(Charge>=1.99f)ReleaseShot();
            }
            if(Contest) {
                ContestClock=Math.Max(0,ContestClock-dt);
                if(ContestClock<=0&&Phase!="shot") {
                    Over=true;
                    Say("CONTEST FINAL / "+ContestPoints+" POINTS");
                    return;
                }
            }
            if(Phase=="inbound") {
                if(StateTime<=0) {
                    Phase="live";
                    SyncBall();
                }
                return;
            }
            if(Phase=="freethrow") {
                if(StateTime<=0&&FreeThrowShooter>=0&&(Roster[FreeThrowShooter].Side!=UserSide||UserSide<0))ResolveFreeThrow(.75f+(float)Roll()*.25f);
                return;
            }
            if(Phase=="break") {
                if(StateTime<=0) {
                    Clock=Quarter<=4?QuarterLength:30;
                    TeamFouls=new int[2];
                    if(Quarter==3)Timeouts=new int[] {
                        3,3
                    };
                    Setup((Quarter-1)%2,"PERIOD "+Quarter);
                }
                return;
            }
            if(Phase=="live"||Phase=="pass"||Phase=="shot"||Phase=="loose") {
                if(!Practice&&!Contest)Clock=Math.Max(0,Clock-dt);
                if(Phase=="live"||Phase=="pass") {
                    ShotClock=Math.Max(0,ShotClock-dt);
                    PossessionTime+=dt;
                    PlayTime+=dt;
                    if(ShotClock<=0) {
                        if(BallOwner!=null)BallOwner.Box.Turnovers++;
                        Setup(1-Possession,"SHOT CLOCK VIOLATION");
                        CheckPeriod();
                        return;
                    }
                }
            }
            if(Phase=="live") {
                if(UserSide>=0&&User!=null) {
                    if(!Contest)Move(User,moveX,moveY,dt,sprint);
                    if(Charging)User.Pose="fake";
                }
                RunAI(dt);
                SyncBall();
                if(BallOwner!=null) {
                    float x=Mirror(BallOwner.X,Possession);
                    if(x>=47)CrossedHalf=true;
                    if(CrossedHalf&&x<45) {
                        BallOwner.Box.Turnovers++;
                        Setup(1-Possession,"BACKCOURT VIOLATION");
                    }
                    else if(!CrossedHalf) {
                        Backcourt-=dt;
                        if(Backcourt<=0) {
                            BallOwner.Box.Turnovers++;
                            Setup(1-Possession,"EIGHT SECOND VIOLATION");
                        }
                    }
                }
            }
            else if(Phase=="pass") {
                Flight+=dt;
                float f=Util.Clamp(Flight/FlightDuration,0,1);
                BallX=StartX+(TargetX-StartX)*f;
                BallY=StartY+(TargetY-StartY)*f;
                BallHeight=(float)Math.Sin(f*Math.PI)*(PointsValue==1?8:2)+2;
                MoveOffBall(dt);
                if(f>.15&&f<.88) {
                    Actor defender=Roster.Where(a=>a.OnCourt&&a.Side!=Possession&&Util.Distance(a.X,a.Y,BallX,BallY)<2.3f).FirstOrDefault();
                    if(defender!=null&&Roll()<.02+defender.Data.Defense*.0006) {
                        Roster[LastPasser].Box.Turnovers++;
                        defender.Box.Steals++;
                        TakeBall(defender,"PASS INTERCEPTED");
                    }
                }
                if(Phase=="pass"&&f>=1) {
                    Actor target=Roster[PassTarget];
                    Owner=PassTarget;
                    Phase="live";
                    BallHeight=0;
                    if(target.Side==UserSide)Controlled=Owner;
                    SyncBall();
                }
            }
            else if(Phase=="shot") {
                Flight+=dt;
                float f=Util.Clamp(Flight/FlightDuration,0,1);
                BallX=StartX+(TargetX-StartX)*f;
                BallY=StartY+(TargetY-StartY)*f;
                BallHeight=2+(float)Math.Sin(f*Math.PI)*13;
                MoveOffBall(dt);
                if(f>=1)ResolveShot();
            }
            else if(Phase=="loose") {
                if(User!=null)Move(User,moveX,moveY,dt,sprint);
                foreach(var a in Roster.Where(a=>a.OnCourt&&a!=User))Toward(a,BallX,BallY,dt,.85f);
                if(StateTime<=0) {
                    Actor rebounder=Roster.Where(a=>a.OnCourt).OrderBy(a=>Util.Distance(a.X,a.Y,BallX,BallY)-a.Data.Rebounding*.035f-(a.Action>0?1:0)).First();
                    bool offensive=rebounder.Side==Possession;
                    rebounder.Box.Rebounds++;
                    TakeBall(rebounder,offensive?"OFFENSIVE BOARD / 14 SECONDS":"DEFENSIVE REBOUND");
                    if(offensive)ShotClock=14;
                }
            }
            CheckPeriod();
        }
        void CheckPeriod() {
            if(Practice||Contest||Clock>0||Over||Phase=="shot"||Phase=="freethrow")return;
            if(Quarter<4||HomeScore==AwayScore) {
                Quarter++;
                if(Quarter>4)Overtimes++;
                Phase="break";
                StateTime=2.2f;
                Charging=false;
                Say(Quarter>4?"OVERTIME / 30 SECONDS":Quarter==3?"HALFTIME":"END OF PERIOD");
            }
            else {
                Over=true;
                Phase="final";
                Say("FINAL / "+HomeScore+" - "+AwayScore);
            }
        }
        void Move(Actor a,float dx,float dy,float dt,bool sprint) {
            float len=(float)Math.Sqrt(dx*dx+dy*dy);
            if(len<.05f) {
                a.VX=a.VY=0;
                return;
            }
            len=Math.Max(1,len);
            float speed=a.EffectiveSpeed*(sprint&&a.Fatigue>.25f?1.25f:1);
            if(Charging&&a==BallOwner)speed*=.2f;
            if(a.Action>0&&a.Pose=="highcatch")speed*=.2f;
            float nx=Util.Clamp(a.X+dx/len*speed*dt,.8f,93.2f),ny=Util.Clamp(a.Y+dy/len*speed*dt,1,49);
            foreach(var b in Roster.Where(b=>b.OnCourt&&b!=a)) {
                float distance=Util.Distance(nx,ny,b.X,b.Y);
                if(distance<1.9f) {
                    float push=(1.9f-distance)*.5f;
                    float xx=nx-b.X,yy=ny-b.Y;
                    float d=Math.Max(.1f,distance);
                    nx+=xx/d*push;
                    ny+=yy/d*push;
                }
            }
            a.VX=(nx-a.X)/dt;
            a.VY=(ny-a.Y)/dt;
            a.X=Util.Clamp(nx,.8f,93.2f);
            a.Y=Util.Clamp(ny,1,49);
            a.Step+=dt*7;
            if(a.Action<=0)a.Pose="run";
        }
        void Toward(Actor a,float x,float y,float dt,float scale) {
            float dx=x-a.X,dy=y-a.Y,d=Util.Distance(x,y,a.X,a.Y);
            if(d<.5f)return;
            Move(a,dx/Math.Max(1,d)*scale,dy/Math.Max(1,d)*scale,dt,false);
        }
        void MoveOffBall(float dt) {
            foreach(var a in Roster.Where(a=>a.OnCourt&&a!=User)) {
                float tx=Phase=="shot"?Hoop(Possession):BallX,ty=Phase=="shot"?25:BallY;
                Toward(a,tx+(a.Slot-2)*2,ty+(a.Side==Possession?-1:1)*5,dt,.5f);
            }
        }
        void RunAI(float dt) {
            Actor owner=BallOwner;
            if(owner==null)return;
            foreach(var a in Roster.Where(a=>a.OnCourt&&a!=User).ToList()) {
                if(Phase!="live")break;
                if(a==owner) {
                    float d=Util.Distance(a.X,a.Y,Hoop(a.Side),25);
                    float quality=ContestLevel(a);
                    float target=Mirror(a.X,a.Side)<48?60:a.Data.Shooting>84?70:84;
                    Toward(a,Mirror(target,a.Side),25+(float)Math.Sin(PossessionTime*.7+a.Slot)*12,dt,.83f);
                    if((PossessionTime>2&&(d<10||d<26&&quality<.35f&&Roll()<dt*.9))||ShotClock<2) {
                        Shoot(.64f+(float)Roll()*.14f);
                        break;
                    }
                    if(PossessionTime>2&&Roll()<dt*.7) {
                        Actor targetPlayer=Roster.Where(b=>b.OnCourt&&b.Side==a.Side&&b!=a).OrderBy(b=>ContestLevel(b)+Util.Distance(b.X,b.Y,Hoop(a.Side),25)*.01f).FirstOrDefault();
                        if(targetPlayer!=null&&ContestLevel(targetPlayer)<quality+.1f)BeginPass(targetPlayer,false);
                    }
                }
                else if(a.Side==Possession) {
                    Route route=ActivePlay.Routes.Find(r=>r.Slot==a.Slot);
                    float tx=65+a.Slot*3,ty=new float[] {
                        25,5,45,15,36
                    }
                    [a.Slot];
                    if(route!=null&&route.Points.Count>0) {
                        int node=Math.Min(route.Points.Count-1,(int)(PlayTime/2.5f));
                        tx=route.Points[node].X;
                        ty=route.Points[node].Y;
                    }
                    if(ScreenTime>0&&a.Slot==4) {
                        tx=Mirror(owner.X,owner.Side)+3;
                        ty=owner.Y+2;
                    }
                    Toward(a,Mirror(tx,a.Side),ty,dt,.85f);
                }
                else {
                    Actor mark=Roster.Find(b=>b.OnCourt&&b.Side==Possession&&b.Slot==a.Slot)??owner;
                    float tx,ty;
                    if(DefenseScheme==1&&a.Side==UserSide) {
                        tx=Mirror(a.Slot<2?69:82,Possession);
                        ty=new float[] {
                            14,36,7,25,43
                        }
                        [a.Slot];
                    }
                    else if(DefenseScheme==2&&a.Side==UserSide) {
                        tx=mark.X-Attack(Possession)*1.5f;
                        ty=mark.Y;
                    }
                    else {
                        tx=mark.X+(Hoop(Possession)-mark.X)*.16f;
                        ty=mark.Y+(25-mark.Y)*.16f;
                    }
                    if(mark==owner) {
                        tx=owner.X+Attack(Possession)*2.5f;
                        ty=owner.Y;
                    }
                    if(ScreenTime>0&&Util.Distance(a.X,a.Y,owner.X,owner.Y)<7&&Roll()<dt*.4)a.Action=.6f;
                    Toward(a,tx,ty,dt,a.Action>0?.4f:.93f);
                    if(mark==owner&&Util.Distance(a.X,a.Y,owner.X,owner.Y)<3&&Roll()<dt*(.025+Difficulty*.02)) {
                        if(Roll()<.7) {
                            a.Box.Steals++;
                            owner.Box.Turnovers++;
                            TakeBall(a,"DEFENSE FORCES A TURNOVER");
                        }
                        else {
                            a.Box.Fouls++;
                            TeamFouls[a.Side]++;
                            if(TeamFouls[a.Side]>=5) {
                                FreeThrowShooter=Owner;
                                FreeThrows=2;
                                BeginFreeThrows();
                            }
                            else Setup(Possession,"FOUL / SIDE OUT");
                        }
                        break;
                    }
                    if(Charging&&Util.Distance(a.X,a.Y,owner.X,owner.Y)<5&&Roll()<dt*1.2) {
                        a.Action=.4f;
                        a.Pose="highcatch";
                    }
                }
            }
        }
        public void SimulateRest() {
            if(Over)return;
            Charging=false;
            FreeThrows=0;
            FreeThrowShooter=-1;
            float seconds=Clock+Math.Max(0,4-Quarter)*QuarterLength;
            if(seconds<1)seconds=30;
            int possessions=Math.Max(2,(int)(seconds/7));
            for(int n=0;n<possessions;n++)SimPossession(n%2);
            while(HomeScore==AwayScore) {
                SimPossession(0);
                SimPossession(1);
                Overtimes++;
            }
            Clock=0;
            Over=true;
            Phase="final";
            Say("FINAL / "+HomeScore+" - "+AwayScore);
        }
        void SimPossession(int side) {
            var offense=Roster.Where(q=>q.Side==side&&q.Data.InjuryRounds==0&&q.Box.Fouls<6).OrderByDescending(q=>q.OnCourt?1000+q.Data.OVR:q.Data.OVR).Take(8).ToList();
            var defense=Roster.Where(q=>q.Side!=side&&q.Data.InjuryRounds==0&&q.Box.Fouls<6).OrderByDescending(q=>q.OnCourt?1000+q.Data.OVR:q.Data.OVR).Take(8).ToList();
            if(offense.Count==0||defense.Count==0)return;
            foreach(var actor in offense.Concat(defense)) {
                actor.Box.Games=1;
                actor.Box.Seconds+=7f*5/(actor.Side==side?offense.Count:defense.Count);
            }
            Actor a=offense[(int)(Roll()*offense.Count)],defender=defense[(int)(Roll()*defense.Count)];
            if(Roll()<.12+(defender.Data.Defense-a.Data.Handle)*.001) {
                a.Box.Turnovers++;
                if(Roll()<.65)defender.Box.Steals++;
                return;
            }
            int value=Roll()<.36?3:2;
            int defenseRating=(int)defense.Average(q=>q.Data.Defense);
            a.Box.FGA++;
            if(value==3)a.Box.TPA++;
            bool made=Roll()<Util.Clamp(.48f+(a.Data.Shooting-defenseRating)*.007f-(value==3?.11f:0)+(side==0?.025f:0),.18f,.8f);
            if(made) {
                a.Box.FGM++;
                if(value==3)a.Box.TPM++;
                a.Box.Points+=value;
                AddScore(side,value);
                if(Roll()<.6) {
                    Actor passer=offense[(int)(Roll()*offense.Count)];
                    if(passer!=a)passer.Box.Assists++;
                }
            }
            else {
                if(Roll()<.12)defender.Box.Blocks++;
                Actor board=Roll()<.75?defense.OrderByDescending(q=>q.Data.Rebounding+(int)(Roll()*25)).First():offense.OrderByDescending(q=>q.Data.Rebounding+(int)(Roll()*25)).First();
                board.Box.Rebounds++;
            }
            if(Roll()<.08) {
                if(defender.Box.Fouls<5)defender.Box.Fouls++;
                a.Box.FTA+=2;
                for(int j=0;j<2;j++)if(Roll()<.55+(a.Data.Shooting-65)*.01) {
                    a.Box.FTM++;
                    a.Box.Points++;
                    AddScore(side,1);
                }
            }
        }
        public void StartPractice() {
            Practice=true;
            Clock=QuarterLength;
            FixtureId=-1;
            Setup(UserSide,"SHOOTAROUND / NO CLOCK");
        }
        public void StartContest() {
            Contest=true;
            Practice=true;
            FixtureId=-1;
            ContestPoints=ContestShots=0;
            ContestClock=70;
            NextContestPosition();
        }
        void NextContest() {
            ContestShots++;
            if(ContestShots>=25||ContestClock<=0) {
                Over=true;
                Phase="final";
                Say("THREE POINT FINAL / "+ContestPoints+" OF 30");
                return;
            }
            NextContestPosition();
        }
        void NextContestPosition() {
            Actor shooter=Roster.Where(a=>a.OnCourt&&a.Side==UserSide).OrderByDescending(a=>a.Data.Shooting).First();
            foreach(var a in Roster)a.OnCourt=a==shooter;
            float[] xs= {
                88,72,64.5f,72,88
            }
            ,ys= {
                2,8,25,42,48
            };
            int rack=ContestShots/5;
            shooter.X=Mirror(xs[rack],UserSide);
            shooter.Y=ys[rack];
            Owner=Controlled=Roster.IndexOf(shooter);
            Possession=UserSide;
            Phase="live";
            ShotClock=999;
            Backcourt=999;
            CrossedHalf=true;
            SyncBall();
            Say("RACK "+(rack+1)+" / BALL "+(ContestShots%5+1)+(ContestShots%5==4?" / MONEY BALL":""));
        }
    }
}
