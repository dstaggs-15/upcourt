using System;
using System.Collections.Generic;
using System.Linq;
namespace Upcourt  {
    public static class Util  {
        public static float Clamp(float n,float a,float b) {
            return Math.Max(a,Math.Min(b,n));
        }
        public static int Bound(int n,int a,int b) {
            return Math.Max(a,Math.Min(b,n));
        }
        public static float Distance(float x,float y,float a,float b) {
            float dx=x-a,dy=y-b;
            return (float)Math.Sqrt(dx*dx+dy*dy);
        }
        public static readonly string[] Positions= {
            "PG","SG","SF","PF","C"
        };
        public static readonly string[] Cities= {
            "Chicago","New Orleans","Atlanta","Dallas","Miami","Seattle","New York","Los Angeles","Denver","Buffalo","Portland","Nashville","Austin","San Diego","St. Louis","San Antonio"
        };
        public static readonly string[] Mascots= {
            "Current","Crescent","Fire","Stampede","Tides","Evergreens","Empire","Comets","Wolves","Bison","Pioneers","Thunder"
        };
        public static readonly int[] Colors= {
            0x247AB8,0x262735,0xB93848,0xDA7934,0x71C9D9,0x318B65,0x7956B3,0xDA7934,0x247AB8,0xC9AC67,0x318B65,0x7956B3
        };
    }
    public class Stats  {
        public int Games,Points,Rebounds,Assists,Steals,Blocks,Turnovers,Fouls,FGM,FGA,TPM,TPA,FTM,FTA;
        public float Seconds;
        public void Add(Stats s) {
            Games+=s.Games;
            Points+=s.Points;
            Rebounds+=s.Rebounds;
            Assists+=s.Assists;
            Steals+=s.Steals;
            Blocks+=s.Blocks;
            Turnovers+=s.Turnovers;
            Fouls+=s.Fouls;
            FGM+=s.FGM;
            FGA+=s.FGA;
            TPM+=s.TPM;
            TPA+=s.TPA;
            FTM+=s.FTM;
            FTA+=s.FTA;
            Seconds+=s.Seconds;
        }
        public float PerGame(int value) {
            return Games==0?0:(float)value/Games;
        }
        public float Value  {
            get {
                return Points+Rebounds*1.2f+Assists*1.5f+Steals*3+Blocks*3-Turnovers*1.5f-(FGA-FGM)*.5f;
            }
        }
    }
    public class Athlete  {
        public int Experience;
        public int Id,Team,Number,Age,Position,Secondary,Height,Shooting,Finishing,Passing,Handle,Speed,Defense,Rebounding,Stamina,Potential,Salary,Years=2,XP,Morale=75,InjuryRounds,Rotation=18;
        public string Name="",Injury="";
        public Stats Season=new Stats(),Career=new Stats();
        public int OVR  {
            get {
                return (Shooting+Finishing+Passing+Handle+Speed+Defense+Rebounding+Stamina)/8;
            }
        }
        public string Role  {
            get {
                return Util.Positions[Position];
            }
        }
        public bool Eligible(int slot) {
            return Position==slot||Secondary==slot;
        }
        public float Value  {
            get {
                return OVR*3+(Potential-OVR)*(Age<27?1.6f:.4f)+(27-Age)*2+Years*2-Math.Max(0,Salary-10)-InjuryRounds*5;
            }
        }
        public Athlete Copy() {
            return (Athlete)MemberwiseClone();
        }
    }
    public class Club  {
        public int Id,Color,Offense,Defense,Conditioning,Training,Medical,Arena,Fans=60,Ticket=30,Cash=70,Security=75;
        public string City="",Mascot="",Plan="BALANCED";
        public int[] Lineup= {
            -1,-1,-1,-1,-1
        };
        public string Name  {
            get {
                return City+" "+Mascot;
            }
        }
    }
    public class Fixture  {
        public int Id,Round,Home,Away,HomeScore,AwayScore,Series=-1;
        public bool Played;
        public int Winner  {
            get {
                if(!Played||HomeScore==AwayScore)return -1;
                return HomeScore>AwayScore?Home:Away;
            }
        }
        public bool Involves(int team) {
            return Home==team||Away==team;
        }
        public int Other(int team) {
            return Home==team?Away:Home;
        }
    }
    public class Standing  {
        public int Team,W,L,PF,PA;
        public int Diff  {
            get {
                return PF-PA;
            }
        }
    }
    public class Series  {
        public int Id,Stage,High,Low,HighWins,LowWins,Needed;
        public bool Complete  {
            get {
                return HighWins>=Needed||LowWins>=Needed;
            }
        }
        public int Winner  {
            get {
                return Complete?(HighWins>=Needed?High:Low):-1;
            }
        }
    }
    public class Award  {
        public int Season;
        public string Type="",Name="",Team="";
    }
    public class SeasonRecord  {
        public int Season,Team,W,L;
        public string Name="";
        public bool Champion;
    }
    public class DraftPick  {
        public int Pick,Team,Player;
        public string Name="";
    }
    public class RoutePoint  {
        public float X,Y;
        public RoutePoint() {
        }
        public RoutePoint(float x,float y) {
            X=x;
            Y=y;
        }
    }
    public class Route  {
        public int Slot;
        public List<RoutePoint> Points=new List<RoutePoint>();
    }
    public class Play  {
        public string Name="",Style="MOTION";
        public List<Route> Routes=new List<Route>();
        public static List<Play> Builtins() {
            return BasketballPlaybook.Create();
        }
    }
    public class Settings  {
        public int Version=1,Difficulty=1,QuarterSeconds=90,Volume=60;
        public bool AutoSubs=true,ShotAssist=false,Muted=false;
        public string[] Keys= {
            "W","S","A","D","Space","E","Q","F","R","D1","D2","D3","D4","D5","P"
        };
        public static readonly string[] Actions= {
            "Move up","Move down","Move left","Move right","Shoot / block","Pass / steal","Switch player","Crossover","Screen / box out","Pass to PG","Pass to SG","Pass to SF","Pass to PF","Pass to C","Playbook"
        };
    }
    public class SaveData  {
        public int Version=1;
        public League League;
        public Match Match;
        public Settings Settings=new Settings();
        public List<Play> CustomPlays=new List<Play>();
        public int ThreePointBest;
    }
}
