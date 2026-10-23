using System;
using System.Drawing;
using System.Collections.Generic;
namespace Upocourt  {
    public static class PixelFont  {
        static readonly Dictionary<char,string> Glyphs=new Dictionary<char,string>();
        static PixelFont() {
            string[] rows= {
                "A:0E11111F111111","B:1E11111E11111E","C:0E11101010110E","D:1E11111111111E","E:1F10101E10101F","F:1F10101E101010","G:0E11101711110F","H:1111111F111111","I:0E04040404040E","J:0702020212120C","K:11121418141211","L:1010101010101F","M:111B1515111111","N:11191915131311","O:0E11111111110E","P:1E11111E101010","Q:0E11111115120D","R:1E11111E141211","S:0F10100E01011E","T:1F040404040404","U:1111111111110E","V:11111111110A04","W:11111115151B11","X:11110A040A1111","Y:11110A04040404","Z:1F01020408101F","0:0E11131519110E","1:040C040404040E","2:0E11010204081F","3:1E01010601011E","4:02060A121F0202","5:1F10101E01011E","6:0E10101E11110E","7:1F010204080808","8:0E11110E11110E","9:0E11110F01010E","::00040400040400",".:00000000000C0C",",:00000000000C08","-:0000001F000000","+:0004041F040400","/:01010204081010","?:0E110204000004","!:04040404040004","(:02040808080402","):08040202020408","=:00001F001F0000","':04040000000000","<:02040810080402",">:08040201020408","|:04040404040404","[:0E08080808080E","]:0E02020202020E","#:0A0A1F0A1F0A0A","%:19190204081313","_:0000000000001F"
            };
            foreach(string row in rows)Glyphs[row[0]]=row.Substring(2);
            Glyphs['&']="0C12140815120D";
            Glyphs['$']="041E141E051E04";
        }
        public static int Scale(float size) {
            return Math.Max(2,(int)Math.Floor(size/16f+.5f)*2);
        }
        public static float Width(string text,float size) {
            return text.Length*6*Scale(size);
        }
        public static void Draw(Graphics g,string text,float x,float y,float size,Color color) {
            int s=Scale(size);
            using(SolidBrush brush=new SolidBrush(color)) {
                foreach(char raw in text.ToUpperInvariant()) {
                    string bits;
                    char c=raw;
                    if(Glyphs.TryGetValue(c,out bits)&&bits.Length>=14)for(int r=0;r<7;r++) {
                        int value=Convert.ToInt32(bits.Substring(r*2,2),16);
                        for(int col=0;col<5;col++)if((value&(1<<(4-col)))!=0)g.FillRectangle(brush,(float)Math.Round(x+col*s),(float)Math.Round(y+r*s),s,s);
                    }
                    x+=6*s;
                }
            }
        }
    }
    public static class PixelPlayers  {
        static readonly Dictionary<string,Bitmap> Cache=new Dictionary<string,Bitmap>();
        static void R(Graphics g,Color c,int x,int y,int w,int h) {
            using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);
        }
        public static Bitmap Get(Player p) {
            int dir=Math.Abs(p.VX)>Math.Abs(p.VZ)*.75f?(p.VX<0?1:2):(p.VZ<-.2f?3:0);
            if(p.Pose=="spin") {
                int[] turn= {
                    0,1,3,2
                };
                dir=turn[Math.Min(3,Math.Max(0,(int)((.55f-p.PoseTime)/(.55f/4))))];
            }
            int frame=p.Pose=="run"?(int)(p.Step*1.0f)%4:p.Pose=="celebrate"||p.Pose=="clap"?(int)(p.Step*2)%4:p.Pose=="highcatch"||p.Pose=="catch"?(int)(p.PoseTime*6)%3:0;
            string pose=p.Pose=="run"?"run":p.Pose;
            string key=p.Primary+"/"+p.Secondary+"/"+p.Uniform+"/"+p.HasBall+"/"+p.Team+"/"+p.Number+"/"+dir+"/"+frame+"/"+pose;
            Bitmap b;
            if(Cache.TryGetValue(key,out b))return b;
            b=new Bitmap(24,30);
            Color outline=Color.FromArgb(26,30,48),jersey=p.Team==0?Color.FromArgb(33,122,190):Color.FromArgb(185,48,64),dark=p.Team==0?Color.FromArgb(22,67,125):Color.FromArgb(108,30,51),light=p.Team==0?Color.FromArgb(69,188,224):Color.FromArgb(247,104,89),cream=Color.FromArgb(245,232,194),skin=p.Number%3==0?Color.FromArgb(132,79,55):Color.FromArgb(212,149,94),pants=p.Team==0?cream:Color.FromArgb(214,205,180);
            if(p.Team==0&&p.Uniform==1) {
                jersey=Color.FromArgb(37,35,36);
                dark=Color.FromArgb(18,20,25);
                light=Color.FromArgb(202,173,104);
                cream=Color.FromArgb(225,202,145);
                pants=cream;
            }
            if(p.Primary>=0) {
                jersey=Color.FromArgb(255,Color.FromArgb(p.Primary));
                light=Color.FromArgb(255,Color.FromArgb(p.Secondary));
                dark=Color.FromArgb(255,Math.Max(0,jersey.R/2),Math.Max(0,jersey.G/2),Math.Max(0,jersey.B/2));
                cream=light;
                pants=light;
            }
            using(Graphics g=Graphics.FromImage(b)) {
                bool action=pose=="throw"||pose=="lateral"||pose=="handoff";
                int swing=frame==1?-2:frame==3?2:0;
                int ly=23+(frame==1?2:0),ry=23+(frame==3?2:0);
                R(g,outline,7,19,5,9);
                R(g,outline,13,19,5,9);
                R(g,pants,8,19,3,6);
                R(g,pants,14,19,3,6);
                R(g,dark,8,ly,3,3);
                R(g,dark,14,ry,3,3);
                R(g,outline,6,ly+3,6,2);
                R(g,outline,13,ry+3,6,2);
                R(g,cream,7,ly+3,4,1);
                R(g,cream,14,ry+3,4,1);
                R(g,outline,5,10,15,11);
                R(g,jersey,6,11,13,9);
                R(g,dark,7,18,11,2);
                R(g,light,7,11,11,2);
                R(g,outline,3,11+swing,4,8);
                R(g,jersey,4,12+swing,3,4);
                R(g,skin,4,16+swing,2,3);
                R(g,outline,18,11-swing,4,8);
                R(g,jersey,18,12-swing,3,4);
                R(g,skin,19,16-swing,2,3);
                // Same Downfield silhouette, outline, skin palette and pixel grid: bare heads,
                // sleeveless jerseys and shorts replace only the football equipment.
                R(g,outline,7,2,11,9);
                R(g,outline,6,4,13,5);
                R(g,skin,8,3,9,7);
                R(g,skin,7,5,11,4);
                R(g,outline,8,2,9,3);
                R(g,outline,7,4,3,2);
                if(dir==3) {
                    R(g,outline,9,6,2,1);
                    R(g,outline,14,6,2,1);
                    R(g,dark,11,9,3,1);
                }
                else if(dir==1||dir==2) {
                    int face=dir==1?6:16;
                    R(g,skin,face,5,3,4);
                    R(g,outline,dir==1?6:17,6,1,1);
                }
                R(g,skin,4,12+swing,2,7);
                R(g,skin,19,12-swing,2,7);
                R(g,skin,8,23,3,3);
                R(g,skin,14,23,3,3);
                string digits=p.Number.ToString("00");
                string[] nums= {
                    "111101101101111","010110010010111","111001111100111","111001111001111","101101111001001","111100111001111","111100111101111","111001001001001","111101111101111","111101111001111"
                };
                for(int n=0;n<2;n++) {
                    string bits=nums[digits[n]-'0'];
                    for(int j=0;j<15;j++)if(bits[j]=='1')R(g,cream,9+n*4+j%3,13+j/3,1,1);
                }
                if(pose=="catch"||pose=="highcatch") {
                    int reach=pose=="highcatch"?0:6;
                    R(g,outline,3,reach,4,12);
                    R(g,outline,18,reach,4,12);
                    R(g,skin,4,reach,2,10);
                    R(g,skin,19,reach,2,10);
                    R(g,cream,3,reach,4,2);
                    R(g,cream,18,reach,4,2);
                    if(frame==1) {
                        R(g,pants,6,20,5,5);
                        R(g,pants,14,20,5,5);
                    }
                }
                if(pose=="celebrate"||pose=="clap") {
                    int rise=frame%2==0?1:5;
                    R(g,skin,2,rise,4,13-rise);
                    R(g,skin,19,rise,3,13-rise);
                    R(g,cream,1,rise,5,2);
                    R(g,cream,18,rise,5,2);
                }
                if(pose=="drop") {
                    R(g,skin,1,13,6,3);
                    R(g,skin,18,13,6,3);
                    R(g,outline,10,6,5,2);
                }
                if(pose=="fake") {
                    R(g,skin,17,3,4,12);
                    R(g,outline,18,1,6,4);
                    R(g,Color.FromArgb(153,80,42),19,2,4,2);
                    R(g,skin,3,15,8,3);
                }
                if(pose=="truck") {
                    R(g,outline,5,8,15,7);
                    R(g,jersey,6,9,13,5);
                    R(g,skin,3,13,5,4);
                }
                if(pose=="cut") {
                    R(g,outline,3,26,10,3);
                    R(g,cream,4,26,8,1);
                }
                if(pose=="kick") {
                    R(g,pants,14,21,7,3);
                    R(g,outline,19,20,5,3);
                    R(g,cream,19,20,4,1);
                }
                if(pose=="hurdle") {
                    R(g,skin,3,7,4,3);
                    R(g,skin,18,7,4,3);
                    R(g,pants,7,20,11,3);
                }
                if(pose=="juke") {
                    R(g,skin,2,14,5,3);
                    R(g,skin,18,10,5,3);
                }
                if(pose=="block") {
                    R(g,skin,3,9,4,3);
                    R(g,skin,18,9,4,3);
                }
                if(action) {
                    R(g,skin,18,8,3,6);
                    R(g,outline,18,5,5,4);
                    R(g,Color.FromArgb(153,80,42),19,6,3,2);
                }
                if(p.HasBall&&!action) {
                    R(g,outline,18,15,5,5);
                    R(g,Color.FromArgb(154,79,41),19,16,3,3);
                    R(g,cream,20,16,1,2);
                }
            }
            if(pose=="tackle"||pose=="dive") {
                b.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }
            Cache[key]=b;
            return b;
        }
    }
}
