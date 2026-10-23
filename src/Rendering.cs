using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Upcourt  {
    public partial class GameForm  {
        void Rect(float x,float y,float w,float h,Color c) {
            using(var b=new SolidBrush(c))g.FillRectangle(b,(float)Math.Round(x),(float)Math.Round(y),w,h);
        }
        void Ell(float x,float y,float w,float h,Color c) {
            using(var b=new SolidBrush(c))g.FillEllipse(b,x,y,w,h);
        }
        void Line(float x,float y,float xx,float yy,Color c,float w) {
            using(var p=new Pen(c,w))g.DrawLine(p,x,y,xx,yy);
        }
        void TextAt(string s,float x,float y,float size,Color c) {
            PixelFont.Draw(g,s??"",x,y,size,c);
        }
        string Clip(string s,int n) {
            return s.Length<=n?s:s.Substring(0,Math.Max(0,n-2))+"..";
        }
        void Center(string s,float y,float size,Color c) {
            TextAt(s,640-PixelFont.Width(s,size)/2,y,size,c);
        }
        void Panel(float x,float y,float w,float h) {
            Rect(x-4,y-4,w+8,h+8,White);
            Rect(x-2,y-2,w+4,h+4,Ink);
            Rect(x,y,w,h,PanelColor);
        }
        Color TeamColor  {
            get {
                return league==null?Blue:Color.FromArgb(255,Color.FromArgb(league.Team.Color));
            }
        }
        void Hit(float x,float y,float w,float h,Action action) {
            hits.Add(new KeyValuePair<RectangleF,Action>(new RectangleF(x,y,w,h),action));
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using(Graphics buffer=Graphics.FromImage(frame)) {
                g=buffer;
                hits.Clear();
                g.Clear(Ink);
                g.ScaleTransform(.5f,.5f);
                g.SmoothingMode=SmoothingMode.None;
                g.PixelOffsetMode=PixelOffsetMode.Half;
                g.InterpolationMode=InterpolationMode.NearestNeighbor;
                if(screen=="game")DrawGame();
                else if(screen=="designer")DrawDesigner();
                else if(screen=="home")DrawHome();
                else DrawMenu();
                if(typed!=null) {
                    Panel(245,260,790,220);
                    Center(typingLabel,289,24,Gold);
                    TextAt(Clip(typing,22)+((int)(time*2)%2==0?"_":""),280,351,24,White);
                    Center("TYPE / ENTER SAVE / ESC CANCEL",430,12,Mint);
                }
                if(binding>=0) {
                    Panel(245,290,790,160);
                    Center("PRESS A KEY",320,24,Gold);
                    Center("ESC CANCELS",390,12,Mint);
                }
            }
            e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
            e.Graphics.DrawImage(frame,new Rectangle(0,0,ClientSize.Width,ClientSize.Height),0,0,640,400,GraphicsUnit.Pixel);
        }
        void DrawHome() {
            BuildMenu();
            Rect(0,0,1280,7,Gold);
            TextAt("UPCOURT",40,44,56,White);
            TextAt("PICK YOUR GAME",44,141,16,Mint);
            for(int n=0;n<3;n++) {
                int ix=n;
                float x=40+n*407;
                bool chosen=selected==n;
                Panel(x,212,386,382);
                Rect(x+18,220,350,264,n==1?Color.FromArgb(72,75,87):Color.FromArgb(141,96,58));
                if(chosen)Rect(x,212,386,5,Gold);
                if(n==1)OfficeScene(x+15,230,.9f);
                else {
                    for(int stripe=0;stripe<9;stripe++)Rect(x+20,224+stripe*28,346,1,Color.FromArgb(169,120,75));
                    Rect(x+150,238,90,110,TeamColor);
                    Line(x+160,270,x+232,270,White,3);
                    Ell(x+181,282,32,12,Red);
                    for(int i=0;i<3;i++)DrawSprite(x+53+i*105,330+i%2*34,48,64,i==2?0xB93848:0x247AB8,i==2?24:81,"run",time*3+i,0,i==2?1:0);
                    Basketball(x+168,419,20);
                    if(n==2)TextAt("1   2   3   4   5",x+34,462,12,Gold);
                }
                string title=choices[n].Title;
                TextAt(title,x+20,518,20,chosen?Gold:White);
                TextAt(choices[n].Detail,x+18,557,10,Muted);
                Hit(x,212,386,382,()=> {
                    selected=ix;ExecuteChoice(ix);
                }
                );
            }
            int first=3;
            for(int n=first;n<choices.Count;n++) {
                int ix=n;
                float x=40+(n-first)%3*407,y=626+(n-first)/3*66;
                bool chosen=selected==n;
                Rect(x,y,386,53,chosen?Color.FromArgb(60,87,90):PanelColor);
                TextAt(choices[n].Title,x+14,y+18,12,chosen?Gold:White);
                Hit(x,y,386,53,()=> {
                    selected=ix;ExecuteChoice(ix);
                }
                );
            }
            TextAt("DOWNFIELD'S PIXEL DNA / FIVE ON FIVE BASKETBALL",42,774,10,Muted);
            if(notice.Length>0) {
                Rect(30,741,1220,25,Ink);
                TextAt(Clip(notice,99),40,748,10,Gold);
            }
        }
        void DrawMenu() {
            Rect(0,0,1280,7,Gold);
            TextAt("UPCOURT",36,27,28,White);
            TextAt(screen.Replace("trade","TRADE ").ToUpperInvariant(),36,91,20,Mint);
            if(league!=null)TextAt(Clip(league.Team.Name+" / "+league.Stage+" / $"+league.Team.Cash+"M / CAP "+(League.Cap-league.Payroll(league.UserTeam))+"M",75),36,137,12,Muted);
            int first=selected/8*8;
            for(int n=first;n<Math.Min(first+8,choices.Count);n++) {
                int ix=n;
                float y=190+(n-first)*60;
                bool chosen=selected==n;
                Rect(36,y,1208,53,chosen?Color.FromArgb(60,87,90):PanelColor);
                if(chosen)Rect(36,y,5,53,Gold);
                TextAt(Clip(choices[n].Title,95),54,y+7,12,chosen?Gold:White);
                TextAt(Clip(choices[n].Detail,97),54,y+31,10,Muted);
                Hit(36,y,1208,53,()=> {
                    selected=ix;ExecuteChoice(ix);
                }
                );
            }
            if(screen=="player"&&league!=null) {
                Athlete p=league.Player(person);
                if(p!=null) {
                    string skills="SHO "+p.Shooting+" FIN "+p.Finishing+" PAS "+p.Passing+" HND "+p.Handle+" SPD "+p.Speed+" DEF "+p.Defense+" REB "+p.Rebounding+" STA "+p.Stamina;
                    TextAt(skills,38,691,10,Mint);
                }
            }
            if(screen=="office") {
                TextAt("COACH "+Clip(league.Coach,20)+" / "+league.CoachWins+" W - "+league.CoachLosses+" L / "+league.Titles+" TITLES",38,692,12,Gold);
            }
            string message=notice.Length>0?notice:league==null?"":league.Notice;
            TextAt(Clip(message,99),38,724,10,Gold);
            TextAt("ARROWS / A SELECT / B BACK / LB-RB PAGE",38,772,10,Muted);
            string page=(selected/8+1)+" / "+Math.Max(1,(int)Math.Ceiling(choices.Count/8f));
            TextAt(page,1120,772,10,Muted);
            Hit(1080,762,70,32,()=>selected=Math.Max(0,selected-8));
            Hit(1160,762,70,32,()=>selected=Math.Min(choices.Count-1,selected+8));
            Hit(870,762,180,32,()=>Back());
        }
        void OfficeScene(float x,float y,float scale) {
            GraphicsState state=g.Save();
            g.TranslateTransform(x,y);
            g.ScaleTransform(scale,scale);
            Rect(15,16,350,230,Color.FromArgb(68,66,84));
            Rect(15,16,350,29,TeamColor);
            TextAt(league==null?"FRONT OFFICE":Clip(league.Team.Mascot,20),32,25,12,White);
            Rect(38,65,96,64,Color.FromArgb(91,151,169));
            Rect(82,65,6,64,White);
            Rect(38,94,96,6,White);
            Rect(252,64,87,80,Ink);
            Trophy(281,79);
            Rect(120,140,135,31,Color.FromArgb(133,91,55));
            Rect(126,171,12,61,Color.FromArgb(92,62,44));
            Rect(237,171,12,61,Color.FromArgb(92,62,44));
            Rect(170,103,30,34,Color.FromArgb(212,149,94));
            Rect(167,96,36,12,Ink);
            Rect(161,135,46,30,TeamColor);
            Rect(174,117,4,4,Ink);
            Rect(190,117,4,4,Ink);
            g.Restore(state);
        }
        void Trophy(float x,float y) {
            Rect(x+8,y,24,23,Gold);
            Rect(x,y+2,9,16,Gold);
            Rect(x+31,y+2,9,16,Gold);
            Rect(x+15,y+23,10,12,Gold);
            Rect(x+5,y+35,30,6,Color.FromArgb(148,96,58));
        }
        void DrawSprite(float x,float y,float w,float h,int primary,int number,string pose,float step,float vx,int side,float poseTime=.3f) {
            Player p=new Player {
                Primary=primary,Secondary=0xF5E8C2,Number=number%100,Team=side,Pose=pose,Step=step,VX=vx,VZ=2,PoseTime=poseTime
            };
            g.DrawImage(PixelPlayers.Get(p),new Rectangle((int)x,(int)y,(int)w,(int)h));
        }
        void Basketball(float x,float y,float radius) {
            Ell(x-radius,y-radius,radius*2,radius*2,Ink);
            Ell(x-radius+2,y-radius+2,radius*2-4,radius*2-4,Color.FromArgb(202,111,49));
            Line(x-radius+2,y,x+radius-2,y,Color.FromArgb(103,57,37),2);
            Line(x,y-radius+2,x,y+radius-2,Color.FromArgb(103,57,37),2);
        }
        PointF Project(float x,float y,float h) {
            PointF p=camera.Project(x,y,h);
            if(game!=null&&game.ImpactTime>0){p.X+=(float)Math.Sin(time*79)*game.Impact*1.8f;p.Y+=(float)Math.Cos(time*61)*game.Impact;}return p;
        }
        void CourtLine(float x,float y,float xx,float yy,Color c,float w) {
            PointF a=Project(x,y,0),b=Project(xx,yy,0);
            Line(a.X,a.Y,b.X,b.Y,c,w);
        }
        void Quad(float x,float y,float xx,float yy,Color c) {
            PointF[] points= {
                Project(x,y,0),Project(x,yy,0),Project(xx,yy,0),Project(xx,y,0)
            };
            using(var b=new SolidBrush(c))g.FillPolygon(b,points);
        }
        void Arc(float cx,float cy,float radius,float start,float end,Color c,float width) {
            for(float a=start;a<end;a+=.1f) {
                float b=Math.Min(end,a+.1f);
                CourtLine(cx+(float)Math.Cos(a)*radius,cy+(float)Math.Sin(a)*radius,cx+(float)Math.Cos(b)*radius,cy+(float)Math.Sin(b)*radius,c,width);
            }
        }
        void DrawArena() {
            Rect(0,105,1280,589,Color.FromArgb(49,53,67));
            Rect(0,105,1280,53,Color.FromArgb(82,145,171));
            for(int section=0;section<10;section++) {
                int x=section*128;
                Rect(x+6,166,116,98,Color.FromArgb(59,69,86));
                for(int row=0;row<8;row++) {
                    Rect(x+8,171+row*11,112,2,Ink);
                    for(int i=0;i<10;i++) {
                        int hash=(i*41+row*23+x)%137;
                        float xx=x+13+i*10,yy=168+row*11+hash%3;
                        Color skin=hash%3==0?Color.FromArgb(140,91,67):Color.FromArgb(217,172,130);
                        Rect(xx,yy,4,3,skin);
                        Color[] shirts= {
                            Color.FromArgb(255,Color.FromArgb(game.HomeColor)),Color.FromArgb(255,Color.FromArgb(game.AwayColor)),White,Blue,Gold
                        };
                        Rect(xx-1,yy+3,6,4,shirts[hash%5]);
                        if((hash+(int)(time*3))%Math.Max(3,19-(int)(game.CrowdEnergy*8))==0) {
                            Rect(xx-3,yy,2,5,skin);
                            Rect(xx+5,yy-1,2,5,skin);
                        }
                    }
                }
                for(int stair=0;stair<9;stair++)Rect(x,170+stair*10,6,3,Muted);
            }
            Rect(452,120,376,96,Ink);
            Rect(458,126,364,7,Gold);
            TextAt("UPCOURT ARENA",482,141,12,Gold);
            TextAt(game.HomeScore.ToString("00"),489,165,30,White);
            TextAt("VS",624,173,12,Muted);
            TextAt(game.AwayScore.ToString("00"),727,165,30,White);
            Rect(0,266,1280,18,Ink);
            for(int i=0;i<5;i++)TextAt(i%2==0?"UPCOURT":Clip(game.HomeName,20),i*256+12,269,10,i%2==0?Gold:Mint);
            GraphicsState courtState=g.Save();g.SetClip(new Rectangle(0,286,1280,436));
            Quad(-5,-7,99,57,Color.FromArgb(72,71,76));
            Quad(0,-2,94,52,Color.FromArgb(255,Color.FromArgb(game.HomeColor)));
            for(int x=0;x<94;x+=4)Quad(x,0,Math.Min(94,x+4),50,(x/4)%2==0?Color.FromArgb(160,111,66):Color.FromArgb(171,121,75));
            for(int x=0;x<94;x+=2)for(int y=1;y<50;y+=6)CourtLine(x,y,x+1,y,Color.FromArgb(177,127,80),1);
            Quad(0,17,19,33,Color.FromArgb(113,83,64));
            Quad(75,17,94,33,Color.FromArgb(113,83,64));
            CourtLine(0,0,94,0,White,3);
            CourtLine(94,0,94,50,White,3);
            CourtLine(94,50,0,50,White,3);
            CourtLine(0,50,0,0,White,3);
            CourtLine(47,0,47,50,White,2);
            Arc(47,25,6,0,(float)Math.PI*2,White,2);
            foreach(int side in new[] {
                0,1
            }
            ) {
                float hx=side==0?5.25f:88.75f,lane=side==0?19:75,edge=side==0?0:94;
                CourtLine(edge,17,lane,17,White,2);
                CourtLine(edge,33,lane,33,White,2);
                CourtLine(lane,17,lane,33,White,2);
                Arc(lane,25,6,0,(float)Math.PI*2,White,2);
                CourtLine(edge,3,side==0?14:80,3,White,2);
                CourtLine(edge,47,side==0?14:80,47,White,2);
                Arc(hx,25,23.75f,side==0?-1.19f:1.95f,side==0?1.19f:4.33f,White,2);
            }
            PointF center=Project(47,25,0);
            TextAt("U",center.X-8,center.Y-12,24,Gold);
            for(int side=0;side<2;side++) {
                float x=side==0?-1:95;
                for(int n=0;n<7;n++) {
                    PointF p=Project(x,5+n*6,0);
                    DrawSprite(p.X-10,p.Y-28,20,28,side==0?game.HomeColor:game.AwayColor,10+n,"clap",time,0,side);
                }
            }
            g.Restore(courtState);
        }
        void DrawHoop(int side) {
            float x=side==0?5.25f:88.75f;
            PointF basePoint=Project(side==0?1:93,25,0),board=Project(side==0?3:91,25,10),rim=Project(x,25,9);
            Line(basePoint.X,basePoint.Y,board.X,board.Y,Muted,5);
            Rect(board.X-27,board.Y-22,54,32,White);
            Rect(board.X-23,board.Y-18,46,24,Color.FromArgb(155,170,162));
            Rect(board.X-11,board.Y-10,22,17,White);
            Ell(rim.X-12,rim.Y-2,24,10,Red);
            Line(rim.X-9,rim.Y+7,rim.X-5,rim.Y+21,White,2);
            Line(rim.X+9,rim.Y+7,rim.X+5,rim.Y+21,White,2);
            Line(rim.X-5,rim.Y+21,rim.X+5,rim.Y+21,White,2);
        }
        void DrawGame() {
            if(game==null)return;
            if(!camera.Initialized)camera.Follow(game,0);
            DrawArena();
            GraphicsState actionState=g.Save();g.SetClip(new Rectangle(0,286,1280,436));
            DrawHoop(1);
            DrawHoop(0);
            foreach(var a in game.Roster.Where(a=>a.OnCourt).OrderByDescending(a=>a.X)) {
                PointF p=Project(a.X,a.Y,0);
                float scale=camera.SpriteSize(a.X);
                if(a.BoostTime>0){for(int streak=1;streak<4;streak++)Rect(p.X-a.VY*.25f-streak*8,p.Y-scale*.6f+streak*4,13,4,streak==1?Gold:Blue);}
                if(game.FireTime[a.Side]>0){for(int f=0;f<4;f++)Rect(p.X-scale*.4f+f*scale*.23f,p.Y-8-(f+(int)(time*10))%3*6,7,12, f%2==0?Gold:Red);}
                if(game.User==a) {
                    Ell(p.X-scale*.38f,p.Y-3,scale*.76f,9,Gold);
                }
                else if(a.Side==game.UserSide)Ell(p.X-scale*.3f,p.Y-2,scale*.6f,6,Color.FromArgb(42,64,70));
                float jump=a.JumpHeight*3*camera.Zoom;
                DrawSprite(p.X-scale/2,p.Y-scale*1.25f-jump,scale,scale*1.25f,a.Side==0?game.HomeColor:game.AwayColor,a.Data.Number,a.Pose,a.Step,a.VY,a.Side,a.Action);
                if(a.Side==game.UserSide) {
                    TextAt((a.Slot+1).ToString(),p.X-5,p.Y-scale*1.25f-17,10,White);
                }
                if(a==game.User)TextAt(Clip(a.Data.Name,15),p.X-45,p.Y+13,10,Gold);
            }
            PointF referee=Project(game.RefX,game.RefY,0);float refSize=camera.SpriteSize(game.RefX);DrawSprite(referee.X-refSize/2,referee.Y-refSize*1.25f,refSize,refSize*1.25f,0xEEEECC,0,"run",time*4,0,0);TextAt("REF",referee.X-18,referee.Y-refSize*1.25f-18,10,Gold);
            float ballHeight=game.Phase=="shot"||game.Phase=="pass"?game.BallHeight:1.5f+(game.BallOwner==null?0:game.BallOwner.JumpHeight)+(float)Math.Abs(Math.Sin(time*10))*1.5f;
            PointF ball=Project(game.BallX,game.BallY,ballHeight),shadow=Project(game.BallX,game.BallY,0);
            Ell(shadow.X-5,shadow.Y-2,10,4,Color.FromArgb(96,70,51));
            Basketball(ball.X+5,ball.Y,8);
            g.Restore(actionState);
            if(game.BannerTime>0){Rect(166,297,948,56,Ink);Center(Clip(game.ArcadeBanner,39),313,20,game.FireTime[game.Possession]>0?Red:Gold);}
            int heatSide=game.UserSide>=0?game.UserSide:0;Rect(20,357,202,43,Ink);TextAt(game.FireTime[heatSide]>0?"ON FIRE!":"HEAT / X"+game.Combo[heatSide],30,365,10,Gold);Rect(30,386,180,7,PanelColor);Rect(30,386,game.Heat[heatSide]*1.8f,7,game.FireTime[heatSide]>0?Red:Gold);
            Rect(0,0,1280,105,Ink);
            Rect(0,0,1280,6,Gold);
            TextAt(Clip(game.HomeName,25),26,20,16,Mint);
            TextAt(Clip(game.AwayName,25),835,20,16,Red);
            TextAt(game.HomeScore.ToString("00"),276,55,28,White);
            TextAt(game.AwayScore.ToString("00"),966,55,28,White);
            string clock=game.Contest?Math.Ceiling(game.ContestClock).ToString():game.Practice?"PRACTICE":((int)game.Clock/60)+":"+((int)game.Clock%60).ToString("00");
            Center(clock,22,24,White);
            Center(game.Quarter<=4?"Q"+game.Quarter:"OT"+game.Overtimes,65,12,Gold);
            TextAt("SHOT "+Math.Ceiling(game.ShotClock).ToString("00"),696,62,12,game.ShotClock<5?Red:Mint);
            TextAt("TO "+game.Timeouts[0],30,69,12,Muted);
            TextAt("TO "+game.Timeouts[1],1134,69,12,Muted);
            Rect(0,722,1280,78,Ink);
            TextAt(Clip(game.Message,95),28,733,12,Gold);
            TextAt(Clip(game.ShotFeedback,60),28,758,10,Mint);
            TextAt(input.UsesPad?(game.Possession==game.UserSide?"X SHOT / A PASS / Y CROSS / LT+X JAM / LT+RB PUNCH / LB JUMP":"A STEAL / X BLOCK / B SWITCH / LB JUMP / LT+RB PUNCH"):(game.Possession==game.UserSide?"SPACE SHOT / E PASS / J JUMP / G SPIN / H STEP / V JAM / K PUNCH / C CUT":"E STEAL / SPACE BLOCK / J JUMP / Q SWITCH / SHIFT TURBO / K PUNCH"),28,783,10,Muted);
            if(game.User!=null) {
                TextAt("ENERGY "+(int)(game.User.Fatigue*100)+"% / FOULS "+game.User.Box.Fouls,848,759,10,Muted);
            }
            Hit(1070,775,190,25,()=>Open("pause"));
            if(game.Charging) {
                Panel(420,592,440,88);
                TextAt(game.Phase=="freethrow"?"FREE THROW":"SHOT TIMING",439,603,12,White);
                Rect(441,635,398,23,Color.FromArgb(109,46,57));
                float ideal=game.Phase=="freethrow"?.72f:game.BallOwner!=null&&Util.Distance(game.BallOwner.X,game.BallOwner.Y,game.Possession==0?88.75f:5.25f,25)<10?.5f:.72f;
                Rect(441+(ideal-.08f)*398,635,64,23,Gold);
                Rect(441+(ideal-.04f)*398,635,32,23,Mint);
                Rect(441+game.Meter*398,629,5,36,White);
            }
            if(game.Contest) {
                Panel(929,310,304,71);
                TextAt("3PT "+game.ContestPoints+" / 30",944,324,16,Gold);
                TextAt("BEST "+data.ThreePointBest,944,354,10,Mint);
            }
            if(game.Over) {
                Panel(312,307,656,166);
                Center(game.Contest?"CONTEST COMPLETE":"FINAL",333,28,Gold);
                Center(game.Contest?game.ContestPoints+" POINTS":game.HomeScore+" - "+game.AwayScore,386,24,White);
                Center("ENTER / A FOR BOX SCORE",443,12,Mint);
            }
        }
    }
}
