using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Upcourt {
public partial class GameForm {
    string controlsReturn="options";int padCapture=-1,padPending;bool captureRelease,menuRelease;
    void OpenControls(string from){controlsReturn=from;Open("controller");}
    void StartPadCapture(int action){padCapture=action;selected=action;captureRelease=true;padPending=0;notice="Release buttons, then press the new button. Menu or Escape cancels.";input.Clear();}
    bool CaptureControls(){
        if(menuRelease){if(input.RawButtons==0&&input.HeldKeys.Count==0)menuRelease=false;else{input.EndFrame();Invalidate();return true;}}
        if(padCapture<0)return false;
        if(input.Press(Input.Start)||input.Fresh(Keys.Escape)){padCapture=-1;input.Clear();input.EndFrame();Invalidate();return true;}
        if(captureRelease){if(input.RawButtons==0)captureRelease=false;input.EndFrame();Invalidate();return true;}
        string token=ControlBindings.CaptureToken(input.RawButtons,input.PriorRawButtons);
        if(token==null){int triggers=input.RawButtons&0x30000;if(triggers!=0)padPending=triggers;if(triggers==0&&padPending!=0)token=(padPending&0x10000)!=0?"LT":"RT";}
        if(token!=null){ControlBindings.BindPad(data.Settings,padCapture,token);notice=ControlBindings.PadActions[padCapture]+" is now "+token+". Conflicts swapped.";padCapture=-1;menuRelease=true;Save();}
        input.EndFrame();Invalidate();return true;
    }
    void DrawControllerDiagram(){
        Rect(0,0,1280,7,Gold);TextAt("XBOX CONTROLS",38,32,32,White);TextAt("CURRENT BINDINGS / CHANGE THEM BELOW",40,103,12,Mint);
        PointF[] outline={new PointF(247,254),new PointF(299,222),new PointF(592,222),new PointF(650,260),new PointF(714,526),new PointF(658,571),new PointF(550,455),new PointF(340,455),new PointF(224,571),new PointF(169,530)};
        using(var pen=new Pen(White,5))g.DrawPolygon(pen,outline);using(var brush=new SolidBrush(PanelColor))g.FillPolygon(brush,outline);
        Rect(267,187,104,31,Ink);Rect(517,187,104,31,Ink);TextAt("LT",302,195,12,Gold);TextAt("RT",554,195,12,Gold);
        Rect(240,226,135,25,Muted);Rect(513,226,135,25,Muted);TextAt("LB",288,231,12,Ink);TextAt("RB",558,231,12,Ink);
        Ell(262,292,73,73,Ink);Ell(274,304,49,49,Muted);TextAt("L",292,319,12,Ink);
        Ell(481,392,73,73,Ink);Ell(493,404,49,49,Muted);TextAt("R",511,419,12,Ink);
        Rect(342,382,25,80,Ink);Rect(315,409,80,25,Ink);Line(355,393,355,452,Muted,2);Line(326,421,385,421,Muted,2);
        PadButton("Y",568,282,Gold);PadButton("X",524,325,Blue);PadButton("B",612,325,Red);PadButton("A",568,369,Mint);
        Rect(387,303,28,20,Ink);Rect(445,303,28,20,Ink);TextAt("V",393,307,10,White);TextAt("=",452,307,10,White);
        TextAt(data.Settings.SwapSticks?"RIGHT STICK / MOVE":"LEFT STICK / MOVE",204,606,12,Mint);TextAt("VIEW / MUTE    MENU / PAUSE",173,638,12,Muted);
        Panel(778,164,463,505);
        for(int i=0;i<ControlBindings.PadActions.Length;i++){int action=i;float y=179+i*33;TextAt(Clip(data.Settings.PadBindings[i]+" / "+ControlBindings.PadActions[i],35),793,y,10,i==padCapture?Gold:White);Hit(788,y-3,440,29,()=>{Open("padbindings");StartPadCapture(action);});}
        string[] labels={"KEYBOARD BINDS","XBOX BINDS","SWAP STICKS","BACK"};Action[] actions={()=>Open("controls"),()=>Open("padbindings"),()=>{data.Settings.SwapSticks=!data.Settings.SwapSticks;Save();},()=>Open(controlsReturn)};
        for(int i=0;i<4;i++){int index=i;float x=38+i*307;Rect(x,706,284,55,selected==i?Color.FromArgb(60,87,90):PanelColor);TextAt(labels[i],x+14,725,12,selected==i?Gold:White);Hit(x,706,284,55,actions[index]);}
        TextAt("A SELECT / B BACK / CLICK A BINDING TO CHANGE IT",38,781,10,Muted);
    }
    void PadButton(string label,float x,float y,Color color){Ell(x,y,39,39,Ink);Ell(x+4,y+4,31,31,color);TextAt(label,x+14,y+11,12,Ink);}
    string CurrentControlHint(){Settings s=data.Settings;return input.UsesPad?s.PadBindings[0]+" SHOT/BLOCK / "+s.PadBindings[1]+" PASS/STEAL / "+s.PadBindings[5]+" JUMP / "+s.PadBindings[8]+" JAM / "+s.PadBindings[9]+" PUNCH":s.Keys[4]+" SHOT / "+s.Keys[5]+" PASS/STEAL / "+s.Keys[15]+" JUMP / "+s.Keys[18]+" JAM / "+s.Keys[19]+" PUNCH / "+s.UtilityKeys[2]+" PAUSE";}
    void DrawPadCapture(){if(padCapture<0)return;Panel(194,257,892,246);Center("BIND XBOX CONTROL",285,24,Gold);Center(ControlBindings.PadActions[padCapture].ToUpperInvariant(),337,16,White);Center(captureRelease?"RELEASE ALL BUTTONS FIRST":"PRESS A BUTTON / HOLD LT OR RT FOR A COMBO",391,12,Mint);Center("RELEASE A TRIGGER TO BIND IT ALONE",431,12,Muted);Center("MENU / ESC CANCEL",471,12,Muted);}
}
}
