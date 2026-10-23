using System;
using System.Collections.Generic;
namespace Upcourt  {
    public static class BasketballPlaybook  {
        public static List<Play> Create() {
            string[] names= {
                "Five Out","High Pick & Roll","Horns","Spain Pick & Roll","Floppy","Double Drag","Princeton Cuts","Post Split","Corner Hammer","Iverson Cut","Handoff","Four Out One In","Flex","Elevator","Delay","Low Post","UCLA Cut","Backdoor","Wide Pin Down","Triangle","Quick Slip","Baseline Runner","Stagger","Spread Isolation"
            };
            string[] styles= {
                "SPACING","SCREEN","SCREEN","SCREEN","CUT","SCREEN","CUT","POST","SCREEN","CUT","HANDOFF","POST","CUT","SCREEN","HANDOFF","POST","CUT","CUT","SCREEN","POST","SCREEN","CUT","SCREEN","ISOLATION"
            };
            var all=new List<Play>();
            for(int p=0;p<24;p++) {
                Play play=new Play {
                    Name=names[p],Style=styles[p]
                };
                for(int s=0;s<5;s++) {
                    Route route=new Route {
                        Slot=s
                    };
                    float[] startX= {
                        60,72,72,79,79
                    }
                    ,startY= {
                        25,5,45,14,36
                    };
                    route.Points.Add(new RoutePoint(startX[s],startY[s]));
                    float x=68+s%2*10,y=startY[s];
                    switch(p) {
                        case 0:x=s==0?64:s<3?80:69;
                        y=new float[] {
                            25,3,47,9,41
                        }
                        [s];
                        break;
                        case 1:x=s==4?64:s==0?70:80;
                        y=s==4?28:s==0?20:startY[s];
                        break;
                        case 2:x=s>=3?69:81;
                        y=s==3?19:s==4?31:startY[s];
                        break;
                        case 3:x=s==4?65:s==3?76:80;
                        y=s==4?27:s==3?25:startY[s];
                        break;
                        case 4:x=s==1?88:s==3?84:74;
                        y=s==1?34:s==3?39:startY[s];
                        break;
                        case 5:x=s>=3?65+s:76;
                        y=s>=3?21+(s-3)*9:startY[s];
                        break;
                        case 6:x=s==1||s==2?87:69;
                        y=s==1||s==2?25:startY[s];
                        break;
                        case 7:x=s==4?83:s==1?86:s==2?76:70;
                        y=s==4?32:s==1?26:s==2?15:startY[s];
                        break;
                        case 8:x=s==2?86:s==4?80:75;
                        y=s==2?47:s==4?40:startY[s];
                        break;
                        case 9:x=s==1?73:s>=3?71:79;
                        y=s==1?42:s==3?18:s==4?32:startY[s];
                        break;
                        case 10:x=s==1?65:s==4?67:80;
                        y=s==1?23:s==4?27:startY[s];
                        break;
                        case 11:x=s==4?84:70;
                        y=s==4?25:startY[s];
                        break;
                        case 12:x=s==2?87:s==3?84:74;
                        y=s==2?13:s==3?37:startY[s];
                        break;
                        case 13:x=s==1?70:s>=3?75:82;
                        y=s==1?25:s==3?22:s==4?28:startY[s];
                        break;
                        case 14:x=s==4?64:77;
                        y=s==4?25:startY[s];
                        break;
                        case 15:x=s==4?85:s==0?76:s==1?88:72;
                        y=s==4?31:s==0?8:s==1?3:startY[s];
                        break;
                        case 16:x=s==0?86:s==4?74:79;
                        y=s==0?25:s==4?17:startY[s];
                        break;
                        case 17:x=s==2?88:s==4?69:78;
                        y=s==2?26:s==4?36:startY[s];
                        break;
                        case 18:x=s==1?79:s==3?83:70;
                        y=s==1?11:s==3?8:startY[s];
                        break;
                        case 19:x=s==0?76:s==1?88:s==4?83:68;
                        y=s==0?10:s==1?4:s==4?20:startY[s];
                        break;
                        case 20:x=s==4?86:s==0?69:80;
                        y=s==4?25:s==0?20:startY[s];
                        break;
                        case 21:x=s==2?90:s==4?85:74;
                        y=s==2?7:s==4?14:startY[s];
                        break;
                        case 22:x=s==1?72:s==3?76:s==4?81:80;
                        y=s==1?34:s==3?37:s==4?40:startY[s];
                        break;
                        case 23:x=s==0?76:s<3?88:68;
                        y=s==0?25:s==1?3:s==2?47:s==3?7:43;
                        break;
                    }
                    route.Points.Add(new RoutePoint(x,y));
                    float endX=x,endY=y;
                    if(play.Style=="SCREEN"&&s==4) {
                        endX=86;
                        endY=25;
                    }
                    else if(play.Style=="CUT"&&(s==1||s==2)) {
                        endX=88;
                        endY=25;
                    }
                    else if(play.Style=="HANDOFF"&&s==1) {
                        endX=81;
                        endY=15;
                    }
                    else if(play.Style=="POST"&&s==4) {
                        endX=86;
                        endY=29;
                    }
                    else if(p==9&&s==1) {
                        endX=81;
                        endY=45;
                    }
                    route.Points.Add(new RoutePoint(endX,endY));
                    play.Routes.Add(route);
                }
                all.Add(play);
            }
            return all;
        }
    }
}
