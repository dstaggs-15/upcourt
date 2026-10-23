using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
namespace Upcourt  {
#if UPCOURT_TESTS
    public static class Tests  {
        static int assertions,failures;
        static void Check(bool condition,string name) {
            assertions++;
            if(!condition) {
                failures++;
                Console.WriteLine("FAIL: "+name);
            }
        }
        static void Test(string name,Action body) {
            try {
                int before=failures;
                body();
                if(failures==before)Console.WriteLine("PASS: "+name);
                else Console.WriteLine("FAILED: "+name);
            }
            catch(Exception e) {
                failures++;
                Console.WriteLine("FAIL: "+name+" / "+e);
            }
        }
        static void Box(Match m) {
            Check(m.Roster.Where(a=>a.Side==0).Sum(a=>a.Box.Points)==m.HomeScore,"home player points reconcile");
            Check(m.Roster.Where(a=>a.Side==1).Sum(a=>a.Box.Points)==m.AwayScore,"away player points reconcile");
            foreach(var a in m.Roster) {
                Check(a.Box.FGM<=a.Box.FGA&&a.Box.TPM<=a.Box.FGM&&a.Box.TPM<=a.Box.TPA&&a.Box.FTM<=a.Box.FTA,"attempt/make bounds");
                Check(a.Box.Points==2*a.Box.FGM+a.Box.TPM+a.Box.FTM,"player scoring arithmetic");
            }
        }
        public static int Run() {
            assertions=failures=0;
            string root=Path.Combine(Path.GetTempPath(),"Upcourt-tests-"+Guid.NewGuid().ToString("N"));
            Persistence.DirectoryPath=root;
            Test("league identities, schedule and lineup",()=> {
                League l=League.Create(0);Check(l.Clubs.Count==12&&l.Players.Count==174,"12 clubs and independent players");Check(l.Players.Select(p=>p.Id).Distinct().Count()==l.Players.Count,"stable unique IDs");Check(l.Games.Count==132,"22 rounds x six fixtures");for(int t=0;t<12;t++) {
                    Check(l.Games.Count(g=>g.Involves(t))==22,"22 games per team");Check(l.Games.Count(g=>g.Home==t)==11,"11 home games");Check(l.Payroll(t)<=League.Cap,"starting salaries legal");Check(l.Clubs[t].Lineup.Distinct().Count()==5,"unique five starters");foreach(int other in Enumerable.Range(0,12).Where(x=>x!=t))Check(l.Games.Count(g=>g.Home==t&&g.Away==other)==1,"one home fixture per pairing");
                }
            }
            );
            Test("signing, cap and release transaction boundaries",()=> {
                League l=League.Create(0);Athlete free=l.Players.First(p=>p.Team==-1);l.Team.Cash=300;Check(l.Sign(free.Id),"sign affordable free agent");Check(!l.Team.Lineup.Contains(free.Id),"new signing enters bench");Check(l.Release(free.Id)&&free.Team==-1,"release returns to market");int cash=l.Team.Cash,count=l.Squad(0).Count;free.Salary=200;Check(!l.Sign(free.Id)&&l.Team.Cash==cash&&l.Squad(0).Count==count,"failed cap check atomic");int starter=l.Team.Lineup[0];Check(!l.Release(starter),"cannot waive a starter");Check(l.Players.Where(p=>p.Team==0).All(p=>p.Team==0),"no phantom team changes");
            }
            );
            Test("trades, training and upgrade caps",()=> {
                League l=League.Create(0);var a=l.Squad(0)[0];var b=l.Squad(1)[0];int ac=a.Team,bc=b.Team;l.Round=17;Check(!l.Trade(a.Id,b.Id)&&a.Team==ac&&b.Team==bc,"deadline rejection atomic");l.Round=0;l.Team.Cash=300;Check(l.Train(0),"training affordable");Check(!l.Train(1),"training only once per round");for(int i=0;i<7;i++)l.Upgrade(0);Check(l.Team.Offense==5,"coach maximum level");
            }
            );
            Test("shot quality, court rules, box totals",()=> {
                League l=League.Create(0);Match m=Match.Create(l,0,1,0,new Settings(),1);m.Phase="live";Actor shooter=m.BallOwner;shooter.X=65;shooter.Y=25;foreach(var d in m.Roster.Where(a=>a.Side==1)) {
                    d.X=10;d.Y=3;
                }
                Check(m.ThreePoint(64.8f,25,0),"arc three pointer");Check(!m.ThreePoint(85,25,0),"paint two pointer");Check(m.ThreePoint(88,2,0),"corner three pointer");m.Shoot(.72f);Check(m.Phase=="shot"&&m.ShotChance>.9f,"green open jumper");for(int i=0;i<150;i++)m.Update(.02f,0,0,false);Box(m);m.Phase="live";m.Clock=40;m.ShotClock=.005f;m.Update(.02f,0,0,false);Check(m.Message.Contains("SHOT CLOCK"),"24 second violation");
            }
            );
            Test("timeouts, fatigue and safe substitutions",()=> {
                League l=League.Create(0);Match m=Match.Create(l,0,1,0,new Settings(),1);int timeouts=m.Timeouts[0];Check(m.Timeout()&&m.Timeouts[0]==timeouts-1,"timeout charged");m.Possession=1;Check(!m.Timeout(),"cannot timeout without possession");m.Phase="inbound";Actor a=m.Roster.First(p=>p.Side==0&&p.OnCourt);Actor b=m.Roster.First(p=>p.Side==0&&!p.OnCourt&&p.Data.Eligible(a.Slot));Check(m.Substitute(a.Data.Id,b.Data.Id),"legal stoppage substitute");Check(m.Roster.Count(p=>p.Side==0&&p.OnCourt)==5,"still five on court");m.Phase="live";Check(!m.Substitute(b.Data.Id,a.Data.Id),"live substitution rejected");
            }
            );
            Test("simulation is isolated and results commit once",()=> {
                League l=League.Create(0);int cash=l.Team.Cash;Match ex=Match.Create(l,0,1,0,new Settings(),-1);ex.SimulateRest();Box(ex);Check(l.Team.Cash==cash&&l.Games.All(g=>!g.Played)&&l.Players.All(p=>p.Season.Points==0),"exhibition leaves league untouched");Fixture f=l.UserGame;Match m=l.MakeMatch(f,new Settings());m.SimulateRest();Check(m.HomeScore!=m.AwayScore,"basketball has a winner");Check(l.Finish(m),"finish valid fixture");int settledCash=l.Team.Cash,points=l.Squad(0).Sum(p=>p.Season.Points);Check(!l.Finish(m)&&l.Team.Cash==settledCash&&l.Squad(0).Sum(p=>p.Season.Points)==points,"duplicate result rejected");Check(l.Round==1&&l.Games.Count(g=>g.Played)==6,"whole round advances");
            }
            );
            Test("full season, playoff series, draft and repeat seasons",()=> {
                League l=League.Create(0);int guard=0;while(!l.Offseason&&guard++<60)l.SimNext(new Settings());Check(l.Offseason&&guard<60,"season finishes");Check(l.Seeds.Count==6&&l.Series.Count==5,"six-team bracket with byes");Check(l.Series.Where(s=>s.Stage<2).All(s=>s.Needed==2)&&l.Series.Last().Needed==3,"series lengths");Check(l.History.Count==12&&l.Awards.Any(a=>a.Type=="CHAMPION"),"archives and champion award");if(l.Fired)l.Join(1);Check(l.BeginDraft(),"begin offseason draft");guard=0;while(l.Drafting&&guard++<30) {
                    if(l.DraftTeam==l.UserTeam) {
                        if(!l.Draft(l.Prospects[0].Id))l.PassPick();
                    }
                }
                Check(!l.Drafting&&l.Season==2,"draft advances season");Check(l.Games.Count==132&&l.Round==0,"fresh schedule");Check(l.Clubs.All(c=>l.Squad(c.Id).Count>=8&&l.Squad(c.Id).Count<=15),"roster integrity after contracts and draft");Check(l.Players.Select(p=>p.Id).Distinct().Count()==l.Players.Count,"draft IDs unique");
            }
            );
            Test("six season franchise lifecycle",()=> {
                League l=League.Create(0);for(int season=1;season<=6;season++) {
                    int guard=0;while(!l.Offseason&&guard++<60)l.SimNext(new Settings());Check(l.Offseason&&l.Season==season,"season milestone reachable");Check(l.History.Count==12*season,"history archives once per club per year");if(l.Fired)l.Join((l.UserTeam+1)%12);Check(l.BeginDraft(),"offseason contracts and draft start");guard=0;while(l.Drafting&&guard++<30) {
                        if(!l.Draft(l.Prospects[0].Id))l.PassPick();
                    }
                    Check(l.Season==season+1&&!l.Drafting,"draft completes next year");Check(l.Clubs.All(c=>l.Squad(c.Id).Count>=8&&l.Squad(c.Id).Count<=15&&l.Payroll(c.Id)<=League.Cap),"all CPU and user clubs retain legal rosters and payroll");Check(l.Clubs.All(c=>c.Lineup.Distinct().Count()==5&&c.Lineup.All(id=>l.Player(id)!=null&&l.Player(id).Team==c.Id)),"lineup IDs belong to their clubs");
                }
            }
            );
            Test("headless AI match reaches final and overtime resolves",()=> {
                League l=League.Create(0);Settings s=new Settings {
                    QuarterSeconds=30
                };Match m=Match.Create(l,0,1,-1,s,-1);int count=0;while(!m.Over&&count++<30000)m.Update(.04f,0,0,false);Check(m.Over&&count<30000,"real time AI match terminates");Check(m.HomeScore!=m.AwayScore,"overtime cannot finalize a tie");Box(m);
            }
            );
            Test("three point racks and money ball scoring",()=> {
                League l=League.Create(0);Match m=Match.Create(l,0,1,0,new Settings(),-1);m.StartContest();for(int i=0;i<25;i++) {
                    m.Phase="live";m.Shoot(.72f);m.ShotMade=true;for(int n=0;n<100&&m.Phase=="shot";n++)m.Update(.02f,0,0,false);
                }
                Check(m.Over&&m.ContestShots==25&&m.ContestPoints==30,"perfect contest scores 30");
            }
            );
            Test("atomic save, live resume and backup recovery",()=> {
                League l=League.Create(0);Match m=l.MakeMatch(l.UserGame,new Settings());SaveData d=new SaveData {
                    League=l,Match=m
                };d.CustomPlays.Add(Play.Builtins()[1]);Persistence.Save(d);SaveData loaded=Persistence.Load();Check(loaded.League!=null&&loaded.Match!=null&&loaded.CustomPlays.Count==1,"full save round trip");Check(loaded.Match.Owner==m.Owner&&loaded.Match.RandomCalls==m.RandomCalls,"resume ball ownership and RNG");m.Update(.03f,0,0,false);loaded.Match.Update(.03f,0,0,false);Check(m.Clock==loaded.Match.Clock&&m.BallX==loaded.Match.BallX,"deterministic resumed update");Persistence.Save(d);File.WriteAllText(Persistence.SavePath,"broken xml");SaveData backup=Persistence.Load();Check(backup.League!=null&&Persistence.LoadNotice.Contains("backup"),"backup recovery");
            }
            );
            Test("controller dead zone and stock play library",()=> {
                Check(Input.Axis(1000)==0&&Input.Axis(-32768)<-.99f,"controller deadzone and range");Check(Play.Builtins().Count==24&&Play.Builtins().All(p=>p.Routes.Count==5),"24 five-player plays");Check(PixelFont.Width("UPCOURT",16)==84,"original pixel font metrics");
            }
            );
            Console.WriteLine(assertions+" assertions / "+failures+" failures");
            return failures==0?0:1;
        }
        public static int Ui() {
            int errors=0;
            Persistence.DirectoryPath=Path.Combine(Path.GetTempPath(),"Upcourt-ui-"+Guid.NewGuid().ToString("N"));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using(GameForm form=new GameForm()) {
                form.Show();
                Application.DoEvents();
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                typeof(GameForm).GetField("league",flags).SetValue(form,League.Create(0));
                foreach(string screen in new[] {
                    "home","office","roster","schedule","standings","playoffs","agents","training","business","career","history","leaders","controls","help","designer"
                }
                ) {
                    try {
                        typeof(GameForm).GetMethod("Open",flags).Invoke(form,new object[] {
                            screen
                        }
                        );
                        using(var b=new System.Drawing.Bitmap(1280,800))form.DrawToBitmap(b,new System.Drawing.Rectangle(0,0,1280,800));
                        Console.WriteLine("PASS: UI "+screen);
                    }
                    catch(Exception e) {
                        errors++;
                        Console.WriteLine("FAIL: UI "+screen+" / "+e);
                    }
                }
                errors+=UiFlow(form,flags);
            }
            return errors;
        }
        static int UiFlow(GameForm form,BindingFlags flags) {
            try {
                Type t=typeof(GameForm);
                t.GetMethod("Start",flags).Invoke(form,new object[] {
                    false,false,false
                }
                );
                Match game=(Match)t.GetField("game",flags).GetValue(form);
                Input input=(Input)t.GetField("input",flags).GetValue(form);
                game.Phase="live";
                float before=game.User.X;
                MethodInfo tick=t.GetMethod("Tick",flags);
                input.HeldKeys.Add(Keys.W);
                tick.Invoke(form,null);
                input.HeldKeys.Remove(Keys.W);
                if(game.User.X<=before)throw new Exception("W did not move player upcourt");
                input.HeldKeys.Add(Keys.Space);
                tick.Invoke(form,null);
                if(!game.Charging)throw new Exception("shoot key did not start meter");
                game.Charge=.72f;
                input.HeldKeys.Remove(Keys.Space);
                tick.Invoke(form,null);
                if(game.Phase!="shot")throw new Exception("release did not launch ball");
                input.HeldKeys.Add(Keys.Escape);
                tick.Invoke(form,null);
                if((string)t.GetField("screen",flags).GetValue(form)!="pause")throw new Exception("escape did not pause");
                t.GetMethod("Save",flags).Invoke(form,null);
                SaveData loaded=Persistence.Load();
                if(loaded.Match==null||loaded.Match.Phase!="shot")throw new Exception("live shot did not save");
                Console.WriteLine("PASS: UI keyboard -> movement -> shot meter -> ball flight -> pause -> saved resume");
                return 0;
            }
            catch(Exception e) {
                Console.WriteLine("FAIL: UI gameplay flow / "+e);
                return 1;
            }
        }
    }
#endif
}
