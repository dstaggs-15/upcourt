using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
namespace Upcourt  {
    public partial class GameForm  {
        Play editPlay;
        int editSlot,editSource=-1;
        Play ClonePlay(Play p) {
            using(var stream=new MemoryStream()) {
                var serializer=new XmlSerializer(typeof(Play));
                serializer.Serialize(stream,p);
                stream.Position=0;
                return (Play)serializer.Deserialize(stream);
            }
        }
        void DrawDesigner() {
            if(editPlay==null)editPlay=ClonePlay(Play.Builtins()[0]);
            Rect(0,0,1280,7,Gold);
            TextAt("CREATE A PLAY",38,36,28,White);
            TextAt(Clip(editPlay.Name,30),38,93,16,Mint);
            TextAt("SELECT 1-5 BELOW / CLICK COURT TO ADD CUT WAYPOINTS",38,137,12,Muted);
            Rect(40,190,930,495,Color.FromArgb(160,111,66));
            Rect(40,190,930,5,White);
            Rect(40,680,930,5,White);
            Line(505,190,505,685,White,2);
            Ell(445,377,120,120,Color.FromArgb(150,100,60));
            foreach(var route in editPlay.Routes) {
                Color c=route.Slot==editSlot?Gold:new[] {
                    Mint,Blue,Red,White,Color.Orchid
                }
                [route.Slot%5];
                PointF? prior=null;
                foreach(var point in route.Points) {
                    PointF here=new PointF(40+point.X/94*930,190+point.Y/50*495);
                    if(prior.HasValue)Line(prior.Value.X,prior.Value.Y,here.X,here.Y,c,3);
                    Ell(here.X-5,here.Y-5,10,10,c);
                    prior=here;
                }
                if(route.Points.Count>0) {
                    var first=route.Points[0];
                    TextAt((route.Slot+1).ToString(),40+first.X/94*930,173+first.Y/50*495,12,c);
                }
            }
            for(int s=0;s<5;s++) {
                int slot=s;
                float y=190+s*48;
                Rect(997,y,238,39,s==editSlot?Color.FromArgb(60,87,90):PanelColor);
                TextAt((s+1)+" / "+Util.Positions[s],1014,y+12,12,s==editSlot?Gold:White);
                Hit(997,y,238,39,()=>editSlot=slot);
            }
            DesignerButton("CLEAR ROUTE",440,()=>editPlay.Routes[editSlot].Points.Clear());
            DesignerButton("NAME PLAY",490,()=>Type("PLAY NAME",editPlay.Name,name=> {
                if(!string.IsNullOrWhiteSpace(name))editPlay.Name=name.Trim();
            }
            ));
            DesignerButton("SAVE TO BOOK",540,()=> {
                if(editSource<0&&data.CustomPlays.Count>=40) {
                    notice="Playbook full (40). Delete a saved play.";return;
                }
                if(editSource>=0)data.CustomPlays[editSource]=ClonePlay(editPlay);else {
                    data.CustomPlays.Add(ClonePlay(editPlay));editSource=data.CustomPlays.Count-1;
                }
                Save();notice="Saved "+editPlay.Name;
            }
            );
            DesignerButton("USE PLAY",590,()=> {
                if(game!=null&&!game.Over) {
                    game.ActivePlay=ClonePlay(editPlay);game.PlayTime=0;Open("game");
                }
                else notice="Start a match to use this call.";
            }
            );
            DesignerButton("DELETE / BACK",640,()=> {
                if(editSource>=0) {
                    data.CustomPlays.RemoveAt(editSource);editSource=-1;Save();
                }
                Open("calls");
            }
            );
            TextAt(Clip(notice,95),40,721,10,Gold);
            TextAt("MAX 12 WAYPOINTS PER PLAYER / ESC BACK / SAVE BEFORE LEAVING",40,772,10,Muted);
        }
        void DesignerButton(string name,float y,Action action) {
            Rect(997,y,238,39,PanelColor);
            TextAt(name,1009,y+12,10,White);
            Hit(997,y,238,39,action);
        }
        void DesignerClick(PointF point) {
            foreach(var hit in hits.ToList())if(hit.Key.Contains(point)) {
                hit.Value();
                return;
            }
            if(point.X<40||point.X>970||point.Y<190||point.Y>685)return;
            Route route=editPlay.Routes.Find(r=>r.Slot==editSlot);
            if(route==null) {
                route=new Route {
                    Slot=editSlot
                };
                editPlay.Routes.Add(route);
            }
            if(route.Points.Count>=12) {
                notice="Route limit reached. Clear it to redraw.";
                return;
            }
            route.Points.Add(new RoutePoint((point.X-40)/930*94,(point.Y-190)/495*50));
        }
    }
}
