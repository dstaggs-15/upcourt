using System;
using System.Drawing;
namespace Upcourt {
public sealed class CourtCamera {
    public float FocusX=47,FocusY=25,Zoom=1.7f,Time;public bool Initialized;
    public void Follow(Match game,float dt){
        if(game==null)return;Time+=dt;
        float x=game.BallX,y=game.BallY;
        if(game.BallOwner!=null){x+=game.BallOwner.VX*.18f;y+=game.BallOwner.VY*.12f;}
        if(game.Phase=="shot"){float p=Util.Clamp(game.Flight/Math.Max(.01f,game.FlightDuration),0,1);x=x*(1-p*.3f)+game.TargetX*p*.3f;}
        x=Util.Clamp(x,8,86);y=Util.Clamp(y,12,38);float targetZoom=game.Contest?1.85f:game.Phase=="shot"?1.9f:1.7f;
        if(!Initialized){FocusX=x;FocusY=y;Zoom=targetZoom;Initialized=true;return;}
        float blend=1-(float)Math.Exp(-Math.Max(0,dt)*5);FocusX+=(x-FocusX)*blend;FocusY+=(y-FocusY)*blend;Zoom+=(targetZoom-Zoom)*blend;
    }
    public PointF Project(float x,float y,float height){float delta=x-FocusX,depth=Math.Max(20,65+delta*.45f),perspective=65/depth;return new PointF(640+(y-FocusY)*820/depth*Zoom,570-delta*7*Zoom*perspective-height*5.5f*Zoom*perspective);}
    public float SpriteSize(float x){return 36.8f*Zoom*65/Math.Max(20,65+(x-FocusX)*.45f);}
}
}
