using System;
using System.Collections.Generic;
using System.Linq;
namespace Upocourt  {
    public class League  {
        public const int Cap=140,MaxRoster=15;
        public int Version=1,UserTeam,Season=1,Round,NextId=1,NextFixture=1,TrainingRound=-1,CoachWins,CoachLosses,Titles,DraftIndex;
        public string Coach="COACH",Notice="Welcome to your front office.";
        public bool Offseason,Drafting,Fired;
        public List<Club> Clubs=new List<Club>();
        public List<Athlete> Players=new List<Athlete>();
        public List<Fixture> Games=new List<Fixture>();
        public List<Series> Series=new List<Series>();
        public List<int> Seeds=new List<int>();
        public List<string> News=new List<string>();
        public List<Award> Awards=new List<Award>();
        public List<SeasonRecord> History=new List<SeasonRecord>();
        public List<Athlete> Prospects=new List<Athlete>();
        public List<int> DraftOrder=new List<int>();
        public List<DraftPick> DraftPicks=new List<DraftPick>();
        public Club Team  {
            get {
                return Clubs[UserTeam];
            }
        }
        public string Stage  {
            get {
                return Drafting?"DRAFT - PICK "+(DraftIndex+1):Offseason?"OFFSEASON":Round<22?"ROUND "+(Round+1)+" / 22":"PLAYOFFS";
            }
        }
        public Fixture UserGame  {
            get {
                return Games.Find(x=>x.Round==Round&&!x.Played&&x.Involves(UserTeam));
            }
        }
        public static League Create(int team) {
            League l=new League();
            l.UserTeam=Util.Bound(team,0,11);
            Random r=new Random(7213);
            for(int t=0;t<12;t++) {
                Club c=new Club();
                c.Id=t;
                c.City=Util.Cities[t];
                c.Mascot=Util.Mascots[t];
                c.Color=Util.Colors[t];
                c.Plan=t%3==0?"CONTEND":t%3==1?"REBUILD":"BALANCED";
                l.Clubs.Add(c);
                for(int i=0;i<12;i++)l.Players.Add(l.Generate(r,t,i%5,71+r.Next(14)));
                l.BestLineup(t);
            }
            for(int i=0;i<30;i++)l.Players.Add(l.Generate(r,-1,i%5,70+i));
            l.Schedule();
            return l;
        }
        public Athlete Generate(Random r,int team,int pos,int rating) {
            Athlete p=new Athlete();
            p.Id=NextId++;
            p.Team=team;
            p.Position=pos;
            p.Secondary=pos==0?1:pos==4?3:pos+(r.Next(2)==0?-1:1);
            p.Number=p.Id%100;
            p.Age=20+r.Next(13);
            p.Experience=Math.Max(0,p.Age-21);
            p.Height=Util.Bound(73+pos*2+r.Next(5),72,87);
            string[] first= {
                "J.","C.","D.","M.","R.","T.","A.","K.","L.","S.","N.","B."
            };
            string[] last= {
                "Hayes","Vale","Reed","Park","Stone","Bell","Walker","Brooks","Turner","James","Cole","Rivers","King","Chase","Blake","Sutton","Miles","Hill","Ford","Price","Young","Ward","Grant","Ross"
            };
            p.Name=first[p.Id%first.Length]+" "+last[(p.Id/first.Length+p.Id*7)%last.Length];
            p.Shooting=Skill(r,rating+(pos<3?4:-5));
            p.Finishing=Skill(r,rating+2);
            p.Passing=Skill(r,rating+(pos==0?8:-2));
            p.Handle=Skill(r,rating+(pos<2?6:-4));
            p.Speed=Skill(r,rating+(pos<2?5:-4));
            p.Defense=Skill(r,rating);
            p.Rebounding=Skill(r,rating+(pos>2?10:-8));
            p.Stamina=Skill(r,rating);
            p.Potential=Math.Min(99,p.OVR+r.Next(5,18));
            p.Salary=SalaryFor(p.OVR);
            p.Years=2+r.Next(3);
            return p;
        }
        static int Skill(Random r,int n) {
            return Util.Bound(n+r.Next(-5,6),55,99);
        }
        public static int SalaryFor(int ovr) {
            return Math.Max(2,2+(ovr-65)/3+Math.Max(0,ovr-85)/2);
        }
        public List<Athlete> Squad(int t) {
            return Players.Where(p=>p.Team==t).OrderBy(p=>p.Position).ThenByDescending(p=>p.OVR).ToList();
        }
        public int Payroll(int t) {
            return Squad(t).Sum(p=>p.Salary);
        }
        public Athlete Player(int id) {
            return Players.Find(p=>p.Id==id);
        }
        public int Rating(int t) {
            var squad=Squad(t).Where(p=>p.InjuryRounds==0).OrderByDescending(p=>p.OVR).Take(8).ToList();
            return squad.Count==0?60:(int)squad.Average(p=>p.OVR);
        }
        public void Log(string text) {
            Notice=text;
            News.Insert(0,"S"+Season+" R"+(Round+1)+": "+text);
            if(News.Count>100)News.RemoveAt(100);
        }
        public void BestLineup(int t) {
            var squad=Squad(t).Where(p=>p.InjuryRounds==0).ToList();
            HashSet<int> used=new HashSet<int>();
            for(int s=0;s<5;s++) {
                var best=squad.Where(p=>!used.Contains(p.Id)).OrderByDescending(p=>p.OVR+(p.Eligible(s)?12:0)).FirstOrDefault();
                Clubs[t].Lineup[s]=best==null?-1:best.Id;
                if(best!=null)used.Add(best.Id);
            }
        }
        public bool Start(int id,int slot) {
            Athlete p=Player(id);
            if(p==null||p.Team!=UserTeam||slot<0||slot>4||p.InjuryRounds>0)return Fail("Select a healthy roster player.");
            if(!p.Eligible(slot))return Fail("Player must have this primary or secondary position.");
            int prior=Array.IndexOf(Team.Lineup,id);
            if(prior>=0&&prior!=slot) {
                Athlete displaced=Player(Team.Lineup[slot]);
                Athlete replacement=displaced!=null&&displaced.Eligible(prior)&&displaced.InjuryRounds==0?displaced:Squad(UserTeam).Where(q=>q.Id!=id&&!Team.Lineup.Contains(q.Id)&&q.InjuryRounds==0&&q.Eligible(prior)).OrderByDescending(q=>q.OVR).FirstOrDefault();
                if(replacement==null)return Fail("No eligible player can cover the vacated starting position.");
                Team.Lineup[prior]=replacement.Id;
            }
            Team.Lineup[slot]=id;
            Log(p.Name+" starts at "+Util.Positions[slot]+".");
            return true;
        }
        bool Fail(string text) {
            Notice=text;
            return false;
        }
        public bool CanManage() {
            return !Fired&&!Drafting;
        }
        public bool Sign(int id) {
            Athlete p=Player(id);
            if(!CanManage()||p==null||p.Team!=-1)return Fail("Choose a free agent.");
            int fee=4+Math.Max(0,p.OVR-70)/2;
            if(Squad(UserTeam).Count>=MaxRoster)return Fail("Roster full (15). Release a reserve.");
            if(Payroll(UserTeam)+p.Salary>Cap)return Fail("Contract exceeds the $140M salary cap.");
            if(Team.Cash<fee)return Fail("Signing fee requires $"+fee+"M.");
            Team.Cash-=fee;
            p.Team=UserTeam;
            p.Years=3;
            Log("Signed "+p.Name+" to the bench for $"+p.Salary+"M per season.");
            return true;
        }
        public bool Release(int id) {
            Athlete p=Player(id);
            if(!CanManage()||p==null||p.Team!=UserTeam)return false;
            if(Team.Lineup.Contains(id)||Squad(UserTeam).Count<=8)return Fail("Keep eight players and replace starters before releasing.");
            int buyout=Math.Max(1,p.Salary/3);
            if(Team.Cash<buyout)return Fail("Buyout needs $"+buyout+"M.");
            Team.Cash-=buyout;
            p.Team=-1;
            Log("Released "+p.Name+" ($"+buyout+"M buyout).");
            return true;
        }
        public bool Renew(int id) {
            Athlete p=Player(id);
            if(!CanManage()||p==null||p.Team!=UserTeam)return false;
            int salary=SalaryFor(p.OVR),fee=3+Math.Max(0,p.OVR-70)/4;
            if(p.Years>=4)return Fail("Contract already runs four seasons.");
            if(Payroll(UserTeam)-p.Salary+salary>Cap||Team.Cash<fee)return Fail("Need cap room and $"+fee+"M renewal fee.");
            Team.Cash-=fee;
            p.Salary=salary;
            p.Years=4;
            Log("Renewed "+p.Name+" for four seasons.");
            return true;
        }
        public string TradeReview(int giveId,int getId) {
            Athlete a=Player(giveId),b=Player(getId);
            if(!CanManage()||a==null||b==null||a.Team!=UserTeam||b.Team<0||b.Team==UserTeam)return "Choose one player from each club.";
            if(Round>=16&&!Offseason)return "Trade deadline has passed (round 17).";
            if(Payroll(UserTeam)-a.Salary+b.Salary>Cap||Payroll(b.Team)-b.Salary+a.Salary>Cap)return "One club would exceed the salary cap.";
            float offered=a.Value,required=b.Value;
            var other=Clubs[b.Team];
            if(other.Plan=="REBUILD")offered+=(26-a.Age)*2;
            if(other.Plan=="CONTEND"&&other.Lineup.Contains(b.Id))required+=12;
            int replacement=Squad(b.Team).Where(p=>p.Id!=b.Id&&p.Eligible(b.Position)).Select(p=>p.OVR).DefaultIfEmpty(50).Max();
            if(!a.Eligible(b.Position))required+=Math.Max(0,b.OVR-replacement)*2;
            if(offered<required-10)return "Declined: need more player value / positional depth.";
            if(Team.Cash<3)return "Trade administration needs $3M.";
            return "ACCEPT / $3M fee";
        }
        public bool Trade(int give,int get) {
            string review=TradeReview(give,get);
            if(!review.StartsWith("ACCEPT"))return Fail(review);
            Athlete a=Player(give),b=Player(get);
            int t=b.Team;
            a.Team=t;
            b.Team=UserTeam;
            Team.Cash-=3;
            BestLineup(UserTeam);
            BestLineup(t);
            Log("Traded "+a.Name+" for "+b.Name+".");
            return true;
        }
        public bool Train(int focus) {
            if(!CanManage()||Offseason)return Fail("Training is available during the season.");
            if(TrainingRound==Round)return Fail("Training already completed this round.");
            if(Team.Cash<3)return Fail("Training costs $3M.");
            TrainingRound=Round;
            Team.Cash-=3;
            foreach(var p in Squad(UserTeam).Where(p=>p.InjuryRounds==0)) {
                p.XP+=3+Team.Training+Team.Conditioning;
                p.Morale=Util.Bound(p.Morale+2,0,100);
                Develop(p,focus);
            }
            Log("Training complete: "+new[] {
                "SHOOTING","DEFENSE","CONDITIONING"
            }
            [focus]+".");
            return true;
        }
        static void Develop(Athlete p,int focus) {
            int need=p.Age<27?10:17;
            while(p.XP>=need&&p.OVR<p.Potential) {
                p.XP-=need;
                if(focus==0) {
                    p.Shooting=Math.Min(99,p.Shooting+1);
                    p.Finishing=Math.Min(99,p.Finishing+1);
                    p.Passing=Math.Min(99,p.Passing+1);
                }
                else if(focus==1) {
                    p.Defense=Math.Min(99,p.Defense+1);
                    p.Rebounding=Math.Min(99,p.Rebounding+1);
                }
                else {
                    p.Speed=Math.Min(99,p.Speed+1);
                    p.Stamina=Math.Min(99,p.Stamina+1);
                }
            }
        }
        public bool Upgrade(int kind) {
            if(!CanManage())return false;
            int[] levels= {
                Team.Offense,Team.Defense,Team.Conditioning,Team.Training,Team.Medical,Team.Arena
            };
            int level=levels[kind],cost=8+level*6;
            if(level>=5)return Fail("Maximum level reached (5).");
            if(Team.Cash<cost)return Fail("Upgrade needs $"+cost+"M.");
            Team.Cash-=cost;
            if(kind==0)Team.Offense++;
            if(kind==1)Team.Defense++;
            if(kind==2)Team.Conditioning++;
            if(kind==3)Team.Training++;
            if(kind==4)Team.Medical++;
            if(kind==5)Team.Arena++;
            Log("Upgrade complete / level "+(level+1)+".");
            return true;
        }
        public bool Relocate(int city,string mascot,int color) {
            if(!CanManage()||city<0||city>=Util.Cities.Length||string.IsNullOrWhiteSpace(mascot))return false;
            if(Clubs.Any(c=>c.Id!=UserTeam&&c.City==Util.Cities[city]))return Fail("That city already has a club.");
            int cost=Team.City==Util.Cities[city]?0:15;
            if(Team.Cash<cost)return Fail("Relocation costs $15M.");
            Team.Cash-=cost;
            Team.City=Util.Cities[city];
            Team.Mascot=mascot.Trim().Substring(0,Math.Min(16,mascot.Trim().Length));
            Team.Color=color;
            Log("Welcome to "+Team.Name+".");
            return true;
        }
        public List<Standing> Table() {
            var rows=Clubs.Select(c=>new Standing {
                Team=c.Id
            }
            ).ToList();
            foreach(var g in Games.Where(g=>g.Played&&g.Series<0)) {
                var h=rows[g.Home];
                var a=rows[g.Away];
                h.PF+=g.HomeScore;
                h.PA+=g.AwayScore;
                a.PF+=g.AwayScore;
                a.PA+=g.HomeScore;
                if(g.HomeScore>g.AwayScore) {
                    h.W++;
                    a.L++;
                }
                else {
                    a.W++;
                    h.L++;
                }
            }
            return rows.OrderByDescending(s=>s.W).ThenByDescending(s=>s.Diff).ThenByDescending(s=>s.PF).ThenBy(s=>s.Team).ToList();
        }
        void Schedule() {
            Games.Clear();
            Seeds.Clear();
            Series.Clear();
            int[] ring=Enumerable.Range(0,12).ToArray();
            for(int r=0;r<11;r++) {
                for(int i=0;i<6;i++) {
                    int h=r%2==0?ring[i]:ring[11-i],a=r%2==0?ring[11-i]:ring[i];
                    AddFixture(r,h,a,-1);
                    AddFixture(r+11,a,h,-1);
                }
                int last=ring[11];
                for(int i=11;i>1;i--)ring[i]=ring[i-1];
                ring[1]=last;
            }
            Round=0;
            Offseason=false;
        }
        void AddFixture(int round,int h,int a,int series) {
            Games.Add(new Fixture {
                Id=NextFixture++,Round=round,Home=h,Away=a,Series=series
            }
            );
        }
        public Match MakeMatch(Fixture g,Settings settings) {
            return Match.Create(this,g.Home,g.Away,UserTeam,settings,g.Id);
        }
        public bool SimNext(Settings settings) {
            Fixture g=UserGame;
            if(g==null) {
                Advance();
                return true;
            }
            Match m=MakeMatch(g,settings);
            m.SimulateRest();
            return Finish(m);
        }
        public bool Finish(Match m) {
            Fixture f=Games.Find(g=>g.Id==m.FixtureId&&g.Round==Round&&!g.Played);
            if(f==null||!m.Over||m.HomeScore==m.AwayScore||m.HomeClub!=f.Home||m.AwayClub!=f.Away)return false;
            ApplyResult(f,m);
            foreach(var g in Games.Where(g=>g.Round==Round&&!g.Played).ToList()) {
                Match cpu=Match.Create(this,g.Home,g.Away,-1,new Settings(),g.Id);
                cpu.SimulateRest();
                ApplyResult(g,cpu);
            }
            Advance();
            return true;
        }
        void ApplyResult(Fixture f,Match m) {
            f.HomeScore=m.HomeScore;
            f.AwayScore=m.AwayScore;
            f.Played=true;
            foreach(var a in m.Roster) {
                Athlete p=Player(a.Data.Id);
                if(p==null)continue;
                p.Season.Add(a.Box);
                p.Career.Add(a.Box);
                p.XP+=2;
                Develop(p,(p.Id+Round)%3);
            }
            foreach(int t in new[] {
                f.Home,f.Away
            }
            ) {
                Club c=Clubs[t];
                bool won=f.Winner==t;
                float attendance=Util.Clamp((.45f+c.Fans*.007f-(c.Ticket-30)*.008f),.25f,1);
                int revenue=t==f.Home?(int)(12*attendance*(c.Ticket/30f)+c.Arena):5;
                int expense=4+Payroll(t)/30+(c.Offense+c.Defense+c.Conditioning)/3;
                c.Cash=Util.Bound(c.Cash+revenue-expense,-30,300);
                c.Fans=Util.Bound(c.Fans+(won?3:-2),15,99);
                c.Security=Util.Bound(c.Security+(won?3:-4),0,100);
                foreach(var p in Squad(t)) {
                    if(p.InjuryRounds>0)p.InjuryRounds--;
                    p.Morale=Util.Bound(p.Morale+(won?2:-1),20,99);
                }
                Random rng=new Random(Season*9127+f.Id*17+t);
                if(rng.NextDouble()<.075-c.Medical*.009) {
                    var candidates=Squad(t).Where(p=>p.InjuryRounds==0).ToList();
                    if(candidates.Count>8) {
                        Athlete hurt=candidates[rng.Next(candidates.Count)];
                        hurt.InjuryRounds=1+rng.Next(3);
                        hurt.Injury="ANKLE SPRAIN";
                        Log(hurt.Name+" out "+hurt.InjuryRounds+" rounds.");
                    }
                }
                if(t==UserTeam) {
                    if(won)CoachWins++;
                    else CoachLosses++;
                    Log(c.Mascot+" "+(t==f.Home?f.HomeScore:f.AwayScore)+" - "+(t==f.Home?f.AwayScore:f.HomeScore)+" "+Clubs[f.Other(t)].Mascot+" / cash $"+c.Cash+"M");
                }
            }
            if(f.Series>=0) {
                Series s=Series[f.Series];
                if(f.Winner==s.High)s.HighWins++;
                else s.LowWins++;
            }
        }
        public void Advance() {
            if(Offseason||Drafting)return;
            foreach(var g in Games.Where(g=>g.Round==Round&&!g.Played).ToList()) {
                Match cpu=Match.Create(this,g.Home,g.Away,-1,new Settings(),g.Id);
                cpu.SimulateRest();
                ApplyResult(g,cpu);
            }
            CpuTransactions();
            Round++;
            if(Round==22) {
                Seeds=Table().Take(6).Select(s=>s.Team).ToList();
                NewSeries(0,Seeds[2],Seeds[5],2);
                NewSeries(0,Seeds[3],Seeds[4],2);
            }
            if(Round>=22)PlayoffRound();
        }
        void NewSeries(int stage,int h,int a,int needed) {
            Series.Add(new Series {
                Id=Series.Count,Stage=stage,High=h,Low=a,Needed=needed
            }
            );
        }
        void PlayoffRound() {
            var active=Series.Where(s=>!s.Complete).ToList();
            if(active.Count==0) {
                int stage=Series.Max(s=>s.Stage);
                var winners=Series.Where(s=>s.Stage==stage).Select(s=>s.Winner).OrderBy(t=>Seeds.IndexOf(t)).ToList();
                if(stage==0) {
                    NewSeries(1,Seeds[0],winners[1],2);
                    NewSeries(1,Seeds[1],winners[0],2);
                }
                else if(stage==1)NewSeries(2,winners[0],winners[1],3);
                else {
                    Archive(winners[0]);
                    return;
                }
                active=Series.Where(s=>!s.Complete).ToList();
            }
            foreach(var s in active) {
                int game=s.HighWins+s.LowWins;
                bool highHome=game%2==0;
                AddFixture(Round,highHome?s.High:s.Low,highHome?s.Low:s.High,s.Id);
            }
        }
        void Archive(int champion) {
            if(History.Any(h=>h.Season==Season))return;
            foreach(var row in Table())History.Add(new SeasonRecord {
                Season=Season,Team=row.Team,Name=Clubs[row.Team].Name,W=row.W,L=row.L,Champion=row.Team==champion
            }
            );
            Awards.Add(new Award {
                Season=Season,Type="CHAMPION",Name=Clubs[champion].Name,Team=Clubs[champion].Name
            }
            );
            foreach(string type in new[] {
                "MVP","DEFENDER","ROOKIE"
            }
            ) {
                Athlete p=Players.Where(q=>q.Team>=0&&q.Season.Games>0&&(type!="ROOKIE"||q.Experience==0)).OrderByDescending(q=>type=="DEFENDER"?(q.Season.Steals*3+q.Season.Blocks*3+q.Season.Rebounds*.4f):q.Season.Value).FirstOrDefault();
                if(p!=null)Awards.Add(new Award {
                    Season=Season,Type=type,Name=p.Name,Team=Clubs[p.Team].Name
                }
                );
            }
            if(champion==UserTeam) {
                Titles++;
                Team.Cash+=25;
            }
            Offseason=true;
            Log(Clubs[champion].Name+" win the championship!");
            if(Team.Security<=15) {
                Fired=true;
                Log("Owner has dismissed you. Choose a new club from Jobs.");
            }
        }
        void CpuTransactions() {
            Random r=new Random(Season*233+Round*29);
            foreach(var c in Clubs.Where(c=>c.Id!=UserTeam)) {
                if(Round<16&&c.Cash>15) {
                    Athlete p=Players.Where(q=>q.Team==-1&&q.InjuryRounds==0&&Payroll(c.Id)+q.Salary<=Cap).OrderByDescending(q=>q.OVR+(c.Plan=="REBUILD"?(27-q.Age):0)).FirstOrDefault();
                    var weakest=Squad(c.Id).OrderBy(q=>q.OVR).First();
                    if(p!=null&&p.OVR>weakest.OVR+3) {
                        int fee=4+Math.Max(0,p.OVR-70)/2;
                        if(c.Cash>=fee&&Squad(c.Id).Count<MaxRoster) {
                            c.Cash-=fee;
                            p.Team=c.Id;
                            p.Years=3;
                        }
                    }
                }
                if(Round==8&&c.Cash>25) {
                    c.Offense=Math.Min(5,c.Offense+1);
                    c.Cash-=15;
                }
                BestLineup(c.Id);
            }
            if(Round==10) {
                var a=Players.FirstOrDefault(p=>p.Team!=UserTeam&&p.Team>=0&&p.Age>=29&&Clubs[p.Team].Plan=="REBUILD");
                if(a!=null) {
                    var b=Players.FirstOrDefault(p=>p.Team!=UserTeam&&p.Team>=0&&p.Team!=a.Team&&p.Age<25&&p.Position==a.Position&&Math.Abs(p.Value-a.Value)<12&&Payroll(p.Team)-p.Salary+a.Salary<=Cap&&Payroll(a.Team)-a.Salary+p.Salary<=Cap);
                    if(b!=null) {
                        int t=a.Team;
                        a.Team=b.Team;
                        b.Team=t;
                        Log("League trade: "+a.Name+" / "+b.Name);
                    }
                }
            }
        }
        public bool Join(int t) {
            if(t<0||t>=12||(!Offseason&&!Fired&&Games.Any(g=>g.Played)))return Fail("Change clubs between seasons.");
            UserTeam=t;
            Fired=false;
            Team.Security=Math.Max(60,Team.Security);
            Log(Coach+" joins "+Team.Name+".");
            return true;
        }
        public bool BeginDraft() {
            if(!Offseason||Drafting||Fired)return false;
            Random r=new Random(Season*701);
            DraftOrder=Table().OrderBy(s=>s.W).ThenBy(s=>s.Diff).Select(s=>s.Team).ToList();
            int champion=History.First(h=>h.Season==Season&&h.Champion).Team;
            DraftOrder.Remove(champion);
            DraftOrder.Add(champion);
            foreach(var p in Players.ToList()) {
                p.Age++;
                p.Experience++;
                p.Years--;
                if(p.Age>=36&&r.Next(3)==0) {
                    Players.Remove(p);
                    continue;
                }
                if(p.Years<=0) {
                    p.Team=-1;
                    p.Years=2;
                    p.Salary=SalaryFor(p.OVR);
                }
                if(p.Age>=31) {
                    p.Speed=Math.Max(55,p.Speed-2);
                    p.Stamina=Math.Max(55,p.Stamina-2);
                }
                p.InjuryRounds=0;
            }
            for(int t=0;t<12;t++) {
                while(Squad(t).Count<8) {
                    Athlete free=Players.Where(p=>p.Team==-1&&Payroll(t)+p.Salary<=Cap).OrderByDescending(p=>p.OVR).FirstOrDefault();
                    if(free==null) {
                        free=Generate(r,t,Squad(t).Count%5,65);
                        free.Salary=2;
                        Players.Add(free);
                    }
                    else free.Team=t;
                }
                BestLineup(t);
            }
            Prospects.Clear();
            DraftPicks.Clear();
            for(int i=0;i<36;i++) {
                Athlete p=Generate(r,-2,i%5,68+r.Next(15));
                p.Age=19+r.Next(3);
                p.Experience=0;
                p.Potential=Math.Min(99,p.OVR+10+r.Next(10));
                p.Salary=3;
                p.Years=3;
                Prospects.Add(p);
            }
            DraftIndex=0;
            Drafting=true;
            CpuDraft();
            return true;
        }
        public int DraftTeam  {
            get {
                return DraftOrder.Count==0?-1:DraftOrder[DraftIndex%12];
            }
        }
        public bool Draft(int id) {
            if(!Drafting||DraftTeam!=UserTeam)return false;
            Athlete p=Prospects.Find(x=>x.Id==id);
            if(p==null)return false;
            if(Squad(UserTeam).Count>=MaxRoster||Payroll(UserTeam)+3>Cap)return Fail("Need one roster slot and $3M cap room. Use Pass Pick if necessary.");
            SelectProspect(p);
            CpuDraft();
            return true;
        }
        public void PassPick() {
            if(Drafting&&DraftTeam==UserTeam) {
                DraftIndex++;
                Log("Passed draft pick.");
                CpuDraft();
            }
        }
        void SelectProspect(Athlete p) {
            int team=DraftTeam;
            p.Team=team;
            Players.Add(p);
            Prospects.Remove(p);
            DraftPicks.Add(new DraftPick {
                Pick=DraftIndex+1,Team=team,Player=p.Id,Name=p.Name
            }
            );
            DraftIndex++;
        }
        void CpuDraft() {
            while(DraftIndex<24&&DraftTeam!=UserTeam) {
                int t=DraftTeam;
                Athlete p=Prospects.OrderByDescending(x=>x.Potential+x.OVR).First();
                if(Squad(t).Count<MaxRoster&&Payroll(t)+3<=Cap)SelectProspect(p);
                else DraftIndex++;
            }
            if(DraftIndex>=24) {
                Drafting=false;
                foreach(var p in Prospects) {
                    p.Team=-1;
                    Players.Add(p);
                }
                Prospects.Clear();
                foreach(var p in Players)p.Season=new Stats();
                Season++;
                TrainingRound=-1;
                foreach(var c in Clubs) {
                    c.Cash=Math.Max(35,c.Cash);
                    c.Security=Math.Max(45,c.Security);
                    BestLineup(c.Id);
                }
                Schedule();
                Log("Season "+Season+" begins. Set your rotation.");
            }
        }
    }
}
