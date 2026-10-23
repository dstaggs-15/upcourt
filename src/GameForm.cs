using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace Upcourt  {
    public class Choice  {
        public string Title,Detail;
        public Action Run;
        public Choice(string title,string detail,Action run) {
            Title=title;
            Detail=detail;
            Run=run;
        }
    }
    public partial class GameForm:Form  {
        readonly Input input=new Input();
        readonly Timer timer=new Timer();
        readonly Stopwatch watch=new Stopwatch();
        readonly Audio audio=new Audio();
        readonly Bitmap frame=new Bitmap(640,400);
        SaveData data;
        League league;
        Match game;
        Graphics g;
        string screen="home",back="home",notice="",typing="",typingLabel="";
        Action<string> typed;
        int selected,person=-1,tradeGive=-1,otherTeam=1,homeTeam,awayTeam=2,menuRepeat,binding=-1;
        bool full,closing,pausedForFocus;
        double last,saveAt;
        float time;
        List<Choice> choices=new List<Choice>();
        List<KeyValuePair<RectangleF,Action>> hits=new List<KeyValuePair<RectangleF,Action>>();
        static readonly Color Ink=Color.FromArgb(24,27,46),Muted=Color.FromArgb(155,170,162),White=Color.FromArgb(246,235,203),Mint=Color.FromArgb(117,220,162),Gold=Color.FromArgb(247,195,85),Blue=Color.FromArgb(50,150,202),Red=Color.FromArgb(215,74,79),PanelColor=Color.FromArgb(39,46,61);
        public GameForm() {
            data=Persistence.Load();
            audio.Volume=data.Settings.Volume;
            audio.Muted=data.Settings.Muted;
            league=data.League;
            game=data.Match;
            notice=Persistence.LoadNotice;
            Text="UPCOURT | Pixel Basketball";
            ClientSize=new Size(1280,800);
            MinimumSize=new Size(960,600);
            BackColor=Ink;
            DoubleBuffered=true;
            KeyPreview=true;
            StartPosition=FormStartPosition.CenterScreen;
            KeyDown+=KeyPressed;
            KeyUp+=(s,e)=>input.HeldKeys.Remove(e.KeyCode);
            KeyPress+=(s,e)=> {
                if(typed!=null&&!char.IsControl(e.KeyChar)&&typing.Length<20)typing+=e.KeyChar;
            };
            MouseDown+=(s,e)=>ClickAt(e.Location);
            Deactivate+=(s,e)=> {
                input.Clear();
                if(screen=="game"&&!game.Over) {
                    game.Charging=false;
                    pausedForFocus=true;
                    Open("pause");
                }
                audio.Muted=true;
            };
            Activated+=(s,e)=> {
                audio.Muted=data.Settings.Muted;
                input.Clear();
            };
            FormClosing+=(s,e)=> {
                closing=true;
                Save();
            };
            timer.Interval=16;
            timer.Tick+=(s,e)=>Tick();
            watch.Start();
            if(!string.IsNullOrEmpty(notice))screen="home";
            timer.Start();
        }
        void KeyPressed(object sender,KeyEventArgs e) {
            if(typed!=null) {
                if(e.KeyCode==Keys.Enter) {
                    var callback=typed;
                    typed=null;
                    callback(typing);
                    Save();
                }
                else if(e.KeyCode==Keys.Escape)typed=null;
                else if(e.KeyCode==Keys.Back&&typing.Length>0)typing=typing.Substring(0,typing.Length-1);
                e.SuppressKeyPress=e.KeyCode==Keys.Enter||e.KeyCode==Keys.Escape||e.KeyCode==Keys.Back;
                return;
            }
            if(binding>=0) {
                if(e.KeyCode!=Keys.Escape) {
                    string key=e.KeyCode.ToString();
                    int other=Array.IndexOf(data.Settings.Keys,key);
                    if(other>=0)data.Settings.Keys[other]=data.Settings.Keys[binding];
                    data.Settings.Keys[binding]=key;
                    Save();
                }
                binding=-1;
                return;
            }
            bool fresh=input.HeldKeys.Add(e.KeyCode);
            input.UsesPad=false;
            if(fresh&&e.KeyCode==Keys.F11)ToggleFull();
            e.Handled=true;
        }
        void Type(string label,string value,Action<string> callback) {
            typingLabel=label;
            typing=value;
            typed=callback;
            input.Clear();
        }
        void ToggleFull() {
            full=!full;
            FormBorderStyle=full?FormBorderStyle.None:FormBorderStyle.Sizable;
            WindowState=full?FormWindowState.Maximized:FormWindowState.Normal;
        }
        void Save() {
            try {
                data.League=league;
                data.Match=game;
                Persistence.Save(data);
            }
            catch(Exception e) {
                notice="SAVE FAILED: "+e.Message;
                if(closing)MessageBox.Show(notice,"Upcourt");
            }
        }
        void Open(string next) {
            screen=next;
            selected=0;
            menuRepeat=12;
            input.Clear();
            audio.Cue("menu");
            BuildMenu();
        }
        void ExecuteChoice(int index) {
            if((screen=="player"||screen=="business"||screen=="relocate")&&HasLive())return;
            choices[index].Run();
        }
        void Add(string title,string detail,Action run) {
            choices.Add(new Choice(title,detail,run));
        }
        void Back() {
            if(screen=="game") {
                Open("pause");
                return;
            }
            if(screen=="pause") {
                Open("game");
                return;
            }
            if(screen=="office"||screen=="setup"||screen=="options"||screen=="help")Open("home");
            else if(screen=="controls")Open("options");
            else if(screen=="player")Open("roster");
            else if(screen=="subin")Open("subs");
            else if(screen=="tradeplayers")Open("tradeclubs");
            else if(screen=="tradeclubs")Open("tradegive");
            else if(screen=="tradegive")Open("office");
            else if(screen=="calls"||screen=="subs"||screen=="box")Open(game!=null&&!game.Over?"pause":"home");
            else if(screen=="designer")Open("calls");
            else Open(league==null?"home":"office");
        }
        void Start(bool franchise,bool practice,bool contest) {
            if(franchise&&league.UserGame==null) {
                notice="No fixture this round. Advance the league from Schedule.";
                Open("schedule");
                return;
            }
            if(franchise) {
                game=league.MakeMatch(league.UserGame,data.Settings);
            }
            else {
                League exhibition=League.Create(homeTeam);
                game=Match.Create(exhibition,homeTeam,awayTeam,homeTeam,data.Settings,-1);
            }
            if(practice)game.StartPractice();
            if(contest)game.StartContest();
            data.Match=game;
            Open("game");
            Save();
        }
        void Complete() {
            if(game==null||!game.Over||game.ResultCommitted)return;
            if(game.Contest) {
                data.ThreePointBest=Math.Max(data.ThreePointBest,game.ContestPoints);
            }
            else if(game.FixtureId>=0&&league!=null) {
                if(!league.Finish(game))notice="Result could not be applied to the current fixture.";
            }
            game.ResultCommitted=true;
            Save();
        }
        void Tick() {
            float dt=(float)Math.Min(.04,watch.Elapsed.TotalSeconds-last);
            last=watch.Elapsed.TotalSeconds;
            time+=dt;
            input.Poll();
            if(typed!=null||binding>=0) {
                input.EndFrame();
                Invalidate();
                return;
            }
            if(input.Fresh(Keys.M)||input.Press(Input.View)) {
                data.Settings.Muted=!data.Settings.Muted;
                audio.Muted=data.Settings.Muted;
                Save();
            }
            if(screen=="game"&&game!=null) {
                if(input.HadPad&&!input.Connected) {
                    Open("pause");
                    notice="Controller disconnected. Keyboard controls are available.";
                }
                else if(input.Fresh(Keys.Escape)||input.Press(Input.Start)) {
                    Open("pause");
                }
                else if(game.Over) {
                    Complete();
                    if(input.Fresh(Keys.Enter)||input.Press(Input.A))Open("box");
                }
                else {
                    Settings s=data.Settings;
                    float mx=input.MX+(input.Action(s,3)?1:0)-(input.Action(s,2)?1:0),my=input.MY+(input.Action(s,1)?1:0)-(input.Action(s,0)?1:0);
                    bool offense=game.UserSide==game.Possession;
                    if(input.ActionPress(s,4)||input.Press(Input.X)) {
                        if(offense||game.Phase=="freethrow")game.StartShot();
                        else game.Block();
                    }
                    if(input.ActionRelease(s,4)||input.Release(Input.X))game.ReleaseShot();
                    if(input.ActionPress(s,5)||input.Press(Input.A)) {
                        if(offense)game.Pass(-1,input.Key(Keys.ControlKey)||input.Hold(Input.LB));
                        else game.Steal();
                    }
                    if(input.ActionPress(s,6)||input.Press(Input.B))game.SwitchNearest();
                    if(input.ActionPress(s,7)||input.Press(Input.Y))game.Skill();
                    if(input.ActionPress(s,8)||input.Press(Input.RB))game.Screen();
                    for(int n=0;n<5;n++)if(input.ActionPress(s,9+n))game.Pass(n,input.Key(Keys.ControlKey));
                    if(input.ActionPress(s,14)) {
                        Open("calls");
                        return;
                    }
                    if(input.Fresh(Keys.T))game.Timeout();
                    int before=game.HomeScore+game.AwayScore;
                    string prior=game.Message;
                    game.Update(dt,-my,mx,input.Sprint);
                    if(game.HomeScore+game.AwayScore>before)audio.Cue("score");
                    else if(game.Message!=prior&&game.Message.Contains("PERIOD"))audio.Cue("buzzer");
                    if(game.Over)Complete();
                    if(time-saveAt>15) {
                        Save();
                        saveAt=time;
                    }
                }
            }
            else {
                BuildMenu();
                if(input.Fresh(Keys.Escape)||input.Press(Input.B)) {
                    Back();
                }
                else {
                    int dir=0;
                    if(input.Fresh(Keys.Down)||input.Press(Input.Down))dir=1;
                    if(input.Fresh(Keys.Up)||input.Press(Input.Up))dir=-1;
                    if(menuRepeat--<=0) {
                        if(input.MY>.55f) {
                            dir=1;
                            menuRepeat=10;
                        }
                        if(input.MY<-.55f) {
                            dir=-1;
                            menuRepeat=10;
                        }
                    }
                    if(choices.Count>0) {
                        if(input.Fresh(Keys.PageDown)||input.Press(Input.RB))dir=8;
                        if(input.Fresh(Keys.PageUp)||input.Press(Input.LB))dir=-8;
                        selected=(selected+dir+choices.Count)%choices.Count;
                        if(input.Fresh(Keys.Enter)||input.Press(Input.A))ExecuteChoice(selected);
                    }
                }
            }
            input.EndFrame();
            Invalidate();
        }
        void BuildMenu() {
            choices.Clear();
            if(screen=="game"||screen=="designer")return;
            if(screen=="home") {
                Add("EXHIBITION","QUICK FIVE ON FIVE",()=> {
                    homeTeam=0;awayTeam=2;back="home";Open("setup");
                }
                );
                Add("FRANCHISE","BUILD YOUR TEAM",()=>Open(league==null?"newcareer":"office"));
                Add("THREE POINT","FIVE RACKS / 25 BALLS",()=> {
                    homeTeam=0;awayTeam=2;back="contest";Open("setup");
                }
                );
                Add("SHOOTAROUND","LEARN THE SHOT METER",()=> {
                    back="practice";Open("setup");
                }
                );
                if(game!=null&&!game.Over)Add("RESUME MATCH",game.HomeName+" VS "+game.AwayName,()=>Open("game"));
                Add("OPTIONS","CONTROLS / AUDIO / GAMEPLAY",()=>Open("options"));
                Add("HOW TO PLAY","BASKETBALL / CONTROLS / CAREER",()=>Open("help"));
                Add("QUIT","AUTOSAVE AND EXIT",()=>Close());
            }
            else if(screen=="newcareer") {
                foreach(var c in League.Create(0).Clubs) {
                    int id=c.Id;
                    Add(c.Name,"START A NEW COACH CAREER",()=> {
                        league=League.Create(id);game=null;Type("COACH NAME","COACH",name=> {
                            league.Coach=string.IsNullOrWhiteSpace(name)?"COACH":name.Trim();Open("office");
                        }
                        );
                    }
                    );
                }
            }
            else if(screen=="setup") {
                Add("HOME / "+Util.Cities[homeTeam],"SELECT TO CYCLE YOUR CLUB",()=>homeTeam=(homeTeam+1)%12);
                Add("AWAY / "+Util.Cities[awayTeam],"SELECT TO CYCLE OPPONENT",()=>awayTeam=(awayTeam+1)%12);
                Add("DIFFICULTY / "+new[] {
                    "ROOKIE","PRO","ALL STAR"
                }
                [data.Settings.Difficulty],"SELECT TO CHANGE",()=>data.Settings.Difficulty=(data.Settings.Difficulty+1)%3);
                Add("START GAME",back=="contest"?"70 SECONDS / MONEY BALLS WORTH TWO":back=="practice"?"UNLIMITED PRACTICE":"FOUR QUARTERS / OVERTIME",()=> {
                    if(homeTeam==awayTeam) {
                        notice="Choose two different clubs.";return;
                    }
                    if(game!=null&&!game.Over) {
                        notice="Finish the suspended match before starting another.";return;
                    }
                    Start(false,back=="practice",back=="contest");
                }
                );
            }
            else if(screen=="office") {
                if(league==null) {
                    Open("newcareer");
                    return;
                }
                Add("PLAY NEXT GAME",league.UserGame==null?"BYE / ADVANCE FROM SCHEDULE":league.Clubs[league.UserGame.Other(league.UserTeam)].Name,()=> {
                    if(game!=null&&!game.Over)Open("game");else if(league.Fired)Open("jobs");else if(league.Drafting)Open("draft");else Start(true,false,false);
                }
                );
                Add("TEAM / ROSTER","LINEUP / ROTATION / CONTRACTS / INJURIES",()=>Open("roster"));
                Add("LEAGUE / SCHEDULE","STANDINGS / PLAYOFFS / SIMULATE",()=>Open("schedule"));
                Add("TRANSFERS","FREE AGENTS / TRADE / DRAFT",()=>Open("transfers"));
                Add("STAFF / TRAINING","COACHES / FACILITIES / DEVELOPMENT",()=>Open("training"));
                Add("BUSINESS / BRAND","CASH / FANS / TICKETS / RELOCATE",()=>Open("business"));
                Add("COACH / HISTORY","RECORD / TROPHIES / JOBS / NEWS",()=>Open("career"));
                Add("PLAYBOOK","24 CALLS / 40 CUSTOM PLAYS",()=>Open("calls"));
                Add("SAVE AND HOME","YOUR CAREER IS SAVED AUTOMATICALLY",()=> {
                    Save();Open("home");
                }
                );
            }
            else if(screen=="roster") {
                foreach(var p in league.Squad(league.UserTeam)) {
                    int id=p.Id;
                    Add(PlayerTitle(p),PlayerDetail(p),()=> {
                        person=id;Open("player");
                    }
                    );
                }
            }
            else if(screen=="player") {
                Athlete p=league.Player(person);
                if(p==null)return;
                for(int s=0;s<5;s++)if(p.Eligible(s)) {
                    int slot=s;
                    Add("START AT "+Util.Positions[s],"SWAPS THE CURRENT STARTER",()=> {
                        league.Start(person,slot);PersistNotice();
                    }
                    );
                }
                Add("ROTATION / "+p.Rotation+" MIN","TARGET SHARE OF A 40 MINUTE GAME",()=> {
                    p.Rotation=p.Rotation>=36?8:p.Rotation+4;Save();
                }
                );
                Add("RENEW CONTRACT","EXTEND TO FOUR SEASONS",()=> {
                    league.Renew(person);PersistNotice();
                }
                );
                Add("RELEASE PLAYER","RESERVES ONLY / CONTRACT BUYOUT",()=> {
                    if(league.Release(person))Open("roster");PersistNotice();
                }
                );
                Add("TRADE PLAYER","CHOOSE A CLUB TO NEGOTIATE",()=> {
                    tradeGive=person;Open("tradeclubs");
                }
                );
                Add("CAREER STATS",StatLine(p.Career),()=> {
                }
                );
                Add("SEASON STATS",StatLine(p.Season),()=> {
                }
                );
            }
            else if(screen=="schedule") {
                Add("PLAY NEXT",league.UserGame==null?"NO USER FIXTURE THIS ROUND":league.Clubs[league.UserGame.Other(league.UserTeam)].Name,()=> {
                    if(game!=null&&!game.Over)Open("game");else Start(true,false,false);
                }
                );
                Add("SIM NEXT / ADVANCE","ADVANCES EVERY CLUB AND SETTLES FINANCES",()=> {
                    if(HasLive())return;if(league.Offseason) {
                        notice="Season complete. Start the draft from Transfers.";return;
                    }
                    league.SimNext(data.Settings);PersistNotice();
                }
                );
                Add("STANDINGS","TOP SIX QUALIFY / TOP TWO RECEIVE BYES",()=>Open("standings"));
                Add("PLAYOFF SERIES","BEST OF THREE / FINALS BEST OF FIVE",()=>Open("playoffs"));
                Add("SEASON LEADERS","POINTS / BOARDS / ASSISTS / DEFENSE",()=>Open("leaders"));
                foreach(var f in league.Games.Where(f=>f.Involves(league.UserTeam)).OrderBy(f=>f.Round)) {
                    Add("R"+(f.Round+1)+" "+league.Clubs[f.Other(league.UserTeam)].Mascot,(f.Home==league.UserTeam?"HOME / ":"AWAY / ")+(f.Played?f.HomeScore+" - "+f.AwayScore:f.Round==league.Round?"NEXT":"UPCOMING"),()=> {
                    }
                    );
                }
            }
            else if(screen=="standings") {
                int rank=0;
                foreach(var row in league.Table()) {
                    rank++;
                    Add(rank+" "+league.Clubs[row.Team].Name,row.W+" W / "+row.L+" L / DIFF "+row.Diff+(rank<=2?" / BYE":rank<=6?" / PLAYOFF":""),()=> {
                    }
                    );
                }
            }
            else if(screen=="playoffs") {
                if(league.Series.Count==0)Add("PROJECTED SEEDS","FINAL SEEDS LOCK AFTER ROUND 22",()=> {
                }
                );
                foreach(var s in league.Series)Add(new[] {
                    "FIRST ROUND","SEMIFINAL","FINALS"
                }
                [s.Stage]+" / "+league.Clubs[s.High].Mascot+" VS "+league.Clubs[s.Low].Mascot,s.HighWins+" - "+s.LowWins+" / FIRST TO "+s.Needed+(s.Complete?" / "+league.Clubs[s.Winner].Mascot+" ADVANCE":""),()=> {
                }
                );
            }
            else if(screen=="transfers") {
                Add("FREE AGENTS","SIGN TO YOUR BENCH / $140M CAP",()=>Open("agents"));
                Add("TRADE","VALUE / AGE / POTENTIAL / BOTH CAPS",()=>Open("tradegive"));
                Add(league.Drafting?"CONTINUE DRAFT":"START NEXT SEASON DRAFT","TWO ROUNDS / REVERSE STANDINGS",()=> {
                    if(HasLive())return;if(!league.Drafting&&!league.BeginDraft()) {
                        notice="Finish the season before the draft. Dismissed coaches need a new job.";return;
                    }
                    Open("draft");Save();
                }
                );
                Add("DRAFT RESULTS","ALL PICKS FROM THIS OFFSEASON",()=>Open("draftresults"));
            }
            else if(screen=="agents") {
                foreach(var p in league.Players.Where(p=>p.Team==-1).OrderByDescending(p=>p.OVR)) {
                    int id=p.Id;
                    Add(PlayerTitle(p),"$"+p.Salary+"M SALARY / SIGNING FEE $"+(4+Math.Max(0,p.OVR-70)/2)+"M / AGE "+p.Age,()=> {
                        if(HasLive())return;league.Sign(id);PersistNotice();
                    }
                    );
                }
            }
            else if(screen=="tradegive") {
                foreach(var p in league.Squad(league.UserTeam)) {
                    int id=p.Id;
                    Add(PlayerTitle(p),"OFFER THIS PLAYER",()=> {
                        tradeGive=id;Open("tradeclubs");
                    }
                    );
                }
            }
            else if(screen=="tradeclubs") {
                foreach(var c in league.Clubs.Where(c=>c.Id!=league.UserTeam)) {
                    int id=c.Id;
                    Add(c.Name,c.Plan+" / PAYROLL $"+league.Payroll(id)+"M",()=> {
                        otherTeam=id;Open("tradeplayers");
                    }
                    );
                }
            }
            else if(screen=="tradeplayers") {
                foreach(var p in league.Squad(otherTeam)) {
                    int id=p.Id;
                    Add(PlayerTitle(p),league.TradeReview(tradeGive,id),()=> {
                        if(HasLive())return;league.Trade(tradeGive,id);PersistNotice();
                    }
                    );
                }
            }
            else if(screen=="draft") {
                if(!league.Drafting) {
                    Add("DRAFT COMPLETE",league.Stage,()=>Open("office"));
                }
                else {
                    Add("PASS PICK","USE WHEN CAP OR ROSTER SPACE IS FULL",()=> {
                        league.PassPick();Save();
                    }
                    );
                    foreach(var p in league.Prospects.OrderByDescending(p=>p.OVR)) {
                        int id=p.Id;
                        Add(PlayerTitle(p),"POT "+p.Potential+" / AGE "+p.Age+" / $3M ROOKIE CONTRACT",()=> {
                            league.Draft(id);PersistNotice();
                        }
                        );
                    }
                }
            }
            else if(screen=="draftresults") {
                foreach(var p in league.DraftPicks)Add("#"+p.Pick+" "+p.Name,league.Clubs[p.Team].Name,()=> {
                }
                );
            }
            else if(screen=="training") {
                for(int i=0;i<3;i++) {
                    int focus=i;
                    Add("TRAIN "+new[] {
                        "SHOOTING","DEFENSE","CONDITIONING"
                    }
                    [i],"$3M / ONCE PER ROUND / XP AND MORALE",()=> {
                        if(HasLive())return;league.Train(focus);PersistNotice();
                    }
                    );
                }
                int[] levels= {
                    league.Team.Offense,league.Team.Defense,league.Team.Conditioning,league.Team.Training,league.Team.Medical,league.Team.Arena
                };
                for(int i=0;i<6;i++) {
                    int k=i;
                    Add(new[] {
                        "OFFENSIVE COACH","DEFENSIVE COACH","CONDITIONING COACH","TRAINING FACILITY","MEDICAL FACILITY","ARENA"
                    }
                    [i]+" / LV "+levels[i],"UPGRADE $"+(8+levels[i]*6)+"M / MAX 5",()=> {
                        if(HasLive())return;league.Upgrade(k);PersistNotice();
                    }
                    );
                }
            }
            else if(screen=="business") {
                Club c=league.Team;
                Add("CASH $"+c.Cash+"M / CAP $"+(League.Cap-league.Payroll(league.UserTeam))+"M","FANS "+c.Fans+" / OWNER CONFIDENCE "+c.Security,()=> {
                }
                );
                Add("TICKET PRICE / $"+c.Ticket,"CYCLE $20-$60 / HIGH PRICES REDUCE ATTENDANCE",()=> {
                    c.Ticket=c.Ticket>=60?20:c.Ticket+5;Save();
                }
                );
                Add("MASCOT / "+c.Mascot,"KEEP YOUR ROSTER AND HISTORY",()=>Type("MASCOT NAME",c.Mascot,name=> {
                    if(!string.IsNullOrWhiteSpace(name))c.Mascot=name.Trim();
                }
                ));
                Add("TEAM COLOR","CYCLE DOWNFIELD'S FLAT UNIFORM COLORS",()=> {
                    int i=Array.IndexOf(Util.Colors,c.Color);c.Color=Util.Colors[(i+1)%Util.Colors.Length];Save();
                }
                );
                Add("RELOCATE","$15M / AVAILABLE CITIES",()=>Open("relocate"));
                Add("FINANCE RULES","HOME GATE + ARENA / PAYROLL + STAFF COSTS",()=> {
                }
                );
            }
            else if(screen=="relocate") {
                for(int i=0;i<Util.Cities.Length;i++) {
                    int city=i;
                    Add(Util.Cities[i],"RELOCATE / $15M",()=> {
                        league.Relocate(city,league.Team.Mascot,league.Team.Color);PersistNotice();
                    }
                    );
                }
            }
            else if(screen=="career") {
                Add(league.Coach+" / "+league.CoachWins+" W - "+league.CoachLosses+" L",league.Titles+" CHAMPIONSHIPS",()=>Type("COACH NAME",league.Coach,name=> {
                    if(!string.IsNullOrWhiteSpace(name))league.Coach=name.Trim();
                }
                ));
                Add("TROPHIES / HISTORY","ARCHIVED SEASONS AND AWARDS",()=>Open("history"));
                Add("JOBS / CHANGE TEAM","COACH RECORD STAYS WITH YOU",()=>Open("jobs"));
                Add("LEAGUE NEWS","TRANSACTIONS / RESULTS / INJURIES",()=>Open("news"));
            }
            else if(screen=="history") {
                foreach(var a in league.Awards.AsEnumerable().Reverse())Add("S"+a.Season+" / "+a.Type,a.Name+" / "+a.Team,()=> {
                }
                );
                foreach(var h in league.History.AsEnumerable().Reverse())Add("S"+h.Season+" / "+h.Name,h.W+" W - "+h.L+" L"+(h.Champion?" / CHAMPION":""),()=> {
                }
                );
            }
            else if(screen=="jobs") {
                foreach(var c in league.Clubs) {
                    int id=c.Id;
                    Add(c.Name,c.Plan+" / CONFIDENCE "+c.Security,()=> {
                        if(HasLive())return;league.Join(id);PersistNotice();
                    }
                    );
                }
            }
            else if(screen=="news") {
                foreach(string n in league.News)Add(n,"",()=> {
                }
                );
            }
            else if(screen=="leaders") {
                foreach(var p in league.Players.Where(p=>p.Team>=0&&p.Season.Games>0).OrderByDescending(p=>p.Season.Value).Take(36))Add(p.Name+" / "+league.Clubs[p.Team].Mascot,StatLine(p.Season),()=> {
                }
                );
            }
            else if(screen=="pause") {
                Add("RESUME",pausedForFocus?"PAUSED WHEN WINDOW LOST FOCUS":"BACK TO THE COURT",()=> {
                    pausedForFocus=false;Open("game");
                }
                );
                Add("PLAYBOOK","CALL A PLAY / CREATE YOUR OWN",()=>Open("calls"));
                Add("SUBSTITUTIONS","AVAILABLE AT A STOPPAGE",()=>Open("subs"));
                Add("TIMEOUT",game.Timeouts[game.UserSide]+" REMAIN / MUST HAVE POSSESSION",()=> {
                    notice=game.Timeout()?"Timeout called.":"Cannot call timeout in this state.";Save();
                }
                );
                Add("DEFENSE / "+new[] {
                    "MAN TO MAN","2-3 ZONE","FULL COURT PRESS"
                }
                [game.DefenseScheme],"SELECT TO CHANGE SCHEME",()=>game.DefenseScheme=(game.DefenseScheme+1)%3);
                Add("BOX SCORE","LIVE PLAYER PRODUCTION",()=>Open("box"));
                Add("SIM REST","RESULT COUNTS FOR A LEAGUE FIXTURE",()=> {
                    game.SimulateRest();Complete();Open("box");
                }
                );
                Add("SAVE AND HOME","RESUME THIS EXACT MATCH LATER",()=> {
                    Save();Open("home");
                }
                );
            }
            else if(screen=="calls") {
                int index=0;
                foreach(var p in Play.Builtins().Concat(data.CustomPlays)) {
                    Play play=p;
                    int n=index++;
                    Add(play.Name,(n<24?"STOCK / ":"SAVED / ")+play.Style,()=> {
                        if(game!=null&&!game.Over) {
                            game.ActivePlay=play;game.PlayTime=0;Open("game");
                        }
                        else {
                            editPlay=ClonePlay(play);editSource=n>=24?n-24:-1;Open("designer");
                        }
                    }
                    );
                }
                Add("CREATE A PLAY","SELECT A PLAYER AND CLICK WAYPOINTS",()=> {
                    editPlay=ClonePlay(Play.Builtins()[0]);editPlay.Name="CUSTOM PLAY";editSource=-1;Open("designer");
                }
                );
                for(int i=0;i<data.CustomPlays.Count;i++) {
                    int ix=i;
                    Add("EDIT / "+data.CustomPlays[i].Name,"CHANGE ROUTES OR DELETE",()=> {
                        editPlay=ClonePlay(data.CustomPlays[ix]);editSource=ix;Open("designer");
                    }
                    );
                }
            }
            else if(screen=="subs") {
                foreach(var a in game.Roster.Where(a=>a.Side==game.UserSide&&a.OnCourt)) {
                    Actor outPlayer=a;
                    Add("OUT / "+a.Data.Name,"ENERGY "+(int)(a.Fatigue*100)+"% / FOULS "+a.Box.Fouls,()=> {
                        person=outPlayer.Data.Id;Open("subin");
                    }
                    );
                }
            }
            else if(screen=="subin") {
                foreach(var a in game.Roster.Where(a=>a.Side==game.UserSide&&!a.OnCourt)) {
                    int id=a.Data.Id;
                    Add("IN / "+a.Data.Name,"ENERGY "+(int)(a.Fatigue*100)+"% / "+a.Data.Role,()=> {
                        notice=game.Substitute(person,id)?"Substitution complete.":"Need a stoppage and healthy, eligible reserve.";Save();Open("subs");
                    }
                    );
                }
            }
            else if(screen=="box") {
                Add(game.HomeName+" "+game.HomeScore+" - "+game.AwayScore+" "+game.AwayName,game.Over?"FINAL / RESULT SAVED":"LIVE BOX SCORE",()=> {
                    if(game.Over)Open(game.FixtureId>=0?"office":"home");else Open("pause");
                }
                );
                foreach(var a in game.Roster.OrderBy(a=>a.Side).ThenByDescending(a=>a.Box.Points))Add(a.Data.Name+" / "+(a.Side==0?"HOME":"AWAY"),a.Box.Points+" PTS "+a.Box.Rebounds+" REB "+a.Box.Assists+" AST / FG "+a.Box.FGM+"-"+a.Box.FGA+" / 3P "+a.Box.TPM+"-"+a.Box.TPA+" / FT "+a.Box.FTM+"-"+a.Box.FTA,()=> {
                }
                );
            }
            else if(screen=="options") {
                var s=data.Settings;
                Add("DIFFICULTY / "+new[] {
                    "ROOKIE","PRO","ALL STAR"
                }
                [s.Difficulty],"APPLIES TO NEW MATCHES",()=> {
                    s.Difficulty=(s.Difficulty+1)%3;Save();
                }
                );
                Add("QUARTER LENGTH / "+s.QuarterSeconds+" SEC","CYCLE 60 / 90 / 120 / 180",()=> {
                    int[] values= {
                        60,90,120,180
                    };s.QuarterSeconds=values[(Array.IndexOf(values,s.QuarterSeconds)+1)%4];Save();
                }
                );
                Add("SHOT ASSIST / "+(s.ShotAssist?"ON":"OFF"),"TIMING ASSIST / APPLIES TO NEW MATCHES",()=> {
                    s.ShotAssist=!s.ShotAssist;Save();
                }
                );
                Add("AUTO SUBS / "+(s.AutoSubs?"ON":"OFF"),"FATIGUE / FOUL TROUBLE / ROTATION",()=> {
                    s.AutoSubs=!s.AutoSubs;if(game!=null)game.AutoSubs=s.AutoSubs;Save();
                }
                );
                Add("VOLUME / "+s.Volume+"%","SELECT TO CYCLE / M QUICK MUTE",()=> {
                    s.Volume=(s.Volume+20)%120;audio.Volume=s.Volume;Save();
                }
                );
                Add("KEYBOARD BINDINGS","CONFLICTING KEYS SWAP",()=>Open("controls"));
                Add("FULLSCREEN","F11 ALSO TOGGLES",()=>ToggleFull());
                Add("CREDITS / LICENSE","DOWNFIELD ART / MIT SOURCE",()=>{using(var stream=typeof(GameForm).Assembly.GetManifestResourceStream("Upcourt.License"))using(var reader=new System.IO.StreamReader(stream))MessageBox.Show(reader.ReadToEnd(),"Upcourt / License");});
            }
            else if(screen=="controls") {
                for(int i=0;i<Settings.Actions.Length;i++) {
                    int action=i;
                    Add(Settings.Actions[i]+" / "+data.Settings.Keys[i],"SELECT THEN PRESS A KEY / ESC CANCELS",()=>binding=action);
                }
            }
            else if(screen=="help") {
                string[,] rows= {
                    {
                        "MOVE / SPRINT","WASD / SHIFT | LEFT STICK / RT"
                    }
                    , {
                        "SHOOT / BLOCK","HOLD SPACE AND RELEASE | HOLD X AND RELEASE"
                    }
                    , {
                        "PASS / STEAL","E | A / CTRL+E LOBS / 1-5 ICON PASS"
                    }
                    , {
                        "SWITCH PLAYER","Q | B"
                    }
                    , {
                        "CROSSOVER","F | Y / USE WITH MOVEMENT"
                    }
                    , {
                        "SCREEN / BOX OUT","R | RB"
                    }
                    , {
                        "PLAYBOOK / TIMEOUT","P / T | PAUSE MENU"
                    }
                    , {
                        "PAUSE / MUTE / FULLSCREEN","ESC / M / F11 | MENU / VIEW"
                    }
                    , {
                        "SHOT METER","GREEN AT MARK / SHORT SHOTS RELEASE EARLIER"
                    }
                    , {
                        "BASKETBALL RULES","24 SEC CLOCK / 8 SEC BACKCOURT / 6 FOULS OUT"
                    }
                    , {
                        "THREE POINT CONTEST","25 BALLS / EVERY FIFTH WORTH TWO / 70 SEC"
                    }
                    , {
                        "FRANCHISE","22 ROUNDS / TOP SIX / PLAYOFF SERIES / DRAFT"
                    }
                    , {
                        "SAVING","AUTO EVERY 15 SEC / SAVE AND HOME RESUMES"
                    }
                };
                for(int i=0;i<rows.GetLength(0);i++)Add(rows[i,0],rows[i,1],()=> {
                }
                );
            }
            if(choices.Count==0)Add("NOTHING HERE YET","ESC / B TO GO BACK",()=>Back());
            selected=Util.Bound(selected,0,choices.Count-1);
        }
        bool HasLive() {
            if(game!=null&&!game.Over) {
                notice="Finish or simulate the suspended match before changing the league.";
                return true;
            }
            return false;
        }
        void PersistNotice() {
            notice=league.Notice;
            Save();
        }
        string PlayerTitle(Athlete p) {
            return "#"+p.Number+" "+p.Name+" / "+p.Role+" / "+p.OVR+" OVR";
        }
        string PlayerDetail(Athlete p) {
            return "AGE "+p.Age+" POT "+p.Potential+" / $"+p.Salary+"M / "+p.Years+" YR"+(p.InjuryRounds>0?" / HURT "+p.InjuryRounds:" / "+(league.Team.Lineup.Contains(p.Id)?"STARTER":"BENCH"));
        }
        string StatLine(Stats s) {
            return s.Games+" GP / "+s.PerGame(s.Points).ToString("0.0")+" PPG / "+s.PerGame(s.Rebounds).ToString("0.0")+" RPG / "+s.PerGame(s.Assists).ToString("0.0")+" APG";
        }
        void ClickAt(Point location) {
            if(typed!=null||binding>=0)return;
            PointF p=new PointF(location.X*1280f/ClientSize.Width,location.Y*800f/ClientSize.Height);
            if(screen=="designer") {
                DesignerClick(p);
                return;
            }
            foreach(var hit in hits.ToList())if(hit.Key.Contains(p)) {
                hit.Value();
                Invalidate();
                break;
            }
        }
        protected override void Dispose(bool disposing) {
            if(disposing) {
                timer.Dispose();
                frame.Dispose();
                audio.Dispose();
            }
            base.Dispose(disposing);
        }
        public void Preview(string path,bool court) {
            timer.Stop();
            if(court) {
                League l=League.Create(0);
                game=Match.Create(l,0,2,0,data.Settings,-1);
                game.Phase="live";
                screen="game";
            }
            else screen="home";
            using(Bitmap b=new Bitmap(1280,800)) {
                using(Graphics preview=Graphics.FromImage(b))OnPaint(new PaintEventArgs(preview,new Rectangle(0,0,1280,800)));
                b.Save(path,ImageFormat.Png);
            }
        }
    }
}
